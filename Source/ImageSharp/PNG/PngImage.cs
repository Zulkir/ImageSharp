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

using System.IO;

namespace ImageSharp.PNG
{
    public class PngImage
    {
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public BitDepth BitDepth { get; private set; }
        public ColorType ColorType { get; private set; }
        public CompressionMethod CompressionMethod { get; private set; }
        public FilterMethod FilterMethod { get; private set; }
        public InterlaceMethod InterlaceMethod { get; private set; }
        public Palette Palette { get; private set; }

        //readonly byte[] transparency;

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
                if (pHeader->LengthFlipped.FlipEndianness() != Header.DataLength)
                    throw new InvalidDataException("IHDR length must be exactly 13 bytes");

                Width = pHeader->WidthFlipped.FlipEndianness();
                Height = pHeader->HeightFlipped.FlipEndianness();
                BitDepth = pHeader->BitDepth;
                ColorType = pHeader->ColorType;
                CompressionMethod = pHeader->CompressionMethod;
                FilterMethod = pHeader->FilterMethod;
                InterlaceMethod = pHeader->InterlaceMethod;

                bool endFound = false;

                while (!endFound)
                {
                    if (remaining < ChunkBeginning.StructLength)
                        throw new InvalidDataException("The file data ends abruptly");
                    remaining -= ChunkBeginning.StructLength;

                    var pChunkBeginning = (ChunkBeginning*) p;
                    p += ChunkBeginning.StructLength;

                    int length = (int)pChunkBeginning->LengthFlipped.FlipEndianness();

                    byte* chunkData = p;
                    p += length;

                    uint crc = (*(uint*) p).FlipEndianness();
                    p += 4;

                    // todo: check CRC

                    switch (pChunkBeginning->ChunkType.Value)
                    {
                        case Constants.PLTE:
                        {
                            if (Palette != null)
                                throw new InvalidDataException("PLTE chunk appears twice");

                            if (remaining < length + 4)
                                throw new InvalidDataException("The file data ends abruptly");
                            remaining -= length + 4;

                            Palette = new Palette((PaletteEntry*)chunkData, length / 3);
                            break;
                        } 
                        case Constants.IDAT:
                        {
                            break;
                        } 
                        case Constants.IEND:
                        {
                            if (length != 0)
                                throw new InvalidDataException("IEND chunk must be empty");

                            endFound = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
