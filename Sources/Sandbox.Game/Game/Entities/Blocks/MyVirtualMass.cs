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
using VRage;
using VRage.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_VirtualMass))]
    class MyVirtualMass : MyFunctionalBlock, IMyPowerConsumer, IMyVirtualMass
    {
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

        #endregion

        public MyVirtualMass()
            : base()
        {
            m_baseIdleSound.Init("BlockArtMass");
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            if (Physics != null)
            {
                Physics.Close();
            }

            var detectorShape = new HkBoxShape(new Vector3(cubeGrid.GridSize / 3.0f));
            var massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(detectorShape.HalfExtents, BlockDefinition.VirtualMass);
            Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
            Physics.IsPhantom = false;
            Physics.CreateFromCollisionObject(detectorShape, Vector3.Zero, WorldMatrix, massProperties, MyPhysics.VirtualMassLayer);
            Physics.Enabled = IsWorking && cubeGrid.Physics != null && cubeGrid.Physics.Enabled;
            Physics.RigidBody.Activate();
            detectorShape.Base.RemoveReference();

            UpdateText();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

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

        float IMyVirtualMass.VirtualMass
        {
            get { return BlockDefinition.VirtualMass; }
        }
    }
}
