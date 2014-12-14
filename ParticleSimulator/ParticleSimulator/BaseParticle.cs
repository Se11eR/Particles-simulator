using Microsoft.Xna.Framework;

namespace ParticleSimulator
{
    internal abstract class BaseParticle
    {
        protected Vector2 __C;
        protected readonly float __M;
        protected Vector2 __V;

        protected BaseParticle(Vector2 coords, Vector2 speed, float m)
        {
            __C = coords;
            __M = m;
            __V = speed;
        }

        public abstract void Update(float fraction);

        protected abstract void CheckResolveEdges();
    }
}