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
using System.IO;
using System.Runtime.InteropServices;

namespace ImageSharp.DDS
{
    public class DdsTexture
    {
        public DxgiFormat DxgiFormat { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public D3DFormat D3DFormat { get; private set; }
        public ResourceDimension Dimension { get; private set; }
        public ResourceMiscFlags MiscFlags { get; private set; }
        public byte[][] Data { get; private set; }
        public MipInfo[] MipInfos { get; private set; }

        public DdsTexture(DxgiFormat dxgiFormat,
            int width, int height = 1, int depth = 1, 
            int arraySize = 1, int mipCount = 0,
            ResourceDimension dimension = ResourceDimension.Unknown, 
            ResourceMiscFlags miscFlags = 0
            )
        {
            if (dxgiFormat == DxgiFormat.UNKNOWN)
                throw new ArgumentException("Formats can not be 'unknown'");
            if (width <= 0)
                throw new ArgumentException("Width must be greater than zero");
            if (height <= 0)
                throw new ArgumentException("Height must be greater than zero");
            if (depth <= 0)
                throw new ArgumentException("Depth must be greater than zero");
            if (arraySize <= 0)
                throw new ArgumentException("Array size must be greater than zero");
            if (mipCount < 0)
                throw new ArgumentException("Mip count must can not be negative");
            if (dimension != ResourceDimension.Unknown && dimension != ResourceDimension.Texture3D && depth != 1)
                throw new ArgumentException(string.Format("Dimension is {0}, but Depth is not 1", dimension));
            if ((dimension == ResourceDimension.Buffer || dimension == ResourceDimension.Texture1D) && height != 1)
                throw new ArgumentException(string.Format("Dimension is {0}, but Height is not 1", dimension));
            if (dimension == ResourceDimension.Buffer && mipCount > 1)
                throw new ArgumentException("Dimension is Buffer, but MipCount is not 0 or 1");

            Width = width;
            Height = height;
            Depth = depth;

            if (depth != 1)
                Dimension = ResourceDimension.Texture3D;
            else if (height != 1)
                Dimension = ResourceDimension.Texture2D;
            else
                Dimension = ResourceDimension.Texture1D;

            MiscFlags = miscFlags;

            D3DFormat = Helper.D3DFormatFromDxgi(dxgiFormat);

            Data = new byte[arraySize][];

            if (mipCount == 0)
            {
                int mipWidth = width;
                int mipHeight = height;
                int mipDepth = depth;

                mipCount = 1;
                while (mipWidth != 1 || mipHeight != 1 || mipDepth != 1)
                {
                    if (mipWidth != 1) mipWidth /= 2;
                    if (mipHeight != 1) mipHeight /= 2;
                    if (mipDepth != 1) mipDepth /= 2;
                    mipCount++;
                }
            }

            int chainSize;
            CalculateMipInfos(mipCount, -1, -1, out chainSize);

            for (int i = 0; i < Data.Length; i++)
                Data[i] = new byte[chainSize];
        }

        public unsafe DdsTexture(byte[] fileData, int byteOffset = 0)
        {
            fixed (byte* pFileData = fileData)
            {
                byte* p = pFileData + byteOffset;
                int remaining = fileData.Length - byteOffset;

                if (remaining < 128)
                    throw new InvalidDataException("File data ends abruptly");
                remaining -= 128;

                if (*(uint*)p != Helper.Magic)
                    throw new InvalidDataException("'DDS ' magic number is missing or incorect");
                p += 4;

                var pHeader = (Header*)p;
                p += Header.StructLength;

                if (pHeader->StructSize != Header.StructLength)
                    throw new InvalidDataException("Header structure size must be 124 bytes");

                if ((pHeader->Flags & 
                    (HeaderFlags.Caps | HeaderFlags.Width | HeaderFlags.Height | HeaderFlags.PixelFormat)) != 
                    (HeaderFlags.Caps | HeaderFlags.Width | HeaderFlags.Height | HeaderFlags.PixelFormat))
                    throw new InvalidDataException("One of the required header flags is missing");

                if (!pHeader->Caps.HasFlag(Caps.Texture))
                    throw new InvalidDataException("Required Caps.Texture flag is missing");
                
                if (pHeader->Flags.HasFlag(HeaderFlags.MipMapCount) ^ pHeader->Caps.HasFlag(Caps.MipMap))
                    throw new InvalidDataException("Flags 'HeaderFlags.MipMapCount' and 'Caps.MipMap' must be present or not at the same time");
                
                Width = (int)pHeader->Width;
                Height = (int)pHeader->Height;
                Depth = pHeader->Flags.HasFlag(HeaderFlags.Depth) ? (int)pHeader->Depth : 1;

                int chainSize;
                bool dataAlreadyCopied = false;

                if (pHeader->PixelFormat.FourCC != Helper.DX10)
                {
                    Dimension = pHeader->Caps.HasFlag(Caps.Complex) && pHeader->Caps2.HasFlag(Caps2.Volume)
                        ? ResourceDimension.Texture3D
                        : Height == 1
                                ? ResourceDimension.Texture1D
                                : ResourceDimension.Texture2D;

                    DxgiFormat dxgiFormat;
                    D3DFormat d3dFormat;
                    Helper.DetermineFormat(ref pHeader->PixelFormat, out dxgiFormat, out d3dFormat);
                    DxgiFormat = dxgiFormat;
                    D3DFormat = d3dFormat;

                    CalculateMipInfos(
                        pHeader->Flags.HasFlag(HeaderFlags.MipMapCount) ? (int)pHeader->MipMapCount : -1,
                        pHeader->Flags.HasFlag(HeaderFlags.Pitch) ? (int)pHeader->LinearSize : -1,
                        pHeader->Flags.HasFlag(HeaderFlags.LinearSize) ? (int)pHeader->LinearSize : -1,
                        out chainSize);

                    if (!(pHeader->Caps.HasFlag(Caps.Complex) && pHeader->Caps2.HasFlag(Caps2.CubeMap)))
                    {
                        Data = new byte[1][];
                    }
                    else
                    {
                        MiscFlags |= ResourceMiscFlags.TextureCube;

                        Data = new byte[6][];

                        for (int i = 0; i < 6; i++)
                        {
                            Data[i] = new byte[chainSize];

                            if (pHeader->Caps2.HasFlag(Helper.CubeMapFaces[i]))
                            {
                                if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                                remaining -= chainSize;
                                Marshal.Copy((IntPtr)p, Data[i], 0, chainSize);
                                p += chainSize;
                            }
                        }

                        dataAlreadyCopied = true;
                    }
                }
                else
                {
                    if (remaining < HeaderDx10.StructLength)
                        throw new InvalidDataException("File data ends abruptly");
                    remaining -= HeaderDx10.StructLength;

                    var pHeaderDx10 = (HeaderDx10*)p;
                    p += HeaderDx10.StructLength;

                    Dimension = pHeaderDx10->ResourceDimension;
                    MiscFlags = pHeaderDx10->MiscFlags;

                    DxgiFormat = pHeaderDx10->Format;
                    D3DFormat = Helper.D3DFormatFromDxgi(DxgiFormat);

                    CalculateMipInfos(
                        pHeader->Flags.HasFlag(HeaderFlags.MipMapCount) ? (int)pHeader->MipMapCount : -1,
                        pHeader->Flags.HasFlag(HeaderFlags.Pitch) ? (int)pHeader->LinearSize : -1,
                        pHeader->Flags.HasFlag(HeaderFlags.LinearSize) ? (int)pHeader->LinearSize : -1,
                        out chainSize);

                    Data = new byte[pHeaderDx10->ArraySize][];
                }

                if (!dataAlreadyCopied)
                {
                    for (int i = 0; i < Data.Length; i++)
                    {
                        Data[i] = new byte[chainSize];

                        if (remaining < chainSize) 
                            throw new InvalidDataException("File data ends abruptly");
                        remaining -= chainSize;

                        Marshal.Copy((IntPtr)p, Data[i], 0, chainSize);
                        p += chainSize;
                    }
                }
            }
        }

        void CalculateMipInfos(int mipCount, int pitch, int linearSize, out int chainSize)
        {
            if (Helper.IsFormatCompressed(DxgiFormat, D3DFormat))
                CalculateMipInfosCompressed(mipCount, linearSize, out chainSize);
            else
                CalculateMipInfosNoncompressed(mipCount, pitch, out chainSize);
        }

        unsafe void CalculateMipInfosNoncompressed(int mipCount, int pitch, out int chainSize)
        {
            if (mipCount == -1)
                mipCount = 1;

            int bytesPerPixel = Helper.FormatBits(DxgiFormat, D3DFormat) / 8;

            if (pitch == -1)
            {
                pitch = Width * bytesPerPixel;
                if ((pitch & 0x3) != 0)
                    pitch += 4 - (pitch & 0x3);
            }

            MipInfos = new MipInfo[mipCount];
            fixed (MipInfo* infos = MipInfos)
            {
                infos[0] = new MipInfo
                {
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                    OffsetInBytes = 0,
                    SizeInBytes = Height * pitch
                };

                for (int i = 1; i < mipCount; i++)
                {
                    infos[i] = new MipInfo
                    {
                        Width = Math.Max(1, infos[i - 1].Width / 2),
                        Height = Math.Max(1, infos[i - 1].Height / 2),
                        Depth = Math.Max(1, infos[i - 1].Depth / 2),
                        OffsetInBytes = infos[i - 1].OffsetInBytes + infos[i - 1].SizeInBytes,
                    };

                    int mipPitch = infos[i].Width * bytesPerPixel;
                    if ((mipPitch & 0x3) != 0)
                        mipPitch += 4 - (mipPitch & 0x3);

                    infos[i].SizeInBytes = mipPitch * infos[i].Height * infos[i].Depth;
                }

                chainSize = infos[mipCount - 1].OffsetInBytes + infos[mipCount - 1].SizeInBytes;
            }
        }

        unsafe void CalculateMipInfosCompressed(int mipCount, int linearSize, out int chainSize)
        {
            if (mipCount == -1)
                mipCount = 1;

            int multiplyer =
                        DxgiFormat == DxgiFormat.BC1_TYPELESS || DxgiFormat == DxgiFormat.BC1_UNORM ||
                        DxgiFormat == DxgiFormat.BC1_UNORM_SRGB || D3DFormat == D3DFormat.Dxt1
                            ? 8
                            : 16;

            if (linearSize == -1)
                linearSize = Math.Max(1, Width / 4) * Math.Max(1, Height / 4) * multiplyer;

            MipInfos = new MipInfo[mipCount];
            fixed (MipInfo* infos = MipInfos)
            {
                infos[0] = new MipInfo
                {
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                    OffsetInBytes = 0,
                    SizeInBytes = linearSize
                };

                for (int i = 1; i < mipCount; i++)
                {
                    infos[i] = new MipInfo
                    {
                        Width = Math.Max(1, infos[i - 1].Width / 2),
                        Height = Math.Max(1, infos[i - 1].Height / 2),
                        Depth = Math.Max(1, infos[i - 1].Depth / 2),
                        OffsetInBytes = infos[i - 1].OffsetInBytes + infos[i - 1].SizeInBytes,
                    };

                    infos[i].SizeInBytes =
                        Math.Max(1, infos[i].Width / 4) * Math.Max(1, infos[i].Height / 4)
                        * infos[i].Depth * multiplyer;
                }

                chainSize = infos[mipCount - 1].OffsetInBytes + infos[mipCount - 1].SizeInBytes;
            }
        }
    }
}
