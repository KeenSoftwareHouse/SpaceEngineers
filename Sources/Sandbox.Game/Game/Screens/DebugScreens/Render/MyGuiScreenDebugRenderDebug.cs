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

            AddCheckBox("Draw IDs", MyRenderProxy.Settings.DisplayIDs, (x) => MyRenderProxy.Settings.DisplayIDs = x.IsChecked);
            AddCheckBox("Draw AABBs", MyRenderProxy.Settings.DisplayAabbs, (x) => MyRenderProxy.Settings.DisplayAabbs = x.IsChecked);

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

            m_currentPosition.Y += 0.01f;

            AddLabel("Scene objects", Color.Yellow.ToVector4(), 1.2f);

            AddCheckBox("Draw non-merge-instanced", MyRenderProxy.Settings.DrawNonMergeInstanced, (x) => MyRenderProxy.Settings.DrawNonMergeInstanced = x.IsChecked);
            AddCheckBox("Draw merge-instanced", MyRenderProxy.Settings.DrawMergeInstanced, (x) => MyRenderProxy.Settings.DrawMergeInstanced = x.IsChecked);

            m_currentPosition.Y += 0.01f;

            AddCheckBox("Draw standard meshes", MyRenderProxy.Settings.DrawMeshes, (x) => MyRenderProxy.Settings.DrawMeshes = x.IsChecked);
            AddCheckBox("Draw standard instanced meshes", MyRenderProxy.Settings.DrawInstancedMeshes, (x) => MyRenderProxy.Settings.DrawInstancedMeshes = x.IsChecked);
            AddCheckBox("Draw glass", MyRenderProxy.Settings.DrawGlass, (x) => MyRenderProxy.Settings.DrawGlass = x.IsChecked);
            AddCheckBox("Draw alphamasked", MyRenderProxy.Settings.DrawAlphamasked, (x) => MyRenderProxy.Settings.DrawAlphamasked = x.IsChecked);
            AddCheckBox("Draw billboards", MyRenderProxy.Settings.DrawBillboards, (x) => MyRenderProxy.Settings.DrawBillboards = x.IsChecked);
            AddCheckBox("Draw impostors", MyRenderProxy.Settings.DrawImpostors, (x) => MyRenderProxy.Settings.DrawImpostors = x.IsChecked);
            AddCheckBox("Draw voxels", MyRenderProxy.Settings.DrawVoxels, (x) => MyRenderProxy.Settings.DrawVoxels = x.IsChecked);
            AddCheckBox("Draw occlusion queries debug", MyRenderProxy.Settings.DrawOcclusionQueriesDebug, 
                (x) => MyRenderProxy.Settings.DrawOcclusionQueriesDebug = x.IsChecked);


            AddButton("Print particle effects", onClick: (x) => VRage.Game.MyParticlesManager.LogEffects());
            
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
