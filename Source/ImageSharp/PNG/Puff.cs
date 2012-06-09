#region License
/*
Copyright (c) 2012 Daniil Rodin

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.
*/
#endregion

/*
 This code is mostly a rewritten code from 
 http://svn.ghostscript.com/ghostscript/tags/zlib-1.2.3/contrib/puff/puff.c
 by Mark Adler that is subject to a similar ZLib license.
 */

using System;
using System.IO;

namespace ImageSharp.PNG
{
    public unsafe class Puff
    {
        /// <summary>
        /// input and output state
        /// </summary>
        struct State
        {
            /// <summary>
            /// output buffer
            /// </summary>
            public byte* outBuffer;
            /// <summary>
            /// available space at outBuffer
            /// </summary>
            public uint outlen;
            /// <summary>
            /// bytes written to out so far
            /// </summary>
            public uint outcnt;

            /// <summary>
            /// input buffer
            /// </summary>
            public byte* inBuffer;
            /// <summary>
            /// available input at inBuffer
            /// </summary>
            public uint inlen;
            /// <summary>
            /// bytes read so far
            /// </summary>
            public uint incnt;

            /// <summary>
            /// bit buffer
            /// </summary>
            public int bitbuf;
            /// <summary>
            /// number of bits in bit buffer
            /// </summary>
            public int bitcnt;
        }

        /// <summary>
        /// Huffman code decoding tables.  count[1..MAXBITS] is the number of symbols of
        /// each length, which for a canonical code are stepped through in order.
        /// symbol[] are the symbol values in canonical order, where the number of
        /// entries is the sum of the counts in count[].  The decoding process can be
        /// seen in the function decode() below.
        /// </summary>
        struct Huffman
        {
            /// <summary>
            /// number of symbols of each length
            /// </summary>
            public short* count;
            /// <summary>
            /// canonically ordered symbols
            /// </summary>
            public short* symbol;
        }

        /// <summary>
        /// for no output option
        /// </summary>
        static readonly byte* NIL = (byte*)IntPtr.Zero;

        /// <summary>
        /// maximum bits in a code
        /// </summary>
        const int MAXBITS = 15;
        /// <summary>
        /// maximum number of literal/length codes
        /// </summary>
        const int MAXLCODES = 286;
        /// <summary>
        /// maximum number of distance codes
        /// </summary>
        const int MAXDCODES = 30;
        /// <summary>
        /// maximum codes lengths to read
        /// </summary>
        const int MAXCODES = MAXLCODES + MAXDCODES;
        /// <summary>
        /// number of fixed literal/length codes 
        /// </summary>
        const int FIXLCODES = 288;

        int bits(State* s, int need)
        {
            long val;           /* bit accumulator (can use up to 20 bits) */

            /* load at least need bits into val */
            val = s->bitbuf;
            while (s->bitcnt < need) {
                if (s->incnt == s->inlen) throw new InvalidDataException("Output of input"); //longjmp(s->env, 1);   /* out of input */
                val |= (long)(s->inBuffer[s->incnt++]) << s->bitcnt;  /* load eight bits */
                s->bitcnt += 8;
            }

            /* drop need bits and update buffer, always zero to seven bits left */
            s->bitbuf = (int)(val >> need);
            s->bitcnt -= need;

            /* return need bits, zeroing the bits above that */
            return (int)(val & ((1L << need) - 1));
        }

