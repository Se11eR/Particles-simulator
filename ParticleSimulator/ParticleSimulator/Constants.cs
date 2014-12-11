namespace ParticleSimulator
{
    public static class Constants
    {
        public const int GAME_UNIT_WIDTH = 64;
        public const int GAME_UNIT_HEIGHT = 48;

        public const float PARTICLE_RADIUS = 0.15f;
        public const float PARTICLE_MASS = 1;
        public const float PARTICLE_MAX_SPEED_UPS = 3;

        public const int PARTICLE_COUNT = 5000;

        public const int UNIT_PIXEL_SIZE = 20;

        public const int GAME_VIEW_WIDTH = UNIT_PIXEL_SIZE * GAME_UNIT_WIDTH;
        public const int GAME_VIEW_HEIGHT = UNIT_PIXEL_SIZE * GAME_UNIT_HEIGHT;

        public const float SQRT2 = 1.41421356237f;
    }
}
