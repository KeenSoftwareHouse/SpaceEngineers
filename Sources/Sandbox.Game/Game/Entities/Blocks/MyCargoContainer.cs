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
using Sandbox.ModAPI;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game.ModAPI.Ingame;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CargoContainer))]
    public class MyCargoContainer : MyTerminalBlock, IMyConveyorEndpointBlock, IMyCargoContainer, IMyInventoryOwner
    {
        private MyCargoContainerDefinition m_cargoDefinition;
        
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

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            // Backward compatibility - inventory component not defined in definition files and in entity container
            if (this.GetInventory() == null)
            {
                MyInventory inventory = new MyInventory(m_cargoDefinition.InventorySize.Volume, m_cargoDefinition.InventorySize, MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive);
                Components.Add<MyInventoryBase>(inventory);
                
                if (m_containerType != null && MyFakes.RANDOM_CARGO_PLACEMENT && (cargoBuilder.Inventory == null || cargoBuilder.Inventory.Items.Count == 0))
                    SpawnRandomCargo();
            }

            //Backward compatibility
            if (cargoBuilder.Inventory != null && cargoBuilder.Inventory.Items.Count > 0)
                this.GetInventory().Init(cargoBuilder.Inventory);

            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");
            this.GetInventory().SetFlags(MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive);
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

            //This is de/serialized through component container
            //if (!MyFakes.ENABLE_MEDIEVAL_INVENTORY)
            //    cargoBuilder.Inventory = Inventory.GetObjectBuilder();
            //else
            //    cargoBuilder.Inventory = null;

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
            if (containerDefinition != null && containerDefinition.Items.Length > 0)
            {
                this.GetInventory().GenerateContent(containerDefinition);
            }
        }

        #region Inventory

        bool UseConveyorSystem
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

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to cargo container but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    this.GetInventory().ContentsChanged += Inventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory from cargo container is different type than MyInventory?! Check this.");
            if (removedInventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    removedInventory.ContentsChanged -= Inventory_ContentsChanged;
            }
        }

        #endregion

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory(), true);
            base.OnDestroy();
        }

        #region IMyInventoryOwner

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
            return this.GetInventory(index);
        }

        #endregion

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
