using System.Diagnostics;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_VirtualMass))]
    public class MyVirtualMass : MyFunctionalBlock, IMyVirtualMass
    {
        #region Properties

        private new MyVirtualMassDefinition BlockDefinition
        {
            get { return (MyVirtualMassDefinition)base.BlockDefinition; }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        #endregion

        public MyVirtualMass()
            : base()
        {
            m_baseIdleSound.Init("BlockArtMass");
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            ResourceSink = new MyResourceSinkComponent();
            ResourceSink.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);

            base.Init(objectBuilder, cubeGrid);
			
			ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			ResourceSink.Update();

            if (Physics != null)
            {
                Physics.Close();
            }

            var detectorShape = new HkBoxShape(new Vector3(cubeGrid.GridSize / 3.0f));
            var massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(detectorShape.HalfExtents, BlockDefinition.VirtualMass);
            Physics = new Sandbox.Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
            Physics.IsPhantom = false;
            Physics.CreateFromCollisionObject(detectorShape, Vector3.Zero, WorldMatrix, massProperties, MyPhysics.CollisionLayers.VirtualMassLayer);
            Physics.Enabled = IsWorking && cubeGrid.Physics != null && cubeGrid.Physics.Enabled;
            Physics.RigidBody.Activate();
            detectorShape.Base.RemoveReference();

            UpdateText();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

        }

        public override void OnBuildSuccess(long builtBy)
        {
			ResourceSink.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
			ResourceSink.Update();
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
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
			ResourceSink.Update();
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentMass));
            DetailedInfo.Append(IsWorking ? BlockDefinition.VirtualMass.ToString() : "0");
            DetailedInfo.Append(" kg\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
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
            if (Physics == null)
                return;

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
			ResourceSink.Update();
        }

        float ModAPI.Ingame.IMyVirtualMass.VirtualMass
        {
            get { return BlockDefinition.VirtualMass; }
        }
    }
}
