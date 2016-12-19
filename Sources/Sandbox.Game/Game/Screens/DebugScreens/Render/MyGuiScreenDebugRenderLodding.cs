using System;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Render", "Lodding")]
    public class MyGuiScreenDebugRenderLodding : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderLodding()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Lodding", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            AddLabel("The new pipeline - lod shift", Color.White, 1f);
            AddSlider("GBuffer", MySector.NewPipelineSettings.GBufferLodding.LodShift, 0, 6, (x) => MySector.NewPipelineSettings.GBufferLodding.LodShift = (int)Math.Round(x.Value));
            if (MySector.NewPipelineSettings.CascadeDepthLoddings.Length >= 6)
            {
                AddSlider("CSM_0", MySector.NewPipelineSettings.CascadeDepthLoddings[0].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[0].LodShift = (int) Math.Round(x.Value));
                AddSlider("CSM_1", MySector.NewPipelineSettings.CascadeDepthLoddings[1].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[1].LodShift = (int) Math.Round(x.Value));
                AddSlider("CSM_2", MySector.NewPipelineSettings.CascadeDepthLoddings[2].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[2].LodShift = (int) Math.Round(x.Value));
                AddSlider("CSM_3", MySector.NewPipelineSettings.CascadeDepthLoddings[3].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[3].LodShift = (int) Math.Round(x.Value));
                AddSlider("CSM_4", MySector.NewPipelineSettings.CascadeDepthLoddings[4].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[4].LodShift = (int) Math.Round(x.Value));
                AddSlider("CSM_5", MySector.NewPipelineSettings.CascadeDepthLoddings[5].LodShift, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[5].LodShift = (int) Math.Round(x.Value));
            }
            AddSlider("Single depth", MySector.NewPipelineSettings.SingleDepthLodding.LodShift, 0, 6, (x) => MySector.NewPipelineSettings.SingleDepthLodding.LodShift = (int)Math.Round(x.Value));


            AddLabel("The new pipeline - min lod", Color.White, 1f);
            AddSlider("GBuffer", MySector.NewPipelineSettings.GBufferLodding.MinLod, 0, 6, (x) => MySector.NewPipelineSettings.GBufferLodding.LodShift = (int)Math.Round(x.Value));
            if (MySector.NewPipelineSettings.CascadeDepthLoddings.Length >= 6)
            {
                AddSlider("CSM_0", MySector.NewPipelineSettings.CascadeDepthLoddings[0].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[0].MinLod = (int) Math.Round(x.Value));
                AddSlider("CSM_1", MySector.NewPipelineSettings.CascadeDepthLoddings[1].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[1].MinLod = (int) Math.Round(x.Value));
                AddSlider("CSM_2", MySector.NewPipelineSettings.CascadeDepthLoddings[2].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[2].MinLod = (int) Math.Round(x.Value));
                AddSlider("CSM_3", MySector.NewPipelineSettings.CascadeDepthLoddings[3].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[3].MinLod = (int) Math.Round(x.Value));
                AddSlider("CSM_4", MySector.NewPipelineSettings.CascadeDepthLoddings[4].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[4].MinLod = (int) Math.Round(x.Value));
                AddSlider("CSM_5", MySector.NewPipelineSettings.CascadeDepthLoddings[5].MinLod, 0, 6,
                    (x) => MySector.NewPipelineSettings.CascadeDepthLoddings[5].MinLod = (int) Math.Round(x.Value));
            }
            AddSlider("Single depth", MySector.NewPipelineSettings.SingleDepthLodding.MinLod, 0, 6, (x) => MySector.NewPipelineSettings.SingleDepthLodding.MinLod = (int)Math.Round(x.Value));

            AddLabel("The new pipeline - global", Color.White, 1f);
            AddSlider("Object distance mult", MySector.NewPipelineSettings.GlobalLodding.ObjectDistanceMult, 0.01f, 2, (x) => MySector.NewPipelineSettings.GlobalLodding.ObjectDistanceMult = x.Value);
            AddSlider("Object distance add", MySector.NewPipelineSettings.GlobalLodding.ObjectDistanceAdd, 0, 10, (x) => MySector.NewPipelineSettings.GlobalLodding.ObjectDistanceAdd = x.Value);
            AddCheckBox("Update lods", () => MySector.NewPipelineSettings.GlobalLodding.IsUpdateEnabled,
                (x) => MySector.NewPipelineSettings.GlobalLodding.IsUpdateEnabled = x);
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
            MyRenderProxy.UpdateNewPipelineSettings(MySector.NewPipelineSettings);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderLodding";
        }
    }
}
