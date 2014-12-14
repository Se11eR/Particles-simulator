namespace ParticleSimulator
{
    internal interface IBoundingCircleCollisionResolver
    {
        void ResolveCollision(IBoundingCircle particle1, IBoundingCircle particle2);
    }
}