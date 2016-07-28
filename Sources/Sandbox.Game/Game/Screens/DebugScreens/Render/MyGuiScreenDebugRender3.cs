using Sandbox.Graphics.GUI;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{

#if !XB1

    [MyDebugScreen("Render", "Outdoor rendering settings", MyDirectXSupport.DX11)]
    class MyGuiScreenDebugRender3 : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRender3()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug 3", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCheckBox("Freeze terrain queries", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FreezeTerrainQueries));

            m_currentPosition.Y += 0.01f;
            AddLabel("Terrain shader constants", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Detailed texture range 0", 5.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.TerrainDetailD0));
            AddSlider("Detailed texture range 1", 5.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.TerrainDetailD1));
            AddSlider("Detailed texture range 2", 5.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.TerrainDetailD2));
            AddSlider("Detailed texture range 3", 5.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.TerrainDetailD3));

            m_currentPosition.Y += 0.01f;
            AddLabel("Grass", Color.Yellow.ToVector4(), 1.2f);
            //AddCheckBox("Postprocess", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassPostprocess));
            AddSlider("Grass maximum draw distance", 0.0f, 1000f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassMaxDrawDistance));
            AddSlider("Postprocess close distance", 0.0f, 200.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassPostprocessCloseDistance));
            AddSlider("Patch clipping distance", 0.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassGeometryClippingDistance));
            AddSlider("Scaling near distance", 0.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassGeometryScalingNearDistance));
            AddSlider("Scaling far distance", 0.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassGeometryScalingFarDistance));
            AddSlider("Scaling factor", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GrassGeometryDistanceScalingFactor));

            m_currentPosition.Y += 0.01f;
            AddLabel("Wind", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Strength", 0.0f, 10f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.WindStrength));
            AddSlider("Azimuth", 0.0f, 360f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.WindAzimuth));

            //m_currentPosition.Y += 0.01f;
            //AddLabel("Foliage lods", Color.Yellow.ToVector4(), 1.2f);

            //AddSlider("Lod 0 distance", 0.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FoliageLod0Distance));
            //AddSlider("Lod 1 distance", 0.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FoliageLod1Distance));
            //AddSlider("Lod 2 distance", 0.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FoliageLod2Distance));
            //AddSlider("Lod 3 distance", 0.0f, 500.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FoliageLod3Distance));
            //AddCheckBox("Draw debug bounding boxes", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnableFoliageDebug));
            //AddCheckBox("Freeze viewer", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FreezeFoliageViewer));

            //AddButton(new StringBuilder("Rebuild now"), delegate { VRageRender.MyRenderProxy.RebuildCullingStructure(); });
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRender3";
        }

    }

#endif

}