        int stored(State* s)
        {
            uint len;       /* length of stored block */

            /* discard leftover bits from current byte (assumes s->bitcnt < 8) */
            s->bitbuf = 0;
            s->bitcnt = 0;

            /* get length and check against its one's complement */
            if (s->incnt + 4 > s->inlen) return 2;      /* not enough input */
            len = s->inBuffer[s->incnt++];
            len |= (uint)s->inBuffer[s->incnt++] << 8;
            if (s->inBuffer[s->incnt++] != (~len & 0xff) ||
                s->inBuffer[s->incnt++] != ((~len >> 8) & 0xff))
                return -2;                              /* didn't match complement! */

            /* copy len bytes from in to out */
            if (s->incnt + len > s->inlen) return 2;    /* not enough input */
            if (s->outBuffer != NIL) {
                if (s->outcnt + len > s->outlen)
                    return 1;                           /* not enough output space */
                while (len-- != 0)
                    s->outBuffer[s->outcnt++] = s->inBuffer[s->incnt++];
            }
            else {                                      /* just scanning */
                s->outcnt += len;
                s->incnt += len;
            }

            /* done with a valid stored block */
            return 0;
        }

        int decode(State* s, Huffman* h)
        {
            int len;            /* current number of bits in code */
            int code;           /* len bits being decoded */
            int first;          /* first code of length len */
            int count;          /* number of codes of length len */
            int index;          /* index of first code of length len in symbol table */
            int bitbuf;         /* bits from stream */
            int left;           /* bits left in next or left to process */
            short *next;        /* next number of codes */

            bitbuf = s->bitbuf;
            left = s->bitcnt;
            code = first = index = 0;
            len = 1;
            next = h->count + 1;
            while (true) {
                while (left-- != 0) {
                    code |= bitbuf & 1;
                    bitbuf >>= 1;
                    count = *next++;
                    if (code < first + count) { /* if length len, return symbol */
                        s->bitbuf = bitbuf;
                        s->bitcnt = (s->bitcnt - len) & 7;
                        return h->symbol[index + (code - first)];
                    }
                    index += count;             /* else update for next length */
                    first += count;
                    first <<= 1;
                    code <<= 1;
                    len++;
                }
                left = (MAXBITS+1) - len;
                if (left == 0) break;
                if (s->incnt == s->inlen) throw new InvalidDataException("Output of input"); //longjmp(s->env, 1);   /* out of input */
                //if (s->incnt == s->inlen) throw new Exception("Out of length");
                bitbuf = s->inBuffer[s->incnt++];
                if (left > 8) left = 8;
            }
            return -9;                          /* ran out of codes */
        }

        int construct(Huffman* h, short* length, int n)
        {
            int symbol;         /* current symbol when stepping through length[] */
            int len;            /* current length when stepping through h->count[] */
            int left;           /* number of possible codes left of current length */
            short* offs = stackalloc short[MAXBITS+1];      /* offsets in symbol table for each length */

            /* count number of codes of each length */
            for (len = 0; len <= MAXBITS; len++)
                h->count[len] = 0;
            for (symbol = 0; symbol < n; symbol++)
                (h->count[length[symbol]])++;   /* assumes lengths are within bounds */
            if (h->count[0] == n)               /* no codes! */
                return 0;                       /* complete, but decode() will fail */

            /* check for an over-subscribed or incomplete set of lengths */
            left = 1;                           /* one possible code of zero length */
            for (len = 1; len <= MAXBITS; len++) {
                left <<= 1;                     /* one more bit, double codes left */
                left -= h->count[len];          /* deduct count from possible codes */
                if (left < 0) return left;      /* over-subscribed--return negative */
            }                                   /* left > 0 means incomplete */

            /* generate offsets into symbol table for each length for sorting */
            offs[1] = 0;
            for (len = 1; len < MAXBITS; len++)
                offs[len + 1] = (short)(offs[len] + h->count[len]);

            /*
             * put symbols in table sorted by length, by symbol order within each
             * length
             */
            for (symbol = 0; symbol < n; symbol++)
                if (length[symbol] != 0)
                    h->symbol[offs[length[symbol]]++] = (short)symbol;

            /* return zero for complete set, positive for incomplete set */
            return left;
        }

        static readonly short[] lens = { /* Size base for length codes 257..285 */
                3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
                35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258};
        static readonly short[] lext = { /* Extra bits for length codes 257..285 */
                0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
                3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0};
        static readonly short[] dists = { /* Offset base for distance codes 0..29 */
                1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
                257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
                8193, 12289, 16385, 24577};
        static readonly short[] dext = { /* Extra bits for distance codes 0..29 */
                0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
                7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
                12, 12, 13, 13};

