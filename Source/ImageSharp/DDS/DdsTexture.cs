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
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public DxgiFormat DxgiFormat { get; private set; }
        public D3DFormat D3DFormat { get; private set; }
        public byte[][] Data { get; private set; }
        public MipInfo[] MipInfos { get; private set; }
        public ResourceDimension Dimension { get; private set; }
        public ResourceMiscFlags MiscFlags { get; private set; }

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

                if ((pHeader->Flags & (HeaderFlags.Caps | HeaderFlags.Width | HeaderFlags.Height | HeaderFlags.PixelFormat)) != 0)
                    throw new InvalidDataException("One of the required header flags is missing");

                if (!pHeader->Caps.HasFlag(Caps.Texture))
                    throw new InvalidDataException("Required Caps.Texture flag is missing");
                
                if (pHeader->Flags.HasFlag(HeaderFlags.MipMapCount) ^ pHeader->Caps.HasFlag(Caps.MipMap))
                    throw new InvalidDataException("Flags 'HeaderFlags.MipMapCount' and 'Caps.MipMap' must be present or not at the same time");
                
                Width = (int)pHeader->Width;
                Height = (int)pHeader->Height;
                Depth = pHeader->Flags.HasFlag(HeaderFlags.Depth) ? (int)pHeader->Depth : 1;
                
                int mipCount = pHeader->Caps.HasFlag(HeaderFlags.MipMapCount) ? (int) pHeader->MipMapCount : 1;
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
                    Helper.DetermineFormat9(ref pHeader->PixelFormat, out dxgiFormat, out d3dFormat);
                    DxgiFormat = dxgiFormat;
                    D3DFormat = d3dFormat;

                    CalculateMipInfos(mipCount, (int)pHeader->LinearSize, out chainSize);

                    if (!(pHeader->Caps.HasFlag(Caps.Complex) && pHeader->Caps2.HasFlag(Caps2.CubeMap)))
                    {
                        Data = new byte[1][];
                    }
                    else
                    {
                        MiscFlags |= ResourceMiscFlags.TextureCube;

                        Data = new byte[6][];

                        for (int i = 0; i < 6; i++)
                            Data[i] = new byte[chainSize];

                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapPositiveX))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[0], 0, chainSize);
                            p += chainSize;
                        }
                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapNegativeX))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[1], 0, chainSize);
                            p += chainSize;
                        }
                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapPositiveY))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[2], 0, chainSize);
                            p += chainSize;
                        }
                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapNegativeY))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[3], 0, chainSize);
                            p += chainSize;
                        }
                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapPositiveZ))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[4], 0, chainSize);
                            p += chainSize;
                        }
                        if (pHeader->Caps2.HasFlag(Caps2.CubeMapNegativeZ))
                        {
                            if (remaining < chainSize) throw new InvalidDataException("File data ends abruptly");
                            remaining -= chainSize;
                            Marshal.Copy((IntPtr)p, Data[5], 0, chainSize);
                            p += chainSize;
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

                    CalculateMipInfos(mipCount, (int)pHeader->LinearSize, out chainSize);

                    Data = new byte[pHeaderDx10->ArraySize][];
                }

                if (!dataAlreadyCopied)
                {
                    for (int i = 0; i < Data.Length; i++)
                    {
                        Data[i] = new byte[i];

                        if (remaining < chainSize) 
                            throw new InvalidDataException("File data ends abruptly");
                        remaining -= chainSize;

                        Marshal.Copy((IntPtr)p, Data[5], 0, chainSize);
                        p += chainSize;
                    }
                }
            }
        }

        unsafe void CalculateMipInfos(int mipCount, int linearSize, out int chainSize)
        {
            bool compressed = Helper.IsFormatCompressed(DxgiFormat, D3DFormat);

            MipInfos = new MipInfo[mipCount];
            fixed (MipInfo* infos = MipInfos)
            {
                infos[0] = new MipInfo
                {
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                    OffsetInBytes = 0,
                    SizeInBytes = compressed ? linearSize : Height*linearSize
                };

                if (!compressed)
                {
                    int bytesPerPixel = linearSize/Width;
                    for (int i = 1; i < mipCount; i++)
                    {
                        infos[i] = new MipInfo
                        {
                            Width = Math.Max(1, infos[i - 1].Width/2),
                            Height = Math.Max(1, infos[i - 1].Height/2),
                            Depth = Math.Max(1, infos[i - 1].Depth/2),
                            OffsetInBytes = infos[i - 1].OffsetInBytes + infos[i - 1].SizeInBytes,
                        };

                        int pitch = infos[i].Width*bytesPerPixel;
                        if ((pitch & 0x3) != 0)
                            pitch += 4 - (pitch & 0x3);

                        infos[i].SizeInBytes = pitch*infos[i].Height*infos[i].Depth;
                    }
                }
                else
                {
                    int multiplyer =
                        DxgiFormat == DxgiFormat.BC1_TYPELESS || DxgiFormat == DxgiFormat.BC1_UNORM ||
                        DxgiFormat == DxgiFormat.BC1_UNORM_SRGB || D3DFormat == D3DFormat.Dxt1
                            ? 8
                            : 16;

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
                            Math.Max(1, infos[i].Width / 4) * Math.Max(1, infos[i].Height / 4) * multiplyer;
                    }
                }

                chainSize = infos[mipCount - 1].OffsetInBytes + infos[mipCount - 1].SizeInBytes;
            }
        }
    }
}
