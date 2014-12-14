using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            bool edge = false;
            Vector2 mtv = Vector2.Zero;
            Vector2 normal = Vector2.Zero;
            if (C.X - R <= 0)
            {
                edge = true;
                normal = new Vector2(1, 0);
                mtv = Vector2.Subtract(new Vector2(R, (__V.Y * (R - C.X) / __V.X) + C.Y), C);
            }
            else if(C.X + R >= Constants.GAME_UNIT_WIDTH)
            {
                edge = true;
                normal = new Vector2(-1, 0);
                mtv =
                    Vector2.Subtract(
                                     new Vector2(Constants.GAME_UNIT_WIDTH - R,
                                                 (__V.Y * (Constants.GAME_UNIT_WIDTH - R - C.X) / __V.X) + C.Y),
                                     C);
            }
            if (C.Y + R >= Constants.GAME_UNIT_HEIGHT)
            {
                edge = true;
                normal = new Vector2(0, 1);
                mtv =
                    Vector2.Subtract(
                                     new Vector2(
                                         (__V.X * (Constants.GAME_UNIT_HEIGHT - R - C.Y) / __V.Y) + C.X,
                                         Constants.GAME_UNIT_HEIGHT - R),
                                     C);
            }
            else if (C.Y - R <= 0)
            {
                edge = true;
                normal = new Vector2(0, -1);
                mtv = Vector2.Subtract(new Vector2((__V.X * (R - C.Y) / __V.Y) + C.X, R), C);
            }

            if (edge)
            {
                //Resolve
                C = Vector2.Add(C, mtv);

                //Reflect V from wall's normal
                __V = Vector2.Reflect(__V, normal);
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/345838/ball-to-ball-collision-detection-and-handling
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public static void ResolveCollision(Particle p1, Particle p2)
        {
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
