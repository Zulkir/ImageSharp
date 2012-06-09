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

namespace ImageSharp.BMP
{
    public class BmpImage
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public BPP BitsPerPixel { get; private set; }
        public int RowPitch { get; private set; }
        public byte[] Data { get; private set; }

        public BmpImage(int width, int height, BPP bitsPerPixel)
        {
            Width = width;
            Height = height;
            BitsPerPixel = bitsPerPixel;
            RowPitch = (width * (int)bitsPerPixel / 32) * 4;
            Data = new byte[RowPitch * height * (int)bitsPerPixel];
        }

        public unsafe void SaveToStream(Stream stream)
        {
            byte[] headersData = new byte[54];
            fixed (byte* pHeaders = headersData)
            {
                var pFileHeader = (BmpFileHeader*) pHeaders;
                pFileHeader->HeaderField = Constants.BM;
                pFileHeader->SizeInBytes = 54 + Data.Length;
                pFileHeader->PixelArrayOffset = 54;

                var pDipHeader = (DibHeader*) pHeaders + 40;
                pDipHeader->Width = Width;
                pDipHeader->Height = Height;
                pDipHeader->NumPlanes = 1;
                pDipHeader->BitsPerPixel = BitsPerPixel;
                pDipHeader->Compression = Compression.Rgb;
                pDipHeader->ImageSize = Data.Length;
                pDipHeader->PixelsPerMeterHorizontal = 2835;
                pDipHeader->PixelsPerMeterVertical = 2835;
            }
            stream.Write(headersData, 0, 54);
            stream.Write(Data, 0, Data.Length);
        }

        public void SaveToFile(string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                SaveToStream(stream);
            }
        }
    }
}
