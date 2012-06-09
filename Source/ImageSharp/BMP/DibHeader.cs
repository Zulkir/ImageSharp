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

using System.Runtime.InteropServices;

namespace ImageSharp.BMP
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 40)]
    public struct DibHeader
    {
        public int StructureSize;
        public int Width;
        public int Height;
        public ushort NumPlanes;
        public BPP BitsPerPixel;
        public Compression Compression;
        public int ImageSize;
        public int PixelsPerMeterHorizontal;
        public int PixelsPerMEterVertical;
        public int NumPaletteColors;
        public int NumImportantPaletteColors;

        public const int StructSize = 40;
    }
}
