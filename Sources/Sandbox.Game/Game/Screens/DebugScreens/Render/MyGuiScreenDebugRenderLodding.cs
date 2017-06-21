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
            AddSlider("GBuffer", MySector.Lodding.UserSettings.GBuffer.LodShift, 0, 6, (x) => MySector.Lodding.UserSettings.GBuffer.LodShift = (int)Math.Round(x.Value));
            if (MySector.Lodding.UserSettings.CascadeDepths.Length >= 6)
            {
                AddSlider("CSM_0", MySector.Lodding.UserSettings.CascadeDepths[0].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[0].LodShift = (int)Math.Round(x.Value));
                AddSlider("CSM_1", MySector.Lodding.UserSettings.CascadeDepths[1].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[1].LodShift = (int)Math.Round(x.Value));
                AddSlider("CSM_2", MySector.Lodding.UserSettings.CascadeDepths[2].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[2].LodShift = (int)Math.Round(x.Value));
                AddSlider("CSM_3", MySector.Lodding.UserSettings.CascadeDepths[3].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[3].LodShift = (int)Math.Round(x.Value));
                AddSlider("CSM_4", MySector.Lodding.UserSettings.CascadeDepths[4].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[4].LodShift = (int)Math.Round(x.Value));
                AddSlider("CSM_5", MySector.Lodding.UserSettings.CascadeDepths[5].LodShift, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[5].LodShift = (int)Math.Round(x.Value));
            }
            AddSlider("Single depth", MySector.Lodding.UserSettings.SingleDepth.LodShift, 0, 6, (x) => MySector.Lodding.UserSettings.SingleDepth.LodShift = (int)Math.Round(x.Value));


            AddLabel("The new pipeline - min lod", Color.White, 1f);
            AddSlider("GBuffer", MySector.Lodding.UserSettings.GBuffer.MinLod, 0, 6, (x) => MySector.Lodding.UserSettings.GBuffer.MinLod = (int)Math.Round(x.Value));
            if (MySector.Lodding.UserSettings.CascadeDepths.Length >= 6)
            {
                AddSlider("CSM_0", MySector.Lodding.UserSettings.CascadeDepths[0].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[0].MinLod = (int)Math.Round(x.Value));
                AddSlider("CSM_1", MySector.Lodding.UserSettings.CascadeDepths[1].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[1].MinLod = (int)Math.Round(x.Value));
                AddSlider("CSM_2", MySector.Lodding.UserSettings.CascadeDepths[2].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[2].MinLod = (int)Math.Round(x.Value));
                AddSlider("CSM_3", MySector.Lodding.UserSettings.CascadeDepths[3].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[3].MinLod = (int)Math.Round(x.Value));
                AddSlider("CSM_4", MySector.Lodding.UserSettings.CascadeDepths[4].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[4].MinLod = (int)Math.Round(x.Value));
                AddSlider("CSM_5", MySector.Lodding.UserSettings.CascadeDepths[5].MinLod, 0, 6,
                    (x) => MySector.Lodding.UserSettings.CascadeDepths[5].MinLod = (int)Math.Round(x.Value));
            }
            AddSlider("Single depth", MySector.Lodding.UserSettings.SingleDepth.MinLod, 0, 6, (x) => MySector.Lodding.UserSettings.SingleDepth.MinLod = (int)Math.Round(x.Value));

            AddLabel("The new pipeline - global", Color.White, 1f);
            AddSlider("Object distance mult", MySector.Lodding.UserSettings.Global.ObjectDistanceMult, 0.01f, 2, (x) => MySector.Lodding.UserSettings.Global.ObjectDistanceMult = x.Value);
            AddSlider("Object distance add", MySector.Lodding.UserSettings.Global.ObjectDistanceAdd, 0, 10, (x) => MySector.Lodding.UserSettings.Global.ObjectDistanceAdd = x.Value);
            AddCheckBox("Update lods", () => MySector.Lodding.UserSettings.Global.IsUpdateEnabled,
                (x) => MySector.Lodding.UserSettings.Global.IsUpdateEnabled = x);
            AddCheckBox("Display lod", MyRenderProxy.Settings.DisplayGbufferLOD, (x) => MyRenderProxy.Settings.DisplayGbufferLOD = x.IsChecked);
            AddCheckBox("Enable lod selection", MySector.Lodding.UserSettings.Global.EnableLodSelection,
                (x) => MySector.Lodding.UserSettings.Global.EnableLodSelection = x.IsChecked);
            AddSlider("Lod selection", MySector.Lodding.UserSettings.Global.LodSelection, 0, 5, (x) => MySector.Lodding.UserSettings.Global.LodSelection = (int)Math.Round(x.Value));
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
            MyRenderProxy.UpdateNewPipelineSettings(MySector.NewPipelineSettings);
            MySector.Lodding.ApplyUserSettings();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderLodding";
        }
    }
}
