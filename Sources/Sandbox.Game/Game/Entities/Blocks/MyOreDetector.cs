#region Using

using System.Collections.Generic;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using System.Text;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Sync;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OreDetector))]
    public class MyOreDetector : MyFunctionalBlock, IMyComponentOwner<MyOreDetectorComponent>, IMyOreDetector
    {
        private MyOreDetectorDefinition m_definition;
        private Dictionary <string, Vector3D> m_closestEachElement = new Dictionary <string, Vector3D>(); //I use the same collection to reduce heap allocations.

        private MyOreDetectorComponent m_oreDetectorComponent = new MyOreDetectorComponent();  
        bool getOreRateLimited; 

        Sync<bool> m_broadcastUsingAntennas;

        public MyOreDetector()
        {
 	    getOreRateLimited = true;
	
#if XB1 // XB1_SYNC_NOREFLECTION
            m_broadcastUsingAntennas = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_broadcastUsingAntennas.ValueChanged += (entity) => BroadcastChanged();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyOreDetector>())
                return;
            base.CreateTerminalControls();
            var range = new MyTerminalControlSlider<MyOreDetector>("Range", MySpaceTexts.BlockPropertyTitle_OreDetectorRange, MySpaceTexts.BlockPropertyDescription_OreDetectorRange);
            range.SetLimits(1, 100);
            range.DefaultValue = 100;
            range.Getter = (x) => x.Range;
            range.Setter = (x, v) => x.Range = v;
            range.Writer = (x, result) => result.AppendInt32((int)x.m_oreDetectorComponent.DetectionRadius).Append(" m");
            MyTerminalControlFactory.AddControl(range);

            var broadcastUsingAntennas = new MyTerminalControlCheckbox<MyOreDetector>("BroadcastUsingAntennas", MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas, MySpaceTexts.BlockPropertyDescription_BroadcastUsingAntennas);
            broadcastUsingAntennas.Getter = (x) => x.m_oreDetectorComponent.BroadcastUsingAntennas;
            broadcastUsingAntennas.Setter = (x, v) => x.m_broadcastUsingAntennas.Value = v;
            broadcastUsingAntennas.EnableAction();
            MyTerminalControlFactory.AddControl(broadcastUsingAntennas);
        }

        void BroadcastChanged()
        {
            BroadcastUsingAntennas = m_broadcastUsingAntennas;
        }

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            m_definition = BlockDefinition as MyOreDetectorDefinition;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                m_definition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_ORE_DETECTOR,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0f);
            ResourceSink = sinkComp;
           
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;

            base.Init(objectBuilder, cubeGrid);


            var ob = objectBuilder as MyObjectBuilder_OreDetector;

            m_oreDetectorComponent.DetectionRadius = ob.DetectionRadius;
            if (m_oreDetectorComponent.DetectionRadius == 0)
                m_oreDetectorComponent.DetectionRadius = m_definition.MaximumRange;

            m_oreDetectorComponent.BroadcastUsingAntennas = ob.BroadcastUsingAntennas;
            m_broadcastUsingAntennas.Value = m_oreDetectorComponent.BroadcastUsingAntennas;

            m_oreDetectorComponent.OnCheckControl += OnCheckControl;
            ResourceSink.Update();
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

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
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
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
	    getOreRateLimited = true;
	    
            base.UpdateBeforeSimulation100();
            if (HasLocalPlayerAccess())
            {
                m_oreDetectorComponent.Update (PositionComp.GetPosition());
            }

            else
            {
                m_oreDetectorComponent.Clear();
            }
        }

        bool OnCheckControl()
        {
            bool isControlled = Sandbox.Game.World.MySession.Static.ControlledEntity != null && ((MyEntity)Sandbox.Game.World.MySession.Static.ControlledEntity).Parent == Parent;
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

        bool ModAPI.Ingame.IMyOreDetector.BroadcastUsingAntennas { get { return m_oreDetectorComponent.BroadcastUsingAntennas; } }
        float ModAPI.Ingame.IMyOreDetector.Range { get { return Range; } }

        public void GetOreMarkers (ref List <ModAPI.Ingame.MyOreMarker> usersList) //Imprinting on the reference parameter is cheaper than a return List<T> due to heap allocations. 
        {                 
            if (getOreRateLimited)
            {
                getOreRateLimited = false;
                usersList.Clear();
                Vector3D blockCoordinates = new Vector3D (base.PositionComp.GetPosition());
                m_oreDetectorComponent.Update (blockCoordinates, false);

                foreach (MyEntityOreDeposit deposit in m_oreDetectorComponent.DetectedDeposits)
                {
                    for (int i = 0; i < deposit.Materials.Count; i++)
                    {                                                 
                        MyEntityOreDeposit.Data depositData = deposit.Materials[i];
                        Vector3D cachesPosition = new Vector3D();
                        depositData.ComputeWorldPosition (deposit.VoxelMap, out cachesPosition);                    
                        string cachesElement = deposit.Materials[i].Material.MinedOre;

                        if (m_closestEachElement.ContainsKey (cachesElement) == false)
                        {
                            m_closestEachElement.Add (cachesElement, cachesPosition); //I decided Dictionary was the best way to group nearest markers since all I need is two variables.                            
                        }

                        else
                        {      
                            Vector3D difference = blockCoordinates - cachesPosition;                        
                            Vector3D previousDifference = m_closestEachElement[cachesElement] - cachesPosition; 
                            float distanceToCache = (float) difference.LengthSquared(); //explicitly converted in order to estimate the actual hud markers as close as possible.                   
                            float previousDistance = (float) previousDifference.LengthSquared();
                                                                           
                            if (distanceToCache < previousDistance)    
                            {                                                                             
                                m_closestEachElement[cachesElement] = cachesPosition; //I only want the nearest of each element. 
                            }                                                       
                        }
                    }
                }
                       
                foreach (KeyValuePair <string, Vector3D> marker in m_closestEachElement)
                {
                    usersList.Add (new ModAPI.Ingame.MyOreMarker (marker.Key, marker.Value));
                }
                m_closestEachElement.Clear();
            }
        }       
    }
}
