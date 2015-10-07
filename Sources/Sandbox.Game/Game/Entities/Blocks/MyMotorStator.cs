using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Debugging;
using Sandbox.Game.GameSystems.Electricity;

using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.GameSystems.Conveyors;

namespace Sandbox.Game.Entities.Cube
{
    using Sandbox.Engine.Models;
    using VRage.Groups;
    using Sandbox.Game.Screens.Terminal.Controls;
    using Sandbox.ModAPI.Ingame;
    using Sandbox.Game.Localization;
    using VRage;
    using VRage.ModAPI;
    using VRage.Network;
    using Sandbox.Engine.Multiplayer;

    [MyCubeBlockType(typeof(MyObjectBuilder_MotorStator))]
    class MyMotorStator : MyMotorBase, IMyConveyorEndpointBlock, Sandbox.ModAPI.IMyMotorStator
    {
        const float NormalizedToRadians = (float)(2.0f * Math.PI);
        const float DegreeToRadians = (float)(Math.PI / 180.0f);

        private HkVelocityConstraintMotor m_motor;

        public readonly Sync<float> Torque;
        public readonly Sync<float> BrakingTorque;
        public readonly Sync<float> TargetVelocity;
        private readonly Sync<float> m_minAngle;
        private readonly Sync<float> m_maxAngle;
        private bool m_limitsActive;
        private bool m_isAttached = false;
        protected bool m_canBeDetached = false;

        private float m_currentAngle;

