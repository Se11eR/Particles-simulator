using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using ParticleSimulator.Game;

namespace ParticleSimulator.ParallelSpartialSubdivision
{
    internal class ParallelSpartialSubdivisionCD : IUniformCellCollisionDetector
    {
        private readonly int __ParticlesCount;
        private readonly IBoundingCircleCollisionResolver __Resolver;
        private readonly CellIdArrayMember[] __CellIdArray;
        private readonly ObjectIdArrayMember[] __ObjectIdArray;
        private int __TotalCellIDs;
        private float __CellSize;
        private IBoundingCircle[] __Particles;

        public ParallelSpartialSubdivisionCD(int particlesCount, IBoundingCircleCollisionResolver resolver)
        {
            __ParticlesCount = particlesCount;
            __Resolver = resolver;
            __CellIdArray = new CellIdArrayMember[__ParticlesCount * 4];
            __ObjectIdArray = new ObjectIdArrayMember[__ParticlesCount];
        }

        public float CellSize
        {
            get { return __CellSize; }
            set { __CellSize = value; }
        }


        /// <summary>
        /// Parallel spartial subdivision algorithm as described in 
        /// http://http.developer.nvidia.com/GPUGems3/gpugems3_ch32.html
        /// 
        /// Adapted for CPU usage
        /// </summary>
        /// <param name="particles"></param>
        /// <param name="largestR"></param>
        /// <param name="testAction"></param>
        public void PerformTest(IBoundingCircle[] particles, float largestR)
        {
            //Clear arrays
            ClearArrays();

            //Determine new cell size
            __CellSize = 1.5f * largestR * Constants.SQRT2 * 2;
            __Particles = particles;

            //Initialize CellIDArray and ObjectIDArray
            __TotalCellIDs = 0;
            Parallel.For(0,
                __ParticlesCount,
                InitAction);

            //Sort CellIDArray
            //TODO: GPU sort, or Parallel CPU sort
            CpuRadixSort(__CellIdArray);

            //Create Collision cell list from sorted CellIDArray
            var collisionCellList = new ConcurrentQueue<CollisionCellListMember>();
            var blockSize = __CellIdArray.Length / Environment.ProcessorCount;
            var tasks = new Task[Environment.ProcessorCount];
            var taskCounter = 0;
            for (var i = 0; i < __CellIdArray.Length; i += blockSize)
            {
                if (i > __CellIdArray.Length - blockSize)
                    blockSize = __CellIdArray.Length - i;

                var i1 = i;
                var size = blockSize;
                tasks[taskCounter] = Task.Factory.StartNew(
                                      () =>
                                      CreateCollisionCellListParallel(collisionCellList,
                                                                      __CellIdArray,
                                                                      i1,
                                                                      size));
                taskCounter++;
            }
            Task.WaitAll(tasks);

            //Traverse CollisionCellList and perform narrow fase collision detection
            //4 passed as 2 dimension version
            var list = collisionCellList.ToArray();
            //4 passes
            for (int T = 0; T < 4; T++)
            {
                var t = T;
                Parallel.ForEach(list, curCell => ProcessCollisionCell(curCell, t));
            }
        }

        private void ClearArrays()
        {
            for (var i = 0; i < __CellIdArray.Length; i++)
            {
                __CellIdArray[i] = CellIdArrayMember.GetNull();
            }
            for (var i = 0; i < __ObjectIdArray.Length; i++)
            {
                __ObjectIdArray[i] = ObjectIdArrayMember.GetNull();
            }
        }

        private int InitPCells(Vector2 homeCell, float cellSize, IBoundingCircle curPart, ObjectIdArrayMember obj, int i)
        {
            var pCells = 0;
            //left-up
            var c = homeCell * cellSize;
            if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.ModifiableR * curPart.ModifiableR)
            {
                pCells++;
                var coords = homeCell - Vector2.One;
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      CellCoordsHash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //up
            if (Math.Abs(curPart.Coords.Y - c.Y) <= curPart.ModifiableR)
            {
                pCells++;
                var coords = homeCell - Vector2.UnitY;
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      CellCoordsHash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //up-right
            c = (homeCell + Vector2.UnitX) * cellSize;
            if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.ModifiableR * curPart.ModifiableR)
            {
                pCells++;
                var coords = new Vector2(homeCell.X + 1, homeCell.Y - 1);
                __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                      CellCoordsHash(coords),
                                                                      i,
                                                                      GetCellType(coords), coords);
                obj.SetPBit(GetCellType(coords));
            }

