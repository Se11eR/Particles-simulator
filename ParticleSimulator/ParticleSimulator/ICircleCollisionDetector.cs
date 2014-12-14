namespace ParticleSimulator
{
    internal interface ICircleCollisionDetector
    {
        void PerformTest(IBoundingCircle[] particles, float largestR);
    }
}