using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using ParticleSimulator;

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

    internal abstract class BaseParticleFactory
    {
        public abstract List<BaseParticle> Emit(int count);
    }

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

    internal interface IBoundingCircleCollisionResolver
    {
        void ResolveCollision(IBoundingCircle particle1, IBoundingCircle particle2);
    }

    internal class CircleParticle : BaseParticle, IBoundingCircle
    {
        private class CircleParticleCR : IBoundingCircleCollisionResolver
        {
            private static bool AreColliding(IBoundingCircle p1, IBoundingCircle p2)
            {
                return Vector2.DistanceSquared(p1.Coords, p2.Coords) <= Math.Pow((p1.R + p2.R), 2);
            }

            /// <summary>
            /// http://stackoverflow.com/questions/345838/ball-to-ball-collision-detection-and-handling
            /// </summary>
            /// <param name="particle1"></param>
            /// <param name="particle2"></param>
            public void ResolveCollision(IBoundingCircle particle1, IBoundingCircle particle2)
            {
                var p1 = particle1 as CircleParticle;
                var p2 = particle2 as CircleParticle;

                if (AreColliding(p1, p2))
                {
                    var delta = Vector2.Subtract(p1.Coords, p2.Coords);
                    var dist = delta.Length();
                    if (dist == 0)
                    {
                        dist = p1.R + p2.R - 1;
                        delta = new Vector2(1.0f, 0.0f);
                    }

                    Vector2 mtv = Vector2.Multiply(delta, (p1.R + p2.R - dist) / dist);
                    float im1 = 1 / p1.__M;
                    float im2 = 1 / p2.__M;
                    p1.__C += Vector2.Multiply(mtv, (im1 / (im1 + im2)));
                    p2.__C -= Vector2.Multiply(mtv, (im2 / (im1 + im2)));

                    //----

                    var v = Vector2.Subtract(p1.__V, p2.__V);
                    mtv.Normalize();
                    float vn;
                    Vector2.Dot(ref v, ref mtv, out vn);
                    if (vn > 0.0f)
                        return;

                    const float RESTITUTION = 1f;
                    var i = -((1f + RESTITUTION) * vn) / (im1 + im2);
                    var impulse = Vector2.Multiply(mtv, i);

                    p1.__V += Vector2.Multiply(impulse, im1);
                    p2.__V -= Vector2.Multiply(impulse, im2);
                }
            }

            public static void CheckResolveEdges(CircleParticle p)
            {
                bool edge = false;
                Vector2 mtv = Vector2.Zero;
                Vector2 normal = Vector2.Zero;
                if (p.__C.X - p.R <= 0)
                {
                    edge = true;
                    normal = new Vector2(1, 0);
                    mtv = Vector2.Subtract(new Vector2(p.R, (p.__V.Y * (p.R - p.__C.X) / p.__V.X) + p.__C.Y), p.__C);
                }
                else if (p.__C.X + p.R >= Constants.GAME_UNIT_WIDTH)
                {
                    edge = true;
                    normal = new Vector2(-1, 0);
                    mtv =
                        Vector2.Subtract(
                                         new Vector2(Constants.GAME_UNIT_WIDTH - p.R,
                                                     (p.__V.Y * (Constants.GAME_UNIT_WIDTH - p.R - p.__C.X) / p.__V.X) + p.__C.Y),
                                         p.__C);
                }
                if (p.__C.Y + p.R >= Constants.GAME_UNIT_HEIGHT)
                {
                    edge = true;
                    normal = new Vector2(0, 1);
                    mtv =
                        Vector2.Subtract(
                                         new Vector2(
                                             (p.__V.X * (Constants.GAME_UNIT_HEIGHT - p.R - p.__C.Y) / p.__V.Y) + p.__C.X,
                                             Constants.GAME_UNIT_HEIGHT - p.R),
                                         p.__C);
                }
                else if (p.__C.Y - p.R <= 0)
                {
                    edge = true;
                    normal = new Vector2(0, -1);
                    mtv = Vector2.Subtract(new Vector2((p.__V.X * (p.R - p.__C.Y) / p.__V.Y) + p.__C.X, p.R), p.__C);
                }

                if (edge)
                {
                    //Resolve
                    p.__C = Vector2.Add(p.__C, mtv);

                    //Reflect V from wall's normal
                    p.__V = Vector2.Reflect(p.__V, normal);
                }
            }
        }

        private static readonly Object __SLock = new Object();
        private static CircleParticleCR __Resolver;

        private float __ModifiableR;
        public float __R;

        public CircleParticle(Vector2 coords, Vector2 speed, float radius, float m)
            :base(coords, speed, m)
        {
            __R = radius;
        }

        public static IBoundingCircleCollisionResolver Resolver
        {
            get
            {
                if (__Resolver != null) return __Resolver;
                Monitor.Enter(__SLock);
                var temp = new CircleParticleCR();
                Interlocked.Exchange(ref __Resolver, temp);
                Monitor.Exit(__SLock);
                return __Resolver;
            }
        }

        public float ModifiableR
        {
            get { return __ModifiableR; }
            set { __ModifiableR = value; }
        }

        public Vector2 Coords
        {
            get
            {
                return __C;
            }
        }

        public float R
        {
            get
            {
                return __R;
            }
        }

        public override void Update(float fraction)
        {
            CheckResolveEdges();
            __C += __V * fraction;
            __R = Constants.PARTICLE_RADIUS;
        }

        protected override void CheckResolveEdges()
        {
            CircleParticleCR.CheckResolveEdges(this);
        }
    }
}
