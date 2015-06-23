using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
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
        public MyHumanoidBotActionProxy HumanoidActions { get { return m_actions as MyHumanoidBotActionProxy; } }
        public MyHumanoidBotDefinition HumanoidDefinition { get { return m_botDefinition as MyHumanoidBotDefinition; } }
        public MyHumanoidBotLogic HumanoidLogic { get { return AgentLogic as MyHumanoidBotLogic; } }

        public override bool ShouldFollowPlayer 
        { // MW:TODO remove hack
            set { HumanoidActions.ShouldFollowPlayer = value; }
            get { return HumanoidActions.ShouldFollowPlayer; }
        }

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
            if (m_player.Controller.ControlledEntity is MyCharacter) // when loaded player already controls entity
            {
                var character = m_player.Controller.ControlledEntity as MyCharacter;
                if (character.CurrentWeapon == null && StartingWeaponId.SubtypeId != MyStringHash.NullOrEmpty)
                {
                    AddItems(character);
                }
            }

            Sandbox.Game.Gui.MyCestmirDebugInputComponent.PlacedAction += DebugGoto;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            Sandbox.Game.Gui.MyCestmirDebugInputComponent.PlacedAction -= DebugGoto;
        }

        private void AddItems(MyCharacter character)
        {
            character.GetInventory(0).Clear();

            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(StartingWeaponId.SubtypeName);
            character.GetInventory(0).AddItems(1, ob);

            foreach (var weaponDef in HumanoidDefinition.InventoryItems)
            {
                ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(weaponDef.SubtypeName);
                character.GetInventory(0).AddItems(1, ob);
            }

            character.SwitchToWeapon(StartingWeaponId);
        }

        protected override void Controller_ControlledEntityChanged(IMyControllableEntity oldEntity, IMyControllableEntity newEntity)
        {
            base.Controller_ControlledEntityChanged(oldEntity, newEntity);
            if (newEntity is MyCharacter)
            {
                var character = m_player.Controller.ControlledEntity as MyCharacter;
                character.EnableJetpack(false);
                if (StartingWeaponId.SubtypeId != MyStringHash.NullOrEmpty)
                {
                    AddItems(newEntity as MyCharacter);
                }
            }
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            if (HumanoidEntity == null) return;

            HumanoidActions.AiTarget.DebugDraw();

            var headMatrix = HumanoidEntity.GetHeadMatrix(true, true, false, true);
        //    VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 1.0f, false);
            VRageRender.MyRenderProxy.DebugDrawLine3D(headMatrix.Translation, headMatrix.Translation + headMatrix.Forward * 30, Color.HotPink, Color.HotPink, false);
            VRageRender.MyRenderProxy.DebugDrawAxis(HumanoidEntity.PositionComp.WorldMatrix, 1.0f, false);
            var invHeadMatrix = headMatrix;
            invHeadMatrix.Translation = Vector3.Zero;
            invHeadMatrix = Matrix.Transpose(invHeadMatrix);
            invHeadMatrix.Translation = headMatrix.Translation;
        }

        public virtual void DebugGoto(Vector3D point, MyEntity entity = null)
        {
            if (m_player.Id.SerialId == 0) return;

            /*{
                var path = MyAIComponent.Static.Pathfinding.FindPathGlobal(m_navigation.PositionAndOrientation.Translation, point, entity);
                Navigation.FollowPath(path);

                var statues = MyBarbarianComponent.Static.GetAllStatues();
                double closestSq = double.MaxValue;
                MyEntity closestStatue = null;
                Vector3D currentPos = Navigation.PositionAndOrientation.Translation;
                foreach (var statue in statues)
                {
                    double dsq = Vector3D.DistanceSquared(currentPos, statue.WorldMatrix.Translation);
                    if (dsq < closestSq)
                    {
                        closestSq = dsq;
                        closestStatue = statue;
                    }
                    if (

                    if (statue.CubeGrid == targetGrid)
                    {
                        inoutTarget.SetTargetCube(statue.SlimBlock.Position, statue.CubeGrid.EntityId);
                        return MyBehaviorTreeState.SUCCESS;
                    }
                }

                if (closestStatue == null) return;

                //MyBBMemoryTarget target = new MyBBMemoryTarget();
                var target = HumanoidActions.AiTarget as MyAiTarget;
                target.SetTargetEntity(closestStatue);
                target.GotoTarget(m_navigation);
            }*/
            m_navigation.ResetAiming(true);
            m_navigation.Goto(point, 0.0f, entity);
        }
    }
}
