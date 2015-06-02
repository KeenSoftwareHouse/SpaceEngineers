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

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ConveyorTurretBase))]
    abstract class MyLargeConveyorTurretBase : MyLargeTurretBase, IMyConveyorEndpointBlock, IMyInventoryOwner, IMyLargeConveyorTurretBase
    {

        static MyLargeConveyorTurretBase()
        {
            var separator = new MyTerminalControlSeparator<MyLargeConveyorTurretBase>();
            MyTerminalControlFactory.AddControl(separator);
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyLargeConveyorTurretBase>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        protected bool m_useConveyorSystem = true;
        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        private MyMultilineConveyorEndpoint m_endpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_endpoint; }
        }

        public MyLargeConveyorTurretBase()
            : base()
        {
            m_useConveyorSystem = true;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

        }
  
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var builder = objectBuilder as MyObjectBuilder_ConveyorTurretBase;
            var builder2 = objectBuilder as MyObjectBuilder_TurretBase; // Compatibility

            Debug.Assert(builder != null || builder2 != null, "Turret builder was incorrect!");

            if (builder != null)
                m_useConveyorSystem = builder.UseConveyorSystem;
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
                if (MySession.Static.SurvivalMode && Sync.IsServer && IsWorking && m_ammoInventory.VolumeFillFactor < 0.5f)
                {
                    MyGridConveyorSystem.ItemPullRequest(this, m_ammoInventory, OwnerId, m_gunBase.CurrentAmmoMagazineId, 1);
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
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
        }
    }
}
