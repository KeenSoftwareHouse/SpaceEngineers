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

            m_inventory = new MyInventory(m_cargoDefinition.InventorySize.Volume, m_cargoDefinition.InventorySize, MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive, this);

            var cargoBuilder = (MyObjectBuilder_CargoContainer)objectBuilder;
            m_containerType = cargoBuilder.ContainerType;

            if (m_containerType != null && MyFakes.RANDOM_CARGO_PLACEMENT && (cargoBuilder.Inventory == null || cargoBuilder.Inventory.Items.Count == 0))
            {
                SpawnRandomCargo();
            }
            else
            {
                m_inventory.Init(cargoBuilder.Inventory);
            }
            if(MyPerGameSettings.InventoryMass)
                m_inventory.ContentsChanged += Inventory_ContentsChanged;

            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
            UpdateIsWorking();
        }

        void Inventory_ContentsChanged(MyInventory obj)
        {
            CubeGrid.SetInventoryMassDirty();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_CargoContainer cargoBuilder = (MyObjectBuilder_CargoContainer)base.GetObjectBuilderCubeBlock(copy);

            cargoBuilder.Inventory = m_inventory.GetObjectBuilder();
            if (m_containerType != null)
            {
                cargoBuilder.ContainerType = m_containerType;
            }

            return cargoBuilder;
        }

        internal override float GetMass()
        {
            var mass = base.GetMass();
            if (MyPerGameSettings.InventoryMass)
                return mass + (float)m_inventory.CurrentMass;
            else 
                return mass;
        }
        public void SpawnRandomCargo()
        {
            if (m_containerType == null) return;

            MyContainerTypeDefinition containerDefinition = MyDefinitionManager.Static.GetContainerTypeDefinition(m_containerType);
            if (containerDefinition != null && containerDefinition.Items.Count() > 0)
            {
                int itemNumber = MyUtils.GetRandomInt(containerDefinition.CountMin, containerDefinition.CountMax);
                for (int i = 0; i < itemNumber; ++i)
                {
                    MyContainerTypeDefinition.ContainerTypeItem item = containerDefinition.SelectNextRandomItem();
                    MyFixedPoint amount = (MyFixedPoint)MyRandom.Instance.NextFloat((float)item.AmountMin, (float)item.AmountMax);

                    if (MyDefinitionManager.Static.GetPhysicalItemDefinition(item.DefinitionId).HasIntegralAmounts)
                    {
                        amount = MyFixedPoint.Ceiling(amount); // Use ceiling to avoid amounts equal to 0
                    }

                    amount = MyFixedPoint.Min(m_inventory.ComputeAmountThatFits(item.DefinitionId), amount);
                    if (amount > 0)
                    {
                        var inventoryItem = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item.DefinitionId);
                        m_inventory.AddItems(amount, inventoryItem);
                    }
                }
                containerDefinition.DeselectAll();
            }
        }

        #region Inventory
        public int InventoryCount { get { return 1; } }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
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
