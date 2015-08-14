using VRage;
using SharpDX.Direct3D9;
using VRage.Utils;

namespace VRageRender
{
    public abstract class MyComponentBase
    {
        public virtual void LoadContent()
        {
        }

        public virtual void ReloadContent()
        {
            UnloadContent();
            LoadContent();
        }

        public virtual void UnloadContent()
        {
        }
    }

    public abstract class MyRenderComponentBase : MyComponentBase
    {
        public abstract int GetID();

        public virtual void LoadContent(Device device)
        {
            LoadContent();
        }
    }
}
