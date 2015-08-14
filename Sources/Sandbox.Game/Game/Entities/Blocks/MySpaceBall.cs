#region Using

using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.GameSystems.Electricity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using VRageMath;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Components;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SpaceBall))]
    class MySpaceBall : MyFunctionalBlock, IMySpaceBall, IMyComponentOwner<MyDataBroadcaster>, IMyComponentOwner<MyDataReceiver>
    {
        #region Properties

        float m_friction;
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
                    m_friction = value;
                    RaisePropertiesChanged();
                }
            }
        }

        float m_virtualMass;
        public float VirtualMass
        {
            get
            {
                return m_virtualMass;
            }
            set
            {
                if (m_virtualMass != value)
                {
                    m_virtualMass = value;
                    RefreshPhysicsBody();
                    RaisePropertiesChanged();
                }
            }
        }

        float m_restitution;
        public float Restitution
        {
            get { return m_restitution; }
            set
            {
                if (m_restitution != value)
                {
                    m_restitution = value;
                    RaisePropertiesChanged();
                }
            }
        }

        private new MySpaceBallDefinition BlockDefinition
        {
            get { return (MySpaceBallDefinition)base.BlockDefinition; }
        }

        internal MyRadioBroadcaster RadioBroadcaster
        {
            get { return m_radioBroadcaster; }
        }

        internal MyRadioReceiver RadioReceiver
        {
            get { return m_radioReceiver; }
        }

        public new MySyncSpaceBall SyncObject;

        #endregion

        public const float DEFAULT_RESTITUTION = 0.4f;
        public const float DEFAULT_MASS = 100;
        public const float DEFAULT_FRICTION = 0.5f;
        public const float REAL_MAXIMUM_RESTITUTION = 0.9f;
        public const float REAL_MINIMUM_MASS = 0.01f;
        MyRadioReceiver m_radioReceiver;
        MyRadioBroadcaster m_radioBroadcaster;


        static MySpaceBall()
        {
            MyTerminalControlFactory.RemoveBaseClass<MySpaceBall, MyTerminalBlock>();

            var mass = new MyTerminalControlSlider<MySpaceBall>("VirtualMass", MySpaceTexts.BlockPropertyDescription_SpaceBallVirtualMass, MySpaceTexts.BlockPropertyDescription_SpaceBallVirtualMass);
            mass.Getter = (x) => x.VirtualMass;
            mass.Setter = (x, v) => x.SyncObject.SendChangeParamsRequest(v, x.Friction);
            mass.DefaultValueGetter = (x) => DEFAULT_MASS;
            mass.SetLimits(x => 0, x => x.BlockDefinition.MaxVirtualMass);
            mass.Writer = (x, result) => MyValueFormatter.AppendWeightInBestUnit(x.VirtualMass, result);
            mass.EnableActions();
            MyTerminalControlFactory.AddControl(mass);

            if (MyPerGameSettings.BallFriendlyPhysics)
            {
                var friction = new MyTerminalControlSlider<MySpaceBall>("Friction", MySpaceTexts.BlockPropertyDescription_SpaceBallFriction, MySpaceTexts.BlockPropertyDescription_SpaceBallFriction);
                friction.Getter = (x) => x.Friction;
                friction.Setter = (x, v) => x.SyncObject.SendChangeParamsRequest(x.VirtualMass, v);
                friction.DefaultValueGetter = (x) => DEFAULT_FRICTION;
                friction.SetLimits(0, 1.0f);
                friction.Writer = (x, result) => result.AppendInt32((int)(x.Friction * 100)).Append("%");
                friction.EnableActions();
                MyTerminalControlFactory.AddControl(friction);

                var restitution = new MyTerminalControlSlider<MySpaceBall>("Restitution", MySpaceTexts.BlockPropertyDescription_SpaceBallRestitution, MySpaceTexts.BlockPropertyDescription_SpaceBallRestitution);
                restitution.Getter = (x) => x.Restitution;
                restitution.Setter = (x, v) => x.SyncObject.SendChangeRestitutionRequest(v);
                restitution.DefaultValueGetter = (x) => DEFAULT_RESTITUTION;
                restitution.SetLimits(0, 1.0f);
                restitution.Writer = (x, result) => result.AppendInt32((int)(x.Restitution * 100)).Append("%");
                restitution.EnableActions();
                MyTerminalControlFactory.AddControl(restitution);  
            }

            var enableBroadcast = new MyTerminalControlCheckbox<MySpaceBall>("EnableBroadCast", MySpaceTexts.Antenna_EnableBroadcast, MySpaceTexts.Antenna_EnableBroadcast);
            enableBroadcast.Getter = (x) => x.RadioBroadcaster.Enabled;
            enableBroadcast.Setter = (x, v) => x.SyncObject.SendChangeBroadcastRequest(v);
            enableBroadcast.EnableAction();
            MyTerminalControlFactory.AddControl(enableBroadcast);
        }

        public MySpaceBall()
            : base()
        {
            m_baseIdleSound.Init("BlockArtMass");
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_SpaceBall sphereOb = (MyObjectBuilder_SpaceBall)objectBuilder;
            m_virtualMass = sphereOb.VirtualMass;
            m_restitution = sphereOb.Restitution;
            Friction = sphereOb.Friction;

            m_radioReceiver = new MyRadioReceiver(this);
            m_radioBroadcaster = new MyRadioBroadcaster(this, 50);

            IsWorkingChanged += MySpaceBall_IsWorkingChanged;

            UpdateIsWorking();
            RefreshPhysicsBody();
            UpdateRadios(sphereOb.EnableBroadcast);

            SyncObject = new MySyncSpaceBall(this);

            ShowOnHUD = false;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_SpaceBall)base.GetObjectBuilderCubeBlock(copy);
            ob.VirtualMass = m_virtualMass;
            ob.Restitution = Restitution;
            ob.Friction = Friction;
            ob.EnableBroadcast = RadioBroadcaster.Enabled;
            return ob;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            if (CubeGrid.Physics != null && !CubeGrid.IsStatic)
                CubeGrid.Physics.UpdateMass();
            if (Physics != null)
               UpdatePhysics();

            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;

            UpdateEmissivity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            UpdatePhysics();
        }

        private void RefreshPhysicsBody()
        {
            if (CubeGrid.CreatePhysics)
            {
                if (Physics != null)
                {
                    Physics.Close();
                }

                var detectorShape = new HkSphereShape(CubeGrid.GridSize * 0.5f);
                var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(detectorShape.Radius, VirtualMass != 0 ? VirtualMass : 0.01f);
                Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KEYFRAMED_REPORTING);
                Physics.IsPhantom = false;
                Physics.CreateFromCollisionObject(detectorShape, Vector3.Zero, WorldMatrix, massProperties, MyPhysics.VirtualMassLayer);
                UpdateIsWorking();
                Physics.Enabled = IsWorking && CubeGrid.Physics != null && CubeGrid.Physics.Enabled;

                Physics.RigidBody.Activate();
                detectorShape.Base.RemoveReference();

                if (CubeGrid != null && CubeGrid.Physics != null && !CubeGrid.IsStatic)
                    CubeGrid.Physics.UpdateMass();
            }
        }

        private void UpdatePhysics()
        {
            Physics.Enabled = IsWorking && CubeGrid.Physics != null && CubeGrid.Physics.Enabled;
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Cyan, Color.White);
            else
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        public void UpdateRadios(bool isTrue)
        {
            m_radioBroadcaster.Enabled = isTrue;
            m_radioBroadcaster.WantsToBeEnabled = isTrue;
            m_radioReceiver.Enabled = isTrue & Enabled;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            m_radioReceiver.UpdateBroadcastersInRange();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            m_radioReceiver.UpdateBroadcastersInRange();
            UpdateEmissivity();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            UpdateEmissivity();
        }

        private void MySpaceBall_IsWorkingChanged(MyCubeBlock obj)
        {
            UpdateRadios(IsWorking);
        }

        public override void ContactPointCallback(ref MyGridContactInfo value)
        {
            var prop = value.Event.ContactProperties;

            value.EnableDeformation = false;
            value.EnableParticles = false;
            value.RubberDeformation = false;

            if (MyPerGameSettings.BallFriendlyPhysics)
            {
                prop.Friction = Friction;
                prop.Restitution = Restitution > REAL_MAXIMUM_RESTITUTION ? REAL_MAXIMUM_RESTITUTION : Restitution;
            }
        }
        
        bool IMyComponentOwner<MyDataBroadcaster>.GetComponent(out MyDataBroadcaster component)
        {
            component = m_radioBroadcaster;
            return m_radioBroadcaster != null;
        }

        bool IMyComponentOwner<MyDataReceiver>.GetComponent(out MyDataReceiver component)
        {
            component = m_radioReceiver;
            return m_radioReceiver != null;
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();
        }

        internal override float GetMass()
        {
            return VirtualMass > 0 ? VirtualMass : REAL_MINIMUM_MASS;
        }

        float IMyVirtualMass.VirtualMass
        {
            get { return GetMass(); }
        }

        bool IMySpaceBall.IsBroadcasting
        {
            get { return (m_radioBroadcaster == null) ? false : m_radioBroadcaster.Enabled; }
        }
    }
}
