using System;

namespace ParticleSimulator
{
    internal class ObjectIdArrayMember
    {
        public static readonly ObjectIdArrayMember NULL_MEMBER = new ObjectIdArrayMember(true);

        private byte __Bits;

        private ObjectIdArrayMember(bool invallid)
        {
            __Bits = Byte.MaxValue;
        }

        public ObjectIdArrayMember(byte bits)
        {
            __Bits = bits;
        }

        public byte H
        {
            get
            {
                return (byte)(__Bits & 3);
            }
        }

        public byte P
        {
            get
            {
                return (byte)((__Bits >> 2) & 0xF);
            }
        }

        public void SetPBit(int pbit)
        {
            __Bits |= (byte)(1 << (pbit + 2));
        }

        public static int GetPBitsAnd(ObjectIdArrayMember a, ObjectIdArrayMember b)
        {
            return (a.__Bits & b.__Bits) >> 2;
        }

        public override string ToString()
        {
            if (Equals(NULL_MEMBER))
                return "NULL";
            return base.ToString();
        }
    }
}