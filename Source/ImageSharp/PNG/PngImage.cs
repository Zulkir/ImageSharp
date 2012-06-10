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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageSharp.PNG
{
    public class PngImage
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public BitDepth BitDepth { get; set; }
        public ColorType ColorType { get; set; }
        public CompressionMethod CompressionMethod { get; set; }
        public FilterMethod FilterMethod { get; set; }
        public InterlaceMethod InterlaceMethod { get; set; }
        public Palette Palette { get; set; }
        public byte[] Transparency { get; set; }
        public byte[] Data { get; set; }

        public unsafe PngImage(byte[] fileData, int byteOffset = 0)
        {
            fixed (byte* pFileData = fileData)
            {
                byte* p = pFileData;
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
                List<PointerLengthPair> dataParts = null;
                int totalDataLength = 0;
                bool idatFinished = false;

                while (!endFound)
                {
                    if (remaining < ChunkBeginning.StructLength)
                        throw new InvalidDataException("The file data ends abruptly");
                    remaining -= ChunkBeginning.StructLength;

                    var pChunkBeginning = (ChunkBeginning*) p;
                    p += ChunkBeginning.StructLength;

                    int length = (int)pChunkBeginning->LengthFlipped.FlipEndianness();

                    if (remaining < length + 4)
                        throw new InvalidDataException("The file data ends abruptly");
                    remaining -= length + 4;

                    byte* chunkData = p;
                    p += length;

                    uint crc = (*(uint*) p).FlipEndianness();
                    p += 4;

                    // todo: check CRC

                    if (dataParts != null && !idatFinished && pChunkBeginning->ChunkType.Value != Constants.IDAT)
                        idatFinished = true;

                    switch (pChunkBeginning->ChunkType.Value)
                    {
                        case Constants.PLTE:
                        {
                            if (Palette != null)
                                throw new InvalidDataException("PLTE chunk appears twice");

                            Palette = new Palette((PaletteEntry*)chunkData, length / 3);
                            break;
                        } 
                        case Constants.tRNS:
                        {
                            if (dataParts != null)
                                throw new InvalidDataException("tRNS chunk must precede IDAT change");

                            switch (ColorType)
                            {
                                case ColorType.Grayscale:
                                    if (length != 2)
                                        throw new InvalidDataException("For color type Grayscale, tRNS length must be exactly 2 bytes");
                                    break;
                                case ColorType.TrueColor:
                                    if (length != 6)
                                        throw new InvalidDataException("For color type TrueColor, tRNS length must be exactly 6 bytes");
                                    break;
                                case ColorType.PaletteColor:
                                    if (Palette == null)
                                        throw new InvalidDataException("For color type PaletteColor, PLTE chunk must precede tRNS chunk");
                                    if (length > Palette.Entries.Length)
                                        throw new InvalidDataException("For color type PaletteColor, tRNS chunk length must be less than the number of palette entries");
                                    break;
                                default: throw new InvalidDataException(string.Format("tRNS chunk is not supported by the '{0}' color type", ColorType));
                            }
                            Transparency = new byte[length];
                            Marshal.Copy((IntPtr)chunkData, Transparency, 0, length);
                            break;
                        }
                        case Constants.IDAT:
                        {
                            if (idatFinished)
                                throw new InvalidDataException("IDAT chunks must appear consecutively");

                            dataParts = dataParts ?? new List<PointerLengthPair>();
                            dataParts.Add(new PointerLengthPair{Pointer = (IntPtr)chunkData, Length = length});
                            totalDataLength += length;
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

                Data = new byte[Width * Height * ColorType * BitDepth];

                var puff = new Puff();
            }
        }
    }
}
