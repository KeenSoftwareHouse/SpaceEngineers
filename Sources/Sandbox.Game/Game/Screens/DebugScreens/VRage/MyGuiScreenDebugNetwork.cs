using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication.History;
using VRageMath;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("VRage", "Network")]
    class MyGuiScreenDebugNetwork : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_animationComboA;
        MyGuiControlCombobox m_animationComboB;
        MyGuiControlSlider m_blendSlider;

        MyGuiControlCombobox m_animationCombo;
        MyGuiControlCheckbox m_loopCheckbox;

        public MyGuiScreenDebugNetwork()
        {
            RecreateControls(true);
        }

        const float m_forcedPriority = 1;

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Network", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            if (MyMultiplayer.Static != null)
            {
                // client
                AddSlider("Priority multiplier", m_forcedPriority, 0, 16.0f,
                    (slider) => { MyMultiplayer.RaiseStaticEvent(x => MyMultiplayerBase.OnSetPriorityMultiplier, slider.Value); });
                m_currentPosition.Y += 0.01f;
                
                //AddSlider("Smooth ping", MyMultiplayer.Static.ReplicationLayer.UseSmoothPing, 0, 16.0f,
                  //  (slider) => { MyMultiplayer.RaiseStaticEvent(x => MyMultiplayerBase.OnSetPriorityMultiplier, slider.Value); });
                AddCheckBox("Smooth ping", MyMultiplayer.Static.ReplicationLayer.UseSmoothPing, 
                    (x) => MyMultiplayer.Static.ReplicationLayer.UseSmoothPing = x.IsChecked);
                AddSlider("Ping smooth factor", MyMultiplayer.Static.ReplicationLayer.PingSmoothFactor, 0, 3.0f,
                    (slider) => { MyMultiplayer.Static.ReplicationLayer.PingSmoothFactor = slider.Value; });
                AddSlider("Timestamp correction minimum", (float)MyMultiplayer.Static.ReplicationLayer.TimestampCorrectionMinimum, 0, 100.0f,
                    (slider) => { MyMultiplayer.Static.ReplicationLayer.TimestampCorrectionMinimum = (int)slider.Value; });
                AddCheckBox("Smooth timestamp correction", MyMultiplayer.Static.ReplicationLayer.UseSmoothCorrection,
                    (x) => MyMultiplayer.Static.ReplicationLayer.UseSmoothCorrection = x.IsChecked);
                AddSlider("Smooth timestamp correction amplitude", (float)MyMultiplayer.Static.ReplicationLayer.SmoothCorrectionAmplitude, 0, 5.0f,
                    (slider) => { MyMultiplayer.Static.ReplicationLayer.SmoothCorrectionAmplitude = (int)slider.Value; });
                AddCheckBox("Apply time correction", VRage.Network.MyReplicationClient.ApplyCorrectionsDebug,
                    (x) => VRage.Network.MyReplicationClient.ApplyCorrectionsDebug = x.IsChecked);
                m_currentPosition.Y += 0.01f;
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugNetwork";
        }
    }
}
