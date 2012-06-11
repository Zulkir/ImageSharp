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
        public int Width { get; private set; }
        public int Height { get; private set; }
        public BitDepth BitDepth { get; private set; }
        public ColorType ColorType { get; private set; }
        public CompressionMethod CompressionMethod { get; private set; }
        public FilterMethod FilterMethod { get; private set; }
        public InterlaceMethod InterlaceMethod { get; private set; }
        public Palette Palette { get; private set; }
        public byte[] Transparency { get; private set; }
        public byte[] Data { get; private set; }

        #region Convert
        public unsafe void ToRgba8(byte* dest)
        {
            int pixelCount = Width * Height;
            byte* write = dest;

            GCHandle handle = default(GCHandle);
            if (InterlaceMethod == InterlaceMethod.Adam7)
            {
                var interlacedData = new int[pixelCount];
                handle = GCHandle.Alloc(interlacedData, GCHandleType.Pinned);
                write = (byte*)handle.AddrOfPinnedObject();
            }

            const double sixteenToEight = (255.0 / ((1 << 16) - 1) + 0.5);

            switch (ColorType)
            {
                case ColorType.Grayscale:
                    fixed (byte* source = Data)
                    {
                        byte* read = source;
                        switch (BitDepth)
                        {
                            case BitDepth.One:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i += 8)
                                    {
                                        for (int j = 128; j >= 0; j >>= 1)
                                        {
                                            write[0] = write[1] = write[2] = ((*read) & j) == 0 ? (byte)0 : (byte)255;
                                            write[3] = 255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i += 8)
                                    {
                                        for (int j = 128; j >= 0; j >>= 1)
                                        {
                                            bool bit = ((*read) & j) != 0;
                                            write[0] = write[1] = write[2] = bit ? (byte)255 : (byte)0;
                                            write[3] = bit == ((Transparency[1] & 0x1) != 0) ? (byte)0 : (byte)255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                break;
                            case BitDepth.Two:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i += 4)
                                    {
                                        for (int j = 7; j >= 0; j -= 2)
                                        {
                                            write[0] = write[1] = write[2] = (byte)(85 * (((*read) >> j) & 0x3));
                                            write[3] = 255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i += 4)
                                    {
                                        for (int j = 7; j >= 0; j -= 2)
                                        {
                                            int bits = ((*read) >> j) & 0x3;
                                            write[0] = write[1] = write[2] = (byte)(85 * bits);
                                            write[3] = bits == (Transparency[1] & 0x3) ? (byte)0 : (byte)255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                break;
                            case BitDepth.Four:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i += 2)
                                    {
                                        for (int j = 4; j >= 0; j -= 4)
                                        {
                                            write[0] = write[1] = write[2] = (byte)(17 * (((*read) >> j) & 0xf));
                                            write[3] = 255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i += 2)
                                    {
                                        for (int j = 4; j >= 0; j -= 4)
                                        {
                                            int bits = ((*read) >> j) & 0xf;
                                            write[0] = write[1] = write[2] = (byte)(17 * bits);
                                            write[3] = bits == (Transparency[1] & 0xf) ? (byte)0 : (byte)255;
                                            write += 4;
                                        }
                                        read++;
                                    }
                                }
                                break;
                            case BitDepth.Eight:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = write[1] = write[2] = *read;
                                        write[3] = 255;
                                        write += 4;
                                        read++;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = write[1] = write[2] = *read;
                                        write[3] = *read == Transparency[1] ? (byte)0 : (byte)255;
                                        write += 4;
                                        read++;
                                    }
                                }
                                break;
                            case BitDepth.Sixteen:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = write[1] = write[2] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                        write[3] = 255;
                                        write += 4;
                                        read += 2;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = write[1] = write[2] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                        write[3] = (read[0] == Transparency[0] && read[1] == Transparency[1]) ? (byte)0 : (byte)255;
                                        write += 4;
                                        read += 2;
                                    }
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case ColorType.TrueColor:
                    fixed (byte* source = Data)
                    {
                        byte* read = source;
                        switch (BitDepth)
                        {
                            case BitDepth.Eight:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = read[0];
                                        write[1] = read[1];
                                        write[2] = read[2];
                                        write[3] = 255;

                                        write += 4;
                                        read += 3;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = read[0];
                                        write[1] = read[1];
                                        write[2] = read[2];
                                        write[3] =
                                            read[0] == Transparency[1] && read[1] == Transparency[3] && read[2] == Transparency[5] 
                                            ? (byte)0 : (byte)255;

                                        write += 4;
                                        read += 3;
                                    }
                                }
                                break;
                            case BitDepth.Sixteen:
                                if (Transparency == null)
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                        write[1] = (byte)(((read[2] << 8) | read[3]) * sixteenToEight);
                                        write[2] = (byte)(((read[4] << 8) | read[5]) * sixteenToEight);
                                        write[3] = 255;

                                        write += 4;
                                        read += 6;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < pixelCount; i++)
                                    {
                                        write[0] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                        write[1] = (byte)(((read[2] << 8) | read[3]) * sixteenToEight);
                                        write[2] = (byte)(((read[4] << 8) | read[5]) * sixteenToEight);
                                        write[3] =
                                            read[0] == Transparency[0] && read[1] == Transparency[1] &&
                                            read[2] == Transparency[2] && read[3] == Transparency[3] &&
                                            read[4] == Transparency[4] && read[5] == Transparency[5]
                                            ? (byte)0 : (byte)255;

                                        write += 4;
                                        read += 6;
                                    }
                                }
                                
                                break;
                            case BitDepth.One:
                            case BitDepth.Two:
                            case BitDepth.Four:
                                throw new NotSupportedException();
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case ColorType.PaletteColor:
                    fixed (byte* source = Data)
                    {
                        fixed (PaletteEntry* palette = Palette.Entries)
                        {
                            byte* read = source;
                            switch (BitDepth)
                            {
                                case BitDepth.One:
                                    if (Transparency == null)
                                    {
                                        for (int i = 0; i < pixelCount; i += 8)
                                        {
                                            for (int j = 7; j >= 0; j--)
                                            {
                                                byte index = (byte)((*read) >> j);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = 255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < pixelCount; i += 8)
                                        {
                                            for (int j = 7; j >= 0; j--)
                                            {
                                                byte index = (byte)((*read) >> j);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = index < Transparency.Length ? Transparency[index] : (byte)255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    break;
                                case BitDepth.Two:
                                    if (Transparency == null)
                                    {
                                        for (int i = 0; i < pixelCount; i += 4)
                                        {
                                            for (int j = 7; j >= 0; j -= 2)
                                            {
                                                byte index = (byte)(((*read) >> j) & 0x3);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = 255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < pixelCount; i += 4)
                                        {
                                            for (int j = 7; j >= 0; j -= 2)
                                            {
                                                byte index = (byte)(((*read) >> j) & 0x3);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = index < Transparency.Length ? Transparency[index] : (byte)255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    break;
                                case BitDepth.Four:
                                    if (Transparency == null)
                                    {
                                        for (int i = 0; i < pixelCount; i += 2)
                                        {
                                            for (int j = 4; j >= 0; j -= 4)
                                            {
                                                byte index = (byte)(((*read) >> j) & 0xf);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = 255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < pixelCount; i += 2)
                                        {
                                            for (int j = 4; j >= 0; j -= 4)
                                            {
                                                byte index = (byte)(((*read) >> j) & 0xf);
                                                write[0] = palette[index].Red;
                                                write[1] = palette[index].Green;
                                                write[2] = palette[index].Blue;
                                                write[3] = index < Transparency.Length ? Transparency[index] : (byte)255;
                                                write += 4;
                                            }
                                            read++;
                                        }
                                    }
                                    break;
                                case BitDepth.Eight:
                                    if (Transparency == null)
                                    {
                                        for (int i = 0; i < pixelCount; i++)
                                        {
                                            byte index = *read;
                                            write[0] = palette[index].Red;
                                            write[1] = palette[index].Green;
                                            write[2] = palette[index].Blue;
                                            write[3] = 255;

                                            write += 4;
                                            read++;
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < pixelCount; i++)
                                        {
                                            byte index = *read;
                                            write[0] = palette[index].Red;
                                            write[1] = palette[index].Green;
                                            write[2] = palette[index].Blue;
                                            write[3] = index < Transparency.Length ? Transparency[index] : (byte)255;

                                            write += 4;
                                            read++;
                                        }
                                    }
                                    break;
                                case BitDepth.Sixteen:
                                    throw new NotSupportedException();
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                    break;
                case ColorType.GrayscaleAlpha:
                    fixed (byte* source = Data)
                    {
                        byte* read = source;
                        switch (BitDepth)
                        {
                            case BitDepth.Eight:
                                for (int i = 0; i < pixelCount; i++)
                                {
                                    write[0] = write[1] = write[2] = read[0];
                                    write[3] = read[1];

                                    write += 4;
                                    read += 2;
                                }
                                break;
                            case BitDepth.Sixteen:
                                for (int i = 0; i < pixelCount; i++)
                                {
                                    write[0] = write[1] = write[2] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                    write[3] = (byte)(((read[2] << 8) | read[3]) * sixteenToEight);

                                    write += 4;
                                    read += 4;
                                }
                                break;
                            case BitDepth.One:
                            case BitDepth.Two:
                            case BitDepth.Four:
                                throw new NotSupportedException();
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    break;
                case ColorType.TrueColorAlpha:
                    switch (BitDepth)
                    {
                        case BitDepth.Eight:
                            Marshal.Copy(Data, 0, (IntPtr)dest, pixelCount);
                            break;
                        case BitDepth.Sixteen:
                            fixed (byte* source = Data)
                            {
                                byte* read = source;
                                for (int i = 0; i < pixelCount; i++)
                                {
                                    write[0] = (byte)(((read[0] << 8) | read[1]) * sixteenToEight);
                                    write[1] = (byte)(((read[2] << 8) | read[3]) * sixteenToEight);
                                    write[2] = (byte)(((read[4] << 8) | read[5]) * sixteenToEight);
                                    write[3] = (byte)(((read[6] << 8) | read[7]) * sixteenToEight);

                                    write += 4;
                                    read += 8;
                                }
                            }
                            break;
                        case BitDepth.One:
                        case BitDepth.Two:
                        case BitDepth.Four:
                            throw new NotSupportedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (InterlaceMethod == InterlaceMethod.Adam7)
            {
                #region Deinterlace
                int* interlaced = (int*)handle.AddrOfPinnedObject();
                int* regular = (int*) dest;

                int width = Width;
                int height = Height;

                for (int pass = 1; pass <= 7; pass++)
                {
                    int xInit;
                    int yInit;
                    int xAdd;
                    int yAdd;

                    switch (pass)
                    {
                        case 1:
                            xInit = 0;
                            yInit = 0;
                            xAdd = 8;
                            yAdd = 8 * width;
                            break;
                        case 2:
                            xInit = 4;
                            yInit = 0;
                            xAdd = 8;
                            yAdd = 8 * width;
                            break;
                        case 3:
                            xInit = 0;
                            yInit = 4;
                            xAdd = 4;
                            yAdd = 8 * width;
                            break;
                        case 4:
                            xInit = 2;
                            yInit = 0;
                            xAdd = 4;
                            yAdd = 4 * width;
                            break;
                        case 5:
                            xInit = 0;
                            yInit = 2;
                            xAdd = 2;
                            yAdd = 4 * width;
                            break;
                        case 6:
                            xInit = 1;
                            yInit = 0;
                            xAdd = 2;
                            yAdd = 2 * width;
                            break;
                        case 7:
                            xInit = 0;
                            yInit = 1;
                            xAdd = 1;
                            yAdd = 2 * width;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    int xOffset = xInit;
                    int yOffset = yInit;

                    while (yOffset < pixelCount)
                    {
                        if (xOffset >= width)
                        {
                            xOffset = xInit;
                            yOffset += yAdd;
                            continue;
                        }

                        regular[yOffset + xOffset] = *interlaced;
                        xOffset += xAdd;
                        interlaced++;
                    }
                }

                handle.Free();
                #endregion
            }
        }
        #endregion

        #region Decode
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
                    // todo: check ZLib header

                    uint destLen = (uint)Data.Length;
                    uint sourceLen = (uint) totalCompressedDataLength - 6;

                    var puffResult = puff.DoPuff(pData, &destLen, compressedDataParts, &sourceLen);
                    if (puffResult != 0)
                        throw new InvalidDataException(string.Format("Decompressing the image data failed with the code {0}", puffResult));

                    if (destLen != Data.Length)
                        throw new InvalidDataException(string.Format("Expected decompressed data size was {0}, but {1} were recieved", Data.Length, destLen));
                    if (sourceLen != totalCompressedDataLength - 6)
                        throw new InvalidDataException(string.Format("Expected compressed data size was {0}, but was actually {1}", totalCompressedDataLength, sourceLen));

                    // todo: check ZLib crc
                }
            }

            fixed (byte* pData = Data)
            {
                int numPasses = InterlaceMethod == InterlaceMethod.Adam7 ? 7 : 1;

                for (int pass = 1; pass <= numPasses; pass++)
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
        #endregion
    }
}
