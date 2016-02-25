namespace VRage.Game.Components
{
    public abstract class MyDebugRenderComponentBase
    {
        public virtual void PrepareForDraw() { }
        public abstract bool DebugDraw();
        public abstract void DebugDrawInvalidTriangles();
    }
}
