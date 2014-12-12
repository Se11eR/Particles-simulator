using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ParticleSimulator
{
    internal class Particle
    {
        private Vector2 __V;
        
        private float __ScaledR;
        private readonly float __M;

        public Vector2 C;
        public float R;

        public Particle(Vector2 coords, Vector2 speed, float radius, float m)
        {
            C = coords;
            __V = speed;
            R = radius;
            __M = m;
        }

        public float M
        {
            get
            {
                return __M;
            }
        }

        public float ScaledR
        {
            get { return __ScaledR; }
            set { __ScaledR = value; }
        }

        public void Update(float fraction)
        {
            CheckEdges();
            C += __V * fraction;
            R = Constants.PARTICLE_RADIUS;
        }

        private static bool AreColliding(Particle p1, Particle p2)
        {
            return Vector2.DistanceSquared(p1.C, p2.C) <= Math.Pow((p1.R + p2.R), 2);
        }

        private void CheckEdges()
        {
            if (C.X - R <= 0)
            {
                ResolveCollision(this, new Particle(new Vector2(-R, C.Y), Vector2.Zero, R, Single.MaxValue), true);
            }
            else if(C.X + R >= Constants.GAME_UNIT_WIDTH)
            {
                ResolveCollision(this, new Particle(new Vector2(Constants.GAME_UNIT_WIDTH + R, C.Y), Vector2.Zero, R, Single.MaxValue), true);
            }
            if (C.Y + R >= Constants.GAME_UNIT_HEIGHT)
            {
                ResolveCollision(this, new Particle(new Vector2(C.X, Constants.GAME_UNIT_HEIGHT + R), Vector2.Zero, R, Single.MaxValue), true);
            }
            else if (C.Y - R <= 0)
            {
                ResolveCollision(this, new Particle(new Vector2(C.X, -R), Vector2.Zero, R, Single.MaxValue), true);
            }
        }

        private static void ResolveCollision(Particle p1, Particle p2, bool isEdge)
        {
            if (p1 == p2)
                return;

            if (AreColliding(p1, p2))
            {
                var delta = p1.C - p2.C;
                var dist = delta.Length();
                if (dist == 0)
                {
                    dist = p1.R + p2.R - 1;
                    delta = new Vector2(1.0f, 0.0f);
                }

                var mtd = delta * ((p1.R + p2.R) - dist) / dist;
                float im1 = 1 / p1.M;
                float im2 = 1 / p2.M;

                p1.C += mtd * (im1 / im1 + im2);
                p2.C -= mtd * (im2 / im1 + im2);

                //----

                //var v = p1.V - p2.V;
                //mtd.Normalize();
                //float vn;
                //Vector2.Dot(ref v, ref mtd, out vn);
                //if (vn > 0)
                //    return;

                //const float restitution = 1f;
                //var i = -((1f + restitution) * vn) / (im1 + im2);
                //var impulse = mtd * i;

                //p1.V += impulse * im1;
                //p2.V -= impulse * im2;

                Vector2 n = new Vector2(p2.C.X - p1.C.X, p2.C.Y - p1.C.Y);
                Vector2 un = n;
                un.Normalize();
                Vector2 ut = new Vector2(-un.Y, un.X);

                float v1ns;
                Vector2.Dot(ref p1.__V, ref un, out v1ns);
                float v1ts;
                Vector2.Dot(ref p1.__V, ref ut, out v1ts);
                float v2ns;
                Vector2.Dot(ref p2.__V, ref un, out v2ns);
                float v2ts;
                Vector2.Dot(ref p2.__V, ref ut, out v2ts);

                var v1newn = (((p1.M - p2.M) * v1ns + 2f * p2.M * v2ns) / (p1.M + p2.M)) * un;
                var v2newn = (((p2.M - p1.M) * v1ns + 2f * p2.M * v1ns) / (p1.M + p2.M)) * un;

                p1.__V = v1newn + v1ts * ut;
                p2.__V = v2newn + v2ts * ut;
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/345838/ball-to-ball-collision-detection-and-handling
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public static void ResolveCollision(Particle p1, Particle p2)
        {
            if (p1 == p2)
                return;

            if (AreColliding(p1, p2))
            {
                var delta = Vector2.Subtract(p1.C, p2.C);
                var dist = delta.Length();
                if (dist == 0)
                {
                    dist = p1.R + p2.R - 1;
                    delta = new Vector2(1.0f, 0.0f);
                }

                Vector2 mtv = Vector2.Multiply(delta, (p1.R + p2.R - dist) / dist);
                float im1 = 1 / p1.M;
                float im2 = 1 / p2.M;
                p1.C += Vector2.Multiply(mtv, (im1 / (im1 + im2)));
                p2.C -= Vector2.Multiply(mtv, (im2 / (im1 + im2)));

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
    }
}
