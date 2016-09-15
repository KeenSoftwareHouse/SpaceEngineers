using System.Collections.Generic;
using System.Text;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;
using Sandbox.Game.World;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "Debug")]
    class MyGuiScreenDebugRenderDebug : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderDebug()
        {
            RecreateControls(true);
        }

        private List<MyGuiControlCheckbox> m_cbs = new List<MyGuiControlCheckbox>();

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Debug", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("Wireframe", MyRenderProxy.Settings.Wireframe, (x) => MyRenderProxy.Settings.Wireframe = x.IsChecked);
            AddCheckBox("Enable multithreading", MyRenderProxy.Settings.EnableParallelRendering, (x) => MyRenderProxy.Settings.EnableParallelRendering = x.IsChecked);
            AddCheckBox("Amortize workload", MyRenderProxy.Settings.AmortizeBatchWork, (x) => MyRenderProxy.Settings.AmortizeBatchWork = x.IsChecked);
            AddCheckBox("Force IC", MyRenderProxy.Settings.ForceImmediateContext, (x) => MyRenderProxy.Settings.ForceImmediateContext = x.IsChecked);
            AddCheckBox("Force Slow CPU", MyRenderProxy.Settings.ForceSlowCPU, (x) => MyRenderProxy.Settings.ForceSlowCPU = x.IsChecked);
            AddCheckBox("Render thread as worker", MyRenderProxy.Settings.RenderThreadAsWorker, (x) => MyRenderProxy.Settings.RenderThreadAsWorker = x.IsChecked);
            AddCheckBox("Total parrot view", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_NAMES));
            AddCheckBox("Debug missing file textures", MyRenderProxy.Settings.UseDebugMissingFileTextures, (x) => MyRenderProxy.Settings.UseDebugMissingFileTextures = x.IsChecked);
            AddButton("Print textures log", onClick: PrintUsedFileTexturesIntoLog);
            AddCheckBox("Display transparency heat map", MyRenderProxy.Settings.DisplayTransparencyHeatMap, (x) => MyRenderProxy.Settings.DisplayTransparencyHeatMap = x.IsChecked);
            AddCheckBox("Use transparency heat map in grayscale", MyRenderProxy.Settings.DisplayTransparencyHeatMapInGrayscale, (x) => MyRenderProxy.Settings.DisplayTransparencyHeatMapInGrayscale = x.IsChecked);
            
            // TODO: Proper fix saving environment before re-enable it
            //AddButton("Save environment", onClick: SaveEnvironment);
        }

        void PrintUsedFileTexturesIntoLog(MyGuiControlButton sender)
        {
            MyRenderProxy.PrintAllFileTexturesIntoLog();
        }

        void SaveEnvironment(MyGuiControlButton sender)
        {
            MySector.SaveEnvironmentDefinition();
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderDebug";
        }
    }

#endif
}
