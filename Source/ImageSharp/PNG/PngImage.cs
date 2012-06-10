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
        public int Width { get; set; }
        public int Height { get; set; }
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

                if (*(ulong*)p != Helper.PngSignature)
                    throw new InvalidDataException("PNG signature is missing or incorect");
                p += 8;
                
                // IHDR
                if (remaining < Header.StructLength)
                    throw new InvalidDataException("The file data ends abruptly");
                remaining -= Header.StructLength;

                var pHeader = (Header*) p;
                p += Header.StructLength;

                if (pHeader->ChunkType.Value != Helper.IHDR)
                    throw new InvalidDataException(string.Format("IHDR chunk expected, but {0} found.", pHeader->ChunkType.ToString()));
                if (pHeader->LengthFlipped.FlipEndianness() != Header.DataLength)
                    throw new InvalidDataException("IHDR length must be exactly 13 bytes");

                Width = (int)pHeader->WidthFlipped.FlipEndianness();
                Height = (int)pHeader->HeightFlipped.FlipEndianness();
                BitDepth = pHeader->BitDepth;
                ColorType = pHeader->ColorType;
                CompressionMethod = pHeader->CompressionMethod;
                FilterMethod = pHeader->FilterMethod;
                InterlaceMethod = pHeader->InterlaceMethod;

                bool endFound = false;
                List<PointerLengthPair> compressedDataParts = null;
                int totalCompressedDataLength = 0;
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

                    if (compressedDataParts != null && !idatFinished && pChunkBeginning->ChunkType.Value != Helper.IDAT)
                        idatFinished = true;

                    switch (pChunkBeginning->ChunkType.Value)
                    {
                        case Helper.PLTE:
                        {
                            if (Palette != null)
                                throw new InvalidDataException("PLTE chunk appears twice");

                            Palette = new Palette((PaletteEntry*)chunkData, length / 3);
                            break;
                        } 
                        case Helper.tRNS:
                        {
                            if (compressedDataParts != null)
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
                        case Helper.IDAT:
                        {
                            if (idatFinished)
                                throw new InvalidDataException("IDAT chunks must appear consecutively");

                            compressedDataParts = compressedDataParts ?? new List<PointerLengthPair>();
                            compressedDataParts.Add(new PointerLengthPair{Pointer = (IntPtr)chunkData, Length = length});
                            totalCompressedDataLength += length;
                            break;
                        } 
                        case Helper.IEND:
                        {
                            if (length != 0)
                                throw new InvalidDataException("IEND chunk must be empty");

                            endFound = true;
                            break;
                        }
                    }
                }

                if (compressedDataParts == null)
                    throw new InvalidDataException("No mandatory IDAT chunks found");

                var puff = new Puff();
                Data = new byte[Helper.SizeOfFilteredImageData(Width, Height, ColorType, BitDepth)];
                
                fixed (byte* pData = Data)
                {
                    uint destLen = (uint)Data.Length;
                    uint sourceLen = (uint) totalCompressedDataLength - 6;

                    var puffResult = puff.DoPuff(pData, &destLen, compressedDataParts, &sourceLen);
                    if (puffResult != 0)
                        throw new InvalidDataException(string.Format("Decompressing the image data failed with the code {0}", puffResult));

                    if (compressedDataParts.Count == 1)
                    {
                        /*
                        var zlibBeginning = (ZlibBeginning*)compressedDataParts[0].Pointer;
                        if (zlibBeginning->CompressionFlags != 8)
                            throw new InvalidDataException("ZLib compression flags must be 8");*/
                        /*
                        byte* source = (byte*)compressedDataParts[0].Pointer + 2;
                        var puffResult = puff.DoPuff(pData, &destLen, source, &sourceLen);
                        if (puffResult != 0)
                            throw new InvalidDataException(string.Format("Decompressing the image data failed with the code {0}", puffResult));*/
                    }
                    else
                    {
                        /*
                        var compressedData = new byte[totalCompressedDataLength];
                        int offset = 0;
                        foreach (var part in compressedDataParts)
                        {
                            Marshal.Copy(part.Pointer, compressedData, offset, part.Length);
                            offset += part.Length;
                        }

                        fixed (byte* pCompressedData = compressedData)
                        {
                            var puffResult = puff.DoPuff(pData, &destLen, pCompressedData + 2, &sourceLen);
                            if (puffResult != 0)
                                throw new InvalidDataException(string.Format("Decompressing the image data failed with the code {0}", puffResult));
                        }*/
                    }

                    if (destLen != Data.Length)
                        throw new InvalidDataException(string.Format("Expected decompressed data size was {0}, but {1} were recieved", Data.Length, destLen));
                    if (sourceLen != totalCompressedDataLength - 6)
                        throw new InvalidDataException(string.Format("Expected compressed data size was {0}, but was actually {1}", totalCompressedDataLength, sourceLen));
                }
            }

            fixed (byte* pData = Data)
            {
                int numPasses = InterlaceMethod == InterlaceMethod.Adam7 ? 7 : 1;

                for (int pass = 0; pass < numPasses; pass++)
                {
                    int passWidthInBytes = Helper.SizeOfImageRow(
                        InterlaceMethod == InterlaceMethod.Adam7 ? Helper.InterlacedPassWidth(pass, Width) : Width, 
                        ColorType, BitDepth);

                    if (passWidthInBytes != 0)
                    {
                        byte* rawRow = pData;
                        byte* filteredRow = pData;
                        int bpp = Helper.BytesPerPixelCeil(ColorType, BitDepth);

                        // First row
                        var filterType = (FilterType)filteredRow[0]; filteredRow++;
                        switch (filterType)
                        {
                            case FilterType.None:
                                for (int x = 0; x < passWidthInBytes; x++)
                                    rawRow[x] = filteredRow[x];
                                break;
                            case FilterType.Sub:
                                for (int x = 0; x < bpp; x++)
                                    rawRow[x] = filteredRow[x];
                                for (int x = bpp; x < passWidthInBytes; x++)
                                    rawRow[x] = (byte)(filteredRow[x] + rawRow[x - bpp]);
                                break;
                            case FilterType.Up:
                                for (int x = 0; x < passWidthInBytes; x++)
                                    rawRow[x] = filteredRow[x];
                                break;
                            case FilterType.Average:
                                for (int x = 0; x < bpp; x++)
                                    rawRow[x] = filteredRow[x];
                                for (int x = bpp; x < passWidthInBytes; x++)
                                    rawRow[x] = (byte)(filteredRow[x] + rawRow[x - bpp] / 2);
                                break;
                            case FilterType.Parth:
                                for (int x = 0; x < bpp; x++)
                                    rawRow[x] = filteredRow[x];
                                for (int x = bpp; x < passWidthInBytes; x++)
                                    rawRow[x] = (byte)(filteredRow[x] + Helper.PaethPredictor(rawRow[x - bpp], 0, 0));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("filterType");
                        }

                        // Other rows

                        for (int y = 1; y < Height; y++)
                        {
                            byte* prevRawRow = rawRow;
                            rawRow += passWidthInBytes;
                            filteredRow += passWidthInBytes;

                            filterType = (FilterType)filteredRow[0]; filteredRow++;
                            switch (filterType)
                            {
                                case FilterType.None:
                                    for (int x = 0; x < passWidthInBytes; x++)
                                        rawRow[x] = filteredRow[x];
                                    break;
                                case FilterType.Sub:
                                    for (int x = 0; x < bpp; x++)
                                        rawRow[x] = filteredRow[x];
                                    for (int x = bpp; x < passWidthInBytes; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + rawRow[x - bpp]);
                                    break;
                                case FilterType.Up:
                                    for (int x = 0; x < passWidthInBytes; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + prevRawRow[x]);
                                    break;
                                case FilterType.Average:
                                    for (int x = 0; x < bpp; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + prevRawRow[x] / 2);
                                    for (int x = bpp; x < passWidthInBytes; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + (rawRow[x - bpp] + prevRawRow[x]) / 2);
                                    break;
                                case FilterType.Parth:
                                    for (int x = 0; x < bpp; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + Helper.PaethPredictor(0, prevRawRow[0], 0));
                                    for (int x = bpp; x < passWidthInBytes; x++)
                                        rawRow[x] = (byte)(filteredRow[x] + Helper.PaethPredictor(rawRow[x - bpp], prevRawRow[x], prevRawRow[x - bpp]));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException("filterType");
                            }
                        }
                    }
                }
            }

            // Done
        }
    }
}
