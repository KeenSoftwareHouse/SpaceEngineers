using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication.History;
using VRageMath;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("VRage", "Network Prediction")]
    class MyGuiScreenDebugNetworkPrediction : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_animationComboA;
        MyGuiControlCombobox m_animationComboB;
        MyGuiControlSlider m_blendSlider;

        MyGuiControlCombobox m_animationCombo;
        MyGuiControlCheckbox m_loopCheckbox;

        public MyGuiScreenDebugNetworkPrediction()
        {
            RecreateControls(true);
        }

        const float m_forcedPriority = 1;

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Network Prediction", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f;

            if (MyMultiplayer.Static != null)
            {
                AddCheckBox("Set transform corrections", MyPredictedSnapshotSync.SetTransformCorrections,
                    (x) => MyPredictedSnapshotSync.SetTransformCorrections = x.IsChecked);
                AddCheckBox("Set physics corrections", MyPredictedSnapshotSync.SetPhysicsCorrections,
                    (x) => MyPredictedSnapshotSync.SetPhysicsCorrections = x.IsChecked);
                m_currentPosition.Y += 0.01f;

                AddSlider("Velocity change to reset", MyPredictedSnapshotSync.MinVelocityChangeToReset, 0, 30.0f,
                    (slider) => { MyPredictedSnapshotSync.MinVelocityChangeToReset = slider.Value; });
                m_currentPosition.Y += 0.01f;

                AddSlider("Delta factor", MyPredictedSnapshotSync.DeltaFactor, 0, 1.0f,
                    (slider) => { MyPredictedSnapshotSync.DeltaFactor = slider.Value; });
                AddSlider("Smooth iterations", MyPredictedSnapshotSync.SmoothTimesteps, 0, 1000.0f,
                    (slider) => { MyPredictedSnapshotSync.SmoothTimesteps = (int)slider.Value; });
                AddCheckBox("Smooth position corrections", MyPredictedSnapshotSync.SmoothPositionCorrection,
                    (x) => MyPredictedSnapshotSync.SmoothPositionCorrection = x.IsChecked);
                AddSlider("Minimum pos delta", MyPredictedSnapshotSync.MinPositionDelta, 0, 0.5f,
                    (slider) => { MyPredictedSnapshotSync.MinPositionDelta = slider.Value; });
                AddSlider("Maximum pos delta", MyPredictedSnapshotSync.MaxPositionDelta, 0, 5.0f,
                    (slider) => { MyPredictedSnapshotSync.MaxPositionDelta = slider.Value; });
                AddSlider("Reference linear velocity", MyPredictedSnapshotSync.ReferenceLinearVelocity, 0, 100.0f,
                    (slider) => { MyPredictedSnapshotSync.ReferenceLinearVelocity = slider.Value; });
                AddCheckBox("Smooth linear velocity corrections", MyPredictedSnapshotSync.SmoothLinearVelocityCorrection,
                    (x) => MyPredictedSnapshotSync.SmoothLinearVelocityCorrection = x.IsChecked);
                AddSlider("Minimum linVel delta", MyPredictedSnapshotSync.MinLinearVelocityDelta, 0, 0.5f,
                    (slider) => { MyPredictedSnapshotSync.MinLinearVelocityDelta = slider.Value; });
                AddSlider("Maximum linVel delta", MyPredictedSnapshotSync.MaxLinearVelocityDelta, 0, 5.0f,
                    (slider) => { MyPredictedSnapshotSync.MaxLinearVelocityDelta = slider.Value; });
                AddCheckBox("Smooth rotation corrections", MyPredictedSnapshotSync.SmoothRotationCorrection,
                    (x) => MyPredictedSnapshotSync.SmoothRotationCorrection = x.IsChecked);
                AddSlider("Minimum angle delta", MathHelper.ToDegrees(MyPredictedSnapshotSync.MinRotationAngle), 0, 90.0f,
                    (slider) => { MyPredictedSnapshotSync.MinRotationAngle = MathHelper.ToRadians(slider.Value); });
                AddSlider("Maximum angle delta", MathHelper.ToDegrees(MyPredictedSnapshotSync.MaxRotationAngle), 0, 90.0f,
                    (slider) => { MyPredictedSnapshotSync.MaxRotationAngle = MathHelper.ToRadians(slider.Value); });
                AddSlider("Reference angular velocity", MyPredictedSnapshotSync.ReferenceAngularVelocity, 0, 6.0f,
                    (slider) => { MyPredictedSnapshotSync.ReferenceAngularVelocity = slider.Value; });
                AddCheckBox("Smooth angular velocity corrections", MyPredictedSnapshotSync.SmoothAngularVelocityCorrection,
                    (x) => MyPredictedSnapshotSync.SmoothAngularVelocityCorrection = x.IsChecked);
                AddSlider("Minimum angle delta", MyPredictedSnapshotSync.MinAngularVelocityDelta, 0, 1.0f,
                    (slider) => { MyPredictedSnapshotSync.MinAngularVelocityDelta = slider.Value; });
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugNetworkPrediction";
        }
    }
}
