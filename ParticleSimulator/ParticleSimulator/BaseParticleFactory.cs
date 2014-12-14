using System.Collections.Generic;

namespace ParticleSimulator
{
    internal abstract class BaseParticleFactory
    {
        public abstract List<BaseParticle> Emit(int count);
    }
}