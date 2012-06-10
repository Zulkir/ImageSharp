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

namespace ImageSharp.PNG
{
    public static class Helper
    {
        public const ulong PngSignature = (137ul  | 80ul << 8 | 78ul << 16 | 71ul << 24 | 13ul << 32 | 10ul << 40 | 26ul << 48 | 10ul << 56);
        public const uint ChunkOverheadLength = 12;
        public const uint IHDR = ((uint)'I' | (uint)'H' << 8 | (uint)'D' << 16 | (uint)'R' << 24);
        public const uint PLTE = ((uint)'P' | (uint)'L' << 8 | (uint)'T' << 16 | (uint)'E' << 24);
        public const uint IDAT = ((uint)'I' | (uint)'D' << 8 | (uint)'A' << 16 | (uint)'T' << 24);
        public const uint IEND = ((uint)'I' | (uint)'E' << 8 | (uint)'N' << 16 | (uint)'D' << 24);
        public const uint tRNS = ((uint)'t' | (uint)'R' << 8 | (uint)'N' << 16 | (uint)'S' << 24);
    }
}