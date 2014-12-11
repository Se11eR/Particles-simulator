using System;
using Microsoft.Xna.Framework;

namespace ParticleSimulator
{
    internal struct CellIdArrayMember
    {
        public static readonly CellIdArrayMember NULL_MEMBER = new CellIdArrayMember(true);

        public readonly byte IsHCell;
        public readonly uint CellHash;
        public readonly uint ObjectIndex;
        public readonly byte Type;
        public readonly Vector2 Coords_debug;

        public CellIdArrayMember(bool isHCell, uint cellHash, int objectIndex, sbyte type, Vector2 coordsDebug)
        {
            IsHCell = (byte)(isHCell ? 1 : 0);
            CellHash = cellHash;
            Coords_debug = coordsDebug;
            ObjectIndex = (uint)objectIndex;
            Type = (byte)type;
        }

        private CellIdArrayMember(bool dummy)
        {
            Coords_debug = Vector2.Zero;
            IsHCell = Byte.MaxValue;
            CellHash = UInt32.MaxValue;
            ObjectIndex = UInt32.MaxValue;
            Type = Byte.MaxValue;
        }

        public override string ToString()
        {
            if (!Equals(CellIdArrayMember.NULL_MEMBER))
                return String.Format("Coords: {4}, CH: {0}, Obj: {1}, T: {2}, H: {3}",
                    CellHash,
                    ObjectIndex,
                    Type,
                    IsHCell,
                    Coords_debug);
            else
                return "NULL";
        }
    }
}