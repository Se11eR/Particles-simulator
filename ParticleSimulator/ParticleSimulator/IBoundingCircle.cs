using Microsoft.Xna.Framework;

namespace ParticleSimulator
{
    internal interface IBoundingCircle
    {
        float ModifiableR
        {
            get;
            set;
        }

        Vector2 Coords
        {
            get;
        }

        float R
        {
            get;
        }
    }
}