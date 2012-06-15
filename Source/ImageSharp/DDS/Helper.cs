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
        public static void DetermineFormat(ref PixelFormat pixelFormat, out DxgiFormat dxgiFormat, out D3DFormat d3dFormat)
        {
            switch (pixelFormat.Flags)
            {
                case PixelFormatFlags.Rgb | PixelFormatFlags.AlphaPixels:
                    switch (pixelFormat.RgbBitCount)
                    {
                        case 32:

                    }
                    break;
                case PixelFormatFlags.Alpha:
                    break;
                case PixelFormatFlags.FourCC:
                    break;
                case PixelFormatFlags.Rgb:
                    break;
                case PixelFormatFlags.Yuv:
                    break;
                case PixelFormatFlags.Luminance:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
    }
}
