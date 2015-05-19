#region Using

using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using System.Text;
using VRageMath;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OreDetector))]
    class MyOreDetector : MyFunctionalBlock, IMyPowerConsumer, IMyComponentOwner<MyOreDetectorComponent>, IMyOreDetector
    {
        private MyOreDetectorDefinition m_definition;

        MyOreDetectorComponent m_oreDetectorComponent = new MyOreDetectorComponent();

        public new MySyncOreDetector SyncObject;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        static MyOreDetector()
        {
            var range = new MyTerminalControlSlider<MyOreDetector>("Range", MySpaceTexts.BlockPropertyTitle_OreDetectorRange, MySpaceTexts.BlockPropertyDescription_OreDetectorRange);
            range.SetLimits(1, 100);
            range.DefaultValue = 100;
            range.Getter = (x) => x.Range;
            range.Setter = (x, v) => x.Range = v;
            range.Writer = (x, result) => result.AppendInt32((int)x.m_oreDetectorComponent.DetectionRadius).Append(" m");

            var broadcastUsingAntennas = new MyTerminalControlCheckbox<MyOreDetector>("BroadcastUsingAntennas", MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas, MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas);
            broadcastUsingAntennas.Getter = (x) => x.m_oreDetectorComponent.BroadcastUsingAntennas;
            broadcastUsingAntennas.Setter = (x, v) => x.SyncObject.SendChangeOreDetector(v);
            broadcastUsingAntennas.EnableAction();
            MyTerminalControlFactory.AddControl(broadcastUsingAntennas);
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_definition = BlockDefinition as MyOreDetectorDefinition;

            SyncObject = new MySyncOreDetector(this);

            var ob = objectBuilder as MyObjectBuilder_OreDetector;

            m_oreDetectorComponent.DetectionRadius = ob.DetectionRadius;
            if (m_oreDetectorComponent.DetectionRadius == 0)
                m_oreDetectorComponent.DetectionRadius = m_definition.MaximumRange;

            m_oreDetectorComponent.BroadcastUsingAntennas = ob.BroadcastUsingAntennas;

            m_oreDetectorComponent.OnCheckControl += OnCheckControl;  

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                MyEnergyConstants.MAX_REQUIRED_POWER_ORE_DETECTOR,
                () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0f);
            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(PowerReceiver,this));

        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_OreDetector;
            builder.DetectionRadius = m_oreDetectorComponent.DetectionRadius;
            builder.BroadcastUsingAntennas = m_oreDetectorComponent.BroadcastUsingAntennas;
            return builder;
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White); ;
        }

        public override void OnUnregisteredFromGridSystems()
        {
            m_oreDetectorComponent.Clear();
            base.OnUnregisteredFromGridSystems();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (HasLocalPlayerAccess())
            {
                m_oreDetectorComponent.Update(PositionComp.GetPosition());
            }
            else
            {
                m_oreDetectorComponent.Clear();
            }
        }

        bool OnCheckControl()
        {
            bool isControlled = Sandbox.Game.World.MySession.ControlledEntity != null && ((MyEntity)Sandbox.Game.World.MySession.ControlledEntity).Parent == Parent;
            return IsWorking && isControlled;
        }

        public float Range
        {
            get 
            {
                return (m_oreDetectorComponent.DetectionRadius / m_definition.MaximumRange) * 100f;
            }
            set
            {
                if (m_oreDetectorComponent.DetectionRadius != value)
                {
                    m_oreDetectorComponent.DetectionRadius = (value / 100f) * m_definition.MaximumRange;
                    RaisePropertiesChanged();
                }
                
            }
        }

        bool IMyComponentOwner<MyOreDetectorComponent>.GetComponent(out MyOreDetectorComponent component)
        {
            component = m_oreDetectorComponent;
            return IsWorking;
        }

        public bool BroadcastUsingAntennas
        {
            get { return m_oreDetectorComponent.BroadcastUsingAntennas; }
            set 
            { 
                m_oreDetectorComponent.BroadcastUsingAntennas = value;
                RaisePropertiesChanged();
            }
        }
        bool IMyOreDetector.BroadcastUsingAntennas { get { return m_oreDetectorComponent.BroadcastUsingAntennas; } }
        float IMyOreDetector.Range { get { return Range; } }
    }
}
