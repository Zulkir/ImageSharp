﻿#region License
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

using System.IO;

namespace ImageSharp.PNG
{
    public class PngImage
    {
        readonly uint width;
        readonly uint height;
        readonly BitDepth bitDepth;
        readonly ColorType colorType;
        readonly CompressionMethod compressionMethod;
        readonly FilterMethod filterMethod;
        readonly InterlaceMethod interlaceMethod;

        readonly Palette palette;

        public uint Width { get { return width; } }
        public uint Height { get { return height; } }
        public BitDepth BitDepth { get { return bitDepth; } }
        public ColorType ColorType { get { return colorType; } }
        public CompressionMethod CompressionMethod { get { return compressionMethod; } }
        public FilterMethod FilterMethod { get { return filterMethod; } }
        public InterlaceMethod InterlaceMethod { get { return interlaceMethod; } }

        public Palette Palette { get { return palette; } }

        public unsafe PngImage(byte[] fileData, int byteOffset = 0)
        {
            fixed (byte* data = fileData)
            {
                byte* p = data;
                int remaining = fileData.Length - byteOffset;

                // Signature
                if (remaining < 8) 
                    throw new InvalidDataException("The file data ends abruptly");
                remaining -= 8;

                if (*(ulong*)p != Constants.PngSignature)
                    throw new InvalidDataException("PNG signature is missing or incorect");
                p += 8;
                
                // IHDR
                if (remaining < Header.StructLength)
                    throw new InvalidDataException("The file data ends abruptly");
                remaining -= Header.StructLength;

                var pHeader = (Header*) p;
                p += Header.StructLength;

                if (pHeader->ChunkType.Value != Constants.IHDR)
                    throw new InvalidDataException(string.Format("IHDR chunk expected, but {0} found.", pHeader->ChunkType.ToString()));
                if (pHeader->Length.FlipEndianness() != Header.DataLength)
                    throw new InvalidDataException("IHDR length must be exactly 13 bytes");

                width = pHeader->Width.FlipEndianness();
                height = pHeader->Height.FlipEndianness();
                bitDepth = pHeader->BitDepth;
                colorType = pHeader->ColorType;
                compressionMethod = pHeader->CompressionMethod;
                filterMethod = pHeader->FilterMethod;
                interlaceMethod = pHeader->InterlaceMethod;

                while (true)
                {
                    if (remaining < ChunkBeginning.StructLength)
                        throw new InvalidDataException("The file data ends abruptly");
                    remaining -= ChunkBeginning.StructLength;

                    var pChunkBeginning = (ChunkBeginning*) p;
                    p += ChunkBeginning.StructLength;

                    switch (pChunkBeginning->ChunkType.Value)
                    {
                        case Constants.PLTE:
                        {
                            if (remaining < pChunkBeginning->Length + 4)
                                throw new InvalidDataException("The file data ends abruptly");
                            remaining -= (int)pChunkBeginning->Length + 4;

                            var pEntries = (PaletteEntry*) p;
                            p += (int)pChunkBeginning->Length + 4;

                            palette = new Palette(pEntries, (int)pChunkBeginning->Length / 3);
                        }
                        break;
                    }
                }
            }
        }
    }
}
