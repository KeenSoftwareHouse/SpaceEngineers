using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Graphics.TransparentGeometry.Particles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Game.EntityComponents;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using VRage.Components;
using VRage.Utils;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Collector))]
    class MyCollector : MyFunctionalBlock, IMyInventoryOwner, IMyConveyorEndpointBlock,IMyCollector
    {
        static MyCollector()
        {
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyCollector>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        private MyInventory m_inventory;
        private bool m_useConveyorSystem = true;
        private MyMultilineConveyorEndpoint m_multilineConveyorEndpoint;

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            var def = BlockDefinition as MyPoweredCargoContainerDefinition;
            var ob = objectBuilder as MyObjectBuilder_Collector;
            m_inventory = new MyInventory(def.InventorySize.Volume, def.InventorySize, MyInventoryFlags.CanSend, this);
            m_inventory.Init(ob.Inventory);
            m_inventory.ContentsChanged += Inventory_ContentChangedCallback;
            if (Sync.IsServer && CubeGrid.CreatePhysics)
                LoadDummies();

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute(def.ResourceSinkGroup),
                MyEnergyConstants.MAX_REQUIRED_POWER_COLLECTOR,
                () => base.CheckIsWorking() ? ResourceSink.MaxRequiredInput : 0f);
	        ResourceSink = sinkComp;
			ResourceSink.Update();
			ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink,this));

            SlimBlock.ComponentStack.IsFunctionalChanged += UpdateReceiver;
            base.EnabledChanged += UpdateReceiver;

            m_useConveyorSystem = ob.UseConveyorSystem;
        }

        void UpdateReceiver(MyTerminalBlock block)
        {
            ResourceSink.Update();
        }

        void UpdateReceiver()
        {
            ResourceSink.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_Collector;
            ob.Inventory = m_inventory.GetObjectBuilder();
            ob.UseConveyorSystem = m_useConveyorSystem;
            return ob;
        }


        HashSet<MyFloatingObject> m_entitiesToTake = new HashSet<MyFloatingObject>();
       
        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (Sync.IsServer && IsWorking && m_useConveyorSystem && m_inventory.GetItems().Count > 0)
            {
                MyGridConveyorSystem.PushAnyRequest(this, m_inventory, OwnerId);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            Debug.Assert(Sync.IsServer, "Connector can take objects only on the server!");

            base.UpdateOnceBeforeFrame();
            if (m_entitiesToTake.Count > 0)
            {
                MyParticleEffect effect;
                if(MyParticlesManager.TryCreateParticleEffect((int) MyParticleEffectsIDEnum.Smoke_Collector, out effect))
                    effect.WorldMatrix = MatrixD.CreateWorld(m_entitiesToTake.ElementAt(0).PositionComp.GetPosition(), WorldMatrix.Down, WorldMatrix.Forward);

            }
            foreach (var entity in m_entitiesToTake)
            {
                var floatingEntity = entity as MyFloatingObject;
                m_inventory.TakeFloatingObject(entity);
                m_soundEmitter.PlaySound(m_actionSound);
            }
            //m_entitiesToTake.Clear();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected override void OnStartWorking()
        {
            base.OnStartWorking();
            if(Physics != null)
                Physics.Enabled = true;
        }

        protected override void OnStopWorking()
        {
            base.OnStopWorking();
            if (Physics != null)
                Physics.Enabled = false;
        }
        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory);
            base.OnDestroy();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        private void LoadDummies()
        {
            var finalModel = Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (dummy.Key.ToLower().Contains("collector"))
                {
                    var matrix = dummy.Value.Matrix;
                    Vector3 halfExtents, position;
                    Quaternion orientation;
                    GetBoxFromMatrix(matrix, out halfExtents, out position, out orientation);
                    //difference is small but colision detection seems more stable
                    //if (false)
                    //{

                    //    var detectorShape = new HkBoxShape(halfExtents);
                    //    Physics = new Engine.Physics.MyPhysicsBody(this, Engine.Physics.RigidBodyFlag.RBF_STATIC);
                    //    Physics.IsPhantom = true;
                    //    //Physics.ReportAllContacts = true;
                    //    Physics.CreateFromCollisionObject(detectorShape, matrix.Translation, WorldMatrix, null, MyPhysics.CollectorCollisionLayer);
                    //    Physics.Enabled = IsWorking;
                    //    Physics.RigidBody.ContactPointCallbackEnabled = true;
                    //    Physics.RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
                    //    detectorShape.Base.RemoveReference();
                    //}
                    //else
                    {
                        var detectorShape = CreateFieldShape(halfExtents);
                        Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_STATIC);
                        Physics.IsPhantom = true;
                        Physics.CreateFromCollisionObject(detectorShape, matrix.Translation, WorldMatrix, null, MyPhysics.CollectorCollisionLayer);
                        Physics.Enabled = true;//IsWorking;
                        Physics.RigidBody.ContactPointCallbackEnabled = false;
                        //Physics.RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
                        detectorShape.Base.RemoveReference();
                    }
                    break;
                }
            }
        }

        private void Inventory_ContentChangedCallback(MyInventoryBase inventory)
        {
            if (!Sync.IsServer)
                return;
        
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private HkBvShape CreateFieldShape(Vector3 extents)
        {
            var phantom = new HkPhantomCallbackShape(phantom_Enter, phantom_Leave);
            var detectorShape = new HkBoxShape(extents);
            return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
        }

        private void phantom_Leave(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            if (!Sync.IsServer)
                return;
            VRage.ProfilerShort.Begin("CollectorLeave");
            var entities = body.GetAllEntities();
            foreach(var entity in entities)
                m_entitiesToTake.Remove(entity as MyFloatingObject);
            entities.Clear();
            VRage.ProfilerShort.End();
        }

        private void phantom_Enter(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            if (!Sync.IsServer)
                return;
            VRage.ProfilerShort.Begin("CollectorEnter");
            var entities = body.GetAllEntities();
            foreach (var entity in entities)
            {
                if (entity is MyFloatingObject)
                {
                    m_entitiesToTake.Add(entity as MyFloatingObject);
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                }
            }
            entities.Clear();
            VRage.ProfilerShort.End();
            //if (!Sync.IsServer)
            //    return;
            //var entity = body.GetEntity();
            //if (entity is MyFloatingObject)
            //{
            //    m_inventory.TakeFloatingObject(entity as MyFloatingObject);
            //}
        }

        //isnt used
        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            if (!Sync.IsServer)
                return;
            var entity = value.GetOtherEntity(this);
            if (entity is MyFloatingObject)
            {
                m_entitiesToTake.Add(entity as MyFloatingObject);
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        private void GetBoxFromMatrix(Matrix m, out Vector3 halfExtents, out Vector3 position, out Quaternion orientation)
        {
            var world = Matrix.Normalize(m) * this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.Abs(m.Scale) / 2;
            halfExtents = new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z);
            position = world.Translation;
        }


        public int InventoryCount
        {
            get { return 1; }
        }

        public MyInventory GetInventory(int index)
        {
            return m_inventory;
        }

        public void SetInventory(MyInventory inventory, int index)
        {
            if(m_inventory != null)
            {
                m_inventory.ContentsChanged -= Inventory_ContentChangedCallback;
            }

            m_inventory = inventory;

            if (m_inventory != null)
            {
                m_inventory.ContentsChanged += Inventory_ContentChangedCallback;
            }
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Storage; }
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_multilineConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_multilineConveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_multilineConveyorEndpoint));
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                if (m_useConveyorSystem != value)
                {
                    m_useConveyorSystem = value;
                    RaisePropertiesChanged();
                }
            }
        }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        ModAPI.Interfaces.IMyInventory ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }

        bool Sandbox.ModAPI.Ingame.IMyCollector.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
        }
    }  
}