            //right
            if (pCells < 3)
            {
                if (Math.Abs(curPart.Coords.X - c.X) <= curPart.ModifiableR)
                {
                    pCells++;
                    var coords = homeCell + Vector2.UnitX;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          CellCoordsHash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //down-right
            if (pCells < 3)
            {
                c = (homeCell + Vector2.One) * cellSize;
                if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.ModifiableR * curPart.ModifiableR)
                {
                    pCells++;
                    var coords = homeCell + Vector2.One;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          CellCoordsHash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }


            //down
            if (pCells < 3)
            {
                if (Math.Abs(curPart.Coords.Y - c.Y) <= curPart.ModifiableR)
                {
                    pCells++;
                    var coords = homeCell + Vector2.UnitY;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          CellCoordsHash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //down-left
            if (pCells < 3)
            {
                c = (homeCell + Vector2.UnitY) * cellSize;
                if (Vector2.DistanceSquared(curPart.Coords, c) <= curPart.ModifiableR * curPart.ModifiableR)
                {
                    pCells++;
                    var coords = new Vector2(homeCell.X - 1, homeCell.Y + 1);
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          CellCoordsHash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            //left
            if (pCells < 3)
            {
                if (Math.Abs(curPart.Coords.X - c.X) <= curPart.ModifiableR)
                {
                    pCells++;
                    var coords = homeCell - Vector2.UnitX;
                    __CellIdArray[i * 4 + pCells] = new CellIdArrayMember(false,
                                                                          CellCoordsHash(coords),
                                                                          i,
                                                                          GetCellType(coords), coords);
                    obj.SetPBit(GetCellType(coords));
                }
            }

            return pCells;
        }

        private void ProcessCollisionCell(CollisionCellListMember curCell, int t)
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

                        __Resolver.ResolveCollision(__Particles[(int)__CellIdArray[i].ObjectIndex],
                                                    __Particles[(int)__CellIdArray[j].ObjectIndex]);
                    }
                }
            }
        }

        private void InitAction(int i)
        {
            var curPart = __Particles[i];
            curPart.ModifiableR = curPart.R * Constants.SQRT2;
            var homeCell = GetCellCoords(__CellSize, curPart.Coords);
            var homeCellId = CellCoordsHash(homeCell);
            __CellIdArray[i * 4] = new CellIdArrayMember(true,
                                                            homeCellId,
                                                            i,
                                                            GetCellType(homeCell), homeCell);
            __ObjectIdArray[i] = new ObjectIdArrayMember((byte)GetCellType(homeCell));

            var pcells = InitPCells(homeCell, __CellSize, curPart, __ObjectIdArray[i], i);
            Interlocked.Add(ref __TotalCellIDs, pcells + 1);
        }

        private static void CreateCollisionCellListParallel(IProducerConsumerCollection<CollisionCellListMember> collisionList,
                                                   CellIdArrayMember[] cellIdArray,
                                                   int startIndex,
                                                   int blockSize)
        {
            var lastIntendedIndex = startIndex + blockSize;

            var curIndex = startIndex + 1;
            uint h = cellIdArray[startIndex].CellHash;
            //Skiping
            if (startIndex > 0)
            {
                while (curIndex < cellIdArray.Length)
                {
                    if (!cellIdArray[curIndex].IsNull && h == cellIdArray[curIndex].CellHash)
                        curIndex++;
                    else
                        break;
                }
            }

            var hCounter = 0;
            var pCounter = 0;
            var counter = 0;
            var lastSequenceStart = -1;

            while (curIndex < cellIdArray.Length)
            {
                if (cellIdArray[curIndex].IsNull)
                {
                    counter = ResetAdd(collisionList,
                                    counter,
                                    ref lastSequenceStart,
                                    ref hCounter,
                                    ref pCounter);

                    if (curIndex > lastIntendedIndex)
                        break;

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

                    //Overllapping (no check for curIndex > lastIntendedIndex)
                }
                else
                {
                    counter = ResetAdd(collisionList,
                            counter,
                            ref lastSequenceStart,
                            ref hCounter,
                            ref pCounter);

                    if (curIndex > lastIntendedIndex)
                        break;
                }

            CONTINUE:
                curIndex++;
            }
        }

        private static int ResetAdd(IProducerConsumerCollection<CollisionCellListMember> collisionList,
                                 int counter,
                                 ref int lastSequenceStart,
                                 ref int hCounter,
                                 ref int pCounter)
        {
            if (counter > 1)
                collisionList.TryAdd(new CollisionCellListMember(lastSequenceStart, hCounter, pCounter));

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
        
        private static Vector2 GetCellCoords(float cellSize, Vector2 coords)
        {
            return new Vector2((int)(coords.X / cellSize), (int)(coords.Y / cellSize));
        }

        private static uint CellCoordsHash(Vector2 c)
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (hashCode * 1073741827) ^ (int)c.X;
                hashCode = (hashCode * 1073741827) ^ (int)c.Y;
                return (uint)hashCode;
            }
        }

        private static sbyte GetCellType(Vector2 coord)
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
