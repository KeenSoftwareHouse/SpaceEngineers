using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
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
using VRage.Audio;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorSuspension))]
    class MyMotorSuspension : MyMotorBase, IMyMotorSuspension
    {
        private bool m_wasSteering;
        private const float m_returnStep = 0.01f;
        private float m_steerAngle = 0;
        private bool m_steerInvert;
        private bool m_revolveInvert;
        private bool m_brake;
        private float m_damping;
        private float m_strenth;
        private float m_friction;
        private float m_height;
        private static List<HkRigidBody> m_tmpList = new List<HkRigidBody>();
        private static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();
        private bool m_wasAccelerating;

        internal float Damping 
        { 
            get
            {
                return m_damping;
            }
            set
            {
                if (m_damping != value)
                {
                    m_damping = value;
                    if (SafeConstraint != null)
                        (m_constraint.ConstraintData as HkWheelConstraintData).SetSuspensionDamping(m_damping);
                }
            }
        }
        internal float Strength
        {
            get
            {
                return m_strenth;
            }
            set
            {
                if (m_strenth != value)
                {
                    m_strenth = value;
                    if (SafeConstraint != null)
                        (m_constraint.ConstraintData as HkWheelConstraintData).SetSuspensionStrength(m_strenth);
                }
            }
        }
        private HkRigidBody SafeBody { get { return (m_rotorGrid != null && m_rotorGrid.Physics != null) ? m_rotorGrid.Physics.RigidBody : null; } }

        public bool Brake
        {
            set
            {
                m_brake = value;
                UpdateBrake();
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
                if (m_friction != value)
                {
                    var wheel = (m_rotorBlock as MyWheel);
                    if (wheel != null)
                        wheel.Friction = MathHelper.Lerp(1, 8, value);
                    m_friction = value;
                }
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
                    m_height = value;

                    if (m_constraint != null)
                        Reattach();
                }
            }
        }

        public float SteerAngle { get { return m_steerAngle; } set { m_steerAngle = value; } }
        public float Power { get; set; }
        public bool Steering { get; set; }
        public bool Propulsion { get; set; }
        public new MySyncMotorSuspension SyncObject { get { return (MySyncMotorSuspension)base.SyncObject; } }
        public new MyMotorSuspensionDefinition BlockDefinition { get { return (MyMotorSuspensionDefinition)base.BlockDefinition; } }
        public new float MaxRotorAngularVelocity { get { return 6 * MathHelper.TwoPi; } }

        static MyMotorSuspension()
        {
            var steering = new MyTerminalControlCheckbox<MyMotorSuspension>("Steering", MySpaceTexts.BlockPropertyTitle_Motor_Steering, MySpaceTexts.BlockPropertyDescription_Motor_Steering);
            steering.Getter = (x) => x.Steering;
            steering.Setter = (x, v) => x.SyncObject.ChangeSteering(v);
            steering.EnableAction();
            MyTerminalControlFactory.AddControl(steering);

            var propulsion = new MyTerminalControlCheckbox<MyMotorSuspension>("Propulsion", MySpaceTexts.BlockPropertyTitle_Motor_Propulsion, MySpaceTexts.BlockPropertyDescription_Motor_Propulsion);
            propulsion.Getter = (x) => x.Propulsion;
            propulsion.Setter = (x, v) => x.SyncObject.ChangePropulsion(v);
            propulsion.EnableAction();
            MyTerminalControlFactory.AddControl(propulsion);

            var damping = new MyTerminalControlSlider<MyMotorSuspension>("Damping", MySpaceTexts.BlockPropertyTitle_Motor_Damping, MySpaceTexts.BlockPropertyTitle_Motor_Damping);
            damping.SetLimits(0, 100);
            damping.Getter = (x) => x.GetDampingForTerminal();
            damping.Setter = (x, v) => x.SyncObject.ChangeDamping(v * 0.002f);
            damping.Writer = (x, res) => res.AppendInt32((int)(x.Damping / 0.002f)).Append("%");
            damping.EnableActions();
            MyTerminalControlFactory.AddControl(damping);

            var strength = new MyTerminalControlSlider<MyMotorSuspension>("Strength", MySpaceTexts.BlockPropertyTitle_Motor_Strength, MySpaceTexts.BlockPropertyTitle_Motor_Strength);
            strength.SetLimits(0, 100);
            strength.Getter = (x) => x.GetStrengthForTerminal();
            strength.Setter = (x, v) => x.SyncObject.ChangeStrength(v * 0.002f);
            strength.Writer = (x, res) => res.AppendInt32((int)(x.Strength / 0.002f)).Append("%");
            strength.EnableActions();
            MyTerminalControlFactory.AddControl(strength);

            var friction = new MyTerminalControlSlider<MyMotorSuspension>("Friction", MySpaceTexts.BlockPropertyTitle_Motor_Friction, MySpaceTexts.BlockPropertyDescription_Motor_Friction);
            friction.SetLimits(0, 100);
            friction.DefaultValue = 150f / 800;
            friction.Getter = (x) => x.GetFrictionForTerminal();
            friction.Setter = (x, v) => x.SyncObject.ChangeFriction(v / 100);
            friction.Writer = (x, res) => res.AppendInt32((int)(x.Friction * 100)).Append("%");
            friction.EnableActions();
            MyTerminalControlFactory.AddControl(friction);

            var power = new MyTerminalControlSlider<MyMotorSuspension>("Power", MySpaceTexts.BlockPropertyTitle_Motor_Power, MySpaceTexts.BlockPropertyDescription_Motor_Power);
            power.SetLimits(0, 100);
            power.DefaultValue = 100;
            power.Getter = (x) => x.GetPowerForTerminal();
            power.Setter = (x, v) => x.SyncObject.ChangePower(v / 100);
            power.Writer = (x, res) => res.AppendInt32((int)(x.Power * 100)).Append("%");
            power.EnableActions();
            MyTerminalControlFactory.AddControl(power);

            var height = new MyTerminalControlSlider<MyMotorSuspension>("Height", MySpaceTexts.BlockPropertyTitle_Motor_Height, MySpaceTexts.BlockPropertyDescription_Motor_Height);
            height.SetLimits((x) => x.BlockDefinition.MinHeight, (x) => x.BlockDefinition.MaxHeight);
            height.DefaultValue = 0;
            height.Getter = (x) => x.GetHeightForTerminal();
            height.Setter = (x, v) => x.SyncObject.ChangeHeight(v);
            height.Writer = (x, res) => res.AppendFormatedDecimal("", x.Height, 2, "m");
            height.EnableActionsWithReset();
            MyTerminalControlFactory.AddControl(height);
        }


        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncMotorSuspension(this);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            Friction = 1.5f / 8;
            //needed because of different weights, could be in definition?
            //m_angularForce = cubeGrid.GridSizeEnum == MyCubeSize.Large ? 10000 : 200;

            var ob = objectBuilder as MyObjectBuilder_MotorSuspension;

            m_steerAngle = ob.SteerAngle;
            Damping = ob.Damping;
            Strength = ob.Strength;
            Steering = ob.Steering;
            Propulsion = ob.Propulsion;
            Friction = ob.Friction;
            Power = ob.Power;
            Height = ob.Height;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentMotorSuspension(this));
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
            ob.Friction = Friction;
            ob.Power = Power;
            ob.Height = Height;
            return ob;
        }

        protected override bool CheckIsWorking()
        {
            var result = base.CheckIsWorking();

            result &= (m_rotorBlock != null && m_rotorBlock.IsWorking);

            return result;
        }

        public void UpdateBrake()
        {
            if (SafeBody != null)
                if (m_brake)
                    SafeBody.AngularDamping = BlockDefinition.PropulsionForce;
                else
                    SafeBody.AngularDamping = 0;
            else
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public void InitControl()
        {
            //sets direction of angular force and steering for concrete block based on position in grid
            var mat = MySession.ControlledEntity.Entity.WorldMatrix * PositionComp.WorldMatrixNormalizedInv;
            Vector3 revolveAxis;
            if (Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Up || Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Down)
                revolveAxis = MySession.ControlledEntity.Entity.WorldMatrix.Forward;
            else if (Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Up || Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Down)
                revolveAxis = MySession.ControlledEntity.Entity.WorldMatrix.Up;
            else
                revolveAxis = MySession.ControlledEntity.Entity.WorldMatrix.Right;
            // - "epsilon"
            var dotCockpit1 = Vector3.Dot(MySession.ControlledEntity.Entity.WorldMatrix.Up, WorldMatrix.Translation - MySession.ControlledEntity.Entity.WorldMatrix.Translation) - 0.0001 > 0;

            Vector3 steerAxis;
            if (Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Forward || Base6Directions.GetClosestDirection(mat.Forward) == Base6Directions.Direction.Backward)
                steerAxis = MySession.ControlledEntity.Entity.WorldMatrix.Forward;
            else if (Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Forward || Base6Directions.GetClosestDirection(mat.Up) == Base6Directions.Direction.Backward)
                steerAxis = MySession.ControlledEntity.Entity.WorldMatrix.Up;
            else
                steerAxis = MySession.ControlledEntity.Entity.WorldMatrix.Right;
            // - "epsilon"
            if (CubeGrid.Physics != null)
            {
                var dotMass = Vector3.Dot(MySession.ControlledEntity.Entity.WorldMatrix.Forward, (WorldMatrix.Translation - CubeGrid.Physics.CenterOfMassWorld)) - 0.0001;
                var dotCockpit = Vector3.Dot(WorldMatrix.Forward, steerAxis);

                m_steerInvert = ((dotMass * dotCockpit) < 0) ^ dotCockpit1;
                m_revolveInvert = ((WorldMatrix.Up - revolveAxis).Length() > 0.1f) ^ dotCockpit1;
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
                var rotorBody = m_rotorGrid.Physics.RigidBody;
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
                data.SetSuspensionDamping(Damping);
                data.SetSuspensionStrength(Strength);
                data.SetSuspensionMaxLimit(BlockDefinition.SuspensionLimit - m_height); // keep the limit at the same level even when changing height
                data.SetSuspensionMinLimit(-BlockDefinition.SuspensionLimit + m_height);
                data.SetInBodySpace(ref posB, ref posA, ref axisB, ref axisA, ref suspensionAx, ref suspensionAx);
                m_constraint = new HkConstraint(rotorBody, CubeGrid.Physics.RigidBody, data);

                m_constraint.WantRuntime = true;
                CubeGrid.Physics.AddConstraint(m_constraint);
                m_constraint.Enabled = true;

                m_rotorBlock.Attach(this);
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
            MyPhysics.GetPenetrationsShape(spShape, ref position, ref q, m_tmpList, MyPhysics.CharacterNetworkCollisionLayer);
            if (m_tmpSet.Count > 1 || m_tmpList.Count > 0)
            {
                m_tmpList.Clear();
				m_tmpSet.Clear();
                if (builtBy == MySession.LocalPlayerId)
                    MyHud.Notifications.Add(MyNotificationSingletons.WheelNotPlaced);
                return false;
            }
            m_tmpList.Clear();
			m_tmpSet.Clear();

            return true;
        }
        internal void Forward()
        {
            Accelerate(BlockDefinition.PropulsionForce * Power, true);
        }

        internal void Backward()
        {
            Accelerate(BlockDefinition.PropulsionForce * Power, false);
        }

        private void Accelerate(float force, bool forward)
        {
            if (!IsWorking)
                return;
            if (m_rotorGrid != null && m_rotorGrid.Physics != null)
            {
                var body = m_rotorGrid.Physics.RigidBody;
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(10, 60), "" + body.LinearVelocity.Length(), Color.Red, 1);
                if (m_revolveInvert == forward)
                    body.ApplyAngularImpulse(m_rotorGrid.WorldMatrix.Up * ((3.5f + (float)Math.Pow(body.LinearVelocity.Length(),1.124f)) * force));
                else
                    body.ApplyAngularImpulse(m_rotorGrid.WorldMatrix.Down * ((3.5f + (float)Math.Pow(body.LinearVelocity.Length(), 1.124f)) * force));
                m_wasAccelerating = true;
            }
        }

        internal void Right()
        {
            Steer(BlockDefinition.SteeringSpeed, true);
        }

        internal void Left()
        {
            Steer(BlockDefinition.SteeringSpeed, false);
        }

        private void Steer(float step, bool toRight)
        {
            if (!IsWorking)
                return;
            m_wasSteering = true;

            if (m_steerInvert == toRight)
            {
                if (m_steerAngle < BlockDefinition.MaxSteer)
                    m_steerAngle += step;
            }
            else
            {
                if (m_steerAngle > -BlockDefinition.MaxSteer)
                    m_steerAngle -= step;
            }

        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            var lastSteer = m_steerAngle;
            var player = Sync.Players.GetControllingPlayer(CubeGrid);
            if ((player != null && player.IsLocalPlayer()) || (player == null && Sync.IsServer))
                if (!m_wasSteering)
                {
                    if (Math.Abs(m_steerAngle) < 0.00001) m_steerAngle = 0;
                    if (m_steerAngle < 0)
                        m_steerAngle += m_returnStep;
                    else if (m_steerAngle > 0)
                        m_steerAngle -= m_returnStep;
                }
            if(m_wasSteering || lastSteer != m_steerAngle)
                SyncObject.UpdateSteer(m_steerAngle);
            m_wasSteering = false;

            if (SafeConstraint != null)
                (m_constraint.ConstraintData as HkWheelConstraintData).SetSteeringAngle(m_steerAngle);
            UpdateSoundState();
            m_wasAccelerating = false;

        }

        protected override void UpdateSoundState()
        {
            if (!MySandboxGame.IsGameReady)
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
            return Damping / 0.002f;
        }
        public float GetStrengthForTerminal()
        {
            return Strength / 0.002f;
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

        bool IMyMotorSuspension.Steering { get { return Steering; } }
        bool IMyMotorSuspension.Propulsion { get { return Propulsion;} }
        float IMyMotorSuspension.Damping { get { return GetDampingForTerminal() ;} }
        float IMyMotorSuspension.Strength { get { return GetStrengthForTerminal(); } }
        float IMyMotorSuspension.Friction { get { return GetFrictionForTerminal(); } }
        float IMyMotorSuspension.Power { get { return GetPowerForTerminal();} }
        float IMyMotorSuspension.Height { get { return GetHeightForTerminal(); } }
    }
}
