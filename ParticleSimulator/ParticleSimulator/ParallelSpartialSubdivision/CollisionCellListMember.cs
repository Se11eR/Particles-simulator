namespace ParticleSimulator.ParallelSpartialSubdivision
{
    internal struct CollisionCellListMember
    {
        public readonly int CellArrayInd;
        public readonly int HCount;
        public readonly int Pcount;

        public CollisionCellListMember(int cellArrayInd, int hCount, int pcount)
            : this()
        {
            CellArrayInd = cellArrayInd;
            HCount = hCount;
            Pcount = pcount;
        }
    }
}