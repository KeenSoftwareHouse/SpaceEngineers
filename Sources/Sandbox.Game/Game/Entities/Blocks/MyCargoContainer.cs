using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.Utils;
using Sandbox.Engine.Utils;
using System.Diagnostics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems.Conveyors;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CargoContainer))]
    class MyCargoContainer : MyTerminalBlock, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyCargoContainer
    {
        private MyCargoContainerDefinition m_cargoDefinition;
        MyInventory m_inventory;
        private bool m_useConveyorSystem = true;

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

        /// <summary>
        /// Use this only for debugging/cheating purposes!
        /// </summary>
        public string ContainerType
        {
            get
            {
                return m_containerType;
            }

            set
            {
                m_containerType = value;
            }
        }
        private string m_containerType = null;

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_cargoDefinition = (MyCargoContainerDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());
            var cargoBuilder = (MyObjectBuilder_CargoContainer)objectBuilder;
            m_containerType = cargoBuilder.ContainerType;

            if (!Components.Has<MyInventoryBase>())
            {
                m_inventory = new MyInventory(m_cargoDefinition.InventorySize.Volume, m_cargoDefinition.InventorySize, MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive, this);
				if(MyFakes.ENABLE_MEDIEVAL_INVENTORY)
					Components.Add<MyInventoryBase>(m_inventory);

                if (m_containerType != null && MyFakes.RANDOM_CARGO_PLACEMENT && (cargoBuilder.Inventory == null || cargoBuilder.Inventory.Items.Count == 0))
                    SpawnRandomCargo();
                else
                    m_inventory.Init(cargoBuilder.Inventory);
            }
            else
            {
                m_inventory = Components.Get<MyInventoryBase>() as MyInventory;
				Debug.Assert(m_inventory != null);
                //m_inventory.Owner = this;
            }

            if(MyPerGameSettings.InventoryMass)
                m_inventory.ContentsChanged += Inventory_ContentsChanged;

            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
            UpdateIsWorking();
        }

        void Inventory_ContentsChanged(MyInventoryBase obj)
        {
            CubeGrid.SetInventoryMassDirty();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_CargoContainer cargoBuilder = (MyObjectBuilder_CargoContainer)base.GetObjectBuilderCubeBlock(copy);

			if (!MyFakes.ENABLE_MEDIEVAL_INVENTORY)
				cargoBuilder.Inventory = m_inventory.GetObjectBuilder();
			else
				cargoBuilder.Inventory = null;

            if (m_containerType != null)
            {
                cargoBuilder.ContainerType = m_containerType;
            }

            return cargoBuilder;
        }

        public void SpawnRandomCargo()
        {
            if (m_containerType == null) return;

            MyContainerTypeDefinition containerDefinition = MyDefinitionManager.Static.GetContainerTypeDefinition(m_containerType);
            if (containerDefinition != null && containerDefinition.Items.Count() > 0)
            {
                m_inventory.GenerateContent(containerDefinition);
            }
        }

        #region Inventory
        public int InventoryCount { get { return 1; } }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        public void SetInventory(MyInventory inventory, int index)
        {
            if(m_inventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    m_inventory.ContentsChanged -= Inventory_ContentsChanged;
            }

            m_inventory = inventory;

            if (m_inventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    m_inventory.ContentsChanged += Inventory_ContentsChanged;
            }
        }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Storage; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                throw new NotImplementedException();
            }
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

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }
        #endregion

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }
    }
}
