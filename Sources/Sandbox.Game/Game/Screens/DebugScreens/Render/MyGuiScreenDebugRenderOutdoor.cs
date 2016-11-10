using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;
using VRage.Utils;

namespace Sandbox.Game.Gui
{

#if !XB1_TMP
    [MyDebugScreen("Render", "Outdoor")]
    class MyGuiScreenDebugRenderOutdoor : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderOutdoor()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Outdoor", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCheckBox("Freeze terrain queries", MyRenderProxy.Settings.FreezeTerrainQueries, (x) => MyRenderProxy.Settings.FreezeTerrainQueries = x.IsChecked);

            m_currentPosition.Y += 0.01f;
            AddLabel("Terrain shader constants", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Detailed texture range 0", MyRenderProxy.Settings.TerrainDetailD0, 5.0f, 500.0f, (x) => MyRenderProxy.Settings.TerrainDetailD0 = x.Value);
            AddSlider("Detailed texture range 1", MyRenderProxy.Settings.TerrainDetailD1, 5.0f, 500.0f, (x) => MyRenderProxy.Settings.TerrainDetailD1 = x.Value);
            AddSlider("Detailed texture range 2", MyRenderProxy.Settings.TerrainDetailD2, 5.0f, 500.0f, (x) => MyRenderProxy.Settings.TerrainDetailD2 = x.Value);
            AddSlider("Detailed texture range 3", MyRenderProxy.Settings.TerrainDetailD3, 5.0f, 500.0f, (x) => MyRenderProxy.Settings.TerrainDetailD3 = x.Value);

            m_currentPosition.Y += 0.01f;
            AddLabel("Grass", Color.Yellow.ToVector4(), 1.2f);
            //AddCheckBox("Postprocess", MyRenderProxy.Settings.GrassPostprocess, (x) => MyRenderProxy.Settings.GrassPostprocess = x.IsChecked);
            AddSlider("Grass maximum draw distance", MyRenderProxy.Settings.GrassMaxDrawDistance, 0.0f, 1000f, (x) => MyRenderProxy.Settings.GrassMaxDrawDistance = x.Value);
            AddSlider("Postprocess close distance", MyRenderProxy.Settings.GrassPostprocessCloseDistance, 0.0f, 200.0f, (x) => MyRenderProxy.Settings.GrassPostprocessCloseDistance = x.Value);
            AddSlider("Patch clipping distance", MyRenderProxy.Settings.GrassGeometryClippingDistance, 0.0f, 1000.0f, (x) => MyRenderProxy.Settings.GrassGeometryClippingDistance = x.Value);
            AddSlider("Scaling near distance", MyRenderProxy.Settings.GrassGeometryScalingNearDistance, 0.0f, 1000.0f, (x) => MyRenderProxy.Settings.GrassGeometryScalingNearDistance = x.Value);
            AddSlider("Scaling far distance", MyRenderProxy.Settings.GrassGeometryScalingFarDistance, 0.0f, 1000.0f, (x) => MyRenderProxy.Settings.GrassGeometryScalingFarDistance = x.Value);
            AddSlider("Scaling factor", MyRenderProxy.Settings.GrassGeometryDistanceScalingFactor, 0.0f, 10.0f, (x) => MyRenderProxy.Settings.GrassGeometryDistanceScalingFactor = x.Value);

            m_currentPosition.Y += 0.01f;
            AddLabel("Wind", Color.Yellow.ToVector4(), 1.2f);

            AddSlider("Strength", MyRenderProxy.Settings.WindStrength, 0.0f, 10f, (x) => MyRenderProxy.Settings.WindStrength = x.Value);
            AddSlider("Azimuth", MyRenderProxy.Settings.WindAzimuth, 0.0f, 360f, (x) => MyRenderProxy.Settings.WindAzimuth = x.Value);

            m_currentPosition.Y += 0.01f;
            AddLabel("Lights", Color.Yellow.ToVector4(), 1.2f);
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderOutdoor";
        }

    }

#endif

}
