namespace VRage.Game.Components
{
    public abstract class MyDebugRenderComponentBase
    {
        public virtual void PrepareForDraw() { }
        public abstract void DebugDraw();
        public abstract void DebugDrawInvalidTriangles();
    }
}
