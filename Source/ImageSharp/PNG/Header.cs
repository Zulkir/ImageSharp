﻿#region License
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

namespace ImageSharp.PNG
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Header
    {
        public uint Length;
        public ChunkType ChunkType;
        public uint Width;
        public uint Height;
        public BitDepth BitDepth;
        public ColorType ColorType;
        public CompressionMethod CompressionMethod;
        public FilterMethod FilterMethod;
        public InterlaceMethod InterlaceMethod;
        public uint Crc;

        public const uint DataLength = 13;
        public const uint StructLength = 25;
    }
}