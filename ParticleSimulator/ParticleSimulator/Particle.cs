using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BrownianMotion
{
    internal class Particle
    {
        private Vector2 __Coords;
        private Vector2 __Speed;
        private float __Radius;
        private float __ScaledR;
        private readonly float __M;

        public static HashSet<Tuple<Particle, Particle>> CheckedPairs = new HashSet<Tuple<Particle, Particle>>(); 

        public Particle(Vector2 coords, Vector2 speed, float radius, float m)
        {
            __Coords = coords;
            __Speed = speed;
            __Radius = radius;
            __M = m;
        }

        public float X
        {
            get
            {
                return Coords.X;
            }
        }

        public float Y
        {
            get
            {
                return Coords.Y;
            }
        }

        public Vector2 V
        {
            get
            {
                return __Speed;
            }
            set
            {
                __Speed = value;
            }
        }

        public float R
        {
            get
            {
                return __Radius;
            }
            set { __Radius = value; }
        }

        public float M
        {
            get
            {
                return __M;
            }
        }

        public Vector2 Coords
        {
            get { return __Coords; }
            set
            {
                __Coords = value;
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
            __Coords += __Speed * fraction;
            __Radius = Constants.PARTICLE_RADIUS;
        }

        private static bool AreColliding(Particle p1, Particle p2)
        {
            return Vector2.DistanceSquared(p1.Coords, p2.Coords) <= Math.Pow((p1.R + p2.R), 2);
        }

        private void CheckEdges()
        {
            if (X - R <= 0)
            {
                ResolveCollision(this, new Particle(new Vector2(-R, Y), Vector2.Zero, R, Single.MaxValue), true);
            }
            else if(X + R >= Constants.GAME_UNIT_WIDTH)
            {
                ResolveCollision(this, new Particle(new Vector2(Constants.GAME_UNIT_WIDTH + R, Y), Vector2.Zero, R, Single.MaxValue), true);
            }
            if (Y + R >= Constants.GAME_UNIT_HEIGHT)
            {
                ResolveCollision(this, new Particle(new Vector2(X, Constants.GAME_UNIT_HEIGHT + R), Vector2.Zero, R, Single.MaxValue), true);
            }
            else if (Y - R <= 0)
            {
                ResolveCollision(this, new Particle(new Vector2(X, -R), Vector2.Zero, R, Single.MaxValue), true);
            }
        }

        private static void ResolveCollision(Particle p1, Particle p2, bool isEdge)
        {
            if (p1 == p2)
                return;

            if (AreColliding(p1, p2))
            {
                var delta = p1.Coords - p2.Coords;
                var dist = delta.Length();
                if (dist == 0)
                {
                    dist = p1.R + p2.R - 1;
                    delta = new Vector2(1.0f, 0.0f);
                }

                var mtd = delta * ((p1.R + p2.R) - dist) / dist;
                float im1 = 1 / p1.M;
                float im2 = 1 / p2.M;

                p1.Coords += mtd * (im1 / im1 + im2);
                p2.Coords -= mtd * (im2 / im1 + im2);

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

                Vector2 n = new Vector2(p2.X - p1.X, p2.Y - p1.Y);
                Vector2 un = n;
                un.Normalize();
                Vector2 ut = new Vector2(-un.Y, un.X);

                float v1ns;
                Vector2.Dot(ref p1.__Speed, ref un, out v1ns);
                float v1ts;
                Vector2.Dot(ref p1.__Speed, ref ut, out v1ts);
                float v2ns;
                Vector2.Dot(ref p2.__Speed, ref un, out v2ns);
                float v2ts;
                Vector2.Dot(ref p2.__Speed, ref ut, out v2ts);

                var v1newn = (((p1.M - p2.M) * v1ns + 2f * p2.M * v2ns) / (p1.M + p2.M)) * un;
                var v2newn = (((p2.M - p1.M) * v1ns + 2f * p2.M * v1ns) / (p1.M + p2.M)) * un;

                p1.V = v1newn + v1ts * ut;
                p2.V = v2newn + v2ts * ut;
            }
        }

        public static void ResolveCollision(Particle p1, Particle p2)
        {
            if (p1 == p2)
                return;

            if (!CheckedPairs.Contains(new Tuple<Particle, Particle>(p1, p2)))
            {
                CheckedPairs.Add(new Tuple<Particle, Particle>(p1, p2));

                if (AreColliding(p1, p2))
                {
                    var delta = p1.Coords - p2.Coords;
                    var dist = delta.Length();
                    if (dist == 0)
                    {
                        dist = p1.R + p2.R - 1;
                        delta = new Vector2(1.0f, 0.0f);
                    }

                    var mtd = delta * ((p1.R + p2.R) - dist) / dist;
                    float im1 = 1 / p1.M;
                    float im2 = 1 / p2.M;

                    p1.Coords += mtd * (im1 / im1 + im2);
                    p2.Coords -= mtd * (im2 / im1 + im2);

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

                    Vector2 n = new Vector2(p2.X - p1.X, p2.Y - p1.Y);
                    Vector2 un = n;
                    un.Normalize();
                    Vector2 ut = new Vector2(-un.Y, un.X);

                    float v1ns;
                    Vector2.Dot(ref p1.__Speed, ref un, out v1ns);
                    float v1ts;
                    Vector2.Dot(ref p1.__Speed, ref ut, out v1ts);
                    float v2ns;
                    Vector2.Dot(ref p2.__Speed, ref un, out v2ns);
                    float v2ts;
                    Vector2.Dot(ref p2.__Speed, ref ut, out v2ts);

                    var v1newn = (((p1.M - p2.M) * v1ns + 2f * p2.M * v2ns) / (p1.M + p2.M)) * un;
                    var v2newn = (((p2.M - p1.M) * v1ns + 2f * p2.M * v1ns) / (p1.M + p2.M)) * un;

                    p1.V = v1newn + v1ts * ut;
                    p2.V = v2newn + v2ts * ut;
                }
            }
            else
            {
                Console.WriteLine("Contains!");
            }
        }
    }
}
