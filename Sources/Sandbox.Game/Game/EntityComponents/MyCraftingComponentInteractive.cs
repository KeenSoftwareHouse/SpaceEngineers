using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Network;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using System.Diagnostics;
using VRage.Game;
using VRage.ModAPI;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_CraftingComponentInteractive))]
    public class MyCraftingComponentInteractive : MyCraftingComponentBase, IMyEventProxy
    {
        // TODO: Move this to definition..
        private int m_lastUpdateTime;
        private bool m_productionEnabled;
        private MyEntity3DSoundEmitter m_soundEmitter;
        private IMyEntity m_lastEntityInteraction;

        public MySoundPair ActionSound { get; set; }

        public MyCraftingComponentInteractive()
        {
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var craftDefinition = definition as MyCraftingComponentInteractiveDefinition;

            System.Diagnostics.Debug.Assert(craftDefinition != null, "Trying to initialize crafting component from wrong definition type?");


            if (craftDefinition != null)
            {
                ActionSound = new MySoundPair(craftDefinition.ActionSound);
                m_craftingSpeedMultiplier = craftDefinition.CraftingSpeedMultiplier;

                foreach (var blueprintClass in craftDefinition.AvailableBlueprintClasses)
                {
                    var classDefinition = MyDefinitionManager.Static.GetBlueprintClass(blueprintClass);
                    System.Diagnostics.Debug.Assert(classDefinition != null, blueprintClass + " blueprint class definition was not found.");
                    m_blueprintClasses.Add(classDefinition);
                }
            }
        }

        protected override void UpdateProduction_Implementation()
        {
            if (!m_productionEnabled)
                return;

            if (IsProducing)
            {
                UpdateCurrentItem();
                UpdateProductionSound();
            }
            else if (!IsProductionDone)
            {
                SelectItemToProduction();

                if (m_currentItem != -1)
                {
                    UpdateCurrentItem();
                    UpdateProductionSound();
                }
            }

            if (!IsProducing && m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
        }

        private void UpdateProductionSound()
        {
            if (m_soundEmitter == null)
            {
                m_soundEmitter = new MyEntity3DSoundEmitter(Entity as MyEntity);
            }

            if (this.m_currentItemStatus < 1f)
            {
                var blueprint = this.GetCurrentItemInProduction();
                if (blueprint != null && blueprint.Blueprint.ProgressBarSoundCue != null)
                {
                    m_soundEmitter.PlaySingleSound(MySoundPair.GetCueId(blueprint.Blueprint.ProgressBarSoundCue));
                }
                else
                {
                    m_soundEmitter.PlaySingleSound(ActionSound);
                }
            }
            else
            {
                m_soundEmitter.StopSound(true);
            }
        }

        protected override void AddProducedItemToInventory(MyBlueprintDefinitionBase definition, MyFixedPoint amountMult)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "This method should be called only on server!");

            if (!Sync.IsServer)
                return;

            MyInventory interactingInventory = null;
            MyInventory innerInventory = (Entity as MyEntity).GetInventory();
            bool resultAdded = false;
            if (m_lastEntityInteraction != null) {
                interactingInventory = (m_lastEntityInteraction as MyEntity).GetInventory();

                if (interactingInventory != null)
                {
                    foreach (var prodItem in definition.Results)
                    {
                        var amountToAdd = prodItem.Amount * amountMult;

                        var inventoryItem = CreateInventoryItem(prodItem.Id, amountToAdd);

                        resultAdded = interactingInventory.Add(inventoryItem, inventoryItem.Amount);

                        if (!resultAdded)
                            resultAdded = innerInventory.Add(inventoryItem, inventoryItem.Amount);

                        System.Diagnostics.Debug.Assert(resultAdded, "Result of adding is false!");
                    }
                }
            }
            else
            {
                if (innerInventory == null)
                {
                    System.Diagnostics.Debug.Fail("Inventory was not found on the entity!");
                    return;
                }

                foreach (var prodItem in definition.Results)
                {
                    var amountToAdd = prodItem.Amount * amountMult;

                    var inventoryItem = CreateInventoryItem(prodItem.Id, amountToAdd);

                    resultAdded = innerInventory.Add(inventoryItem, inventoryItem.Amount);

                    System.Diagnostics.Debug.Assert(resultAdded, "Result of adding is false!");
                }
            }        

            m_lastEntityInteraction = null;
        }

        public override void UpdateCurrentItemStatus(float statusDelta)
        {
            if (!IsProducing)
                return;

            var itemInProduction = GetItemToProduce(m_currentItem);
            if (itemInProduction == null)
            {
                return;
            }
            var blueprint = itemInProduction.Blueprint;

            m_currentItemStatus = Math.Min(1.0f, m_currentItemStatus + (statusDelta * m_craftingSpeedMultiplier) / (blueprint.BaseProductionTimeInSeconds * 1000f));

            //Debug.WriteLine(String.Format("StatusDelta: {0}, StatusDeltaProper: {1}, CurrentItemStatus: {2}", statusDelta, statusDeltaProper, m_currentItemStatus));
        }

        public void SetLastEntityInteraction(IMyEntity entity)
        {
            m_lastEntityInteraction = entity;
        }

        public override string ComponentTypeDebugString
        {
            get { return "Interactive crafting component"; }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_CraftingComponentInteractive;

            // TODO: Initialization
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_CraftingComponentInteractive;

            return ob;
        }

        public override string DisplayNameText
        {
            get
            {
                return (Entity as MyEntity).DisplayNameText;
            }
        }

        public override bool RequiresItemsToOperate
        {
            get { return false; }
        }

        public override bool CanOperate
        {
            get { return true; }

        }

        public override VRage.ObjectBuilders.MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            System.Diagnostics.Debug.Fail("This should not be called, components are serialized through serialize!");
            return null; // This component should be serialized through Serialize..
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            m_elapsedTimeMs = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (!IsProductionDone && CanOperate)
            {
                UpdateProduction_Implementation();
                if (!IsProducing)
                {
                    StopProduction_Implementation();
                }
            }
            else
            {
                StopProduction_Implementation();
            }
        }

        protected override void StartProduction_Implementation()
        {
            base.StartProduction_Implementation();

            var entity = Entity as MyEntity;

            if (entity != null)
            {
                entity.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
            }

            m_productionEnabled = true;

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        protected override void StopProduction_Implementation()
        {
            base.StopOperating_Implementation();

            m_productionEnabled = false;
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
        }
    }
}
