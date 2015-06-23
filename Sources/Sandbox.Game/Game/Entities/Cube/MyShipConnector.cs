using Havok;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Ingame;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage;
using VRage.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShipConnector))]
    class MyShipConnector : MyFunctionalBlock, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyPowerConsumer, IMyShipConnector
    {
        private enum Mode
        {
            Ejector,
            Connector,
        }

        private bool m_throwOut;
        private bool m_collectAll;
        private MyInventory m_inventory;
        private MyAttachableConveyorEndpoint m_attachableConveyorEndpoint;
        private int m_update10Counter;

        // Use the property instead of the field, because the block's transformation has to be applied
        private Vector3D m_connectionPosition;
        private Vector3D ConnectionPosition
        {
            get
            {
                return Vector3D.Transform(m_connectionPosition, this.PositionComp.LocalMatrix);
            }
        }

        private float m_detectorRadius;

        public bool InConstraint { get { return m_constraint != null; } }
        private HkConstraint m_constraint;

        public bool Connected { get; set; }

        /// <summary>
        /// Whether this block created the constraint and should also remove it. Only valid if Connected == true;
        /// </summary>
        private bool Master { get; set; }

        private MyShipConnector m_other;

        private long m_previouslyConnectedEntityId;
        private bool m_previouslyConnected;

        private bool m_defferedDisconnect = false;

        private static HashSet<MySlimBlock> m_tmpBlockSet = new HashSet<MySlimBlock>();
        
        private MyPhysicsBody m_connectorDummy;
        private Mode m_connectorMode = Mode.Ejector;
        private bool HasConstraint = false;
        
        private MyPowerReceiver m_receiver;
        private List<MyEntity> m_detectedGrids = new List<MyEntity>();
        public string DetectGridsCount 
        {
            get 
            {
               return m_detectedGrids.Count.ToString();
            }
        }

        private List<IMyEntity> m_detectedFloaters = new List<IMyEntity>();
        public MyPowerReceiver PowerReceiver
        {
            get { return m_receiver; }
        }

        static MyShipConnector()
        {
            var stateWriter = new MyTerminalControl<MyShipConnector>.WriterDelegate((b, sb) => b.WriteLockStateValue(sb));

            var throwOut = new MyTerminalControlOnOffSwitch<MyShipConnector>("ThrowOut", MySpaceTexts.Terminal_ThrowOut);
            throwOut.Getter = (block) => block.ThrowOut;
            throwOut.Setter = (block, value) => MySyncShipConnector.SendChangePropertyMessage(value, block, MySyncShipConnector.Properties.ThrowOut);
            throwOut.EnableToggleAction();
            MyTerminalControlFactory.AddControl(throwOut);

            var collectAll = new MyTerminalControlOnOffSwitch<MyShipConnector>("CollectAll", MySpaceTexts.Terminal_CollectAll);
            collectAll.Getter = (block) => block.CollectAll;
            collectAll.Setter = (block, value) => MySyncShipConnector.SendChangePropertyMessage(value, block, MySyncShipConnector.Properties.CollectAll);
            collectAll.EnableToggleAction();
            MyTerminalControlFactory.AddControl(collectAll);

            var lockBtn = new MyTerminalControlButton<MyShipConnector>("Lock", MySpaceTexts.BlockActionTitle_Lock, MySpaceTexts.Blank, (b) => b.TryConnect());
            lockBtn.Enabled = (b) => b.IsWorking && b.InConstraint;
            lockBtn.Visible = (b) => b.m_connectorMode == Mode.Connector;
            lockBtn.EnableAction();
            MyTerminalControlFactory.AddControl(lockBtn);

            var unlockBtn = new MyTerminalControlButton<MyShipConnector>("Unlock", MySpaceTexts.BlockActionTitle_Unlock, MySpaceTexts.Blank, (b) => b.TryDisconnect());
            unlockBtn.Enabled = (b) => b.IsWorking && b.InConstraint;
            unlockBtn.Visible = (b) => b.m_connectorMode == Mode.Connector;
            unlockBtn.EnableAction();
            MyTerminalControlFactory.AddControl(unlockBtn);

            var title = MyTexts.Get(MySpaceTexts.BlockActionTitle_SwitchLock);
            MyTerminalAction<MyShipConnector> switchLockAction = new MyTerminalAction<MyShipConnector>("SwitchLock", title, MyTerminalActionIcons.TOGGLE);
            switchLockAction.Action = (b) => b.TrySwitch();
            switchLockAction.Writer = stateWriter;
            switchLockAction.Enabled = (b) => b.m_connectorMode == Mode.Connector;
            MyTerminalControlFactory.AddAction(switchLockAction);
        }

        public void WriteLockStateValue(StringBuilder sb)
        {
            if (InConstraint && Connected)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Locked));
            else if (InConstraint)
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_ReadyToLock));
            else
                sb.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyValue_Unlocked));
        }

        public void TrySwitch()
        {
            if (InConstraint)
            {
                if (Connected)
                    TryDisconnect();
                else
                    TryConnect();
            }
        }

        public void TryConnect()
        {
            if (InConstraint && !Connected)
            {
                if (Sync.IsServer)
                    Connect();
                else
                    MySyncShipConnector.RequestConnect(this);
            }
        }

        public void TryDisconnect()
        {
            if (InConstraint && Connected)
            {
                if (Sync.IsServer)
                    Detach();
                else
                    MySyncShipConnector.RequestDetach(this);
            }
        }

        public bool ThrowOut
        {
            get { return m_throwOut; }
            set
            {
                if (m_throwOut != value)
                {
                    m_throwOut = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public bool CollectAll
        {
            get { return m_collectAll; }
            set
            {
                if (m_collectAll != value)
                {
                    m_collectAll = value;
                    RaisePropertiesChanged();
                }
            }
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_ShipConnector;
            Vector3 inventorySize = (BlockDefinition.Size * CubeGrid.GridSize) * 0.8f; // 0.8 ~= 0.5^(1/3) to make the inventory volume approx. one half of the block size
            m_inventory = new MyInventory(inventorySize.Volume, inventorySize, MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend, this);
            m_inventory.Init(ob.Inventory);
            m_throwOut = ob.ThrowOut;
            m_collectAll = ob.CollectAll;

            SlimBlock.DeformationRatio = ob.DeformationRatio;

            float consumption = MyEnergyConstants.MAX_REQUIRED_POWER_CONNECTOR;
            if (cubeGrid.GridSizeEnum == MyCubeSize.Small)
            {
                consumption *= 0.01f;
            }

            m_receiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Conveyors,
                false,
                consumption,
                () => base.CheckIsWorking() ? PowerReceiver.MaxRequiredInput : 0f
            );
            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;

            SlimBlock.ComponentStack.IsFunctionalChanged += UpdateReceiver;
            base.EnabledChanged += UpdateReceiver;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            
            if(CubeGrid.CreatePhysics)
                LoadDummies();
            if (Physics != null) Physics.Enabled = true;
            if (m_connectorDummy != null)
            {
                m_connectorDummy.Enabled = true;
            }

            if (ob.ConnectedEntityId != 0) 
            {
                m_previouslyConnected = ob.Connected;
                m_previouslyConnectedEntityId = ob.ConnectedEntityId;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }

            IsWorkingChanged += MyShipConnector_IsWorkingChanged;

            AddDebugRenderComponent(new Components.MyDebugRenderCompoonentShipConnector(this));
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_defferedDisconnect)
            {
                Debug.Assert(Connected, "Deferred disconnect was requested on connector, but it is not connected now!");
                if (Connected)
                    Detach();
                m_defferedDisconnect = false;
                return;
            }

            if (m_previouslyConnectedEntityId != 0)
            {
                MyEntity connectedEntity;
                MyEntities.TryGetEntityById(m_previouslyConnectedEntityId, out connectedEntity);

                Debug.Assert(m_previouslyConnected == false || connectedEntity != null, "Could not find connected entity of the ship connector");
                if (connectedEntity != null)
                {
                    Debug.Assert(connectedEntity is MyShipConnector, "Entity connected to a ship connector was not a ship connector");
                    if (connectedEntity is MyShipConnector)
                    {
                        var otherConnector = connectedEntity as MyShipConnector;

                        // Only one of the connected blocks has to re-create the connection
                        if (!otherConnector.InConstraint || otherConnector.m_other != this)
                        {
                            if (m_previouslyConnected)
                            {
                                ConnectOnInit(otherConnector);
                            }
                            else
                            {
                                CreateConstraintInit(otherConnector);
                            }
                        }
                    }
                }

                m_previouslyConnectedEntityId = 0;
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_ShipConnector;
            ob.Inventory = m_inventory.GetObjectBuilder();
            ob.ThrowOut = m_throwOut;
            ob.CollectAll = m_collectAll;
            if (m_previouslyConnectedEntityId != 0)
            {
                ob.ConnectedEntityId = m_previouslyConnectedEntityId;
                ob.Connected = m_previouslyConnected;
            }
            else
            {
                ob.ConnectedEntityId = m_other == null ? 0 : m_other.EntityId;
                ob.Connected = Connected;
            }
            return ob;
        }

        void MyShipConnector_IsWorkingChanged(MyCubeBlock obj)
        {
            Debug.Assert(obj == this);

            if (Connected)
            {
                if (!IsFunctional)
                {
                    Detach();
                }
                else if (!IsWorking)
                {
                    m_defferedDisconnect = true;
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                }
            }

            UpdateEmissivity();
        }

        private void LoadDummies()
        {
            var finalModel = Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                bool isConnector = dummy.Key.ToLower().Contains("connector");
                bool isEjector = isConnector || dummy.Key.ToLower().Contains("ejector");

                if (!isConnector && !isEjector) continue;

                MatrixD dummyLocal = MatrixD.Normalize(dummy.Value.Matrix);
                m_connectionPosition = dummyLocal.Translation;

                dummyLocal *= this.PositionComp.LocalMatrix;

                Vector3 halfExtents = dummy.Value.Matrix.Scale / 2.0f;
                halfExtents = new Vector3(halfExtents.Z, halfExtents.X, halfExtents.Y);
                m_detectorRadius = halfExtents.AbsMax();

                Vector3 center = dummy.Value.Matrix.Translation;

                if (isConnector)
                    m_connectorDummy = CreatePhysicsBody(Mode.Connector, ref dummyLocal, ref center, ref halfExtents);
                if (isEjector)
                    Physics = CreatePhysicsBody(Mode.Ejector, ref dummyLocal, ref center, ref halfExtents);

                if (isConnector) m_connectorMode = Mode.Connector;
                else m_connectorMode = Mode.Ejector;

                break;
            }
        }

        private MyPhysicsBody CreatePhysicsBody(Mode mode, ref MatrixD dummyLocal, ref Vector3 center, ref Vector3 halfExtents)
        {
            // Only create physical shape for ejectors (on client and server) and for connectors on the server
            MyPhysicsBody physics = null;
            if (mode == Mode.Ejector || Sync.IsServer)
            {
                var detectorShape = CreateDetectorShape(halfExtents, mode);
                if (mode == Mode.Connector)
                {
                    physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_BULLET);
                    physics.IsPhantom = true;
                    physics.CreateFromCollisionObject(detectorShape, center, dummyLocal, null, MyPhysics.ObjectDetectionCollisionLayer);
                }
                else
                {
                    physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_STATIC);
                    physics.IsPhantom = true;
                    physics.CreateFromCollisionObject(detectorShape, center, dummyLocal, null, MyPhysics.CollectorCollisionLayer);
                }
                physics.RigidBody.ContactPointCallbackEnabled = true;
                detectorShape.Base.RemoveReference();
            }
            return physics;
        }

        private HkBvShape CreateDetectorShape(Vector3 extents, Mode mode)
        {
            if (mode == Mode.Ejector)
            {
                var phantom = new HkPhantomCallbackShape(phantom_EnterEjector, phantom_LeaveEjector);
                var detectorShape = new HkBoxShape(extents);
                return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
            }
            else
            {
                var phantom = new HkPhantomCallbackShape(phantom_EnterConnector, phantom_LeaveConnector);
                var detectorShape = new HkSphereShape(extents.AbsMax());
                return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
            }
        }

        private void phantom_LeaveEjector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            var updateEmissivity = (m_detectedFloaters.Count == 2);
            m_detectedFloaters.Remove(body.GetEntity());
            if (updateEmissivity)
                UpdateEmissivity();
        }

        private void phantom_LeaveConnector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            var other = body.GetEntity() as MyCubeGrid;
            if (other == null || other == this.CubeGrid)
                return;

            m_detectedGrids.Remove(other);
        }

        private void phantom_EnterEjector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            var entity = body.GetEntity();

            Debug.Assert(entity is MyFloatingObject);
            if (entity is MyFloatingObject)
            {
                var updateEmissivity = (m_detectedFloaters.Count == 1);
                m_detectedFloaters.Add(entity);
                if (updateEmissivity)
                    UpdateEmissivity();
            }
        }

        private void phantom_EnterConnector(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            var other = body.GetEntity() as MyCubeGrid;
            if (other == null || other == this.CubeGrid)
                return;

            m_detectedGrids.Add(other);
        }

        protected IMyEntity GetOtherEntity(ref HkContactPointEvent value)
        {
            if (value.Base.BodyA.GetEntity() == this)
                return value.Base.BodyB.GetEntity();
            else
                return value.Base.BodyA.GetEntity();
        }

        private void GetBoxFromMatrix(Matrix m, out Vector3 halfExtents, out Vector3 position, out Quaternion orientation)
        {
            halfExtents = Vector3.Zero;
            position = Vector3.Zero;
            orientation = Quaternion.Identity;
        }

        private void UpdateReceiver(MyTerminalBlock block)
        {
            PowerReceiver.Update();
        }

        private void UpdateReceiver()
        {
            PowerReceiver.Update();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory);
            base.OnDestroy();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (InConstraint)
            {
                if (Connected)
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, "Emissive1", null, Color.ForestGreen, null, null, 1);
                else
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, "Emissive1", null, Color.Goldenrod, null, null, 1);
            }
            else
            {
                if (!IsWorking && m_connectorMode == Mode.Connector)
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, "Emissive1", null, Color.Black, null, null, 1);
                else if (m_detectedFloaters.Count < 2 || !IsWorking)
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, "Emissive1", null, Color.Gray, null, null, 1);
                else
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, "Emissive1", null, Color.Red, null, null, 1);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (Sync.IsServer && IsWorking)
            {
                m_update10Counter++;
                if (!InConstraint && m_update10Counter % 8 == 0 && Enabled)
                {
                    if (m_collectAll)
                    {
                        MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, true);
                    }
                    if (m_throwOut && m_detectedFloaters.Count < 2)
                    {
                        TryThrowOutItem();
                    }
                }
                if (m_detectedFloaters.Count == 0 && m_connectorMode == Mode.Connector)
                {
                    if (m_update10Counter % 4 == 0 && Enabled && !InConstraint)
                    {
                        var otherConnector = FindOtherConnector();
                        if (otherConnector != null)
                        {
                            var pos = ConstraintPositionWorld();
                            var otherPos = otherConnector.ConstraintPositionWorld();
                            float len = (otherPos - pos).LengthSquared();

                            if (otherConnector.m_connectorMode == Mode.Connector && otherConnector.IsFunctional && (otherPos - pos).LengthSquared() < 0.35f)
                            {
                                CreateConstraint(otherConnector);
                            }
                        }
                    }
                    else if (InConstraint)
                    {
                        var pos = ConstraintPositionWorld();
                        var otherPos = m_other.ConstraintPositionWorld();
                        if ((otherPos - pos).LengthSquared() > 0.5f)
                        {
                            Detach();
                        }
                    }
                }
            }
            else if (Sync.IsServer && !IsWorking)
            {
                if (InConstraint && !Connected)
                {
                    Detach();
                }
            }
        }

        private void TryThrowOutItem()
        {
            float volume = CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.25f : 0.05f;
            var items = m_inventory.GetItems();
            for (int i = 0; i < m_inventory.GetItems().Count; )
            {
                float rnd = MyUtils.GetRandomFloat(0, CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.5f : 0.07f);
                var circle = MyUtils.GetRandomVector3CircleNormalized();
                Vector3 rndPos = Vector3.Transform(ConnectionPosition, CubeGrid.PositionComp.WorldMatrix) + PositionComp.WorldMatrix.Right * circle.X * rnd + PositionComp.WorldMatrix.Up * circle.Z * rnd;

                MyPhysicalItemDefinition def;
                if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(items[i].Content.GetId(), out def))
                    continue;
                Vector3 forward, up;
                float offset = def.Size.Max();
                if (offset == def.Size.Z)
                {
                    forward = PositionComp.WorldMatrix.Forward;
                    up = PositionComp.WorldMatrix.Up;
                }
                else if (offset == def.Size.Y)
                {
                    forward = PositionComp.WorldMatrix.Right;
                    up = PositionComp.WorldMatrix.Forward;
                }
                else
                {
                    forward = PositionComp.WorldMatrix.Up;
                    up = PositionComp.WorldMatrix.Right;
                }
                offset *= 0.5f;
                rndPos += PositionComp.WorldMatrix.Forward * offset;
                MyFixedPoint itemAmount = (MyFixedPoint)(volume / def.Volume);
                if (items[i].Content.TypeId != typeof(MyObjectBuilder_Ore) &&
                    items[i].Content.TypeId != typeof(MyObjectBuilder_Ingot))
                {
                    itemAmount = MyFixedPoint.Ceiling(itemAmount);
                }
                MyParticleEffect effect;
                MyEntity entity;
                if (items[i].Amount < itemAmount)
                {
                    volume -= ((float)items[i].Amount * def.Volume);
                    entity = MyFloatingObjects.Spawn(items[i], rndPos, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up, CubeGrid.Physics);
                    m_inventory.RemoveItems(items[i].ItemId);
                    i++;
                }
                else
                {
                    var tmpItem = new MyPhysicalInventoryItem(items[i].GetObjectBuilder());
                    tmpItem.Amount = itemAmount;
                    entity = MyFloatingObjects.Spawn(tmpItem, rndPos, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up, CubeGrid.Physics);
                    m_inventory.RemoveItems(items[i].ItemId, itemAmount);
                    volume = 0;
                }
                entity.Physics.LinearVelocity += PositionComp.WorldMatrix.Forward * (1);
                
                if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Collector, out effect))
                {
                    //effect.WorldMatrix = Matrix.CreateWorld(PositionComp.GetPosition(), PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                    effect.WorldMatrix = entity.WorldMatrix;
                    effect.Velocity = CubeGrid.Physics.LinearVelocity;
                    
                    foreach (var gen in effect.GetGenerations())
                    {
                        gen.MotionInheritance.AddKey(0, 1f);
                    }
                }
                break;
            }
        }

        private MyShipConnector FindOtherConnector()
        {
            BoundingSphereD sphere = new BoundingSphereD(ConnectionPosition, m_detectorRadius);
            sphere = sphere.Transform(CubeGrid.PositionComp.WorldMatrix);

            var connector = TryFindConnectorInGrid(ref sphere, CubeGrid, this);
            if (connector != null) return connector;

            foreach (var entity in m_detectedGrids)
            {
                Debug.Assert(entity is MyCubeGrid);
                if (!(entity is MyCubeGrid)) continue;

                var grid = entity as MyCubeGrid;
                if (grid == this.CubeGrid) continue;

                connector = TryFindConnectorInGrid(ref sphere, grid, this);
                if (connector != null) return connector;
            }

            return null;
        }

        private static MyShipConnector TryFindConnectorInGrid(ref BoundingSphereD sphere, MyCubeGrid grid, MyShipConnector thisConnector = null)
        {
            m_tmpBlockSet.Clear();
            grid.GetBlocksInsideSphere(ref sphere, m_tmpBlockSet);

            foreach (var block in m_tmpBlockSet)
            {
                if (block.FatBlock == null || !(block.FatBlock is MyShipConnector)) continue;

                var connector = block.FatBlock as MyShipConnector;
                if (connector.InConstraint) continue;
                if (connector == thisConnector) continue;
                if (!connector.IsWorking) continue;
                if (!connector.FriendlyWithBlock(thisConnector)) continue;

                m_tmpBlockSet.Clear();
                return connector;
            }

            m_tmpBlockSet.Clear();
            return null;
        }

        private void CreateConstraint(MyShipConnector otherConnector)
        {
            var posA = ConstraintPositionInGridSpace();
            var posB = otherConnector.ConstraintPositionInGridSpace();
            var axisA = ConstraintAxisGridSpace();
            var axisB = -otherConnector.ConstraintAxisGridSpace();

            CreateConstraintNosync(otherConnector, ref posA, ref posB, ref axisA, ref axisB);
            MySyncShipConnector.AnnounceApproach(this, otherConnector);
        }

        private void CreateConstraintInit(MyShipConnector otherConnector)
        {
            var posA = ConstraintPositionInGridSpace();
            var posB = otherConnector.ConstraintPositionInGridSpace();
            var axisA = ConstraintAxisGridSpace();
            var axisB = -otherConnector.ConstraintAxisGridSpace();

            CreateConstraintNosync(otherConnector, ref posA, ref posB, ref axisA, ref axisB);
        }

        private void CreateConstraintNosync(MyShipConnector otherConnector, ref Vector3 posA, ref Vector3 posB, ref Vector3 axisA, ref Vector3 axisB)
        {
            var data = new HkHingeConstraintData();
            data.SetInBodySpace(ref posA, ref posB, ref axisA, ref axisB);
            var data2 = new HkMalleableConstraintData();
            data2.SetData(data);
            data.ClearHandle();
            data = null;
            data2.Strength = 0.0003f;

            var newConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, otherConnector.CubeGrid.Physics.RigidBody, data2);
            this.Master = true;
            otherConnector.Master = false;
            SetConstraint(otherConnector, newConstraint);
            otherConnector.SetConstraint(this, newConstraint);

            AddConstraint(newConstraint);
        }

        private void SetConstraint(MyShipConnector other, HkConstraint newConstraint)
        {
            Debug.Assert(!InConstraint);

            m_other = other;
            m_constraint = newConstraint;

            UpdateEmissivity();
        }

        private void UnsetConstraint()
        {
            Debug.Assert(InConstraint);

            m_other = null;
            m_constraint = null;

            UpdateEmissivity();
        }

        private void ChangeConstraint(MyShipConnector other, HkConstraint newConstraint)
        {
            Debug.Assert(InConstraint);

            m_other = other;
            m_constraint = newConstraint;

            UpdateEmissivity();
        }

        public Vector3 ConstraintPositionWorld()
        {
            return Vector3.Transform(ConstraintPositionInGridSpace(), CubeGrid.PositionComp.WorldMatrix);
        }

        private Vector3 ConstraintPositionInGridSpace()
        {
            var cubeCenter = (Max + Min) * CubeGrid.GridSize * 0.5f;
            Vector3 centerOffset = ConnectionPosition - cubeCenter;
            centerOffset = Vector3.DominantAxisProjection(centerOffset);

            MatrixI orientation = new MatrixI(Vector3I.Zero, this.Orientation.Forward, this.Orientation.Up);
            Vector3 outExtents;
            Vector3.Transform(ref centerOffset, ref orientation, out outExtents);

            return cubeCenter + centerOffset;
        }

        private Vector3 ConstraintAxisGridSpace()
        {
            var cubeCenter = (Max + Min) * CubeGrid.GridSize * 0.5f;
            var centerOffset = ConnectionPosition - cubeCenter;
            centerOffset = Vector3.Normalize(Vector3.DominantAxisProjection(centerOffset));

            return centerOffset;
        }

        private Vector3 ProjectPerpendicularFromWorld(Vector3 worldPerpAxis)
        {
            var axis = ConstraintAxisGridSpace();
            var localPerpAxis = Vector3.TransformNormal(worldPerpAxis, CubeGrid.PositionComp.WorldMatrixNormalizedInv);
            var projectionLength = Vector3.Dot(localPerpAxis, axis);
            var ret = Vector3.Normalize(localPerpAxis - projectionLength * axis);
            return Vector3.Normalize(localPerpAxis - projectionLength * axis);
        }

        private void Connect()
        {
            Matrix thisMatrix = this.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();
            Matrix otherMatrix = m_other.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();

            ConnectNosync(ref thisMatrix, ref otherMatrix, m_other);
            MySyncShipConnector.AnnounceConnect(this, m_other, ref thisMatrix, ref otherMatrix);
        }

        private void ConnectOnInit(MyShipConnector otherConnector)
        {
            // This can happen if the other grid failed deserializaing during load, but only after the connector was deserialized
            if (otherConnector.CubeGrid.Physics == null) return;

            Matrix thisMatrix = this.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();
            Matrix otherMatrix = otherConnector.CubeGrid.Physics.RigidBody.GetRigidBodyMatrix();

            ConnectInternal(ref thisMatrix, ref otherMatrix, otherConnector, constructor: true);
        }

        private void ConnectNosync(ref Matrix thisMatrix, ref Matrix otherMatrix, MyShipConnector otherConnector)
        {
            Debug.Assert(m_other == otherConnector);
            Debug.Assert(InConstraint);
            Debug.Assert(!Connected);
            Debug.Assert(m_other != null);
            if (m_other != null)
            {
                Debug.Assert(this.m_constraint == m_other.m_constraint);
                Debug.Assert(this == m_other.m_other);
            }

            if (m_constraint != null)
            {
                RemoveConstraint(otherConnector, m_constraint);
            }

            ConnectInternal(ref thisMatrix, ref otherMatrix, otherConnector, constructor: false);
        }

        private void ConnectInternal(ref Matrix thisMatrix, ref Matrix otherMatrix, MyShipConnector otherConnector, bool constructor)
        {
            Debug.Assert(!m_attachableConveyorEndpoint.AlreadyAttached());
            if (m_attachableConveyorEndpoint.AlreadyAttached()) m_attachableConveyorEndpoint.DetachAll();

            m_attachableConveyorEndpoint.Attach(otherConnector.m_attachableConveyorEndpoint);

            var data = new HkFixedConstraintData();
            data.SetInWorldSpace(ref thisMatrix, ref otherMatrix, ref thisMatrix);
            var newConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, otherConnector.CubeGrid.Physics.RigidBody, data);

            this.Connected = true;
            this.Master = true;
            otherConnector.Connected = true;
            otherConnector.Master = false;
            if (!constructor)
            {
                this.ChangeConstraint(otherConnector, newConstraint);
                otherConnector.ChangeConstraint(this, newConstraint);
            }
            else
            {
                this.SetConstraint(otherConnector, newConstraint);
                otherConnector.SetConstraint(this, newConstraint);
            }

            AddConstraint(newConstraint);

            if (CubeGrid != otherConnector.CubeGrid)
            {
                this.OnConstraintAdded(GridLinkTypeEnum.Logical, otherConnector.CubeGrid);
                this.OnConstraintAdded(GridLinkTypeEnum.Physical, otherConnector.CubeGrid);
            }
        }

        private void AddConstraint(HkConstraint newConstraint)
        {
            HasConstraint = true;
            CubeGrid.Physics.AddConstraint(newConstraint);
        }

        public void Detach(bool synchronize = true)
        {
            Debug.Assert(InConstraint);
            Debug.Assert(m_other != null);
            if (!InConstraint || m_other == null) return;

            DetachInternal();

            if (synchronize && Sync.IsServer)
                MySyncShipConnector.AnnounceDetach(this);
        }

        private void DetachInternal()
        {
            if (this.Connected && !this.Master)
            {
                Debug.Assert(m_other.Master);
                Debug.Assert(m_other.Connected);
                if (!m_other.Connected || !m_other.Master) return;

                m_other.DetachInternal();
                return;
            }

            Debug.Assert(this.InConstraint);
            Debug.Assert(this.m_other != null);
            if (!this.InConstraint || m_other == null) return;

            Debug.Assert(m_other.InConstraint);
            Debug.Assert((this.Connected && m_other.Connected) || (!this.Connected && !m_other.Connected));
            Debug.Assert(!this.Connected || (this.Master && !m_other.Master));
            Debug.Assert(this.m_constraint == m_other.m_constraint);
            Debug.Assert(this == m_other.m_other);
            if (!m_other.InConstraint || m_other.m_other == null) return;
            
            var otherConnector = m_other;
            var constraint = this.m_constraint;
            bool wasConnected = this.Connected;

            this.Connected = false;
            this.Master = false;
            this.UnsetConstraint();
            otherConnector.Connected = false;
            otherConnector.Master = false;
            otherConnector.UnsetConstraint();

            RemoveConstraint(otherConnector, constraint);

            if (wasConnected)
            {
                m_attachableConveyorEndpoint.Detach(otherConnector.m_attachableConveyorEndpoint);
                if (CubeGrid != otherConnector.CubeGrid)
                {
                    this.OnConstraintRemoved(GridLinkTypeEnum.Logical, otherConnector.CubeGrid);
                    this.OnConstraintRemoved(GridLinkTypeEnum.Physical, otherConnector.CubeGrid);
                }
            }
        }

        private void RemoveConstraint(MyShipConnector otherConnector, HkConstraint constraint)
        {
            if (this.HasConstraint)
            {
                CubeGrid.Physics.RemoveConstraint(constraint);
                HasConstraint = false;
            }
            else
            {
                otherConnector.CubeGrid.Physics.RemoveConstraint(constraint);
                otherConnector.HasConstraint = false;
            }
            constraint.Dispose();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            if (m_connectorDummy != null)
                m_connectorDummy.Activate();

            UpdateEmissivity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            if (m_connectorDummy != null)
                m_connectorDummy.Deactivate();

            if (InConstraint)
            {
                m_previouslyConnectedEntityId = m_other.EntityId;
                m_previouslyConnected = true;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                Detach(false);
            }
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (Sync.IsServer && InConstraint)
            {
                if (!m_other.FriendlyWithBlock(this))
                    Detach();
            }
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            if (this.m_connectorDummy != null && this.m_connectorDummy.Enabled && this.m_connectorDummy != source)
            {
                m_connectorDummy.OnWorldPositionChanged(source);
            }
        }

        protected override void Closing()
        {
            if (Connected)
            {
                Detach();
            }

            // The connector dummy won't be disposed of automatically, so we have to do it manually
            if (m_connectorDummy != null)
                m_connectorDummy.Close();

            base.Closing();
        }

        public override void DebugDrawPhysics()
        {
            base.DebugDrawPhysics();

            if (m_connectorDummy != null)
                m_connectorDummy.DebugDraw();
        }

      
        public int InventoryCount
        {
            get { return 1; }
        }

        public MyInventory GetInventory(int index)
        {
            return m_inventory;
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Storage; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_attachableConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_attachableConveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_attachableConveyorEndpoint));
        }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        #region Sync class

        [PreloadRequired]
        class MySyncShipConnector
        {
            public enum Properties
            {
                ThrowOut,
                CollectAll,
            }

            [MessageId(8373, P2PMessageEnum.Reliable)]
            struct ChangePropertyMsg
            {
                public long EntityId;
                public BoolBlit Value;
                public Properties Property;
            }

            /// <summary>
            /// Server lets clients know that they should attach the approaching constraint
            /// </summary>
            [MessageIdAttribute(8374, P2PMessageEnum.Reliable)]
            protected struct ApproachMsg
            {
                public long EntityId;
                public long OtherEntityId;
            }

            /// <summary>
            /// Clients request connection
            /// </summary>
            [MessageIdAttribute(8375, P2PMessageEnum.Reliable)]
            protected struct RequestConnectMsg
            {
                public long MasterEntityId;
            }

            /// <summary>
            /// Server confirms/announces connection to the clients
            /// </summary>
            [MessageIdAttribute(8376, P2PMessageEnum.Reliable)]
            protected struct ConnectMsg
            {
                public long MasterEntityId;
                public long SlaveEntityId;

                public Vector3 MasterForward;
                public Vector3 MasterUp;
                public Vector3 MasterTranslation;

                public Vector3 SlaveForward;
                public Vector3 SlaveUp;
                public Vector3 SlaveTranslation;
            }

            /// <summary>
            /// Server tells the clients to remove all constraints
            /// </summary>
            [MessageIdAttribute(8377, P2PMessageEnum.Reliable)]
            protected struct DetachMsg
            {
                public long MasterEntityId;
            }

            static MySyncShipConnector()
            {
                MySyncLayer.RegisterMessage<ApproachMsg>(OnApproach, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<RequestConnectMsg>(OnConnectRequest, MyMessagePermissions.ToServer);
                MySyncLayer.RegisterMessage<ConnectMsg>(OnAttach, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterMessage<DetachMsg>(OnDetachRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<DetachMsg>(OnDetach, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<ChangePropertyMsg>(OnChangePropertyRequest, MyMessagePermissions.Any, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<ChangePropertyMsg>(OnChangeProperty, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            }

            public static void SendChangePropertyMessage(bool newValue, MyShipConnector block, Properties property)
            {
                var msg = new ChangePropertyMsg();
                msg.EntityId = block.EntityId;
                msg.Value = newValue;
                msg.Property = property;

                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
                }
            }

            private static void OnChangePropertyRequest(ref ChangePropertyMsg msg, MyNetworkClient sender)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

            private static void OnChangeProperty(ref ChangePropertyMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);

                if (entity is MyShipConnector)
                {
                    var block = entity as MyShipConnector;

                    switch (msg.Property)
                    {
                        case Properties.CollectAll:
                            block.m_collectAll = msg.Value;
                            break;
                        case Properties.ThrowOut:
                            block.m_throwOut = msg.Value;
                            break;
                    }
                }
            }

            public static void AnnounceApproach(MyShipConnector thisConnector, MyShipConnector otherConnector)
            {
                var msg = new ApproachMsg();
                msg.EntityId = thisConnector.EntityId;
                msg.OtherEntityId = otherConnector.EntityId;

                Sync.Layer.SendMessageToAll(msg);
            }

            private static void OnApproach(ref ApproachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity1, entity2;
                MyEntities.TryGetEntityById(msg.EntityId, out entity1);
                MyEntities.TryGetEntityById(msg.OtherEntityId, out entity2);
                if (entity1 is MyShipConnector && entity2 is MyShipConnector)
                {
                    var connector1 = entity1 as MyShipConnector;
                    var connector2 = entity2 as MyShipConnector;

                    var posA = connector1.ConstraintPositionInGridSpace();
                    var axisA = connector1.ConstraintAxisGridSpace();

                    var posB = connector2.ConstraintPositionInGridSpace();
                    var axisB = -connector2.ConstraintAxisGridSpace();

                    connector1.CreateConstraintNosync(connector2, ref posA, ref posB, ref axisA, ref axisB);
                }
            }

            public static void RequestConnect(MyShipConnector masterConnector)
            {
                var msg = new RequestConnectMsg();
                msg.MasterEntityId = masterConnector.EntityId;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnConnectRequest(ref RequestConnectMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.MasterEntityId, out entity);
                if (entity is MyShipConnector)
                {
                    var connector = entity as MyShipConnector;
                    
                    if (connector.InConstraint && !connector.Connected)
                        connector.Connect();
                }
            }

            public static void AnnounceConnect(MyShipConnector masterConnector, MyShipConnector slaveConnector, ref Matrix masterMatrix, ref Matrix slaveMatrix)
            {
                var msg = new ConnectMsg();

                msg.MasterEntityId = masterConnector.EntityId;
                msg.SlaveEntityId = slaveConnector.EntityId;

                msg.MasterForward = masterMatrix.Forward;
                msg.MasterUp = masterMatrix.Up;
                msg.MasterTranslation = masterMatrix.Translation;
                msg.SlaveForward = slaveMatrix.Forward;
                msg.SlaveUp = slaveMatrix.Up;
                msg.SlaveTranslation = slaveMatrix.Translation;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnAttach(ref ConnectMsg msg, MyNetworkClient sender)
            {
                MyEntity entity1, entity2;
                MyEntities.TryGetEntityById(msg.MasterEntityId, out entity1);
                MyEntities.TryGetEntityById(msg.SlaveEntityId, out entity2);
                if (entity1 is MyShipConnector && entity2 is MyShipConnector)
                {
                    var connector1 = entity1 as MyShipConnector;
                    var connector2 = entity2 as MyShipConnector;

                    Matrix matrix1 = Matrix.CreateWorld(msg.MasterTranslation, msg.MasterForward, msg.MasterUp);
                    Matrix matrix2 = Matrix.CreateWorld(msg.SlaveTranslation, msg.SlaveForward, msg.SlaveUp);

                    connector1.ConnectNosync(ref matrix1, ref matrix2, connector2);
                }
            }

            public static void RequestDetach(MyShipConnector connector)
            {
                var msg = new DetachMsg();
                msg.MasterEntityId = connector.EntityId;

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            private static void OnDetachRequest(ref DetachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.MasterEntityId, out entity);

                if (entity is MyShipConnector)
                {
                    var connector = entity as MyShipConnector;
                    connector.Detach();
                }
            }

            public static void AnnounceDetach(MyShipConnector connector)
            {
                var msg = new DetachMsg();
                msg.MasterEntityId = connector.EntityId;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }

            private static void OnDetach(ref DetachMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.MasterEntityId, out entity);
                if (entity is MyShipConnector)
                {
                    var connector = entity as MyShipConnector;
                    connector.DetachInternal();
                }
            }
        }

        #endregion


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
        bool IMyShipConnector.IsLocked
        {
            get { return IsWorking && InConstraint; }
        }

        bool IMyShipConnector.IsConnected
        {
            get { return Connected; }
        }

        IMyShipConnector IMyShipConnector.OtherConnector
        {
            get { return m_other; }
        }
    }
}
