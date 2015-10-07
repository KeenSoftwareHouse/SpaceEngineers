using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.AI
{
    [BehaviorType(typeof(MyObjectBuilder_HumanoidBotDefinition))]
    public class MyHumanoidBot : MyAgentBot
    {
        public MyCharacter HumanoidEntity { get { return AgentEntity; } }
        public MyHumanoidBotActions HumanoidActions { get { return m_actions as MyHumanoidBotActions; } }
        public MyHumanoidBotDefinition HumanoidDefinition { get { return m_botDefinition as MyHumanoidBotDefinition; } }
        public MyHumanoidBotLogic HumanoidLogic { get { return AgentLogic as MyHumanoidBotLogic; } }

        public override bool IsValidForUpdate
        {
            get
            {
                return base.IsValidForUpdate;
            }
        }

        protected MyDefinitionId StartingWeaponId
        {
            get 
            {
                if (HumanoidDefinition == null)
                    return new MyDefinitionId();
                else
                    return HumanoidDefinition.StartingWeaponDefinitionId; 
            }
        }

        public MyHumanoidBot(MyPlayer player, MyBotDefinition botDefinition)
            : base(player, botDefinition)
        {
            Debug.Assert(botDefinition is MyHumanoidBotDefinition, "Provided bot definition is not of humanoid type");
        }

        protected override void AddItems(MyCharacter character)
        {
            base.AddItems(character);

            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(StartingWeaponId.SubtypeName);
            if (character.WeaponTakesBuilderFromInventory(StartingWeaponId))
            {
                character.GetInventory(0).AddItems(1, ob);
            }

            // else // allowing the inventory items to be added
            {
                foreach (var weaponDef in HumanoidDefinition.InventoryItems)
                {
                    ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(weaponDef.SubtypeName);
                    character.GetInventory(0).AddItems(1, ob);
                }
            }

            character.SwitchToWeapon(StartingWeaponId);

            {
                MyDefinitionId weaponDefinitionId;
                weaponDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), StartingWeaponId.SubtypeName);

                MyWeaponDefinition weaponDefinition;

                if (MyDefinitionManager.Static.TryGetWeaponDefinition(weaponDefinitionId, out weaponDefinition)) //GetWeaponDefinition(StartingWeaponId);
                {
                    if (weaponDefinition.HasAmmoMagazines())
                    {
                        var ammo = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(weaponDefinition.AmmoMagazinesId[0].SubtypeName);
                        character.GetInventory(0).AddItems(3, ammo);
                    }
                }
            }
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            if (HumanoidEntity == null) return;

            HumanoidActions.AiTargetBase.DebugDraw();

            var headMatrix = HumanoidEntity.GetHeadMatrix(true, true, false, true);
        //    VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 1.0f, false);
            VRageRender.MyRenderProxy.DebugDrawLine3D(headMatrix.Translation, headMatrix.Translation + headMatrix.Forward * 30, Color.HotPink, Color.HotPink, false);
            VRageRender.MyRenderProxy.DebugDrawAxis(HumanoidEntity.PositionComp.WorldMatrix, 1.0f, false);
            var invHeadMatrix = headMatrix;
            invHeadMatrix.Translation = Vector3.Zero;
            invHeadMatrix = Matrix.Transpose(invHeadMatrix);
            invHeadMatrix.Translation = headMatrix.Translation;
        }
    }
}
