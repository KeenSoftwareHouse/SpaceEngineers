using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens.DebugScreens
{
#if !XB1_TMP

    [MyDebugScreen("VRage", "Debug draw settings 3")]
    class MyGuiScreenDebugDrawSettings3 : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugDrawSettings3";
        }

        public MyGuiScreenDebugDrawSettings3()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Debug draw settings 3", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCheckBox("Debug decals", MyRenderProxy.Settings.DebugDrawDecals, (x) => MyRenderProxy.Settings.DebugDrawDecals = x.IsChecked);
            AddCheckBox("Decals default material", null, MemberHelper.GetMember(() => MyFakes.ENABLE_USE_DEFAULT_DAMAGE_DECAL));
            AddButton(new StringBuilder("Clear decals"), ClearDecals);
            AddCheckBox("Debug Particles", () => MyDebugDrawSettings.DEBUG_DRAW_PARTICLES, x => MyDebugDrawSettings.DEBUG_DRAW_PARTICLES = x);

            AddCheckBox("Entity update statistics", () => MyDebugDrawSettings.DEBUG_DRAW_ENTITY_STATISTICS, x => MyDebugDrawSettings.DEBUG_DRAW_ENTITY_STATISTICS = x);
        }

        static void ClearDecals(MyGuiControlButton button)
        {
            MyRenderProxy.ClearDecals();
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }

#endif

    }
}
