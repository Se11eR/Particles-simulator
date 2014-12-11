using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BrownianMotion
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BrownianMotion : Game
    {
        GraphicsDeviceManager __Graphics;
        SpriteBatch __SpriteBatch;

        private readonly Random __Random = new Random(Int32.MaxValue / 2);
        private List<Particle> __AllParticles = new List<Particle>();
        private List<Vector2> __VrownianCoords = new List<Vector2>();

        private ParallelSpartialSubdivionCD __Detector;

        public BrownianMotion()
        {
            __Graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferHeight = Constants.GAME_VIEW_HEIGHT,
                PreferredBackBufferWidth = Constants.GAME_VIEW_WIDTH
            };
            IsMouseVisible = true;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            DrawingHelper.Initialize(GraphicsDevice);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            __SpriteBatch = new SpriteBatch(GraphicsDevice);

            __AllParticles = new List<Particle>(Constants.PARTICLE_COUNT);

            for (int i = 0; i < Constants.PARTICLE_COUNT; i++)
            {
                __AllParticles.Add(
                    new Particle(
                        new Vector2((float)__Random.NextDouble() * Constants.GAME_UNIT_WIDTH,
                            (float)__Random.NextDouble() * Constants.GAME_UNIT_HEIGHT),
                        new Vector2((float)__Random.NextDouble() * Constants.PARTICLE_MAX_SPEED_UPS,
                            (float)__Random.NextDouble() * Constants.PARTICLE_MAX_SPEED_UPS), Constants.PARTICLE_RADIUS,
                        Constants.PARTICLE_MASS));
            }

            //__AllParticles.Add(
            //                   new Particle(
            //                       new Vector2((float)Constants.GAME_UNIT_WIDTH / 5,
            //                                   (float)Constants.GAME_UNIT_HEIGHT / 2 + 2),
            //                       new Vector2(+Constants.PARTICLE_MAX_SPEED_UPS * 2,
            //                                   0f),
            //                       Constants.PARTICLE_RADIUS,
            //                       Constants.PARTICLE_MASS));

            //__AllParticles.Add(
            //                   new Particle(
            //                       new Vector2((float)(Constants.GAME_UNIT_WIDTH - 3 * (Constants.GAME_UNIT_WIDTH / 5)),
            //                                   (float)(Constants.GAME_UNIT_HEIGHT / 2)),
            //                       new Vector2(-Constants.PARTICLE_MAX_SPEED_UPS,
            //                                   Constants.PARTICLE_MAX_SPEED_UPS),
            //                       Constants.PARTICLE_RADIUS,
            //                       Constants.PARTICLE_MASS));

            //__AllParticles.Add(
            //                  new Particle(
            //                      new Vector2((float)(Constants.GAME_UNIT_WIDTH - (Constants.GAME_UNIT_WIDTH / 5)),
            //                                  (float)(Constants.GAME_UNIT_HEIGHT / 2)),
            //                      new Vector2(-Constants.PARTICLE_MAX_SPEED_UPS,
            //                                  Constants.PARTICLE_MAX_SPEED_UPS),
            //                      Constants.PARTICLE_RADIUS,
            //                      Constants.PARTICLE_MASS));


            //__VrownianCoords.Add(new Vector2(Constants.GAME_UNIT_WIDTH / 2, Constants.GAME_UNIT_HEIGHT / 2));
            //__AllParticles.Add(
            //                   new Particle(
            //                       __VrownianCoords.Last(),
            //                       Vector2.Zero,
            //                       Constants.PARTICLE_RADIUS * 3.5f,
            //                       Constants.PARTICLE_MASS * 150));

            __Detector = new ParallelSpartialSubdivionCD(__AllParticles.Count);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            var fraction = (float) gameTime.ElapsedGameTime.TotalSeconds;

            //for (var i = 0; i < __AllParticles.Count; i++)
            //{
            //    __AllParticles[i].Update(fraction);
            //    for (var j = i + 1; j < __AllParticles.Count; j++)
            //    {
            //        Particle.ResolveCollision(__AllParticles[i], __AllParticles[j]);
            //    }
            //}

            foreach (var particle in __AllParticles)
            {
                particle.Update(fraction);
            }

            __Detector.PerformTest(__AllParticles, __AllParticles[0].R, Particle.ResolveCollision);

            Particle.CheckedPairs.Clear();

            if (__AllParticles.Any() && __VrownianCoords.Any())
                if (__AllParticles.Last().Coords != __VrownianCoords.Last())
                    __VrownianCoords.Add(__AllParticles.Last().Coords);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            //if (__VrownianCoords.Any())
            //{
            //    DrawingHelper.Begin(PrimitiveType.LineList);
            //    for (int i = 0; i < __VrownianCoords.Count; i++)
            //    {
            //        var next = i + 1;
            //        if (next < __VrownianCoords.Count)
            //            DrawingHelper.DrawLine(__VrownianCoords[i] * Constants.UNIT_PIXEL_SIZE,
            //                                   __VrownianCoords[next] * Constants.UNIT_PIXEL_SIZE,
            //                                   Color.Red);
            //    }
            //    DrawingHelper.End();
            //}

            for (int i = 0; i < __AllParticles.Count; i++)
            {
                DrawingHelper.DrawCircle(__AllParticles[i].Coords * Constants.UNIT_PIXEL_SIZE,
                                         __AllParticles[i].R * Constants.UNIT_PIXEL_SIZE,
                                         Color.Black,
                                         true);
            }

            //DrawingHelper.DrawCircle(__AllParticles[__AllParticles.Count - 1].Coords * Constants.UNIT_PIXEL_SIZE,
            //                             __AllParticles[__AllParticles.Count - 1].R * Constants.UNIT_PIXEL_SIZE,
            //                             Color.Red,
            //                             true);
            DrawingHelper.Begin(PrimitiveType.LineList);
            var pixelCellSize = (int)(__Detector.CellSize * Constants.UNIT_PIXEL_SIZE);
            for (var i = 0; i < Constants.GAME_VIEW_WIDTH; i += pixelCellSize)
            {
                DrawingHelper.DrawLine(new Vector2(i, 0),
                                               new Vector2(i, Constants.GAME_VIEW_HEIGHT),
                                               Color.Black);
            }
            for (var i = 0; i < Constants.GAME_VIEW_HEIGHT; i += pixelCellSize)
            {
                DrawingHelper.DrawLine(new Vector2(0, i),
                                               new Vector2(Constants.GAME_VIEW_WIDTH, i),
                                               Color.Black);
            }
            DrawingHelper.End();
            base.Draw(gameTime);
        }
    }
}