        static MyMotorStator()
        {
            var reverse = new MyTerminalControlButton<MyMotorStator>("Reverse", MySpaceTexts.BlockActionTitle_Reverse, MySpaceTexts.Blank, (b) => b.TargetVelocityRPM = -b.TargetVelocityRPM);
            reverse.EnableAction(MyTerminalActionIcons.REVERSE);
            MyTerminalControlFactory.AddControl(reverse);

            var detach = new MyTerminalControlButton<MyMotorStator>("Detach", MySpaceTexts.BlockActionTitle_Detach, MySpaceTexts.Blank, (b) => MyMultiplayer.RaiseEvent(b, x => x.Detach_Implementation));

            var actionDetach = detach.EnableAction(MyTerminalActionIcons.NONE);
            actionDetach.Enabled = (b) => b.m_canBeDetached;
            detach.Enabled = (b) => b.m_isAttached;
            detach.Visible = (b) => b.m_canBeDetached;
            MyTerminalControlFactory.AddControl(detach);

            var attach = new MyTerminalControlButton<MyMotorStator>("Attach", MySpaceTexts.BlockActionTitle_Attach, MySpaceTexts.Blank, (b) => MyMultiplayer.RaiseEvent(b, x => x.Attach_Implementation));
            var actionAttach = attach.EnableAction(MyTerminalActionIcons.NONE);
            actionAttach.Enabled = (b) => b.m_canBeDetached;
            attach.Enabled = (b) => !b.m_isAttached;
            attach.Visible = (b) => b.m_canBeDetached;
            MyTerminalControlFactory.AddControl(attach);

            var torque = new MyTerminalControlSlider<MyMotorStator>("Torque", MySpaceTexts.BlockPropertyTitle_MotorTorque, MySpaceTexts.BlockPropertyDescription_MotorTorque);
            torque.Getter = (x) => x.Torque;
            torque.Setter = (x, v) => x.Torque.Value = v;
            torque.DefaultValueGetter = (x) => x.MotorDefinition.MaxForceMagnitude;
            torque.Writer = (x, result) => MyValueFormatter.AppendTorqueInBestUnit(x.Torque, result);
            torque.EnableActions();
            torque.Denormalizer = (x, v) => x.DenormalizeTorque(v);
            torque.Normalizer = (x, v) => x.NormalizeTorque(v);
            MyTerminalControlFactory.AddControl(torque);

            var brakingTorque = new MyTerminalControlSlider<MyMotorStator>("BrakingTorque", MySpaceTexts.BlockPropertyTitle_MotorBrakingTorque, MySpaceTexts.BlockPropertyDescription_MotorBrakingTorque);
            brakingTorque.Getter = (x) => x.BrakingTorque;
            brakingTorque.Setter = (x, v) => x.BrakingTorque.Value = v;
            brakingTorque.DefaultValue = 0;
            brakingTorque.Writer = (x, result) => MyValueFormatter.AppendTorqueInBestUnit(x.BrakingTorque, result);
            brakingTorque.EnableActions();
            brakingTorque.Denormalizer = (x, v) => x.DenormalizeTorque(v);
            brakingTorque.Normalizer = (x, v) => x.NormalizeTorque(v);
            MyTerminalControlFactory.AddControl(brakingTorque);

            var targetVelocity = new MyTerminalControlSlider<MyMotorStator>("Velocity", MySpaceTexts.BlockPropertyTitle_MotorTargetVelocity, MySpaceTexts.BlockPropertyDescription_MotorVelocity);
            targetVelocity.Getter = (x) => x.TargetVelocityRPM;
            targetVelocity.Setter = (x, v) => x.TargetVelocityRPM = v;
            targetVelocity.DefaultValue = 0;
            targetVelocity.Writer = (x, result) => result.Concat(x.TargetVelocityRPM, 2).Append(" rpm");
            targetVelocity.EnableActionsWithReset();
            targetVelocity.Denormalizer = (x, v) => x.DenormalizeRPM(v);
            targetVelocity.Normalizer = (x, v) => x.NormalizeRPM(v);
            MyTerminalControlFactory.AddControl(targetVelocity);

            var lowerLimit = new MyTerminalControlSlider<MyMotorStator>("LowerLimit", MySpaceTexts.BlockPropertyTitle_MotorMinAngle, MySpaceTexts.BlockPropertyDescription_MotorLowerLimit);
            lowerLimit.Getter = (x) => x.MinAngle;
            lowerLimit.Setter = (x, v) => x.MinAngle = v;
            lowerLimit.DefaultValue = -361;
            lowerLimit.SetLimits(-361, 360);
            lowerLimit.Writer = (x, result) => WriteAngle(x.m_minAngle, result);
            lowerLimit.EnableActions();
            MyTerminalControlFactory.AddControl(lowerLimit);

            var upperLimit = new MyTerminalControlSlider<MyMotorStator>("UpperLimit", MySpaceTexts.BlockPropertyTitle_MotorMaxAngle, MySpaceTexts.BlockPropertyDescription_MotorUpperLimit);
            upperLimit.Getter = (x) => x.MaxAngle;
            upperLimit.Setter = (x, v) => x.MaxAngle = v;
            upperLimit.DefaultValue = 361;
            upperLimit.SetLimits(-360, 361);
            upperLimit.Writer = (x, result) => WriteAngle(x.m_maxAngle, result);
            upperLimit.EnableActions();
            MyTerminalControlFactory.AddControl(upperLimit);

            var rotorDisplacement = new MyTerminalControlSlider<MyMotorStator>("Displacement", MySpaceTexts.BlockPropertyTitle_MotorRotorDisplacement, MySpaceTexts.BlockPropertyDescription_MotorRotorDisplacement);
            rotorDisplacement.Getter = (x) => x.DummyDisplacement;
            rotorDisplacement.Setter = (x, v) => x.DummyDisplacement = v;
            rotorDisplacement.DefaultValueGetter = (x) => 0.0f;
            rotorDisplacement.SetLimits((x) => x.MotorDefinition.RotorDisplacementMin, (x) => x.MotorDefinition.RotorDisplacementMax);
            rotorDisplacement.Writer = (x, result) => MyValueFormatter.AppendDistanceInBestUnit(x.DummyDisplacement, result);
            rotorDisplacement.Enabled = (b) => b.m_isAttached;
            rotorDisplacement.EnableActions();
            MyTerminalControlFactory.AddControl(rotorDisplacement);
        }

        private float NormalizeRPM(float v)
        {
            return (v / (MaxRotorAngularVelocity * MathHelper.RadiansPerSecondToRPM)) / 2 + 0.5f;
        }

