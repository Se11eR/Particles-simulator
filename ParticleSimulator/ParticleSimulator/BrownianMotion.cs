using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ParticleSimulator
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BrownianMotion : Game
    {
        private GraphicsDeviceManager __Graphics;
        private SpriteBatch __SpriteBatch;

        private readonly Random __Random = new Random(Int32.MaxValue / 2);
        private List<Particle> __AllParticles = new List<Particle>();
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
            __Detector = new ParallelSpartialSubdivionCD(__AllParticles.Count);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            var fraction = (float) gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var particle in __AllParticles)
            {
                particle.Update(fraction);
            }

            __Detector.PerformTest(__AllParticles, __AllParticles[0].R, Particle.ResolveCollision);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            __SpriteBatch.Begin();

            foreach (Particle particle in __AllParticles)
            {
                //TODO: efficient drawing
                __SpriteBatch.DrawCircle(particle.Coords * Constants.UNIT_PIXEL_SIZE,
                                         particle.R * Constants.UNIT_PIXEL_SIZE,
                                         2,
                                         Color.Black,
                                         1);
            }
            
            //var pixelCellSize = (int)(__Detector.CellSize * Constants.UNIT_PIXEL_SIZE);
            //for (var i = 0; i < Constants.GAME_VIEW_WIDTH; i += pixelCellSize)
            //{
            //    __SpriteBatch.DrawLine(new Vector2(i, 0),
            //                           new Vector2(i, Constants.GAME_VIEW_HEIGHT),
            //                           Color.Black);
            //}
            //for (var i = 0; i < Constants.GAME_VIEW_HEIGHT; i += pixelCellSize)
            //{
            //    __SpriteBatch.DrawLine(new Vector2(0, i),
            //                           new Vector2(Constants.GAME_VIEW_WIDTH, i),
            //                           Color.Black);
            //}

            __SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
