#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;

using Sandbox.Game.GameSystems.Conveyors;
using System.Text;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Graphics;
using Sandbox.Game.GUI;
using System.Runtime.InteropServices;
using Sandbox.Game.Screens.Helpers;

using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Blocks;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    public class MyCubeGridSystems
    {
        internal MyPowerDistributor PowerDistributor { get; private set; }
        internal MyGridTerminalSystem TerminalSystem { get; private set; }
        internal MyGridConveyorSystem ConveyorSystem { get; private set; }
        internal MyGridThrustSystem ThrustSystem { get; private set; }
        internal MyGridGyroSystem GyroSystem { get; private set; }
        internal MyGridWeaponSystem WeaponSystem { get; private set; }
        internal MyGridReflectorLightSystem ReflectorLightSystem { get; private set; }
        internal MyGridWheelSystem WheelSystem { get; private set; }
        internal MyGridLandingSystem LandingSystem { get; private set; }
        internal MyGroupControlSystem ControlSystem { get; private set; }
        internal MyGridCameraSystem CameraSystem { get; private set; }
        /// <summary>
        /// Can be null if Oxygen option is disabled
        /// </summary>
        public MyGridOxygenSystem OxygenSystem { get; private set; }
        public MyGridJumpDriveSystem JumpSystem { get; private set; }

        private readonly MyCubeGrid m_cubeGrid;
        protected MyCubeGrid CubeGrid { get { return m_cubeGrid; } }

        private Action<MyBlockGroup> m_terminalSystem_GroupAdded;
        private Action<MyBlockGroup> m_terminalSystem_GroupRemoved;

        private bool m_blocksRegistered = false;

        public MyCubeGridSystems(MyCubeGrid grid)
        {
            m_cubeGrid = grid;

            m_terminalSystem_GroupAdded = TerminalSystem_GroupAdded;
            m_terminalSystem_GroupRemoved = TerminalSystem_GroupRemoved;

            ThrustSystem = new MyGridThrustSystem(m_cubeGrid);
            GyroSystem = new MyGridGyroSystem(m_cubeGrid);
            WeaponSystem = new MyGridWeaponSystem();
            ReflectorLightSystem = new MyGridReflectorLightSystem(m_cubeGrid);
            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                WheelSystem = new MyGridWheelSystem(m_cubeGrid);
            }
            ConveyorSystem = new MyGridConveyorSystem(m_cubeGrid);
            LandingSystem = new MyGridLandingSystem();
            ControlSystem = new MyGroupControlSystem();
            CameraSystem = new MyGridCameraSystem(m_cubeGrid);

            if (MySession.Static.Settings.EnableOxygen)
            {
                OxygenSystem = new MyGridOxygenSystem(m_cubeGrid);
            }
            if (MyPerGameSettings.EnableJumpDrive)
            {
                JumpSystem = new MyGridJumpDriveSystem(m_cubeGrid);
            }

            m_cubeGrid.SyncObject.PowerProducerStateChanged += SyncObject_PowerProducerStateChanged;

            m_blocksRegistered = true;
        }

        public virtual void Init(MyObjectBuilder_CubeGrid builder)
        {
            ThrustSystem.DampenersEnabled = builder.DampenersEnabled;

            if (WheelSystem != null)
                WheelSystem.HandBrake = builder.Handbrake;

            if (MySession.Static.Settings.EnableOxygen)
            {
                OxygenSystem.Init(builder.OxygenAmount);
            }

            if (MyPerGameSettings.EnableJumpDrive)
            {
                JumpSystem.Init(builder.JumpDriveDirection, builder.JumpElapsedTicks);
            }
        }

        public virtual void BeforeBlockDeserialization(MyObjectBuilder_CubeGrid builder)
        {
            ConveyorSystem.BeforeBlockDeserialization(builder.ConveyorLines);
        }

        public virtual void AfterBlockDeserialization()
        {
            ConveyorSystem.AfterBlockDeserialization();
            ConveyorSystem.PowerReceiver.Update();
        }

        public virtual void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("Thrusters and gyro");
            ThrustSystem.UpdateBeforeSimulation();
            GyroSystem.UpdateBeforeSimulation();
            ProfilerShort.End();

            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                ProfilerShort.Begin("Wheels");
                WheelSystem.UpdateBeforeSimulation();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Conveyors");
            ConveyorSystem.UpdateBeforeSimulation();
            ProfilerShort.End();

            ProfilerShort.Begin("Control");
            ControlSystem.UpdateBeforeSimulation();
            ProfilerShort.End();

            ProfilerShort.Begin("Cameras");
            CameraSystem.UpdateBeforeSimulation();
            ProfilerShort.End();

            if (MySession.Static.Settings.EnableOxygen)
            {
                ProfilerShort.Begin("Oxygen");
                OxygenSystem.UpdateBeforeSimulation();
                ProfilerShort.End();
            }

            if (MyPerGameSettings.EnableJumpDrive)
            {
                ProfilerShort.Begin("Jump");
                JumpSystem.UpdateBeforeSimulation();
                ProfilerShort.End();
            }
        }

        public virtual void PrepareForDraw()
        {
            ConveyorSystem.PrepareForDraw();
            CameraSystem.PrepareForDraw();
        }

        public void UpdatePower()
        {
            if (PowerDistributor != null)
            {
                PowerDistributor.UpdateBeforeSimulation10();
            }
        }

        public virtual void UpdateOnceBeforeFrame()
        {
        }

        public virtual void UpdateBeforeSimulation10()
        {
            UpdatePower();
            CameraSystem.UpdateBeforeSimulation10();
            ConveyorSystem.UpdateBeforeSimulation10();
        }

        public virtual void UpdateBeforeSimulation100()
        {
            if (MySession.Static.Settings.EnableOxygen)
            {
                OxygenSystem.UpdateBeforeSimulation100();
            }
        }

        public virtual void GetObjectBuilder(MyObjectBuilder_CubeGrid ob)
        {
            ob.DampenersEnabled = ThrustSystem.DampenersEnabled;

            ConveyorSystem.SerializeLines(ob.ConveyorLines);
            if (ob.ConveyorLines.Count == 0)
                ob.ConveyorLines = null;

            if (WheelSystem != null)
                ob.Handbrake = WheelSystem.HandBrake;

            if (MySession.Static.Settings.EnableOxygen)
            {
                ob.OxygenAmount = OxygenSystem.GetOxygenAmount();
            }

            if (MyPerGameSettings.EnableJumpDrive)
            {
                ob.JumpDriveDirection = JumpSystem.GetJumpDriveDirection();
                ob.JumpElapsedTicks = JumpSystem.GetJumpElapsedTicks();
            }
        }

        public virtual void AddGroup(MyBlockGroup group)
        {
            if (TerminalSystem != null)
            {
                TerminalSystem.GroupAdded -= m_terminalSystem_GroupAdded;
                TerminalSystem.AddUpdateGroup(group);
                TerminalSystem.GroupAdded += m_terminalSystem_GroupAdded;
            }
        }

        public virtual void RemoveGroup(MyBlockGroup group)
        {
            if (TerminalSystem != null)
            {
                TerminalSystem.GroupRemoved -= m_terminalSystem_GroupRemoved;
                TerminalSystem.RemoveGroup(group);
                TerminalSystem.GroupRemoved += m_terminalSystem_GroupRemoved;
            }
        }

        public virtual void OnAddedToGroup(MyGridLogicalGroupData group)
        {
            Debug.Assert(group.TerminalSystem != null, "Terminal system is null!");
            TerminalSystem = group.TerminalSystem;
            PowerDistributor = group.PowerDistributor;
            WeaponSystem = group.WeaponSystem;

            PowerDistributor.AddConsumer(ThrustSystem);
            PowerDistributor.AddConsumer(GyroSystem);
            PowerDistributor.AddConsumer(ConveyorSystem);

            foreach (var g in m_cubeGrid.BlockGroups)
                TerminalSystem.AddUpdateGroup(g);
            TerminalSystem.GroupAdded += m_terminalSystem_GroupAdded;
            TerminalSystem.GroupRemoved += m_terminalSystem_GroupRemoved;

            foreach (var block in m_cubeGrid.GetBlocks())
            {
                if (block.FatBlock == null)
                    continue;
                if (!block.FatBlock.MarkedForClose)
                {
                    var functionalBlock = block.FatBlock as MyTerminalBlock;
                    if (functionalBlock != null)
                        TerminalSystem.Add(functionalBlock);

                    var producer = block.FatBlock as IMyPowerProducer;
                    if (producer != null)
                        PowerDistributor.AddProducer(producer);

                    var consumer = block.FatBlock as IMyPowerConsumer;
                    if (consumer != null)
                        PowerDistributor.AddConsumer(consumer);

                    var socketOwner = block.FatBlock as IMyRechargeSocketOwner;
                    if (socketOwner != null)
                        socketOwner.RechargeSocket.PowerDistributor = group.PowerDistributor;

                    var weapon = block.FatBlock as IMyGunObject<MyDeviceBase>;
                    if (weapon != null)
                        WeaponSystem.Register(weapon);
                }
            }
        }

        public virtual void OnRemovedFromGroup(MyGridLogicalGroupData group)
        {
            if (m_blocksRegistered)
            {
                ProfilerShort.Begin("Removing block groups from grid group");
                TerminalSystem.GroupAdded -= m_terminalSystem_GroupAdded;
                TerminalSystem.GroupRemoved -= m_terminalSystem_GroupRemoved;
                foreach (var g in m_cubeGrid.BlockGroups)
                    TerminalSystem.RemoveGroup(g);
                ProfilerShort.End();

                foreach (var block in m_cubeGrid.GetBlocks())
                {
                    if (block.FatBlock == null)
                        continue;

                    var functionalBlock = block.FatBlock as MyTerminalBlock;
                    if (functionalBlock != null)
                        TerminalSystem.Remove(functionalBlock);

                    var producer = block.FatBlock as IMyPowerProducer;
                    if (producer != null)
                        PowerDistributor.RemoveProducer(producer);

                    var consumer = block.FatBlock as IMyPowerConsumer;
                    if (consumer != null)
                    {
                        PowerDistributor.RemoveConsumer(consumer, resetConsumerInput: false, markedForClose: block.FatBlock.MarkedForClose);
                    }

                    var socketOwner = block.FatBlock as IMyRechargeSocketOwner;
                    if (socketOwner != null)
                        socketOwner.RechargeSocket.PowerDistributor = null;

                    var weapon = block.FatBlock as IMyGunObject<MyDeviceBase>;
                    if (weapon != null)
                        WeaponSystem.Unregister(weapon);
                }
            }

            PowerDistributor.RemoveConsumer(ConveyorSystem, resetConsumerInput: false);
            PowerDistributor.RemoveConsumer(GyroSystem, resetConsumerInput: false);
            PowerDistributor.RemoveConsumer(ThrustSystem, resetConsumerInput: false);

            PowerDistributor = null;
            TerminalSystem = null;
            WeaponSystem = null;
        }

        public void OnAddedToGroup(MyGridPhysicalGroupData group)
        {
            ControlSystem = group.ControlSystem;

            foreach (var block in m_cubeGrid.GetBlocks())
            {
                if (block.FatBlock == null)
                    continue;

                var controllerBlock = block.FatBlock as MyShipController;
                if (controllerBlock != null && controllerBlock.ControllerInfo.Controller != null && controllerBlock.EnableShipControl)
                {
                    ControlSystem.AddControllerBlock(controllerBlock);
                }
            }

            ControlSystem.AddGrid(CubeGrid);
        }

        public void OnRemovedFromGroup(MyGridPhysicalGroupData group)
        {
            ControlSystem.RemoveGrid(CubeGrid);

            if (m_blocksRegistered)
            {
                foreach (var block in m_cubeGrid.GetBlocks())
                {
                    if (block.FatBlock == null)
                        continue;

                    var controllerBlock = block.FatBlock as MyShipController;
                    if (controllerBlock != null && controllerBlock.ControllerInfo.Controller != null && controllerBlock.EnableShipControl)
                    {
                        ControlSystem.RemoveControllerBlock(controllerBlock);
                    }
                }
            }

            ControlSystem = null;
        }

        public virtual void BeforeGridClose()
        {
            ConveyorSystem.IsClosing = true;
            ReflectorLightSystem.IsClosing = true;
        }

        public virtual void AfterGridClose()
        {
            ConveyorSystem.AfterGridClose();
            if (MyPerGameSettings.EnableJumpDrive)
            {
                JumpSystem.AfterGridClose();
            }
            m_blocksRegistered = false;
        }

        public virtual void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_GRID_TERMINAL_SYSTEMS)
            {
                MyRenderProxy.DebugDrawText3D(m_cubeGrid.WorldMatrix.Translation, TerminalSystem.GetHashCode().ToString(), Color.NavajoWhite, 1.0f, false);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_CONVEYORS)
            {
                ConveyorSystem.DebugDraw(m_cubeGrid);
                ConveyorSystem.DebugDrawLinePackets();
            }

            if (MySession.Static.Settings.EnableOxygen && MyDebugDrawSettings.DEBUG_DRAW_OXYGEN)
            {
                OxygenSystem.DebugDraw();
            }
        }

        public virtual bool IsTrash()
        {
            // Powered grids are not trash
            if (this.PowerDistributor.PowerState != MyPowerStateEnum.NoPower)
                return false;

            // Controlled grids are not trash
            if (ControlSystem.IsControlled)
                return false;

            return true;
        }

        public virtual void RegisterInSystems(MyCubeBlock block)
        {
            if (PowerDistributor != null)
            {
                var powerProducer = block as IMyPowerProducer;
                if (powerProducer != null)
                    PowerDistributor.AddProducer(powerProducer);

                var powerConsumer = block as IMyPowerConsumer;
                if (powerConsumer != null)
                    PowerDistributor.AddConsumer(powerConsumer);

                var socketOwner = block as IMyRechargeSocketOwner;
                if (socketOwner != null)
                    socketOwner.RechargeSocket.PowerDistributor = PowerDistributor;
            }

            if (WeaponSystem != null)
            {
                var weapon = block as IMyGunObject<MyDeviceBase>;
                if (weapon != null)
                    WeaponSystem.Register(weapon);
            }

            if (TerminalSystem != null)
            {
                var functionalBlock = block as MyTerminalBlock;
                if (functionalBlock != null)
                    TerminalSystem.Add(functionalBlock);
            }

            // CH: We probably don't need to register controller blocks here. Block that's being added to a grid should not have a controller set
            var controllableBlock = block as MyShipController;
            Debug.Assert(controllableBlock == null || controllableBlock.ControllerInfo.Controller == null, "Controller of added block is not null. Call Cestmir");
            /*if (ControlSystem != null)
            {
                var controllableBlock = block as MyShipController;
                if (controllableBlock != null && controllableBlock.ControllerInfo.Controller != null)
                    ControlSystem.AddControllerBlock(controllableBlock);
            }*/

            var inventoryBlock = block as IMyInventoryOwner;
            if (inventoryBlock != null)
                ConveyorSystem.Add(inventoryBlock);

            var conveyorBlock = block as IMyConveyorEndpointBlock;
            if (conveyorBlock != null)
            {
                conveyorBlock.InitializeConveyorEndpoint();
                ConveyorSystem.AddConveyorBlock(conveyorBlock);
            }

            var segmentBlock = block as IMyConveyorSegmentBlock;
            if (segmentBlock != null)
            {
                segmentBlock.InitializeConveyorSegment();
                ConveyorSystem.AddSegmentBlock(segmentBlock);
            }

            var reflectorLight = block as MyReflectorLight;
            if (reflectorLight != null)
                ReflectorLightSystem.Register(reflectorLight);

            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                var wheel = block as MyMotorSuspension;
                if (wheel != null)
                    WheelSystem.Register(wheel);
            }

            var landingGear = block as IMyLandingGear;
            if (landingGear != null)
                LandingSystem.Register(landingGear);

            var thrust = block as MyThrust;
            if (thrust != null)
                ThrustSystem.Register(thrust);

            var gyro = block as MyGyro;
            if (gyro != null)
                GyroSystem.Register(gyro);

            var camera = block as MyCameraBlock;
            if (camera != null)
                CameraSystem.Register(camera);

            block.OnRegisteredToGridSystems();
        }

        public virtual void UnregisterFromSystems(MyCubeBlock block)
        {
            // Note: PowerDistributor, WeaponSystem and TemrminalSystem can be null on closing (they are not in the ship but in the logical group). That's why they are null-checked
            if (PowerDistributor != null)
            {
                ProfilerShort.Begin("Unregister Power producer");
                var powerProducer = block as IMyPowerProducer;
                if (powerProducer != null)
                    PowerDistributor.RemoveProducer(powerProducer);

                ProfilerShort.BeginNextBlock("Unregister Power consumer");
                var powerConsumer = block as IMyPowerConsumer;
                if (powerConsumer != null)
                    PowerDistributor.RemoveConsumer(powerConsumer);
                ProfilerShort.End();

                var socketOwner = block as IMyRechargeSocketOwner;
                if (socketOwner != null)
                    socketOwner.RechargeSocket.PowerDistributor = null;
            }

            ProfilerShort.Begin("Unregister gun object");
            if (WeaponSystem != null)
            {
                var weapon = block as IMyGunObject<MyDeviceBase>;
                if (weapon != null)
                    WeaponSystem.Unregister(weapon);
            }

            ProfilerShort.BeginNextBlock("Unregister functional block");
            if (TerminalSystem != null)
            {
                var functionalBlock = block as MyTerminalBlock;
                if (functionalBlock != null)
                    TerminalSystem.Remove(functionalBlock);
            }

            // CH: We probably don't need to unregister controller blocks here. It's done in ShipController's OnUnregisteredFromGridSystems
            /*ProfilerShort.BeginNextBlock("Unregister controller block");
            if (ControlSystem != null)
            {
                var controllableBlock = block as MyShipController;
                if (controllableBlock != null && controllableBlock.ControllerInfo.Controller != null)
                    ControlSystem.RemoveControllerBlock(controllableBlock);
            }*/

            ProfilerShort.BeginNextBlock("Unregister inventory block");
            var inventoryBlock = block as IMyInventoryOwner;
            if (inventoryBlock != null)
                ConveyorSystem.Remove(inventoryBlock);

            ProfilerShort.BeginNextBlock("Unregister conveyor block");
            var conveyorBlock = block as IMyConveyorEndpointBlock;
            if (conveyorBlock != null)
                ConveyorSystem.RemoveConveyorBlock(conveyorBlock);

            ProfilerShort.BeginNextBlock("Unregister segment block");
            var segmentBlock = block as IMyConveyorSegmentBlock;
            if (segmentBlock != null)
                ConveyorSystem.RemoveSegmentBlock(segmentBlock);

            ProfilerShort.BeginNextBlock("Unregister Reflector light");
            var reflectorLight = block as MyReflectorLight;
            if (reflectorLight != null)
                ReflectorLightSystem.Unregister(reflectorLight);

            if (MyFakes.ENABLE_WHEEL_CONTROLS_IN_COCKPIT)
            {
                ProfilerShort.BeginNextBlock("Unregister wheel");
                var wheel = block as MyMotorSuspension;
                if (wheel != null)
                    WheelSystem.Unregister(wheel);
            }

            ProfilerShort.BeginNextBlock("Unregister landing gear");
            var gear = block as IMyLandingGear;
            if (gear != null)
                LandingSystem.Unregister(gear);

            ProfilerShort.BeginNextBlock("Unregister thrust");
            var thrust = block as MyThrust;
            if (thrust != null)
                ThrustSystem.Unregister(thrust);

            ProfilerShort.BeginNextBlock("Unregister gyro");
            var gyro = block as MyGyro;
            if (gyro != null)
                GyroSystem.Unregister(gyro);

            ProfilerShort.BeginNextBlock("Unregister camera");
            var camera = block as MyCameraBlock;
            if (camera != null)
                CameraSystem.Unregister(camera);

            ProfilerShort.BeginNextBlock("block.OnUnregisteredFromGridSystems()");
            block.OnUnregisteredFromGridSystems();

            ProfilerShort.End();
        }

        private void SyncObject_PowerProducerStateChanged(MyMultipleEnabledEnum enabledState,long playerId)
        {
            PowerDistributor.ChangeProducersState(enabledState, playerId);
        }

        private void TerminalSystem_GroupRemoved(MyBlockGroup group)
        {
            if (group.CubeGrid != null && group.CubeGrid != m_cubeGrid)
                return;
            foreach (var g in m_cubeGrid.BlockGroups)
                if (g.Name.CompareTo(group.Name) == 0)
                {
                    Debug.Assert(g != group, "TerminalSystem should have own group copy");
                    g.Blocks.Clear();
                    m_cubeGrid.BlockGroups.Remove(g);
                    m_cubeGrid.SyncObject.ModifyGroup(g);
                    break;
                }
        }

        private void TerminalSystem_GroupAdded(MyBlockGroup group)
        {
            foreach (var g in m_cubeGrid.BlockGroups)
                if (g.Name.CompareTo(group.Name) == 0)
                {
                    var oldCount = g.Blocks.Count;
                    g.Blocks.Clear();
                    foreach (var b in group.Blocks)
                        if (b.CubeGrid == m_cubeGrid)
                            g.Blocks.Add(b);
                    if (oldCount != g.Blocks.Count)
                        m_cubeGrid.SyncObject.ModifyGroup(g);
                    if (g.Blocks.Count == 0)
                        m_cubeGrid.BlockGroups.Remove(g);
                    return;
                }
            var gr = new MyBlockGroup(m_cubeGrid);
            gr.Name.AppendStringBuilder(group.Name);
            foreach (var b in group.Blocks)
                if (b.CubeGrid == m_cubeGrid)
                    gr.Blocks.Add(b);
            if (gr.Blocks.Count > 0)
            {
                m_cubeGrid.BlockGroups.Add(gr);
                m_cubeGrid.SyncObject.ModifyGroup(gr);
            }
        }
    }
}