        int codes(State* s, Huffman* lencode, Huffman* distcode)
        {
            int symbol;         /* decoded symbol */
            int len;            /* length for copy */
            uint dist;      /* distance for copy */

            /* decode literals and length/distance pairs */
            do {
                symbol = decode(s, lencode);
                if (symbol < 0) return symbol;  /* invalid symbol */
                if (symbol < 256) {             /* literal: symbol is the byte */
                    /* write out the literal */
                    if (s->outBuffer != NIL) {
                        if (s->outcnt == s->outlen) return 1;
                        s->outBuffer[s->outcnt] = (byte)symbol;
                    }
                    s->outcnt++;
                }
                else if (symbol > 256) {        /* length */
                    /* get and compute length */
                    symbol -= 257;
                    if (symbol >= 29) return -9;        /* invalid fixed code */
                    len = lens[symbol] + bits(s, lext[symbol]);

                    /* get and check distance */
                    symbol = decode(s, distcode);
                    if (symbol < 0) return symbol;      /* invalid symbol */
                    dist = (uint)(dists[symbol] + bits(s, dext[symbol]));
                    if (dist > s->outcnt)
                        return -10;     /* distance too far back */

                    /* copy length bytes from distance bytes back */
                    if (s->outBuffer != NIL) {
                        if (s->outcnt + len > s->outlen) return 1;
                        while (len-- != 0) {
                            s->outBuffer[s->outcnt] = s->outBuffer[s->outcnt - dist];
                            s->outcnt++;
                        }
                    }
                    else
                        s->outcnt += (uint)len;
                }
            } while (symbol != 256);            /* end of block symbol */

            /* done with a valid fixed or dynamic block */
            return 0;
        }

        int virgin = 1;
        short[] lencnt = new short[MAXBITS + 1];
        short[] lensym = new short[FIXLCODES];
        short[] distcnt = new short[MAXBITS + 1];
        short[] distsym = new short[MAXDCODES];
        Huffman lencode = new Huffman{count = lencnt, symbol = lensym};
        Huffman distcode = new Huffman{count = distcnt, symbol = distsym};

        int doFixed(State* s)
        {
            /* build fixed huffman tables if first call (may not be thread safe) */
            if (virgin != 0) {
                int symbol;
                short* lengths = stackalloc short[FIXLCODES];

                /* literal/length table */
                for (symbol = 0; symbol < 144; symbol++)
                    lengths[symbol] = 8;
                for (; symbol < 256; symbol++)
                    lengths[symbol] = 9;
                for (; symbol < 280; symbol++)
                    lengths[symbol] = 7;
                for (; symbol < FIXLCODES; symbol++)
                    lengths[symbol] = 8;
                construct(&lencode, lengths, FIXLCODES);

                /* distance table */
                for (symbol = 0; symbol < MAXDCODES; symbol++)
                    lengths[symbol] = 5;
                construct(&distcode, lengths, MAXDCODES);

                /* do this just once */
                virgin = 0;
            }

            /* decode data until end-of-block code */
            return codes(s, &lencode, &distcode);
        }

