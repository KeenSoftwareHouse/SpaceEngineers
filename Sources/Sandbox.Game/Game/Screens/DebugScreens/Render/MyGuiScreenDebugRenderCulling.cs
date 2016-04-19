using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Culling settings")]
    class MyGuiScreenDebugRenderCulling : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderCulling()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug culling", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderCulling";
        }

    }
}
