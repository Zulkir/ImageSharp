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

namespace ImageSharp.DDS
{
    public static class Helper
    {
        public static void DetermineFormat9(ref PixelFormat pixelFormat, out DxgiFormat dxgiFormat, out D3DFormat d3dFormat)
        {
            switch (pixelFormat.Flags)
            {
                case PixelFormatFlags.Rgb | PixelFormatFlags.AlphaPixels:
                    switch (pixelFormat.RgbBitCount)
                    {
                        case 32:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0x000000ff:
                                    dxgiFormat = DxgiFormat.R8G8B8A8_UNORM;
                                    d3dFormat = D3DFormat.A8B8G8R8;
                                    return;
                                case 0x0000ffff:
                                    dxgiFormat = DxgiFormat.R16G16_UNORM;
                                    d3dFormat = D3DFormat.G16R16;
                                    return;
                                case 0x000003ff:
                                    dxgiFormat = DxgiFormat.R10G10B10A2_UNORM;
                                    d3dFormat = D3DFormat.A2B10G10R10;
                                    return;
                                case 0x00ff0000:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A8R8G8B8;
                                    return;
                                case 0x3ff00000:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A2R10G10B10;
                                    return;
                            }
                            break;
                        case 16:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0x7c00:
                                    dxgiFormat = DxgiFormat.B5G5R5A1_UNORM;
                                    d3dFormat = D3DFormat.A1R5G5B5;
                                    return;
                                case 0x0f00:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A4R4G4B4;
                                    return;
                                case 0x00e0:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A8R3G3B2;
                                    return;
                            }
                            break;
                    }
                    break;
                case PixelFormatFlags.Rgb:
                    switch (pixelFormat.RgbBitCount)
                    {
                        case 32:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0x0000ffff:
                                    dxgiFormat = DxgiFormat.R16G16_UNORM;
                                    d3dFormat = D3DFormat.G16R16;
                                    return;
                                case 0x00ff0000:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.X8R8G8B8;
                                    return;
                                case 0x000000ff:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.X8B8G8R8;
                                    return;
                            }
                            break;
                        case 24:
                            if (pixelFormat.RBitMask == 0xff0000)
                            {
                                dxgiFormat = DxgiFormat.UNKNOWN;
                                d3dFormat = D3DFormat.R8G8B8;
                                return;
                            }
                            break;
                        case 16:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0xf800:
                                    dxgiFormat = DxgiFormat.B5G6R5_UNORM;
                                    d3dFormat = D3DFormat.R5G6B5;
                                    return;
                                case 0x7c00:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.X1R5G5B5;
                                    return;
                                case 0x0f00:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.X4R4G4B4;
                                    return;
                            }
                            break;
                    }
                    break;
                case PixelFormatFlags.Alpha:
                    if (pixelFormat.ABitMask == 0xff)
                    {
                        dxgiFormat = DxgiFormat.A8_UNORM;
                        d3dFormat = D3DFormat.A8;
                        return;
                    }
                    break;
                case PixelFormatFlags.Luminance:
                    switch (pixelFormat.RgbBitCount)
                    {
                        case 16:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0x00ff:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A8L8;
                                    return;
                                case 0xffff:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.L16;
                                    return;
                            }
                            break;
                        case 8:
                            switch (pixelFormat.RBitMask)
                            {
                                case 0xff:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.L8;
                                    return;
                                case 0x0f:
                                    dxgiFormat = DxgiFormat.UNKNOWN;
                                    d3dFormat = D3DFormat.A4L4;
                                    return;
                            }
                            break;
                    }
                    break;
                case PixelFormatFlags.FourCC:
                    switch (pixelFormat.FourCC)
                    {
                        case DXT1:
                            dxgiFormat = DxgiFormat.BC1_UNORM;
                            d3dFormat = D3DFormat.Dxt1;
                            return;
                        case DXT3:
                            dxgiFormat = DxgiFormat.BC2_UNORM;
                            d3dFormat = D3DFormat.Dxt3;
                            return;
                        case DXT5:
                            dxgiFormat = DxgiFormat.BC3_UNORM;
                            d3dFormat = D3DFormat.Dxt5;
                            return;
                        case BC4U:
                            dxgiFormat = DxgiFormat.BC4_UNORM;
                            d3dFormat = D3DFormat.Unknown;
                            return;
                        case BC4S:
                            dxgiFormat = DxgiFormat.BC4_SNORM;
                            d3dFormat = D3DFormat.Unknown;
                            return;
                        case ATI2:
                            dxgiFormat = DxgiFormat.BC5_UNORM;
                            d3dFormat = D3DFormat.Unknown;
                            return;
                        case BC5S:
                            dxgiFormat = DxgiFormat.BC5_SNORM;
                            d3dFormat = D3DFormat.Unknown;
                            return;
                        case RGBG:
                            dxgiFormat = DxgiFormat.R8G8_B8G8_UNORM;
                            d3dFormat = D3DFormat.R8G8_B8G8;
                            return;
                        case GRGB:
                            dxgiFormat = DxgiFormat.G8R8_G8B8_UNORM;
                            d3dFormat = D3DFormat.G8R8_G8B8;
                            return;
                        case 36:
                            dxgiFormat = DxgiFormat.R16G16B16A16_UNORM;
                            d3dFormat = D3DFormat.A16B16G16R16;
                            return;
                        case 110:
                            dxgiFormat = DxgiFormat.R16G16B16A16_SNORM;
                            d3dFormat = D3DFormat.Q16W16V16U16;
                            return;
                        case 111:
                            dxgiFormat = DxgiFormat.R16_FLOAT;
                            d3dFormat = D3DFormat.R16F;
                            return;
                        case 112:
                            dxgiFormat = DxgiFormat.R16G16_FLOAT;
                            d3dFormat = D3DFormat.G16R16F;
                            return;
                        case 113:
                            dxgiFormat = DxgiFormat.R16G16B16A16_FLOAT;
                            d3dFormat = D3DFormat.A16B16G16R16F;
                            return;
                        case 114:
                            dxgiFormat = DxgiFormat.R32_FLOAT;
                            d3dFormat = D3DFormat.R32F;
                            return;
                        case 115:
                            dxgiFormat = DxgiFormat.R32G32_FLOAT;
                            d3dFormat = D3DFormat.G32R32F;
                            return;
                        case 116:
                            dxgiFormat = DxgiFormat.R32G32B32A32_FLOAT;
                            d3dFormat = D3DFormat.A32B32G32R32F;
                            return;
                        case DXT2:
                            dxgiFormat = DxgiFormat.UNKNOWN;
                            d3dFormat = D3DFormat.Dxt2;
                            return;
                        case DXT4:
                            dxgiFormat = DxgiFormat.UNKNOWN;
                            d3dFormat = D3DFormat.Dxt4;
                            return;
                        case UYVY:
                            dxgiFormat = DxgiFormat.UNKNOWN;
                            d3dFormat = D3DFormat.Uyvy;
                            return;
                        case YUY2:
                            dxgiFormat = DxgiFormat.UNKNOWN;
                            d3dFormat = D3DFormat.Yuy2;
                            return;
                        case 117:
                            dxgiFormat = DxgiFormat.UNKNOWN;
                            d3dFormat = D3DFormat.CxV8U8;
                            return;
                        case DX10:
                            throw new InvalidOperationException("This methjod is unappropriate for DX10-flagged formats");
                    }
                    break;
                case PixelFormatFlags.Yuv:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            dxgiFormat = DxgiFormat.UNKNOWN;
            d3dFormat = D3DFormat.Unknown;
        }

        public const uint Magic = 0x20534444; // 'DDS '

        public const uint DXT1 = ((uint)'D' | (uint)'X' << 8 | (uint)'T' << 16 | (uint)'1' << 24);
        public const uint DXT3 = ((uint)'D' | (uint)'X' << 8 | (uint)'T' << 16 | (uint)'3' << 24);
        public const uint DXT5 = ((uint)'D' | (uint)'X' << 8 | (uint)'T' << 16 | (uint)'5' << 24);
        public const uint BC4U = ((uint)'B' | (uint)'C' << 8 | (uint)'4' << 16 | (uint)'U' << 24);
        public const uint BC4S = ((uint)'B' | (uint)'C' << 8 | (uint)'4' << 16 | (uint)'S' << 24);
        public const uint ATI2 = ((uint)'A' | (uint)'T' << 8 | (uint)'I' << 16 | (uint)'2' << 24);
        public const uint BC5S = ((uint)'B' | (uint)'C' << 8 | (uint)'5' << 16 | (uint)'S' << 24);
        public const uint RGBG = ((uint)'R' | (uint)'G' << 8 | (uint)'B' << 16 | (uint)'G' << 24);
        public const uint GRGB = ((uint)'G' | (uint)'R' << 8 | (uint)'G' << 16 | (uint)'B' << 24);
        public const uint DXT2 = ((uint)'D' | (uint)'X' << 8 | (uint)'T' << 16 | (uint)'2' << 24);
        public const uint DXT4 = ((uint)'D' | (uint)'X' << 8 | (uint)'T' << 16 | (uint)'4' << 24);
        public const uint UYVY = ((uint)'U' | (uint)'Y' << 8 | (uint)'V' << 16 | (uint)'Y' << 24);
        public const uint YUY2 = ((uint)'Y' | (uint)'U' << 8 | (uint)'Y' << 16 | (uint)'2' << 24);
        public const uint DX10 = ((uint)'D' | (uint)'X' << 8 | (uint)'1' << 16 | (uint)'0' << 24);
    }
}
