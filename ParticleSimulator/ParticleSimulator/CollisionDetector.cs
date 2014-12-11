using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace BrownianMotion
{
    internal class CollisionDetector
    {
        private struct CellIdArrayMember
        {
            public static readonly CellIdArrayMember NULL_MEMBER = new CellIdArrayMember(true);

            public readonly byte IsHCell;
            public readonly uint CellHash;
            public readonly uint ObjectIndex;
            public readonly byte Type;
            public readonly Vector2 Coords;

            public CellIdArrayMember(bool isHCell, uint cellHash, int objectIndex, sbyte type, Vector2 coords)
            {
                IsHCell = (byte)(isHCell ? 1 : 0);
                CellHash = cellHash;
                Coords = coords;
                ObjectIndex = (uint)objectIndex;
                Type = (byte)type;
            }

            private CellIdArrayMember(bool dummy)
            {
                Coords = Vector2.Zero;
                IsHCell = byte.MaxValue;
                CellHash = uint.MaxValue;
                ObjectIndex = uint.MaxValue;
                Type = byte.MaxValue;
            }

            public override string ToString()
            {
                if (!Equals(CellIdArrayMember.NULL_MEMBER))
                    return String.Format("Coords: {4}, CH: {0}, Obj: {1}, T: {2}, H: {3}",
                                         CellHash,
                                         ObjectIndex,
                                         Type,
                                         IsHCell,
                                         Coords);
                else
                    return "NULL";
            }
        }

        private class ObjectIdArrayMember
        {
            public static readonly ObjectIdArrayMember NULL_MEMBER = new ObjectIdArrayMember(true);

            private byte __Bits;

            private ObjectIdArrayMember(bool invallid)
            {
                __Bits = byte.MaxValue;
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

        private struct CollisionCellListMember
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

        private readonly int __ParticlesCount;

        private readonly CellIdArrayMember[] __CellIdArray;
        private readonly ObjectIdArrayMember[] __ObjectIdArray;
        private int __TotalCellIDs;
        public float CellSize;

        public CollisionDetector(int particlesCount)
        {
            __ParticlesCount = particlesCount;
            __CellIdArray = new CellIdArrayMember[__ParticlesCount * 4];
            __ObjectIdArray = new ObjectIdArrayMember[__ParticlesCount * 4];
        }

        public void PerformTest(List<Particle> particles, float largestR, Action<Particle, Particle> testAction)
        {
            for (int i = 0; i < __CellIdArray.Length; i++)
            {
                __CellIdArray[i] = CellIdArrayMember.NULL_MEMBER;
            }
            for (int i = 0; i < __ObjectIdArray.Length; i++)
            {
                __ObjectIdArray[i] = ObjectIdArrayMember.NULL_MEMBER;
            }

            CellSize = 1.5f * largestR * Constants.SQRT2 * 2;

            for (int i = 0; i < __ParticlesCount; i++)
            {
                InitAction(particles, CellSize)(i);
            }
            //Parallel.For(0,
            //             __ParticlesCount,
            //             InitAction(particles, cellSize));

            __TotalCellIDs = 0;
            Parallel.ForEach(__CellIdArray,
                             m =>
                             {
                                 if (!m.Equals(CellIdArrayMember.NULL_MEMBER))
                                     Interlocked.Increment(ref __TotalCellIDs);
                             });

            CpuRadixSort(__CellIdArray);

            //var collisionCellList = new ConcurrentQueue<CollisionCellListMember>();
            
            
            //var blockSize = __CellIdArray.Length / Environment.ProcessorCount;
            //var tasks = new Task[Environment.ProcessorCount];
            //var taskCounter = 0;
            //for (int i = 0; i < __CellIdArray.Length; i += blockSize)
            //{
            //    if (i > __CellIdArray.Length - blockSize)
            //        blockSize = __CellIdArray.Length - i;

            //    int i1 = i;
            //    int size = blockSize;
            //    tasks[taskCounter] = Task.Factory.StartNew(
            //                          () =>
            //                          CreateCollisionCellListParallel(collisionCellList,
            //                                                          __CellIdArray,
            //                                                          i1,
            //                                                          size));
            //    taskCounter++;
            //}
            //Task.WaitAll(tasks);

            var collisionCellList = new List<CollisionCellListMember>();

            CreateCollisionCellListParallel(collisionCellList, __CellIdArray, 0, __TotalCellIDs);

            var list = collisionCellList.ToArray();
            //4 passes
            for (int T = 0; T < 4; T++)
            {
                int t = T;
                foreach (var curCell in list)
                {
                    ProcessCollisionCell(particles, testAction, curCell, t);
                }
                //Parallel.ForEach(list, curCell => ProcessCollisionCell(particles, testAction, curCell, t));
            }
        }

        private void ProcessCollisionCell(List<Particle> particles, Action<Particle, Particle> testAction, CollisionCellListMember curCell, int t)
        {
            if (__CellIdArray[curCell.CellArrayInd].Type == t)
            {
                for (int i = curCell.CellArrayInd; i < curCell.CellArrayInd + curCell.HCount + curCell.Pcount; i++)
                {
                    for (int j = i + 1; j < curCell.CellArrayInd + curCell.HCount + curCell.Pcount; j++)
                    {
                        if (__CellIdArray[i].IsHCell == 0 && __CellIdArray[j].IsHCell == 0)
                            continue;

                        var h1 = __ObjectIdArray[__CellIdArray[i].ObjectIndex].H;
                        var h2 = __ObjectIdArray[__CellIdArray[j].ObjectIndex].H;

                        var and =
                            ObjectIdArrayMember.GetPBitsAnd(__ObjectIdArray[__CellIdArray[i].ObjectIndex],
                                                            __ObjectIdArray[__CellIdArray[j].ObjectIndex]);

                        var p1 = __ObjectIdArray[__CellIdArray[i].ObjectIndex].P;
                        var p2 = __ObjectIdArray[__CellIdArray[j].ObjectIndex].P;

                        if (h1 < t && (((and >> h1) & 1) == 1 || h1 == h2 || ((p2 >> h1) & 1) == 1))
                            continue;
                        if (h2 < t && (((and >> h2) & 1) == 1 || h2 == h1 || ((p1 >> h2) & 1) == 1))
                            continue;

                        testAction(particles[(int)__CellIdArray[i].ObjectIndex], particles[(int)__CellIdArray[j].ObjectIndex]);
                    }
                }
            }
        }

        private Action<int> InitAction(List<Particle> particles, float cellSize)
        {
            return i =>
            {
                var curPart = particles[i];
                curPart.R *= Constants.SQRT2;
                var homeCell = GetCellCoords(cellSize, curPart.Coords);
                var homeCellId = Hash(homeCell);
                __CellIdArray[i * 4] = new CellIdArrayMember(true,
                                                             homeCellId,
                                                             i,
                                                             GetCellType(homeCell), homeCell);
                __ObjectIdArray[i] = new ObjectIdArrayMember((byte)GetCellType(homeCell));

                InitPCells(homeCell, cellSize, curPart, __ObjectIdArray[i], i);
            };
        }

        private void CreateCollisionCellListParallel(ICollection<CollisionCellListMember> collisionList,
                                                   CellIdArrayMember[] cellIdArray,
                                                   int startIndex,
                                                   int blockSize)
        {
            int lastIntendedIndex = startIndex + blockSize;

            int hCounter = 0;
            int pCounter = 0;
            int counter = 0;
            var curIndex = startIndex + 1;
            var lastSequenceStart = -1;
            var isSkipping = false;
            while (true)
            {
                //if (curIndex >= cellIdArray.Length || (curIndex > lastIntendedIndex && lastSequenceStart == -1))
                //    break;

                if (curIndex >= cellIdArray.Length || curIndex > lastIntendedIndex)
                    break;

                if (cellIdArray[curIndex].Equals(CellIdArrayMember.NULL_MEMBER))
                {
                    if (isSkipping)
                    {
                        isSkipping = false;
                        goto CONTINUE;
                    }

                    counter = Reset(collisionList,
                                    counter,
                                    ref lastSequenceStart,
                                    ref hCounter,
                                    ref pCounter);

                    goto CONTINUE;
                }

                if (cellIdArray[curIndex].CellHash == cellIdArray[curIndex - 1].CellHash)
                {
                    if (lastSequenceStart == -1)
                        lastSequenceStart = curIndex - 1;

                    counter++;
                    if (cellIdArray[curIndex].IsHCell > 0)
                    {
                        hCounter++;
                    }
                    else
                        pCounter++;

                    if (curIndex - 1 == lastSequenceStart)
                    {
                        counter++;
                        if (cellIdArray[lastSequenceStart].IsHCell > 0)
                        {
                            hCounter++;
                        }
                        else
                            pCounter++;
                    }
                }
                else
                {
                    if (isSkipping)
                    {
                        isSkipping = false;
                        goto CONTINUE;
                    }

                    counter = Reset(collisionList,
                            counter,
                            ref lastSequenceStart,
                            ref hCounter,
                            ref pCounter);
                }

            CONTINUE:
                curIndex++;
            }
        }

        private static int Reset(ICollection<CollisionCellListMember> collisionList,
                                 int counter,
                                 ref int lastSequenceStart,
                                 ref int hCounter,
                                 ref int pCounter)
        {
            if (counter > 1)
                collisionList.Add(new CollisionCellListMember(lastSequenceStart, hCounter, pCounter));

            lastSequenceStart = -1;
            hCounter = 0;
            pCounter = 0;
            counter = 0;
            return counter;
        }

        private static void CpuRadixSort(CellIdArrayMember[] sourceData)
        {
            var size = sourceData.Length;
            //temporary array for every byte iteration
            var tempData = new CellIdArrayMember[size];
            //histogram of the last byte 
            var histogram = new int[256];
            //The prefix sum of the histogram
            var prefixSum = new int[256];

            unsafe
            {
                fixed (CellIdArrayMember* pTempData = tempData)
                fixed (CellIdArrayMember* pSourceData = sourceData)
                {
                    CellIdArrayMember* pTemp = pTempData;
                    CellIdArrayMember* pBck;
                    CellIdArrayMember* pSource = pSourceData;

                    //Loop through every byte of 4 byte integer
                    for (int byteIdx = 0; byteIdx < 4; byteIdx++)
                    {
                        int shift = byteIdx * 8; //total bits to shift the numbers

                        //Calculate histogram of the last byte of the data
                        for (int i = 0; i < size; i++)
                            histogram[(pSource[i].CellHash >> shift) & 0xFF]++;

                        //Calculate prefix-sum of the histogram
                        prefixSum[0] = 0;
                        for (int i = 1; i < 256; i++)
                            prefixSum[i] = prefixSum[i - 1] + histogram[i - 1];

                        //Get the prefix-sum array index of the last byte, increase 
                        //it by one. That gives us the the index we want to place 
                        //the data
                        for (int i = 0; i < size; i++)
                            pTemp[prefixSum[(pSource[i].CellHash >> shift) & 0xFF]++] = pSource[i];

                        //Swap the pointers
                        pBck = pSource;
                        pSource = pTemp;
                        pTemp = pBck;

                        //reset the histogram
                        for (int i = 0; i < 256; i++)
                            histogram[i] = 0;
                    }
                }
            }
        }
        private void InitPCells(Vector2 homeCell, float cellSize, Particle curPart, ObjectIdArrayMember obj, int i)
        {
            int pCells = 0;
            //left-up
            Vector2 c = homeCell * cellSize;
            if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.R * curPart.R)
            {
                pCells++;
                var coords = homeCell - Vector2.One;
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      Hash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //up
            if (Math.Abs(curPart.Y - c.Y) <= curPart.R)
            {
                pCells++;
                var coords = homeCell - Vector2.UnitY;
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      Hash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //up-right
            c = (homeCell + Vector2.UnitX) * cellSize;
            if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.R * curPart.R)
            {
                pCells++;
                var coords = new Vector2(homeCell.X + 1, homeCell.Y - 1);
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      Hash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //right
            if (pCells < 3)
            {
                if (Math.Abs(curPart.X - c.X) <= curPart.R)
                {
                    pCells++;
                    var coords = homeCell + Vector2.UnitX;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          Hash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //down-right
            if (pCells < 3)
            {
                c = (homeCell + Vector2.One) * cellSize;
                if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.R * curPart.R)
                {
                    pCells++;
                    var coords = homeCell + Vector2.One;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          Hash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }


            //down
            if (pCells < 3)
            {
                if (Math.Abs(curPart.Y - c.Y) <= curPart.R)
                {
                    pCells++;
                    var coords = homeCell + Vector2.UnitY;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          Hash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //down-left
            if (pCells < 3)
            {
                c = (homeCell + Vector2.UnitY) * cellSize;
                if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.R * curPart.R)
                {
                    pCells++;
                    var coords = new Vector2(homeCell.X - 1, homeCell.Y + 1);
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          Hash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //left
            if (pCells < 3)
            {
                if (Math.Abs(curPart.X - c.X) <= curPart.R)
                {
                    pCells++;
                    var coords = homeCell - Vector2.UnitX;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          Hash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }
        }

        private static Vector2 GetCellCoords(float cellSize, Vector2 coords)
        {
            return new Vector2((int)(coords.X / cellSize), (int)(coords.Y / cellSize));
        }

        private uint Hash(Vector2 c)
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (hashCode * 1073741827) ^ (int)c.X;
                hashCode = (hashCode * 1073741827) ^ (int)c.Y;
                return (uint)hashCode;
            }
        }

        private sbyte GetCellType(Vector2 coord)
        {
            var x = (int)coord.X;
            var y = (int)coord.Y;

            if (x % 2 == 0 && y % 2 == 0)
            {
                return 0;
            }
            if (x % 2 == 0 && y % 2 != 0)
            {
                return 3;
            }
            if (x % 2 != 0 && y % 2 != 0)
            {
                return 2;
            }
            if (x % 2 != 0 && y % 2 == 0)
            {
                return 1;
            }

            return -1;
        }
    }
}
