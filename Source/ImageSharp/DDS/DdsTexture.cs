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

namespace ImageSharp.DDS
{
    public class DdsTexture
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public DxgiFormat DxgiFormat { get; private set; }
        public D3DFormat D3DFormat { get; private set; }
        public byte[][] MipChains { get; private set; }
        public MipInfo[] MipInfos { get; private set; }

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

                Width = (int)pHeader->Width;
                Height = (int)pHeader->Height;
                Depth = pHeader->Flags.HasFlag(HeaderFlags.Depth) ? (int)pHeader->Depth : 1;

                
            }
        }
    }
}