        private float DenormalizeRPM(float v)
        {
            return (v - 0.5f) * 2 * (MaxRotorAngularVelocity * MathHelper.RadiansPerSecondToRPM);
        }

        #region Terminal properties and value writers

        public static void WriteAngle(float angleRad, StringBuilder result)
        {
            if (float.IsInfinity(angleRad))
                result.Append(MyTexts.Get(MySpaceTexts.BlockPropertyValue_MotorAngleUnlimited));
            else
                result.Concat(MathHelper.ToDegrees(angleRad), 0).Append("°");
        }

        private float NormalizeTorque(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.InterpLogInv(value, 1f, MotorDefinition.MaxForceMagnitude);
        }

        private float DenormalizeTorque(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.InterpLog(value, 1f, MotorDefinition.MaxForceMagnitude);
        }

        public float TargetVelocityRPM
        {
            get { return TargetVelocity * MathHelper.RadiansPerSecondToRPM; }
            set { TargetVelocity.Value = value * MathHelper.RPMToRadiansPerSecond; }
        }

        public float MinAngle
        {
            get { return m_minAngle / DegreeToRadians; }
            set { m_minAngle.Value = value * DegreeToRadians; }
        }

        public float MaxAngle
        {
            get { return m_maxAngle / DegreeToRadians; }
            set { m_maxAngle.Value = value * DegreeToRadians; }
        }
        #endregion

        private static readonly float MIN_LOWER_LIMIT = -NormalizedToRadians - MathHelper.ToRadians(0.5f);

        private static readonly float MAX_UPPER_LIMIT = NormalizedToRadians + MathHelper.ToRadians(0.5f);

        void UpdateText()
        {
            if (SafeConstraint != null)
            {
                DetailedInfo.Clear();
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MotorCurrentAngle)).AppendDecimal(MathHelper.ToDegrees(m_currentAngle), 0).Append("°");

