using VRageMath;

namespace Sandbox.Graphics.GUI
{
    abstract class MyRichLabelPart
    {
        public abstract Vector2 GetSize();        

        public abstract bool Draw(Vector2 position);

        public abstract bool HandleInput(Vector2 position);
    }
}
