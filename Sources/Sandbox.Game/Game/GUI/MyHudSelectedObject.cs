#region Using

using Sandbox.Game.Entities;
using VRage.Game.Entity.UseObject;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudSelectedObject
    {
        private bool m_visible;

        public bool Visible { get { return m_visible; } set { m_visible = value; if (!value) InteractiveObject = null; } }
        public MyHudTexturesEnum TextureEnum = MyHudTexturesEnum.corner;
        public Vector2 HalfSize = Vector2.One * 0.02f;
        public Color Color = MyHudConstants.HUD_COLOR_LIGHT;
        internal IMyUseObject InteractiveObject;
    }
}