                if (!m_limitsActive && !(float.IsNegativeInfinity(m_minAngle) && float.IsPositiveInfinity(m_maxAngle)))
                {
                    DetailedInfo.Append(Environment.NewLine);
                    DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MotorLimitsDisabled));
                }

                RaisePropertiesChanged();
            }
        }

        void ScaleDown()
        {
            while (m_currentAngle > MAX_UPPER_LIMIT)
            {
                m_currentAngle -= 2 * (float)Math.PI;
            }
            SetAngleToPhysics();
        }

        void ScaleUp()
        {
            while (m_currentAngle < MIN_LOWER_LIMIT)
            {
                m_currentAngle += 2 * (float)Math.PI;
            }
            SetAngleToPhysics();
        }

        void SyncAngle(float angle)
        {
            m_currentAngle = angle;
            SetAngleToPhysics();
        }

        void SetAngleToPhysics()
        {
            if (SafeConstraint != null)
            {
                HkLimitedHingeConstraintData.SetCurrentAngle(SafeConstraint, m_currentAngle);
            }
        }

        void FixAngles(bool lowerIsFixed)
        {
            // When out of limits get to limits
            if (m_currentAngle < m_minAngle)
            {
                ScaleUp();
            }

            if (m_currentAngle > m_maxAngle)
            {
                ScaleDown();
            }

            // Min must be smaller than max
            if (m_maxAngle < m_minAngle)
            {
                if (lowerIsFixed)
                    m_maxAngle.Value = m_minAngle;
                else
                    m_minAngle.Value = m_maxAngle;
            }

            if (m_minAngle < MIN_LOWER_LIMIT)
            {
                m_minAngle.Value = float.NegativeInfinity;
            }

            if (m_maxAngle > MAX_UPPER_LIMIT)
            {
                m_maxAngle.Value = float.PositiveInfinity;
            }

            m_limitsActive = false;
            TryActivateLimits();
            if (SafeConstraint != null)
            {
                m_currentAngle = HkLimitedHingeConstraintData.GetCurrentAngle(SafeConstraint);
            }
            UpdateText();

            RaisePropertiesChanged();
        }

        float MoveUp(float numberToMove, float minimum, float moveByMultipleOf)
        {
            while (numberToMove < minimum)
            {
                numberToMove += moveByMultipleOf;
            }
            return numberToMove;
        }

        float MoveDown(float numberToMove, float maximum, float moveByMultipleOf)
        {
            while (numberToMove > maximum)
            {
                numberToMove -= moveByMultipleOf;
            }
            return numberToMove;
        }

        void TryActivateLimits()
        {
            if (float.IsNegativeInfinity(m_minAngle) && float.IsPositiveInfinity(m_maxAngle))
            {
                m_currentAngle = MoveUp(m_currentAngle, 0, MathHelper.TwoPi);
                m_currentAngle = MoveDown(m_currentAngle, MathHelper.TwoPi, MathHelper.TwoPi);
                SetAngleToPhysics();
                m_limitsActive = false;
            }
            else if (!m_limitsActive)
            {
                const float graceDegrees = 5;
                float minimum = m_minAngle - MathHelper.ToRadians(graceDegrees);
                float maximum = m_maxAngle + MathHelper.ToRadians(graceDegrees);
                float angle = m_currentAngle;

                if (angle < minimum)
                {
                    // Change angle to same or smallest bigger than m_minAngle
                    angle = MoveUp(angle, minimum, MathHelper.TwoPi);
                }
                else if (angle > maximum)
                {
                    // Change angle to same or largest smaller than m_maxAngle
                    angle = MoveDown(angle, maximum, MathHelper.TwoPi);
                }

                if (angle >= minimum && angle <= maximum)
                {
                    m_limitsActive = true;
                    m_currentAngle = angle;
                    SetAngleToPhysics();
                    return;
                }
            }
        }

        #region Construction and serialization
        public MyMotorStator()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_canBeDetached = true;

            SyncType.PropertyChanged += SyncType_PropertyChanged;
        }

        void SyncType_PropertyChanged(SyncBase obj)
        {
            if (obj == m_dummyDisplacement && m_constraint != null)
                Reattach();
        }

        [Event, Reliable, Server, Broadcast]
        private void Attach_Implementation()
        {
            // Client calls this on server using terminal, server performs AttachRotor and broadcasts it to all clients
            AttachRotor();
        }

        [Event, Reliable, Server, Broadcast]
        private void Detach_Implementation()
        {
            // Client calls this on server using terminal, server performs DetachRotor and broadcasts it to all clients
            DummyDisplacement = 0.0f;
            DetachRotor();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_MotorStator)objectBuilder;
            Torque.Value = MathHelper.Clamp(DenormalizeTorque(ob.Force), 0f, MotorDefinition.MaxForceMagnitude);
            BrakingTorque.Value = MathHelper.Clamp(DenormalizeTorque(ob.Friction), 0f, MotorDefinition.MaxForceMagnitude);
            TargetVelocity.Value = MathHelper.Clamp(ob.TargetVelocity * MaxRotorAngularVelocity, -MaxRotorAngularVelocity, MaxRotorAngularVelocity);
            m_minAngle.Value = ob.MinAngle ?? float.NegativeInfinity;
            m_maxAngle.Value = ob.MaxAngle ?? float.PositiveInfinity;
            m_limitsActive = ob.LimitsActive;
            m_currentAngle = ob.CurrentAngle;

            DummyDisplacement = ob.DummyDisplacement;
            // We have to limit the displacement, because default value for small rotors is too large
            if (DummyDisplacement < MotorDefinition.RotorDisplacementMin) DummyDisplacement = MotorDefinition.RotorDisplacementMin;
            if (DummyDisplacement > MotorDefinition.RotorDisplacementMax) DummyDisplacement = MotorDefinition.RotorDisplacementMax;

            AddDebugRenderComponent(new Components.MyDebugRenderComponentMotorStator(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_MotorStator)base.GetObjectBuilderCubeBlock(copy);
            ob.Force = NormalizeTorque(Torque);
            ob.Friction = NormalizeTorque(BrakingTorque);
            ob.TargetVelocity = TargetVelocity / MaxRotorAngularVelocity;
            ob.MinAngle = float.IsNegativeInfinity(m_minAngle) ? (float?)null : m_minAngle;
            ob.MaxAngle = float.IsPositiveInfinity(m_maxAngle) ? (float?)null : m_maxAngle;
            ob.CurrentAngle = m_currentAngle;
            ob.LimitsActive = m_limitsActive;
            ob.DummyDisplacement = DummyDisplacement;
            return ob;
        }
        #endregion


        float GetAngle(Quaternion q, Vector3 axis)
        {
            float a2 = 2 * (float)Math.Atan2(new Vector3(q.X, q.Y, q.Z).Length(), q.W);
            var vec = new Vector3(q.X, q.Y, q.Z) / (float)Math.Sin(a2 / 2);
            vec = a2 == 0 ? Vector3.Zero : vec;
            a2 *= Vector3.Dot(vec, axis);
            return a2;
        }

        private bool CheckVelocities()
        {
            if (!MyFakes.WELD_ROTORS)
                return false;
            if (CubeGrid.Physics == null)
                return false;
            if (CubeGrid.Physics.LinearVelocity.Length() > 40)
            {
                if (m_welded)
                    return true;
                else
                {
                    if (m_rotorGrid == null)
                    {
                        if (MyEntities.TryGetEntityById<MyMotorRotor>(m_rotorBlockId, out m_rotorBlock))
                            m_rotorGrid = m_rotorBlock.CubeGrid;
                    }
                }
                if (m_rotorGrid != null && MyWeldingGroups.Static.GetGroup(CubeGrid) != MyWeldingGroups.Static.GetGroup(m_rotorGrid))
                {
                    m_rotorBlockId = m_rotorBlock.EntityId;
                    var topGrid = m_rotorGrid;
                    Detach(reattach: false);
                    MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, topGrid);
                    m_welded = true;
                    return true;
                }
                return false;
            }

            if (CubeGrid.Physics.LinearVelocity.Length() < 39 && m_welded)
            {
                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, m_rotorGrid);
                m_welded = false;
                //if (m_topGrid == null && m_topBlockId != 0)
                //    UpdateOnceBeforeFrame();
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            if (m_welded)
                return true;
            return false;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (MyFakes.REPORT_INVALID_ROTORS)
            {
                //Debug.Assert(m_constraint != null, "Rotor constraint is not created although it should be!");
            }
            if (CheckVelocities())
                return;

            if (m_rotorGrid == null || SafeConstraint == null)
                return;
            if(SafeConstraint.RigidBodyA == SafeConstraint.RigidBodyB) //welded
            {
                SafeConstraint.Enabled = false;
                return;
            }

            var oldAngle = m_currentAngle;
            m_currentAngle = HkLimitedHingeConstraintData.GetCurrentAngle(SafeConstraint);
            if (oldAngle != m_currentAngle)
            {
                UpdateText();
            }
            var data = (HkLimitedHingeConstraintData)m_constraint.ConstraintData;
            data.MaxFrictionTorque = BrakingTorque;

            TryActivateLimits();

            if (!m_limitsActive)
            {
                data.DisableLimits();
            }
            else if (!data.MinAngularLimit.IsEqual(m_minAngle) || !data.MaxAngularLimit.IsEqual(m_maxAngle))
            {
                data.MinAngularLimit = m_minAngle;
                data.MaxAngularLimit = m_maxAngle;

                // Activate even when motor is stopped, so it fixes it's limits
                CubeGrid.Physics.RigidBody.Activate();
                m_rotorGrid.Physics.RigidBody.Activate();
            }
            if (m_limitsActive)
            {
                var handle = LimitReached;
                if (handle != null)
                {
                    if (oldAngle > data.MinAngularLimit && m_currentAngle <= data.MinAngularLimit)
                        handle(false);
                    if (oldAngle < data.MaxAngularLimit && m_currentAngle >= data.MaxAngularLimit)
                        handle(true);
                }
            }

            m_motor.MaxForce = Torque;
            m_motor.MinForce = -Torque;
            m_motor.VelocityTarget = TargetVelocity * Sync.RelativeSimulationRatio;

            bool motorRunning = IsWorking;
            if (data.MotorEnabled != motorRunning)
            {
                data.SetMotorEnabled(m_constraint, motorRunning);
            }

            if (motorRunning && m_rotorGrid != null && !m_motor.VelocityTarget.IsZero())
            {
                CubeGrid.Physics.RigidBody.Activate();
                m_rotorGrid.Physics.RigidBody.Activate();
            }
        }

        public override bool Attach(MyMotorRotor rotor, bool updateSync = false, bool updateGroup = true)
        {
            if (CubeGrid.Physics == null || SafeConstraint != null)
                return false;

            Debug.Assert(SafeConstraint == null);

            if (CubeGrid.Physics.Enabled && rotor != null)
            {
                m_rotorBlock = rotor;
                m_rotorBlockId = rotor.EntityId;

                if (updateSync)
                    SyncObject.AttachRotor(m_rotorBlock);

                m_rotorGrid = m_rotorBlock.CubeGrid;
                if (m_rotorGrid.Physics == null)
                    return false;
                var rotorBody = m_rotorGrid.Physics.RigidBody;
                var data = new HkLimitedHingeConstraintData();
                m_motor = new HkVelocityConstraintMotor(1.0f, 1000000f);

                data.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
                data.Motor = m_motor;
                data.DisableLimits();

                var posA = DummyPosition;
                var posB = rotor.Position * rotor.CubeGrid.GridSize;
                var axisA = PositionComp.LocalMatrix.Up;
                var axisAPerp = PositionComp.LocalMatrix.Forward;
                var axisB = rotor.PositionComp.LocalMatrix.Up;
                var axisBPerp = rotor.PositionComp.LocalMatrix.Forward;
                data.SetInBodySpace(posA, posB, axisA, axisB, axisAPerp, axisBPerp, CubeGrid.Physics, m_rotorGrid.Physics);
                m_constraint = new HkConstraint(CubeGrid.Physics.RigidBody, rotorBody, data);

                m_constraint.WantRuntime = true;
                CubeGrid.Physics.AddConstraint(m_constraint);
                m_constraint.Enabled = true;

                SetAngleToPhysics();

                m_rotorBlock.Attach(this);

                if (updateGroup)
                {
                    OnConstraintAdded(GridLinkTypeEnum.Physical, m_rotorGrid);
                    OnConstraintAdded(GridLinkTypeEnum.Logical, m_rotorGrid);
                }
                m_isAttached = true;
                m_rotorGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;
                return true;
            }

            return false;
        }

        public override bool Detach(bool updateGroup = true, bool reattach = true)
        {
            m_isAttached = false;
            if (m_constraint == null)
                return false;
            Debug.Assert(m_motor != null);
            m_motor.Dispose();
            base.Detach(updateGroup, reattach);
            
            return true;
        }

        protected override float GetModelDummyDisplacement()
        {
            return MotorDefinition.RotorDisplacementInModel;
        }

        protected MyAttachableConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        event Action<bool> LimitReached;
        private bool m_welded;
        event Action<bool> Sandbox.ModAPI.IMyMotorStator.LimitReached
        {
            add { LimitReached += value; }
            remove { LimitReached -= value; }
        }

        public bool CanDebugDraw()
        {
            return (m_rotorGrid != null && m_rotorGrid.Physics != null);
        }

        public void AttachRotor()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public void DetachRotor()
        {
            NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            m_rotorBlockId = 0;
            //do not reattach motor when user selected to detach rotor
            this.Detach(true, false);
        }

        #region Motor API interface
        bool IMyMotorStator.IsAttached { get { return m_isAttached; } }
        float IMyMotorStator.Angle { get { return m_currentAngle; } }
        float IMyMotorStator.Torque { get { return Torque; } }
        float IMyMotorStator.BrakingTorque { get { return BrakingTorque; } }
        float IMyMotorStator.Velocity { get { return TargetVelocityRPM; } }
        float IMyMotorStator.LowerLimit { get { return m_minAngle; } }
        float IMyMotorStator.UpperLimit { get { return m_maxAngle; } }
        float IMyMotorStator.Displacement { get { return m_dummyDisplacement; } }
        #endregion
    }
}
