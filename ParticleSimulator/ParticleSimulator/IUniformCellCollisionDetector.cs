namespace ParticleSimulator
{
    internal interface IUniformCellCollisionDetector : ICircleCollisionDetector
    {
        float CellSize
        {
            get;
            set;
        }
    }
}