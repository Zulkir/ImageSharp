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
 This code is a ported to C# and slightly modified (to support source data split into chunks) code from 
 http://svn.ghostscript.com/ghostscript/tags/zlib-1.2.3/contrib/puff/puff.c
 by Mark Adler that is subject to a similar ZLib license.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace ImageSharp.PNG
{
    public unsafe class Puff
    {
        /// <summary>
        /// output buffer
        /// </summary>
        byte* outBuffer;
        /// <summary>
        /// available space at outBuffer
        /// </summary>
        uint outlen;
        /// <summary>
        /// bytes written to out so far
        /// </summary>
        uint outcnt;

        /// <summary>
        /// input buffer
        /// </summary>
        byte* inBuffer;
        /// <summary>
        /// available input at inBuffer
        /// </summary>
        uint inlen;
        /// <summary>
        /// bytes read so far
        /// </summary>
        uint incnt;

        /// <summary>
        /// bit buffer
        /// </summary>
        int bitbuf;
        /// <summary>
        /// number of Bits in bit buffer
        /// </summary>
        int bitcnt;



        List<PointerLengthPair> pairs;
        int currentPairIndex;
        int incntToJump;

        void JumpToNextSourceChunk()
        {
            currentPairIndex++; 
            var pair = pairs[currentPairIndex]; 
            incntToJump += pair.Length; 
            inBuffer = (byte*)pair.Pointer - incnt;
        }



        /// <summary>
        /// Huffman code decoding tables.  count[1..MAXBITS] is the number of symbols of
        /// each length, which for a canonical code are stepped through in order.
        /// symbol[] are the symbol values in canonical order, where the number of
        /// entries is the sum of the counts in count[].  The decoding process can be
        /// seen in the function Decode() below.
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
        /// maximum Bits in a code
        /// </summary>
        const int MAXBITS = 15;
        /// <summary>
        /// maximum number of literal/length Codes
        /// </summary>
        const int MAXLCODES = 286;
        /// <summary>
        /// maximum number of distance Codes
        /// </summary>
        const int MAXDCODES = 30;
        /// <summary>
        /// maximum Codes lengths to read
        /// </summary>
        const int MAXCODES = MAXLCODES + MAXDCODES;
        /// <summary>
        /// number of fixed literal/length Codes 
        /// </summary>
        const int FIXLCODES = 288;

        int Bits(int need)
        {
            long val;           /* bit accumulator (can use up to 20 Bits) */

            /* load at least need Bits into val */
            val = bitbuf;
            while (bitcnt < need) {
                if (incnt == inlen) throw new InvalidDataException("Output of input"); //longjmp(env, 1);   /* out of input */
                val |= (long)(inBuffer[incnt++]) << bitcnt;  /* load eight Bits */
                if (incnt == incntToJump) JumpToNextSourceChunk();
                bitcnt += 8;
            }

            /* drop need Bits and update buffer, always zero to seven Bits left */
            bitbuf = (int)(val >> need);
            bitcnt -= need;

            /* return need Bits, zeroing the Bits above that */
            return (int)(val & ((1L << need) - 1));
        }

        int Stored()
        {
            uint len;       /* length of Stored block */

            /* discard leftover Bits from current byte (assumes bitcnt < 8) */
            bitbuf = 0;
            bitcnt = 0;

            /* get length and check against its one's complement */
            if (incnt + 4 > inlen) return 2;      /* not enough input */
            len = inBuffer[incnt++];
            if (incnt == incntToJump) JumpToNextSourceChunk();
            len |= (uint)inBuffer[incnt++] << 8;
            if (incnt == incntToJump) JumpToNextSourceChunk();

            byte check1 = inBuffer[incnt++];
            if (incnt == incntToJump) JumpToNextSourceChunk();

            byte check2 = inBuffer[incnt++];
            if (incnt == incntToJump) JumpToNextSourceChunk();

            if (check1 != (~len & 0xff) ||
                check2 != ((~len >> 8) & 0xff))
                return -2;                              /* didn't match complement! */

            /* copy len bytes from in to out */
            if (incnt + len > inlen) return 2;    /* not enough input */
            if (outBuffer != NIL) {
                if (outcnt + len > outlen)
                    return 1;                           /* not enough output space */
                while (len-- != 0)
                {
                    outBuffer[outcnt++] = inBuffer[incnt++];
                    if (incnt == incntToJump) JumpToNextSourceChunk();
                }
            }
            else {                                      /* just scanning */
                outcnt += len;
                incnt += len;
            }

            /* done with a valid Stored block */
            return 0;
        }

        int Decode(Huffman* h)
        {
            int len;            /* current number of Bits in code */
            int code;           /* len Bits being decoded */
            int first;          /* first code of length len */
            int count;          /* number of Codes of length len */
            int index;          /* index of first code of length len in symbol table */
            int bitbuf;         /* Bits from stream */
            int left;           /* Bits left in next or left to process */
            short *next;        /* next number of Codes */

            bitbuf = this.bitbuf;
            left = bitcnt;
            code = first = index = 0;
            len = 1;
            next = h->count + 1;
            while (true) {
                while (left-- != 0) {
                    code |= bitbuf & 1;
                    bitbuf >>= 1;
                    count = *next++;
                    if (code < first + count) { /* if length len, return symbol */
                        this.bitbuf = bitbuf;
                        bitcnt = (bitcnt - len) & 7;
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
                if (incnt == inlen) throw new InvalidDataException("Output of input"); //longjmp(env, 1);   /* out of input */
                //if (incnt == inlen) throw new Exception("Out of length");
                bitbuf = inBuffer[incnt++];
                if (incnt == incntToJump) JumpToNextSourceChunk();
                if (left > 8) left = 8;
            }
            return -9;                          /* ran out of Codes */
        }

        int Construct(Huffman* h, short* length, int n)
        {
            int symbol;         /* current symbol when stepping through length[] */
            int len;            /* current length when stepping through h->count[] */
            int left;           /* number of possible Codes left of current length */
            short* offs = stackalloc short[MAXBITS+1];      /* offsets in symbol table for each length */

            /* count number of Codes of each length */
            for (len = 0; len <= MAXBITS; len++)
                h->count[len] = 0;
            for (symbol = 0; symbol < n; symbol++)
                (h->count[length[symbol]])++;   /* assumes lengths are within bounds */
            if (h->count[0] == n)               /* no Codes! */
                return 0;                       /* complete, but Decode() will fail */

            /* check for an over-subscribed or incomplete set of lengths */
            left = 1;                           /* one possible code of zero length */
            for (len = 1; len <= MAXBITS; len++) {
                left <<= 1;                     /* one more bit, double Codes left */
                left -= h->count[len];          /* deduct count from possible Codes */
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

        static readonly short[] lens = { /* Size base for length Codes 257..285 */
                3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
                35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258};
        static readonly short[] lext = { /* Extra Bits for length Codes 257..285 */
                0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
                3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0};
        static readonly short[] dists = { /* Offset base for distance Codes 0..29 */
                1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
                257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
                8193, 12289, 16385, 24577};
        static readonly short[] dext = { /* Extra Bits for distance Codes 0..29 */
                0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
                7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
                12, 12, 13, 13};

        int Codes(Huffman* lencode, Huffman* distcode)
        {
            int symbol;         /* decoded symbol */
            int len;            /* length for copy */
            uint dist;      /* distance for copy */

            /* Decode literals and length/distance pairs */
            do {
                symbol = Decode(lencode);
                if (symbol < 0) return symbol;  /* invalid symbol */
                if (symbol < 256) {             /* literal: symbol is the byte */
                    /* write out the literal */
                    if (outBuffer != NIL) {
                        if (outcnt == outlen) return 1;
                        outBuffer[outcnt] = (byte)symbol;
                    }
                    outcnt++;
                }
                else if (symbol > 256) {        /* length */
                    /* get and compute length */
                    symbol -= 257;
                    if (symbol >= 29) return -9;        /* invalid fixed code */
                    len = lens[symbol] + Bits(lext[symbol]);

                    /* get and check distance */
                    symbol = Decode(distcode);
                    if (symbol < 0) return symbol;      /* invalid symbol */
                    dist = (uint)(dists[symbol] + Bits(dext[symbol]));
                    if (dist > outcnt)
                        return -10;     /* distance too far back */

                    /* copy length bytes from distance bytes back */
                    if (outBuffer != NIL) {
                        if (outcnt + len > outlen) return 1;
                        while (len-- != 0) {
                            outBuffer[outcnt] = outBuffer[outcnt - dist];
                            outcnt++;
                        }
                    }
                    else
                        outcnt += (uint)len;
                }
            } while (symbol != 256);            /* end of block symbol */

            /* done with a valid fixed or dynamic block */
            return 0;
        }

        int virgin = 1;
        readonly short[] perserveData = new short[(MAXBITS + 1) + FIXLCODES + (MAXBITS + 1) + MAXDCODES];

        int Fixed()
        {
            int result;

            fixed (short* p = perserveData)
            {
                short* lencnt = p;
                short* lensym = lencnt + MAXBITS + 1;
                short* distcnt = lensym + FIXLCODES;
                short* distsym = distcnt + MAXBITS + 1;
                Huffman lencode = new Huffman {count = lencnt, symbol = lensym};
                Huffman distcode = new Huffman { count = distcnt, symbol = distsym };

                /* build fixed huffman tables if first call (may not be thread safe) */
                if (virgin != 0)
                {
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
                    Construct(&lencode, lengths, FIXLCODES);

                    /* distance table */
                    for (symbol = 0; symbol < MAXDCODES; symbol++)
                        lengths[symbol] = 5;
                    Construct(&distcode, lengths, MAXDCODES);

                    /* do this just once */
                    virgin = 0;
                }

                /* Decode data until end-of-block code */
                result = Codes(&lencode, &distcode);
            }

            return result;
        }

        static readonly short[] order = /* permutation of code length Codes */
                { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        int Dynamic()
        {
            int nlen, ndist, ncode;             /* number of lengths in descriptor */
            int index;                          /* index of lengths[] */
            int err;                            /* Construct() return value */
            short* lengths = stackalloc short[MAXCODES];            /* descriptor code lengths */
            short* lencnt = stackalloc short[MAXBITS + 1];
            short* lensym = stackalloc short[MAXLCODES];         /* lencode memory */
            short* distcnt = stackalloc short[MAXBITS + 1]; 
            short* distsym = stackalloc short[MAXDCODES];       /* distcode memory */
            Huffman lencode = new Huffman {count = lencnt, symbol = lensym};          /* length code */
            Huffman distcode = new Huffman {count = distcnt, symbol = distsym};       /* distance code */

            /* get number of lengths in each table, check lengths */
            nlen = Bits(5) + 257;
            ndist = Bits(5) + 1;
            ncode = Bits(4) + 4;
            if (nlen > MAXLCODES || ndist > MAXDCODES)
                return -3;                      /* bad counts */

            /* read code length code lengths (really), missing lengths are zero */
            for (index = 0; index < ncode; index++)
                lengths[order[index]] = (short)Bits(3);
            for (; index < 19; index++)
                lengths[order[index]] = 0;

            /* build huffman table for code lengths Codes (use lencode temporarily) */
            err = Construct(&lencode, lengths, 19);
            if (err != 0) return -4;            /* require complete code set here */

            /* read length/literal and distance code length tables */
            index = 0;
            while (index < nlen + ndist) {
                int symbol;             /* decoded value */
                int len;                /* last length to repeat */

                symbol = Decode(&lencode);
                if (symbol < 16)                /* length in 0..15 */
                    lengths[index++] = (short)symbol;
                else {                          /* repeat instruction */
                    len = 0;                    /* assume repeating zeros */
                    if (symbol == 16) {         /* repeat last length 3..6 times */
                        if (index == 0) return -5;      /* no last length! */
                        len = lengths[index - 1];       /* last length */
                        symbol = 3 + Bits(2);
                    }
                    else if (symbol == 17)      /* repeat zero 3..10 times */
                        symbol = 3 + Bits(3);
                    else                        /* == 18, repeat zero 11..138 times */
                        symbol = 11 + Bits(7);
                    if (index + symbol > nlen + ndist)
                        return -6;              /* too many lengths! */
                    while (symbol-- != 0)            /* repeat last or zero symbol times */
                        lengths[index++] = (short)len;
                }
            }

            /* build huffman table for literal/length Codes */
            err = Construct(&lencode, lengths, nlen);
            if (err < 0 || (err > 0 && nlen - lencode.count[0] != 1))
                return -7;      /* only allow incomplete Codes if just one code */

            /* build huffman table for distance Codes */
            err = Construct(&distcode, lengths + nlen, ndist);
            if (err < 0 || (err > 0 && ndist - distcode.count[0] != 1))
                return -8;      /* only allow incomplete Codes if just one code */

            /* Decode data until end-of-block code */
            return Codes(&lencode, &distcode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest">Pointer to destination pointer</param>
        /// <param name="destlen">Amount of output space</param>
        /// <param name="source">Pointer to source data pointer</param>
        /// <param name="sourcelen">Amount of input available</param>
        /// <returns></returns>
        public int DoPuff(byte* dest, uint* destlen, byte* source, uint* sourcelen)
        {
            //State s;             /* input/output state */
            int last, type;             /* block information */
            int err;                    /* return value */

            /* initialize output state */
            outBuffer = dest;
            outlen = *destlen;                /* ignored if dest is NIL */
            outcnt = 0;

            /* initialize input state */
            inBuffer = source;
            inlen = *sourcelen;
            incnt = 0;
            bitbuf = 0;
            bitcnt = 0;

            try
            {
                /* process blocks until last block or error */
                do {
                    last = Bits(1);         /* one if last block */
                    type = Bits(2);         /* block type 0..3 */
                    err = type == 0 ? Stored() :
                          (type == 1 ? Fixed() :
                           (type == 2 ? Dynamic() :
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
                *destlen = outcnt;
                *sourcelen = incnt;
            }
            return err;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest">Pointer to destination pointer</param>
        /// <param name="destlen">Amount of output space</param>
        /// <param name="sourcePairs"> </param>
        /// <param name="sourcelen">Amount of input available</param>
        /// <returns></returns>
        public int DoPuff(byte* dest, uint* destlen, List<PointerLengthPair> sourcePairs, uint* sourcelen)
        {
            //State s;             /* input/output state */
            int last, type;             /* block information */
            int err;                    /* return value */

            /* initialize output state */
            outBuffer = dest;
            outlen = *destlen;                /* ignored if dest is NIL */
            outcnt = 0;

            /* initialize input state */
            inBuffer = (byte*)sourcePairs[0].Pointer + 2;
            inlen = *sourcelen;
            incnt = 0;
            bitbuf = 0;
            bitcnt = 0;

            pairs = sourcePairs;
            incntToJump = sourcePairs[0].Length - 2;

            try
            {
                /* process blocks until last block or error */
                do
                {
                    last = Bits(1);         /* one if last block */
                    type = Bits(2);         /* block type 0..3 */
                    err = type == 0 ? Stored() :
                          (type == 1 ? Fixed() :
                           (type == 2 ? Dynamic() :
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
                *destlen = outcnt;
                *sourcelen = incnt;
            }
            return err;
        }
    }
}
