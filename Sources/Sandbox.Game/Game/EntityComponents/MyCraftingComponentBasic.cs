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
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_CraftingComponentBasic))]
    [MyComponentBuilder(typeof(MyObjectBuilder_CraftingComponentCharacter),false)]
    public class MyCraftingComponentBasic : MyCraftingComponentBase, IMyEventProxy
    {
        // TODO: Move this to definition..
        private int m_lastUpdateTime;
        private MyEntity3DSoundEmitter m_soundEmitter;

        public MySoundPair ActionSound { get; set; }

        public MyCraftingComponentBasic()
        {            
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var craftDefinition = definition as MyCraftingComponentBasicDefinition;

            System.Diagnostics.Debug.Assert(craftDefinition != null, "Trying to initialize crafting component from wrong definition type?");


            if (craftDefinition != null)
            {
                ActionSound = new MySoundPair(craftDefinition.ActionSound);
                m_craftingSpeedMultiplier = craftDefinition.CraftingSpeedMultiplier;

                foreach (var blueprintClass in craftDefinition.AvailableBlueprintClasses)
                {
                    var classDefinition = MyDefinitionManager.Static.GetBlueprintClass(blueprintClass);
                    System.Diagnostics.Debug.Assert(classDefinition != null, blueprintClass + " blueprint class definition was not found.");
                    if (classDefinition != null)
                    {
                        m_blueprintClasses.Add(classDefinition);
                    }
                }
            }
        }

        protected override void UpdateProduction_Implementation()
        {
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

        public override string ComponentTypeDebugString
        {
            get { return "Character crafting component"; }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_CraftingComponentBasic;
            
            // TODO: Initialization
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_CraftingComponentBasic;           

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

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        protected override void StopProduction_Implementation()
        {
            base.StopOperating_Implementation();

            if (m_soundEmitter != null) 
                m_soundEmitter.StopSound(true);
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            var entity = Entity as MyEntity;

            if (entity != null)
            {
                entity.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }
    }
}