        static readonly short[] order = /* permutation of code length codes */
                { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        int doDynamic(State* s)
        {
            int nlen, ndist, ncode;             /* number of lengths in descriptor */
            int index;                          /* index of lengths[] */
            int err;                            /* construct() return value */
            short* lengths = stackalloc short[MAXCODES];            /* descriptor code lengths */
            short* lencnt = stackalloc short[MAXBITS + 1];
            short* lensym = stackalloc short[MAXLCODES];         /* lencode memory */
            short* distcnt = stackalloc short[MAXBITS + 1]; 
            short* distsym = stackalloc short[MAXDCODES];       /* distcode memory */
            Huffman lencode = new Huffman {count = lencnt, symbol = lensym};          /* length code */
            Huffman distcode = new Huffman {count = distcnt, symbol = distsym};       /* distance code */

            /* get number of lengths in each table, check lengths */
            nlen = bits(s, 5) + 257;
            ndist = bits(s, 5) + 1;
            ncode = bits(s, 4) + 4;
            if (nlen > MAXLCODES || ndist > MAXDCODES)
                return -3;                      /* bad counts */

            /* read code length code lengths (really), missing lengths are zero */
            for (index = 0; index < ncode; index++)
                lengths[order[index]] = (short)bits(s, 3);
            for (; index < 19; index++)
                lengths[order[index]] = 0;

            /* build huffman table for code lengths codes (use lencode temporarily) */
            err = construct(&lencode, lengths, 19);
            if (err != 0) return -4;            /* require complete code set here */

            /* read length/literal and distance code length tables */
            index = 0;
            while (index < nlen + ndist) {
                int symbol;             /* decoded value */
                int len;                /* last length to repeat */

                symbol = decode(s, &lencode);
                if (symbol < 16)                /* length in 0..15 */
                    lengths[index++] = (short)symbol;
                else {                          /* repeat instruction */
                    len = 0;                    /* assume repeating zeros */
                    if (symbol == 16) {         /* repeat last length 3..6 times */
                        if (index == 0) return -5;      /* no last length! */
                        len = lengths[index - 1];       /* last length */
                        symbol = 3 + bits(s, 2);
                    }
                    else if (symbol == 17)      /* repeat zero 3..10 times */
                        symbol = 3 + bits(s, 3);
                    else                        /* == 18, repeat zero 11..138 times */
                        symbol = 11 + bits(s, 7);
                    if (index + symbol > nlen + ndist)
                        return -6;              /* too many lengths! */
                    while (symbol-- != 0)            /* repeat last or zero symbol times */
                        lengths[index++] = (short)len;
                }
            }

            /* build huffman table for literal/length codes */
            err = construct(&lencode, lengths, nlen);
            if (err < 0 || (err > 0 && nlen - lencode.count[0] != 1))
                return -7;      /* only allow incomplete codes if just one code */

            /* build huffman table for distance codes */
            err = construct(&distcode, lengths + nlen, ndist);
            if (err < 0 || (err > 0 && ndist - distcode.count[0] != 1))
                return -8;      /* only allow incomplete codes if just one code */

            /* decode data until end-of-block code */
            return codes(s, &lencode, &distcode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest">Pointer to destination pointer</param>
        /// <param name="destlen">Amount of output space</param>
        /// <param name="source">Pointer to source data pointer</param>
        /// <param name="sourcelen">Amount of input available</param>
        /// <returns></returns>
        public int DoPuff(byte* dest, uint* destlen, byte* source, int* sourcelen)
        {
            State s;             /* input/output state */
            int last, type;             /* block information */
            int err;                    /* return value */

            /* initialize output state */
            s.outBuffer = dest;
            s.outlen = *destlen;                /* ignored if dest is NIL */
            s.outcnt = 0;

            /* initialize input state */
            s.inBuffer = source;
            s.inlen = (uint)*sourcelen;
            s.incnt = 0;
            s.bitbuf = 0;
            s.bitcnt = 0;

            try
            {
                /* process blocks until last block or error */
                do {
                    last = bits(&s, 1);         /* one if last block */
                    type = bits(&s, 2);         /* block type 0..3 */
                    err = type == 0 ? stored(&s) :
                          (type == 1 ? doFixed(&s) :
                           (type == 2 ? doDynamic(&s) :
                            -1));               /* type == 3, invalid */
                    if (err != 0) break;        /* return with error */
                } while (last == 0);
            }
            catch (InvalidDataException)
            {
                err = 2;
            }

            /* update the lengths and return */
            if (err <= 0)
            {
                *destlen = s.outcnt;
                *sourcelen = (int)s.incnt;
            }
            return err;
        }
    }
}
