using System.Diagnostics;
using System.Reflection;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using VRage;
using VRage.Game;
using VRage.ModAPI.Ingame;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ConveyorTurretBase))]
    abstract class MyLargeConveyorTurretBase : MyLargeTurretBase, IMyConveyorEndpointBlock, IMyLargeConveyorTurretBase, IMyInventoryOwner
    {

        static MyLargeConveyorTurretBase()
        {
            var separator = new MyTerminalControlSeparator<MyLargeConveyorTurretBase>();
            MyTerminalControlFactory.AddControl(separator);
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyLargeConveyorTurretBase>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => x.UseConveyorSystem =  v;
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        protected readonly Sync<bool> m_useConveyorSystem;

        private MyMultilineConveyorEndpoint m_endpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_endpoint; }
        }

        public MyLargeConveyorTurretBase()
            : base()
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }
  
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_useConveyorSystem.Value = true;

            var builder = objectBuilder as MyObjectBuilder_ConveyorTurretBase;
            var builder2 = objectBuilder as MyObjectBuilder_TurretBase; // Compatibility

            Debug.Assert(builder != null || builder2 != null, "Turret builder was incorrect!");

            if (builder != null)
                m_useConveyorSystem.Value = builder.UseConveyorSystem;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_ConveyorTurretBase)base.GetObjectBuilderCubeBlock(copy);
            builder.UseConveyorSystem = m_useConveyorSystem;
            return builder;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (m_useConveyorSystem)
            {
                if (MySession.Static.SurvivalMode && Sync.IsServer && IsWorking && this.GetInventory().VolumeFillFactor < 0.5f)
                {
                    MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, m_gunBase.CurrentAmmoMagazineId, 1);
                }
            }
        }

        public void InitializeConveyorEndpoint()
        {
            m_endpoint = new MyMultilineConveyorEndpoint(this);
        }

        bool Sandbox.ModAPI.Ingame.IMyLargeConveyorTurretBase.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
        }

        bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem.Value = value;
            }
        }

        #region IMyInventoryOwner implementation

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return UseConveyorSystem;
            }
            set
            {
                UseConveyorSystem = value;
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return MyEntityExtensions.GetInventory(this, index);
        }

        #endregion
    }
}
