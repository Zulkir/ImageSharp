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

namespace ImageSharp.PNG
{
    public static class Helper
    {
        public static int BytesPerPixelCeil(ColorType colorType, BitDepth bitDepth)
        {
            int bitsPerPixel;
            switch (colorType)
            {
                case ColorType.Grayscale: bitsPerPixel = (int)bitDepth; break;
                case ColorType.TrueColor: bitsPerPixel = 3 * (int)bitDepth; break;
                case ColorType.PaletteColor: bitsPerPixel = (int)bitDepth; break;
                case ColorType.GrayscaleAlpha: bitsPerPixel = 2 * (int)bitDepth; break;
                case ColorType.TrueColorAlpha: bitsPerPixel = 4 * (int)bitDepth; break;
                default: throw new ArgumentOutOfRangeException("colorType");
            }
            return bitsPerPixel % 8 == 0 ? bitsPerPixel / 8 : bitsPerPixel / 8 + 1;
        }

        public static int SizeOfImageRow(int width, ColorType colorType, BitDepth bitDepth)
        {
            switch (colorType)
            {
                case ColorType.Grayscale: return (width * (int)bitDepth) / 8;
                case ColorType.TrueColor: return (width * 3 * (int)bitDepth) / 8;
                case ColorType.PaletteColor: return (width * (int)bitDepth) / 8;
                case ColorType.GrayscaleAlpha: return (width * 2 * (int)bitDepth) / 8;
                case ColorType.TrueColorAlpha: return (width * 4 * (int)bitDepth) / 8;
                default: throw new ArgumentOutOfRangeException("colorType");
            }
        }

        public static int SizeOfImageData(int width, int height, ColorType colorType, BitDepth bitDepth)
        {
            return SizeOfImageRow(width, colorType, bitDepth) * height;
        }

        public static int SizeOfFilteredImageData(int width, int height, ColorType colorType, BitDepth bitDepth)
        {
            return SizeOfImageData(width, height, colorType, bitDepth) + height;
        }

        public static int InterlacedPassWidth(int pass, int baseWidth)
        {
            switch (pass)
            {
                case 1: return (baseWidth % 8) == 0 ? baseWidth / 8 : baseWidth / 8 + 1;
                case 2: return (baseWidth % 8) == 0 ? baseWidth / 8 : baseWidth / 8 + 1;
                case 3: return (baseWidth % 4) == 0 ? baseWidth / 4 : baseWidth / 4 + 1;
                case 4: return (baseWidth % 4) != 3 ? baseWidth / 4 : baseWidth / 4 + 1;
                case 5: return (baseWidth % 2) == 0 ? baseWidth / 2 : baseWidth / 2 + 1;
                case 6: return baseWidth / 2;
                case 7: return baseWidth;
                default: throw new ArgumentOutOfRangeException("pass");
            }
        }

        public static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            return pa <= pb && pa <= pc
                       ? a
                       : pb <= pc ? b : c;
        }

        public const ulong PngSignature = (137ul  | 80ul << 8 | 78ul << 16 | 71ul << 24 | 13ul << 32 | 10ul << 40 | 26ul << 48 | 10ul << 56);
        public const uint ChunkOverheadLength = 12;
        public const uint IHDR = ((uint)'I' | (uint)'H' << 8 | (uint)'D' << 16 | (uint)'R' << 24);
        public const uint PLTE = ((uint)'P' | (uint)'L' << 8 | (uint)'T' << 16 | (uint)'E' << 24);
        public const uint IDAT = ((uint)'I' | (uint)'D' << 8 | (uint)'A' << 16 | (uint)'T' << 24);
        public const uint IEND = ((uint)'I' | (uint)'E' << 8 | (uint)'N' << 16 | (uint)'D' << 24);
        public const uint tRNS = ((uint)'t' | (uint)'R' << 8 | (uint)'N' << 16 | (uint)'S' << 24);
    }
}
