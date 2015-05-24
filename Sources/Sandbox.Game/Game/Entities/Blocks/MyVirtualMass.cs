using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_VirtualMass))]
    class MyVirtualMass : MyFunctionalBlock, IMyPowerConsumer, IMyVirtualMass
    {
        public const float REAL_MINIMUM_MASS = 0.01f;

        #region Properties
        
        private new MyVirtualMassDefinition BlockDefinition
        {
            get { return (MyVirtualMassDefinition)base.BlockDefinition; }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public new MySyncVirtualMass SyncObject;

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

        #endregion

        static MyVirtualMass()
        {
            MyTerminalControlFactory.RemoveBaseClass<MyVirtualMass, MyTerminalBlock>();

            var mass = new MyTerminalControlSlider<MyVirtualMass>("VirtualMass",
                MySpaceTexts.BlockPropertyDescription_SpaceBallVirtualMass,
                MySpaceTexts.BlockPropertyDescription_SpaceBallVirtualMass);
            mass.Getter = (x) => x.VirtualMass;
            mass.Setter = (x, v) => x.SyncObject.SendChangeVirtualMassRequest(v);
            mass.DefaultValueGetter = (x) => x.BlockDefinition.VirtualMass;
            mass.SetLimits(x => 0, x => x.BlockDefinition.MaxVirtualMass);
            mass.Writer = (x, result) => MyValueFormatter.AppendWeightInBestUnit(x.VirtualMass, result);
            mass.EnableActions();
            MyTerminalControlFactory.AddControl(mass);
        }

        public MyVirtualMass()
            : base()
        {
            m_baseIdleSound.Init("BlockArtMass");
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_VirtualMass ob = (MyObjectBuilder_VirtualMass)objectBuilder;
            if (ob.VirtualMass < 0)
            {
                // VirtualMass has not been set so we take its value from BlockDefinition
                ob.VirtualMass = BlockDefinition.VirtualMass;
            }
            m_virtualMass = ob.VirtualMass;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            RefreshPhysicsBody();

            UpdateText();

            SyncObject = new MySyncVirtualMass(this);

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_VirtualMass)base.GetObjectBuilderCubeBlock(copy);
            ob.VirtualMass = m_virtualMass;
            return ob;
        }

        public override void OnBuildSuccess(long builtBy)
        {
            PowerReceiver.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            PowerReceiver.Update();
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        private float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
                return BlockDefinition.RequiredPowerInput;
            else
                return 0.0f;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
            PowerReceiver.Update();
        }

        private void RefreshPhysicsBody()
        {
            if (CubeGrid.CreatePhysics)
            {
                if (Physics != null)
                {
                    Physics.Close();
                }

                var detectorShape = new HkBoxShape(new Vector3(CubeGrid.GridSize / 3.0f));
                var massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(detectorShape.HalfExtents, BlockDefinition.VirtualMass);
                Physics = new Engine.Physics.MyPhysicsBody(this, Engine.Physics.RigidBodyFlag.RBF_DEFAULT);
                Physics.IsPhantom = false;
                Physics.CreateFromCollisionObject(detectorShape, Vector3.Zero, WorldMatrix, massProperties, MyPhysics.VirtualMassLayer);
                Physics.Enabled = IsWorking && CubeGrid.Physics != null && CubeGrid.Physics.Enabled;
                Physics.RigidBody.Activate();
                detectorShape.Base.RemoveReference();

                if (CubeGrid != null && CubeGrid.Physics != null && !CubeGrid.IsStatic)
                    CubeGrid.Physics.UpdateMass();
            }
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentMass));
            DetailedInfo.Append(IsWorking ? BlockDefinition.VirtualMass.ToString() : "0");
            DetailedInfo.Append(" kg\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.RequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.CurrentInput, DetailedInfo);
            RaisePropertiesChanged();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Cyan, Color.White);
            else
                UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();

            Debug.Assert(Physics != null);

            Physics.Enabled = IsWorking && CubeGrid.Physics != null && CubeGrid.Physics.Enabled;
            if (IsWorking)
            {
                Physics.RigidBody.Activate();
            }

            UpdateText();
            UpdateEmissivity();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        internal override float GetMass()
        {
            return VirtualMass > 0 ? VirtualMass : REAL_MINIMUM_MASS;
        }

        float IMyVirtualMass.VirtualMass
        {
            get { return GetMass(); }
        }
    }
}
