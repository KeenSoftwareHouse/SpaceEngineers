using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{

#if !XB1
    [MyDebugScreen("Render", "Shadow settings", MyDirectXSupport.ALL)]
    class MyGuiScreenDebugRenderShadows : MyGuiScreenDebugBase
    {
		public MyGuiScreenDebugRenderShadows()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Shadow settings", Color.Yellow.ToVector4());
            AddShareFocusHint();

			m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

			m_currentPosition.Y += 0.01f;
			AddLabel("General", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable Shadows", () => MyRenderProxy.Settings.EnableShadows, (newValue) => { MyRenderProxy.Settings.EnableShadows = newValue; });
            AddCheckBox("Enable Shadow Blur", () => MyRenderProxy.Settings.EnableShadowBlur, (newValue) => { MyRenderProxy.Settings.EnableShadowBlur = newValue; });
			AddSlider("Shadow fadeout", 0f, 1f, () => MyRenderProxy.Settings.ShadowFadeoutMultiplier, (x) => MyRenderProxy.Settings.ShadowFadeoutMultiplier = x);

            m_currentPosition.Y += 0.01f;
            AddLabel("Shadow cascades", Color.Yellow.ToVector4(), 1.2f);

            AddCheckBox("Force per-frame updating", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.UpdateCascadesEveryFrame));
			AddCheckBox("Show cascade splits", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayShadowsWithDebug));
            AddCheckBox("Show cascade textures", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DrawCascadeTextures));
            AddCheckBox("Display frozen cascades", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayFrozenShadowCascade));
			for (int cascadeIndex = 0; cascadeIndex < MyRenderProxy.Settings.ShadowCascadeCount; ++cascadeIndex)
			{
				int captureIndex = cascadeIndex;
				AddCheckBox("Freeze cascade " + cascadeIndex.ToString(), () => MyRenderProxy.Settings.ShadowCascadeFrozen[captureIndex], (x) => MyRenderProxy.Settings.ShadowCascadeFrozen[captureIndex] = x);
			}

			AddSlider("Max base shadow cascade distance", 1.0f, 20000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShadowCascadeMaxDistance));
			AddSlider("Back offset", 1.0f, 10000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShadowCascadeZOffset));
			AddSlider("Spread factor", 0.0f, 2.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShadowCascadeSpreadFactor));
	/*		for (int cascadeIndex = 0; cascadeIndex < MyRenderProxy.Settings.ShadowCascadeCount; ++cascadeIndex)
			{
				int captureIndex = cascadeIndex;
				AddSlider("Small object culling " + cascadeIndex.ToString(), 0.0f, 2000.0f * (cascadeIndex + 1), MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShadowCascadeSmallSkipThresholds[captureIndex]));
			}*/
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
			return "MyGuiScreenDebugRenderShadows";
        }

    }

#endif
}
