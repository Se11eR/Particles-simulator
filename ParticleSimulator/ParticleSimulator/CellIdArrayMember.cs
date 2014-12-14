using System;
using Microsoft.Xna.Framework;

namespace ParticleSimulator
{
    internal struct CellIdArrayMember
    {
        private static readonly CellIdArrayMember __NullMember = new CellIdArrayMember(true);

        public readonly byte IsHCell;
        public readonly uint CellHash;
        public readonly uint ObjectIndex;
        public readonly byte Type;

        public CellIdArrayMember(bool isHCell, uint cellHash, int objectIndex, sbyte type, Vector2 coordsDebug)
        {
            IsHCell = (byte)(isHCell ? 1 : 0);
            CellHash = cellHash;
            ObjectIndex = (uint)objectIndex;
            Type = (byte)type;
        }

        private CellIdArrayMember(bool dummy)
        {
            IsHCell = Byte.MaxValue;
            CellHash = UInt32.MaxValue;
            ObjectIndex = UInt32.MaxValue;
            Type = Byte.MaxValue;
        }

        public static CellIdArrayMember GetNull()
        {
            return __NullMember;
        }

        public bool IsNull
        {
            get
            {
                return IsHCell == Byte.MaxValue && CellHash == UInt32.MaxValue
                       && ObjectIndex == UInt32.MaxValue && Type == Byte.MaxValue;
            }
        }

        public override string ToString()
        {
            if (!Equals(CellIdArrayMember.__NullMember))
                return String.Format("CH: {0}, Obj: {1}, T: {2}, H: {3}",
                                     CellHash,
                                     ObjectIndex,
                                     Type,
                                     IsHCell);
            else
                return "NULL";
        }
    }
}