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
using System.Text;

namespace ImageSharp.PNG
{
    public struct ChunkType : IEquatable<ChunkType>
    {
        readonly uint value;

        public uint Value { get { return value; } }

        public ChunkType(uint value)
        {
            this.value = value;
        }

        public unsafe ChunkType(char ch0, char ch1, char ch2, char ch3)
        {
            uint temp;
            var p = (sbyte*)&temp;
            p[0] = (sbyte)ch0;
            p[1] = (sbyte)ch1;
            p[2] = (sbyte)ch2;
            p[3] = (sbyte)ch3;
            value = temp;
        }

        public unsafe ChunkType(string text)
        {
            if (text.Length != 4)
                throw new AggregateException("'text' must be exactly four ASCII characters");

            uint temp;
            var p = (sbyte*)&temp;
            p[0] = (sbyte)text[0];
            p[1] = (sbyte)text[1];
            p[2] = (sbyte)text[2];
            p[3] = (sbyte)text[3];
            value = temp;
        }

        public bool IsCritical { get { return (value & 0x00000020) == 0; } }
        public bool IsPublic { get { return (value & 0x00002000) == 0; } }
        public bool IsClassic { get { return (value & 0x00200000) == 0; } }
        public bool IsUnsafeToCopy { get { return (value & 0x20000000) == 0; } }

        #region IEquatable implementation and Object overrides
        public bool Equals(ChunkType other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (obj is ChunkType)
                return Equals((ChunkType) obj);
            return false;
        }

        public override int GetHashCode()
        {
            return (int)value;
        }

        public unsafe override string ToString()
        {
            uint temp = value;
            var p = (sbyte*)&temp;
            return new string(p, 0, 4, Encoding.ASCII);
        }
        #endregion

        static readonly ChunkType _IHDR = new ChunkType("IHDR");
        public static ChunkType IHDR { get { return _IHDR; } }

        static readonly ChunkType _PLTE = new ChunkType("PLTE");
        public static ChunkType PLTE { get { return _PLTE; } }

        static readonly ChunkType _IDAT = new ChunkType("IDAT");
        public static ChunkType IDAT { get { return _IDAT; } }

        static readonly ChunkType _IEND = new ChunkType("IEND");
        public static ChunkType IEND { get { return _IEND; } } 
    }
}
