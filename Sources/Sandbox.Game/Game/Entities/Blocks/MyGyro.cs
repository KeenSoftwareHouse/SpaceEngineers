#region Using

using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System.Reflection;
using System.Text;
using VRageMath;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Terminal.Controls;

using Sandbox.Engine.Utils;
using Sandbox.Engine.Physics;
using System.Diagnostics;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Gyro))]
    class MyGyro : MyFunctionalBlock, IMyGyro
    {
        private MyGyroDefinition m_gyroDefinition;
        private bool m_oldEmissiveState = false;
        private float m_gyroPower = 1f;

        private new MySyncGyro SyncObject;

        public bool IsPowered
        {
            get { return CubeGrid.GridSystems.GyroSystem.PowerReceiver.IsPowered; }
        }

        protected override bool CheckIsWorking()
        {
            return IsPowered && base.CheckIsWorking();
        }

        public float MaxGyroForce
        {
            get { return m_gyroDefinition.ForceMagnitude * m_gyroPower * m_gyroMultiplier; }
        }

        public float RequiredPowerInput
        {
            get { return m_gyroDefinition.RequiredPowerInput * m_gyroPower * m_powerConsumptionMultiplier; }
        }

        public float GyroPower
        {
            get { return m_gyroPower; }
            set
            {
                if (value != m_gyroPower)
                {
                    m_gyroPower = value;
                    UpdateText();
                }
            }
        }

        public bool GyroOverride { get; private set; }

        private Vector3 m_gyroOverrideVelocity;
        public Vector3 GyroOverrideVelocityGrid { get { return Vector3.TransformNormal(m_gyroOverrideVelocity, Orientation); } }

        static MyGyro()
        {
            var gyroPower = new MyTerminalControlSlider<MyGyro>("Power", MySpaceTexts.BlockPropertyTitle_GyroPower, MySpaceTexts.BlockPropertyDescription_GyroPower);
            gyroPower.Getter = (x) => x.GyroPower;
            gyroPower.Setter = (x, v) => { x.GyroPower = v; x.SyncObject.SendChangeGyroPowerRequest(v); };
            gyroPower.Writer = (x, result) => result.AppendInt32((int)(x.GyroPower * 100)).Append(" %");
            gyroPower.DefaultValue = 1;
            gyroPower.EnableActions(MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE);
            MyTerminalControlFactory.AddControl(gyroPower);

            if (MyFakes.ENABLE_GYRO_OVERRIDE)
            {
                var gyroOverride = new MyTerminalControlCheckbox<MyGyro>("Override", MySpaceTexts.BlockPropertyTitle_GyroOverride, MySpaceTexts.BlockPropertyDescription_GyroOverride);
                gyroOverride.Getter = (x) => x.GyroOverride;
                gyroOverride.Setter = (x, v) => { x.SetGyroOverride(v); x.SyncObject.SendGyroOverrideRequest(v); };
                gyroOverride.EnableAction();
                MyTerminalControlFactory.AddControl(gyroOverride);

                // Pitch = X axis, Yaw = Y axis, Roll = Z axis

                var gyroOverrideSliderY = new MyTerminalControlSlider<MyGyro>("Yaw", MySpaceTexts.BlockPropertyTitle_GyroYawOverride, MySpaceTexts.BlockPropertyDescription_GyroYawOverride);
                gyroOverrideSliderY.Getter = (x) => -x.m_gyroOverrideVelocity.Y;
                gyroOverrideSliderY.Setter = (x, v) => { SetGyroTorqueYaw(x, -v); x.SyncObject.SendGyroTorqueRequest(x.m_gyroOverrideVelocity); };
                gyroOverrideSliderY.Writer = (x, result) => result.AppendDecimal(x.m_gyroOverrideVelocity.Y * MathHelper.RadiansPerSecondToRPM, 2).Append(" RPM");
                gyroOverrideSliderY.Enabled = (x) => x.GyroOverride;
                gyroOverrideSliderY.DefaultValue = 0;
                gyroOverrideSliderY.SetDualLogLimits((x) => 0.01f * MathHelper.RPMToRadiansPerSecond, MaxAngularRadiansPerSecond, 0.05f);
                gyroOverrideSliderY.EnableActions(MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE);
                MyTerminalControlFactory.AddControl(gyroOverrideSliderY);

                var gyroOverrideSliderX = new MyTerminalControlSlider<MyGyro>("Pitch", MySpaceTexts.BlockPropertyTitle_GyroPitchOverride, MySpaceTexts.BlockPropertyDescription_GyroPitchOverride);
                gyroOverrideSliderX.Getter = (x) => x.m_gyroOverrideVelocity.X;
                gyroOverrideSliderX.Setter = (x, v) => { SetGyroTorquePitch(x, v); x.SyncObject.SendGyroTorqueRequest(x.m_gyroOverrideVelocity); };
                gyroOverrideSliderX.Writer = (x, result) => result.AppendDecimal(x.m_gyroOverrideVelocity.X * MathHelper.RadiansPerSecondToRPM, 2).Append(" RPM");
                gyroOverrideSliderX.Enabled = (x) => x.GyroOverride;
                gyroOverrideSliderX.DefaultValue = 0;
                gyroOverrideSliderX.SetDualLogLimits((x) => 0.01f * MathHelper.RPMToRadiansPerSecond, MaxAngularRadiansPerSecond, 0.05f);
                gyroOverrideSliderX.EnableActions(MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE);
                MyTerminalControlFactory.AddControl(gyroOverrideSliderX);

                var gyroOverrideSliderZ = new MyTerminalControlSlider<MyGyro>("Roll", MySpaceTexts.BlockPropertyTitle_GyroRollOverride, MySpaceTexts.BlockPropertyDescription_GyroRollOverride);
                gyroOverrideSliderZ.Getter = (x) => -x.m_gyroOverrideVelocity.Z;
                gyroOverrideSliderZ.Setter = (x, v) => { SetGyroTorqueRoll(x, -v); x.SyncObject.SendGyroTorqueRequest(x.m_gyroOverrideVelocity); };
                gyroOverrideSliderZ.Writer = (x, result) => result.AppendDecimal(x.m_gyroOverrideVelocity.Z * MathHelper.RadiansPerSecondToRPM, 2).Append(" RPM");
                gyroOverrideSliderZ.Enabled = (x) => x.GyroOverride;
                gyroOverrideSliderZ.DefaultValue = 0;
                gyroOverrideSliderZ.SetDualLogLimits((x) => 0.01f * MathHelper.RPMToRadiansPerSecond, MaxAngularRadiansPerSecond, 0.05f);
                gyroOverrideSliderZ.EnableActions(MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE);
                MyTerminalControlFactory.AddControl(gyroOverrideSliderZ);
            }
        }

        static float MaxAngularRadiansPerSecond(MyGyro gyro)
        {
            if (gyro.m_gyroDefinition.CubeSize == MyCubeSize.Small)
                return MyGridPhysics.GetSmallShipMaxAngularVelocity();
            else
            {
                Debug.Assert(gyro.m_gyroDefinition.CubeSize == MyCubeSize.Large, "Maximal grid velocity not defined for other grids than small/large");
                return MyGridPhysics.GetLargeShipMaxAngularVelocity();
            }
        }

        public MyGyro()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            m_gyroDefinition = (MyGyroDefinition)BlockDefinition;
            var ob = objectBuilder as MyObjectBuilder_Gyro;
            m_gyroPower = ob.GyroPower;

            if (MyFakes.ENABLE_GYRO_OVERRIDE)
            {
                GyroOverride = ob.GyroOverride;
                m_gyroOverrideVelocity = ob.TargetAngularVelocity;
            }

            SyncObject = new MySyncGyro(this);
            UpdateText();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_Gyro;
            ob.GyroPower = m_gyroPower;
            ob.GyroOverride = GyroOverride;
            ob.TargetAngularVelocity = m_gyroOverrideVelocity;
            return ob;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(RequiredPowerInput, DetailedInfo);
            RaisePropertiesChanged();
        }

        private void UpdateEmissivity()
        {
            if (Enabled && IsPowered)
            {
                if (!m_oldEmissiveState)
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                    m_oldEmissiveState = true;
                }
            }
            else
            {
                if (m_oldEmissiveState)
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White); ;
                    m_oldEmissiveState = false;
                }
            }
        }

        public void SetGyroOverride(bool value)
        {
            if (value != GyroOverride)
            {
                GyroOverride = value;
                RaisePropertiesChanged();
                CubeGrid.GridSystems.GyroSystem.MarkDirty();
            }
        }
        
        private static void SetGyroTorqueYaw(MyGyro gyro, float yawValue)
        {
            var torque = gyro.m_gyroOverrideVelocity;
            torque.Y = yawValue;
            gyro.SetGyroTorque(torque);
        }

        private static void SetGyroTorquePitch(MyGyro gyro, float pitchValue)
        {
            var torque = gyro.m_gyroOverrideVelocity;
            torque.X = pitchValue;
            gyro.SetGyroTorque(torque);
        }

        private static void SetGyroTorqueRoll(MyGyro gyro, float rollValue)
        {
            var torque = gyro.m_gyroOverrideVelocity;
            torque.Z = rollValue;
            gyro.SetGyroTorque(torque);
        }

        public void SetGyroTorque(Vector3 torque)
        {
            m_gyroOverrideVelocity = torque;
            CubeGrid.GridSystems.GyroSystem.MarkDirty();
        }
        float Sandbox.ModAPI.Ingame.IMyGyro.Yaw { get { return -m_gyroOverrideVelocity.Y; } }
        float Sandbox.ModAPI.Ingame.IMyGyro.Pitch { get { return m_gyroOverrideVelocity.X; } }
        float Sandbox.ModAPI.Ingame.IMyGyro.Roll { get { return -m_gyroOverrideVelocity.Z; } }

        private float m_gyroMultiplier = 1f;
        float Sandbox.ModAPI.IMyGyro.GyroStrengthMultiplier
        {
            get
            {
                return m_gyroMultiplier;
            }
            set
            {
                m_gyroMultiplier = value;
                if (m_gyroMultiplier < 0.01f)
                {
                    m_gyroMultiplier = 0.01f;
                }
                if (CubeGrid.GridSystems.GyroSystem != null)
                {
                    CubeGrid.GridSystems.GyroSystem.MarkDirty();
                }
            }
        }

        private float m_powerConsumptionMultiplier = 1f;
        float Sandbox.ModAPI.IMyGyro.PowerConsumptionMultiplier
        {
            get
            {
                return m_powerConsumptionMultiplier;
            }
            set
            {
                m_powerConsumptionMultiplier = value;
                if (m_powerConsumptionMultiplier < 0.01f)
                {
                    m_powerConsumptionMultiplier = 0.01f;
                }

                if (CubeGrid.GridSystems.GyroSystem != null)
                {
                    CubeGrid.GridSystems.GyroSystem.MarkDirty();
                }

                UpdateText();
            }
        }
    }
}

