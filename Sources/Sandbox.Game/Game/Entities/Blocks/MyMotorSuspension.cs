﻿using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorSuspension))]
    public partial class MyMotorSuspension : MyMotorBase
    {
        const float MaxSpeedLimit = 360;//km/h
        private bool m_wasSteering;
        private Sync<float> m_steerAngle;
        private bool m_steerInvert;
        private bool m_revolveInvert;
        private readonly Sync<bool> m_brake;
        private readonly Sync<float> m_damping;
        private readonly Sync<float> m_strenth;
        private readonly Sync<float> m_friction;
        private readonly Sync<float> m_height;
        private readonly Sync<float> m_suspensionTravel;
        private static List<HkBodyCollision> m_tmpList = new List<HkBodyCollision>();
        private static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();
        private bool m_wasAccelerating;
       
        private readonly Sync<float> m_speedLimit;
        public float SpeedLimit
        {
            get
            {
                return m_speedLimit;
            }
            set
            {
                m_speedLimit.Value = value;
            }
        }

        internal float Damping 
        { 
            get
            {
                return (float)Math.Sqrt(m_damping);
            }
            set
            {
                if (m_damping != value * value)
                {
                    m_damping.Value = value * value;
                }
            }
        }
        internal float Strength
        {
            get
            {
                return (float)Math.Sqrt(m_strenth);
            }
            set
            {
                if (m_strenth != value * value)
                {
                    m_strenth.Value = value * value;                 
                }
            }
        }
        private HkRigidBody SafeBody { get { return (m_rotorGrid != null && m_rotorGrid.Physics != null) ? m_rotorGrid.Physics.RigidBody : null; } }

        public bool Brake
        {
            get { return m_brake; }
            set
            {
                m_brake.Value = value;
            }
        }
    
        public float Friction 
        { 
            get
            {
                return m_friction;
            }
            set
            {
                if (m_friction.Value != value)
                {
                    m_friction.Value = value;
                }
            }
        }
        private void PropagateFriction(float value)
        {
            var wheel = (m_rotorBlock as MyWheel);
            if (wheel != null)
            {
                wheel.Friction = MathHelper.Lerp(0, 32, value);
                wheel.CubeGrid.Physics.RigidBody.Friction = wheel.Friction;
            }
        }

        public float Height
        {
            get
            {
                return m_height;
            }
            set
            {
                if (m_height != value)
                {
                    m_height.Value = value;
                }
            }
        }

        public float SuspensionTravel
        {
            get { return m_suspensionTravel; }
            set
            {
                m_suspensionTravel.Value = MathHelper.Clamp(value, 0, 1);          
            }
        }

        private readonly Sync<float> m_maxSteerAngle;

        public float MaxSteerAngle 
        { 
            get
            {
                return m_maxSteerAngle;
            }
            set
            {
                m_maxSteerAngle.Value = value;
            }
        }

        private readonly Sync<float> m_steerSpeed;
        public float SteerSpeed
        {
            get
            {
                return m_steerSpeed;
            }
            set
            {
                m_steerSpeed.Value = value;
            }
        }
        private readonly Sync<float> m_steerReturSpeed;
        public float SteerReturnSpeed
        {
            get
            {
                return m_steerReturSpeed;
            }
            set
            {
                m_steerReturSpeed.Value = value;
            }
        }

        private readonly Sync<bool> m_invertSteer;
        public bool InvertSteer
        {
            get
            {
                return m_invertSteer;
            }
            set
            {
                m_invertSteer.Value = value;
            }
        }

        private readonly Sync<bool> m_invertPropulsion;
        public bool InvertPropulsion
        {
            get
            {
                return m_invertPropulsion;
            }
            set
            {
                m_invertPropulsion.Value = value;
            }
        }

        public float SteerAngle 
        { 
            get 
            { 
                return m_steerAngle; 
            } 
            set 
            {
                m_steerAngle.Value = value; 
            } 
        } // current steering angle

        private readonly Sync<float> m_power;

        public float Power 
        {
            get
            {
                return m_power;
            }
            set
            {
                m_power.Value = value;
            }
        }

        private readonly Sync<bool> m_steering;

        public bool Steering 
        { 
            get
            {
                return m_steering;
            }
            set
            {
                m_steering.Value = value;
            }
        }

        private readonly Sync<bool> m_propulsion;

        public bool Propulsion
        {
            get
            {
                return m_propulsion;
            }
            set
            {
                m_propulsion.Value = value;
            }
        }

        public new MyMotorSuspensionDefinition BlockDefinition { get { return (MyMotorSuspensionDefinition)base.BlockDefinition; } }
        public new float MaxRotorAngularVelocity { get { return 6 * MathHelper.TwoPi; } }

        static MyMotorSuspension()
        {
            var steering = new MyTerminalControlCheckbox<MyMotorSuspension>("Steering", MySpaceTexts.BlockPropertyTitle_Motor_Steering, MySpaceTexts.BlockPropertyDescription_Motor_Steering);
            steering.Getter = (x) => x.Steering;
            steering.Setter = (x, v) => x.Steering = v;
            steering.EnableAction();
            steering.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(steering);

            var maxSteerAngle = new MyTerminalControlSlider<MyMotorSuspension>("MaxSteerAngle", MySpaceTexts.BlockPropertyTitle_Motor_MaxSteerAngle, MySpaceTexts.BlockPropertyDescription_Motor_MaxSteerAngle);
            maxSteerAngle.SetLimits((x) => 0, (x) => x.BlockDefinition.MaxSteer);
            maxSteerAngle.DefaultValue = 0.45f;
            maxSteerAngle.Getter = (x) => x.GetMaxSteerAngleForTerminal();
            maxSteerAngle.Setter = (x, v) => x.MaxSteerAngle = v;
            maxSteerAngle.Writer = (x, res) => MyMotorStator.WriteAngle(x.GetMaxSteerAngleForTerminal(), res);
            maxSteerAngle.EnableActionsWithReset();
            maxSteerAngle.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(maxSteerAngle);

            var steerSpeed = new MyTerminalControlSlider<MyMotorSuspension>("SteerSpeed", MySpaceTexts.BlockPropertyTitle_Motor_SteerSpeed, MySpaceTexts.BlockPropertyDescription_Motor_SteerSpeed);
            steerSpeed.SetLimits((x) => 0, (x) => x.BlockDefinition.SteeringSpeed * 100);
            steerSpeed.DefaultValue = 2f;
            steerSpeed.Getter = (x) => x.GetSteerSpeedForTerminal();
            steerSpeed.Setter = (x, v) => x.SteerSpeed = v / 100;
            steerSpeed.Writer = (x, res) => MyValueFormatter.AppendTorqueInBestUnit(x.GetSteerSpeedForTerminal(), res);
            steerSpeed.EnableActionsWithReset();
            steerSpeed.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(steerSpeed);

            var steerReturnSpeed = new MyTerminalControlSlider<MyMotorSuspension>("SteerReturnSpeed", MySpaceTexts.BlockPropertyTitle_Motor_SteerReturnSpeed, MySpaceTexts.BlockPropertyDescription_Motor_SteerReturnSpeed);
            steerReturnSpeed.SetLimits((x) => 0, (x) => x.BlockDefinition.SteeringSpeed * 100);
            steerReturnSpeed.DefaultValue = 1f;
            steerReturnSpeed.Getter = (x) => x.GetSteerReturnSpeedForTerminal();
            steerReturnSpeed.Setter = (x, v) => x.SteerReturnSpeed = v / 100;
            steerReturnSpeed.Writer = (x, res) => MyValueFormatter.AppendTorqueInBestUnit(x.GetSteerReturnSpeedForTerminal(), res);
            steerReturnSpeed.EnableActionsWithReset();
            steerReturnSpeed.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(steerReturnSpeed);

            var invertSteer = new MyTerminalControlCheckbox<MyMotorSuspension>("InvertSteering", MySpaceTexts.BlockPropertyTitle_Motor_InvertSteer, MySpaceTexts.BlockPropertyDescription_Motor_InvertSteer);
            invertSteer.Getter = (x) => x.InvertSteer;
            invertSteer.Setter = (x, v) => x.InvertSteer = v;
            invertSteer.EnableAction();
            invertSteer.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(invertSteer);

            var propulsion = new MyTerminalControlCheckbox<MyMotorSuspension>("Propulsion", MySpaceTexts.BlockPropertyTitle_Motor_Propulsion, MySpaceTexts.BlockPropertyDescription_Motor_Propulsion);
            propulsion.Getter = (x) => x.Propulsion;
            propulsion.Setter = (x, v) => x.Propulsion = v;
            propulsion.EnableAction();
            propulsion.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(propulsion);

            var invertPropulsion = new MyTerminalControlCheckbox<MyMotorSuspension>("InvertPropulsion", MySpaceTexts.BlockPropertyTitle_Motor_InvertPropulsion, MySpaceTexts.BlockPropertyDescription_Motor_InvertPropulsion);
            invertPropulsion.Getter = (x) => x.InvertPropulsion;
            invertPropulsion.Setter = (x, v) => x.InvertPropulsion = v;
            invertPropulsion.EnableAction();
            invertPropulsion.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(invertPropulsion);

            var power = new MyTerminalControlSlider<MyMotorSuspension>("Power", MySpaceTexts.BlockPropertyTitle_Motor_Power, MySpaceTexts.BlockPropertyDescription_Motor_Power);
            power.SetLimits(0, 100);
            power.DefaultValue = 100;
            power.Getter = (x) => x.GetPowerForTerminal();
            power.Setter = (x, v) => x.Power = v / 100;
            power.Writer = (x, res) => res.AppendInt32((int)(x.Power * 100)).Append("%");
            power.EnableActions();
            power.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(power);

            var friction = new MyTerminalControlSlider<MyMotorSuspension>("Friction", MySpaceTexts.BlockPropertyTitle_Motor_Friction, MySpaceTexts.BlockPropertyDescription_Motor_Friction);
            friction.SetLimits(0, 100);
            friction.DefaultValue = 150f / 800;
            friction.Getter = (x) => x.GetFrictionForTerminal();
            friction.Setter = (x, v) => x.Friction = v / 100;
            friction.Writer = (x, res) => res.AppendInt32((int)(x.Friction * 100)).Append("%");
            friction.EnableActions();
            friction.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(friction);

            var damping = new MyTerminalControlSlider<MyMotorSuspension>("Damping", MySpaceTexts.BlockPropertyTitle_Motor_Damping, MySpaceTexts.BlockPropertyTitle_Motor_Damping);
            damping.SetLimits(0, 100);
            damping.Getter = (x) => x.GetDampingForTerminal();
            damping.Setter = (x, v) => x.Damping = v / 100;
            damping.Writer = (x, res) => res.AppendInt32((int)(x.GetDampingForTerminal())).Append("%");
            damping.EnableActions();
            damping.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(damping);

            var strength = new MyTerminalControlSlider<MyMotorSuspension>("Strength", MySpaceTexts.BlockPropertyTitle_Motor_Strength, MySpaceTexts.BlockPropertyTitle_Motor_Strength);
            strength.SetLimits(0, 100);
            strength.Getter = (x) => x.GetStrengthForTerminal();
            strength.Setter = (x, v) => x.Strength = v / 100;
            strength.Writer = (x, res) => res.AppendInt32((int)(x.GetStrengthForTerminal())).Append("%");
            strength.EnableActions();
            strength.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(strength);

            var height = new MyTerminalControlSlider<MyMotorSuspension>("Height", MySpaceTexts.BlockPropertyTitle_Motor_Height, MySpaceTexts.BlockPropertyDescription_Motor_Height);
            height.SetLimits((x) => x.BlockDefinition.MinHeight, (x) => x.BlockDefinition.MaxHeight);
            height.DefaultValue = 0;
            height.Getter = (x) => x.GetHeightForTerminal();
            height.Setter = (x, v) => x.Height = v;
            height.Writer = (x, res) => MyValueFormatter.AppendDistanceInBestUnit(x.Height, res);
            height.EnableActionsWithReset();
            height.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(height);

            var travel = new MyTerminalControlSlider<MyMotorSuspension>("Travel", MySpaceTexts.BlockPropertyTitle_Motor_SuspensionTravel, MySpaceTexts.BlockPropertyDescription_Motor_SuspensionTravel);
            travel.SetLimits(0, 100);
            travel.DefaultValue = 100;
            travel.Getter = (x) => x.GetSuspensionTravelForTerminal();
            travel.Setter = (x, v) => x.SuspensionTravel = v / 100.0f;
            travel.Writer = (x, res) => res.AppendInt32((int)x.GetSuspensionTravelForTerminal()).Append("%");
            travel.EnableActionsWithReset();
            travel.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(travel);

            var speed = new MyTerminalControlSlider<MyMotorSuspension>("Speed Limit", MySpaceTexts.BlockPropertyTitle_Motor_SuspensionSpeed, MySpaceTexts.BlockPropertyDescription_Motor_SuspensionSpeed);
            speed.SetLimits(0, MaxSpeedLimit);
            speed.DefaultValue = MaxSpeedLimit;
            speed.Getter = (x) => x.SpeedLimit;
            speed.Setter = (x, v) => x.SpeedLimit = v;
            speed.Writer = (x, res) =>
            {
                if (x.SpeedLimit >= MyMotorSuspension.MaxSpeedLimit)
                    res.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_MotorAngleUnlimited));
                else
                    res.AppendInt32((int)x.SpeedLimit).Append("km/h");
            };
            speed.EnableActionsWithReset();
            speed.Enabled = (x) => x.m_constraint != null;
            MyTerminalControlFactory.AddControl(speed);
        }

        public MyMotorSuspension()
        {
            m_brake.ValueChanged += (x) => UpdateBrake();
            m_friction.ValueChanged += (x) =>FrictionChanged();
            m_damping.ValueChanged += (x) => DampingChanged();
            m_strenth.ValueChanged += (x) => StrenghtChanged();
            m_height.ValueChanged += (x) => ReattachConstraint();
            m_suspensionTravel.ValueChanged += (x) => ReattachConstraint();
        }

        void ReattachConstraint()
        {
            if (m_constraint != null)
            {
                Reattach();
            }
        }

        void FrictionChanged()
        {
            PropagateFriction(m_friction);
        }

        void DampingChanged()
        {
            if (SafeConstraint != null)
            {
                (m_constraint.ConstraintData as HkWheelConstraintData).SetSuspensionDamping(Sync.RelativeSimulationRatio * m_damping);
            }
        }

        void StrenghtChanged()
        {
            if (SafeConstraint != null)
            {
                (m_constraint.ConstraintData as HkWheelConstraintData).SetSuspensionStrength(Sync.RelativeSimulationRatio * m_strenth);
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            Friction = 1.5f / 8;
            //needed because of different weights, could be in definition?
            //m_angularForce = cubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 200;

            var ob = objectBuilder as MyObjectBuilder_MotorSuspension;

            m_steerAngle.Value = ob.SteerAngle;
            Damping = ob.Damping;
            Strength = ob.Strength;
            Steering = ob.Steering;
            Propulsion = ob.Propulsion;
            Friction = ob.Friction/4;
            Power = ob.Power;
            Height = ob.Height;
            MaxSteerAngle = ob.MaxSteerAngle;
            SteerSpeed = ob.SteerSpeed;
            SteerReturnSpeed = ob.SteerReturnSpeed;
            InvertSteer = ob.InvertSteer;
            InvertPropulsion = ob.InvertPropulsion;
            SuspensionTravel = ob.SuspensionTravel;
            SpeedLimit = ob.SpeedLimit;
            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_OnHavokSystemIDChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentMotorSuspension(this));
        }

        void CubeGrid_OnHavokSystemIDChanged(int obj)
        {
            CubeGrid_OnPhysicsChanged(CubeGrid);
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (CubeGrid.Physics == null || m_rotorGrid == null || m_rotorGrid.Physics == null)
                return;
            var rotorBody = m_rotorGrid.Physics.RigidBody;
            if (rotorBody == null)
                return;
            var info = HkGroupFilter.CalcFilterInfo(rotorBody.Layer, CubeGrid.GetPhysicsBody().HavokCollisionSystemID, 1, 1);
            rotorBody.SetCollisionFilterInfo(info);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            UpdateBrake();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_MotorSuspension;

            ob.SteerAngle = m_steerAngle;
            ob.Steering = Steering;
            ob.Damping = Damping;
            ob.Strength = Strength;
            ob.Propulsion = Propulsion;
            ob.Friction = Friction*4;
            ob.Power = Power;
            ob.Height = Height;
            ob.MaxSteerAngle = MaxSteerAngle;
            ob.SteerSpeed = SteerSpeed;
            ob.SteerReturnSpeed = SteerReturnSpeed;
            ob.InvertSteer = InvertSteer;
            ob.InvertPropulsion = InvertPropulsion;
            ob.SuspensionTravel = SuspensionTravel;
            ob.SpeedLimit = SpeedLimit;
            return ob;
        }

        protected override bool CheckIsWorking()
        {
            var result = base.CheckIsWorking();

            result &= (m_rotorBlock != null && m_rotorBlock.IsWorking);

            return result;
        }

        protected override void UpdateText()
        {
            // No detailed info here?
        }

        public void UpdateBrake()
        {
            if (SafeBody != null)
                if (m_brake)
                    SafeBody.AngularDamping = BlockDefinition.PropulsionForce;
                else
                {
                    SafeBody.AngularDamping = CubeGrid.Physics.AngularDamping;
                }
            else
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public void InitControl()
        {
            //sets direction of angular force and steering for concrete block based on position in grid
            var mat = MySession.Static.ControlledEntity.Entity.WorldMatrix * PositionComp.WorldMatrixNormalizedInv;
            Vector3 revolveAxis;
            if (Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Up || Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Down)
                revolveAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward;
            else if (Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Up || Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Down)
                revolveAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Up;
            else
                revolveAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Right;
            // - "epsilon"
            var dotCockpit1 = Vector3.Dot(MySession.Static.ControlledEntity.Entity.WorldMatrix.Up, WorldMatrix.Translation - MySession.Static.ControlledEntity.Entity.WorldMatrix.Translation) - 0.0001 > 0;

            Vector3 steerAxis;
            if (Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Forward || Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Backward)
                steerAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward;
            else if (Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Forward || Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Backward)
                steerAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Up;
            else
                steerAxis = MySession.Static.ControlledEntity.Entity.WorldMatrix.Right;
            // - "epsilon"
            if (CubeGrid.Physics != null)
            {
                var dotMass = Vector3.Dot(MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward, (WorldMatrix.Translation - CubeGrid.Physics.CenterOfMassWorld)) - 0.0001;
                var dotCockpit = Vector3.Dot(WorldMatrix.Forward, steerAxis);

                m_steerInvert = ((dotMass * dotCockpit) < 0) ^ dotCockpit1;
                m_revolveInvert = ((WorldMatrix.Up - revolveAxis).Length() > 0.1f) ^ dotCockpit1;
            }
        }

        public override bool Attach(MyMotorRotor rotor, bool updateGroup = true)
        {
            Debug.Assert(rotor != null, "Rotor cannot be null!");
            Debug.Assert(m_constraint == null, "Already attached, call detach first!");
            Debug.Assert(m_rotorBlockId.Value.OtherEntityId == rotor.EntityId, "m_rotorBlockId must be set prior calling Attach");
            if (rotor == null || MarkedForClose || Closed || rotor.MarkedForClose || rotor.Closed || CubeGrid.MarkedForClose || CubeGrid.Closed)
            {
                return false;
            }
            if (CubeGrid.Physics != null && CubeGrid.Physics.Enabled)
            {
                m_rotorBlock = rotor;
                m_rotorGrid = m_rotorBlock.CubeGrid;
                var rotorBody = m_rotorGrid.Physics.RigidBody;
                rotorBody.MaxAngularVelocity = float.MaxValue;
                rotorBody.Restitution = 0.5f;
                CubeGrid.GetPhysicsBody().HavokWorld.BreakOffPartsUtil.UnmarkEntityBreakable(rotorBody);
                if (MyFakes.WHEEL_SOFTNESS)
                {
                    HkUtils.SetSoftContact(rotorBody, null, MyPhysicsConfig.WheelSoftnessRatio, MyPhysicsConfig.WheelSoftnessVelocity);
                }
                var info = HkGroupFilter.CalcFilterInfo(rotorBody.Layer, CubeGrid.GetPhysicsBody().HavokCollisionSystemID, 1, 1);
                rotorBody.SetCollisionFilterInfo(info);
                HkWheelConstraintData data = new HkWheelConstraintData();
                var suspensionAx = PositionComp.LocalMatrix.Forward;
                var posA = DummyPosition + (suspensionAx * m_height);
                var posB = rotor.DummyPosLoc;
                var axisA = PositionComp.LocalMatrix.Up;
                var axisAPerp = PositionComp.LocalMatrix.Forward;
                var axisB = rotor.PositionComp.LocalMatrix.Up;
                //empirical values because who knows what havoc sees behind this 
                //docs say one value should mean same effect for 2 ton or 200 ton vehicle 
                //but we have virtual mass blocks so real mass doesnt corespond to actual "weight" in game and varying gravity
                data.SetSuspensionDamping(Sync.RelativeSimulationRatio * m_damping);
                data.SetSuspensionStrength(Sync.RelativeSimulationRatio * m_strenth);
                //Min/MaxHeight also define the limits of the suspension and SuspensionTravel lowers this limit
                data.SetSuspensionMinLimit((BlockDefinition.MinHeight - m_height) * SuspensionTravel);
                data.SetSuspensionMaxLimit((BlockDefinition.MaxHeight - m_height) * SuspensionTravel);
                data.SetInBodySpace( posB,  posA,  axisB,  axisA,  suspensionAx,  suspensionAx, RotorGrid.Physics, CubeGrid.Physics);
                m_constraint = new HkConstraint(rotorBody, CubeGrid.Physics.RigidBody, data);

                m_constraint.WantRuntime = true;
                CubeGrid.Physics.AddConstraint(m_constraint);
                if(!m_constraint.InWorld)
                {
                    Debug.Fail("Constraint not added!");
                    CubeGrid.Physics.RemoveConstraint(m_constraint);
                    m_constraint = null;
                    return false;
                }
                m_constraint.Enabled = true;

                m_rotorBlock.Attach(this);
                PropagateFriction(m_friction);
                UpdateIsWorking();

                if (updateGroup)
                {
                    OnConstraintAdded(GridLinkTypeEnum.Physical, m_rotorGrid);
                    OnConstraintAdded(GridLinkTypeEnum.Logical, m_rotorGrid);
                }

                return true;
            }

            return false;
        }

        public override void ComputeRotorQueryBox(out Vector3D pos, out Vector3 halfExtents, out Quaternion orientation)
        {
            var world = this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.One * CubeGrid.GridSize * 0.35f;
            halfExtents.Y = CubeGrid.GridSize;
            pos = world.Translation + 0.35f * CubeGrid.GridSize * WorldMatrix.Up;
        }

        protected override bool CanPlaceRotor(MyMotorRotor rotorBlock, long builtBy)
        {
            BoundingSphereD sphere = rotorBlock.Model.BoundingSphere;
            sphere.Center = Vector3D.Transform(sphere.Center, rotorBlock.WorldMatrix);
            CubeGrid.GetBlocksInsideSphere(ref sphere, m_tmpSet);
            HkSphereShape spShape = new HkSphereShape((float)sphere.Radius);
            Quaternion q = Quaternion.Identity;//Quaternion.CreateFromForwardUp(rotorBlock.WorldMatrix.Forward, rotorBlock.WorldMatrix.Up);
            var position = rotorBlock.WorldMatrix.Translation;
            MyPhysics.GetPenetrationsShape(spShape, ref position, ref q, m_tmpList, MyPhysics.CollisionLayers.CharacterNetworkCollisionLayer);
            if (m_tmpSet.Count > 1 || m_tmpList.Count > 0)
            {
                m_tmpList.Clear();
				m_tmpSet.Clear();
                if (builtBy == MySession.Static.LocalPlayerId)
                    MyHud.Notifications.Add(MyNotificationSingletons.WheelNotPlaced);
                return false;
            }
            m_tmpList.Clear();
			m_tmpSet.Clear();

            return true;
        }

        internal void Forward()
        {
            Accelerate(BlockDefinition.PropulsionForce * Power, InvertPropulsion ? false : true);
        }

        internal void Backward()
        {
            Accelerate(BlockDefinition.PropulsionForce * Power, InvertPropulsion ? true : false);
        }

        private void Accelerate(float force, bool forward)
        {
            if (!IsWorking)
                return;
            if (m_rotorGrid == null || m_rotorGrid.Physics == null)
                return;
            if (CubeGrid.Physics == null || MySession.Static.ControlledEntity == null)
                return;
            //speed limiter
            if (Math.Abs(CubeGrid.Physics.LinearVelocity.Dot(MySession.Static.ControlledEntity.Entity.WorldMatrix.Forward)) > SpeedLimit * (1 / 3.6f))
                return;

            var powerRatio = 1f;// m_rotorGrid.Physics.AngularVelocity.Length();

            if (MyFakes.SUSPENSION_POWER_RATIO)
            {
                var wheelDiameter = m_rotorBlock.BlockDefinition.Size.X * m_rotorGrid.GridSize * 0.5f;
                var lin = 1f;
                if (MyDebugDrawSettings.DEBUG_DRAW_SUSPENSION_POWER)
                {
                    for (int i = 2; i < 20; i++)
                    {
                        lin = (i - 1) * 10;
                        powerRatio = 1 - ((lin - 10) / (CubeGrid.Physics.RigidBody.MaxLinearVelocity - 20));
                        var vel0 = Math.Min(1, powerRatio);

                        lin = i * 10;
                        powerRatio = 1 - ((lin - 10) / (CubeGrid.Physics.RigidBody.MaxLinearVelocity - 20));
                        var vel = Math.Min(1, powerRatio);

                        VRageRender.MyRenderProxy.DebugDrawLine2D(new Vector2(300 + i * 20, 400 - vel * 200),
                            new Vector2(300 + (i - 1) * 20, 400 - vel0 * 200), Color.Yellow, Color.Yellow);
                        VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(300 + (i - 1) * 20, 400), ((i - 1) * 10).ToString(), Color.Yellow, 0.35f);

                    }
                }
                lin = m_rotorGrid.Physics.AngularVelocity.Length() * wheelDiameter; // linear velocity at tire surface
                powerRatio = 1 - ((lin - 10) / (CubeGrid.Physics.RigidBody.MaxLinearVelocity - 20));
                powerRatio = MathHelper.Clamp(powerRatio, 0, 1);
                if (MyDebugDrawSettings.DEBUG_DRAW_SUSPENSION_POWER)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(300 + lin * 2, 400 - powerRatio * 200), "I", Color.Red, 0.3f);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(300 - 10, 400 - powerRatio * 200), powerRatio.ToString(), Color.Yellow, 0.35f);
                }
            }

            force *= powerRatio;
            {
                var body = m_rotorGrid.Physics.RigidBody;
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 60), "" + body.LinearVelocity.Length(), Color.Red, 1);
                if (m_revolveInvert == forward)
                    body.ApplyAngularImpulse(body.GetRigidBodyMatrix().Up * /*((3.5f + (float)Math.Pow(body.LinearVelocity.Length(),1.124f)) */ force);
                else
                    body.ApplyAngularImpulse(m_rotorGrid.WorldMatrix.Down * /*((3.5f + (float)Math.Pow(body.LinearVelocity.Length(), 1.124f)) */ force);
                m_wasAccelerating = true;
            }
        }

        internal void Right()
        {
            Steer(SteerSpeed, InvertSteer ? false : true);
        }

        internal void Left()
        {
            Steer(SteerSpeed, InvertSteer ? true : false);
        }

        private void Steer(float step, bool toRight)
        {
            if (!IsWorking)
                return;
            m_wasSteering = true;

            if (m_steerInvert == toRight)
            {
                if (m_steerAngle < MaxSteerAngle)
                    m_steerAngle.Value += step;
            }
            else
            {
                if (m_steerAngle > -MaxSteerAngle)
                    m_steerAngle.Value -= step;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            var lastSteer = m_steerAngle;
            var player = Sync.Players.GetControllingPlayer(CubeGrid);
            if ((player != null && player.IsLocalPlayer) || (player == null && Sync.IsServer))
            {
                if (!m_wasSteering)
                {
                    if (Math.Abs(m_steerAngle) < 0.00001)
                        m_steerAngle.Value = 0;
                    if (m_steerAngle != 0 && SteerReturnSpeed > 0)
                        m_steerAngle.Value = (m_steerAngle > 0 ? Math.Max(m_steerAngle - SteerReturnSpeed, 0) : Math.Min(m_steerAngle + SteerReturnSpeed, 0));
                }
            }
  
            m_wasSteering = false;

            if (SafeConstraint != null)
            {
                HkWheelConstraintData constraint = (m_constraint.ConstraintData as HkWheelConstraintData);
                if (Steering)
                {
                    constraint.SetSteeringAngle(m_steerAngle);
                }
            }

            UpdateSoundState();
            m_wasAccelerating = false;


        }

        protected override void UpdateSoundState()
        {
            if (!MySandboxGame.IsGameReady || m_soundEmitter == null)
                return;

            if (m_rotorGrid == null || m_rotorGrid.Physics == null)
            {
                m_soundEmitter.StopSound(true);
                return;
            }

            if (IsWorking && Math.Abs(m_rotorGrid.Physics.RigidBody.DeltaAngle.W - CubeGrid.Physics.RigidBody.DeltaAngle.W) > 0.0025f)
                m_soundEmitter.PlaySingleSound(BlockDefinition.PrimarySound, true);
            else
                m_soundEmitter.StopSound(false);

            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying))
            {
                float semitones = 4f * (Math.Abs(RotorAngularVelocity.Length()) - 0.5f * MaxRotorAngularVelocity) / MaxRotorAngularVelocity;
                m_soundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones) * (m_wasAccelerating ? 1 : 0.95f);
            }
        }

        public float GetDampingForTerminal()
        {
            return Damping * 100;
        }

        public float GetStrengthForTerminal()
        {
            return Strength * 100;
        }

        public float GetFrictionForTerminal()
        {
            return Friction * 100;
        }

        public float GetPowerForTerminal()
        {
            return Power * 100;
        }

        public float GetHeightForTerminal()
        {
            return Height;
        }

        public float GetMaxSteerAngleForTerminal()
        {
            return MaxSteerAngle;
        }

        public float GetSteerSpeedForTerminal()
        {
            return SteerSpeed * 100;
        }

        public float GetSteerReturnSpeedForTerminal()
        {
            return SteerReturnSpeed * 100;
        }

        public float GetSuspensionTravelForTerminal()
        {
            return SuspensionTravel * 100;
        }
    }
}
