using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.AI.Commands
{
    // MW:TODO bots that ignore change behavior command
    public class MyAiCommandBehavior : IMyAiCommand
    {
        public MyAiCommandBehaviorDefinition Definition
        {
            get;
            private set;
        }

        private static List<MyPhysics.HitInfo> m_tmpHitInfos = new List<MyPhysics.HitInfo>();

        public void InitCommand(MyAiCommandDefinition definition)
        {
            Definition = definition as MyAiCommandBehaviorDefinition;
        }

        public void ActivateCommand()
        {
            if (Definition.CommandEffect == MyAiCommandEffect.TARGET)
            {
                ChangeTarget();
            }
            else if (Definition.CommandEffect == MyAiCommandEffect.OWNED_BOTS)
            {
                ChangeAllBehaviors();
            }
        }

        private void ChangeTarget()
        {
            Vector3D cameraPos, cameraDir;
            if (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity)
            {
                var headMatrix = MySession.Static.ControlledEntity.GetHeadMatrix(true, true);
                cameraPos = headMatrix.Translation;
                cameraDir = headMatrix.Forward;
            }
            else
            {
                cameraPos = MySector.MainCamera.Position;
                cameraDir = MySector.MainCamera.WorldMatrix.Forward;
            }

            m_tmpHitInfos.Clear();
            MyPhysics.CastRay(cameraPos, cameraPos + cameraDir * 20, m_tmpHitInfos, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            if (m_tmpHitInfos.Count == 0)
                return;
            foreach (var hitInfo in m_tmpHitInfos)
            {
                var ent = hitInfo.HkHitInfo.GetHitEntity() as MyCharacter;
                if (ent != null)
                {
                    MyAgentBot bot;
                    if (TryGetBotForCharacter(ent, out bot) && bot.BotDefinition.Commandable)
                    {
                        ChangeBotBehavior(bot);
                    }
                }
            }
        }

        private void ChangeAllBehaviors()
        {
            foreach (var entry in MyAIComponent.Static.Bots.GetAllBots())
            {
                var localBot = entry.Value;
                var agent = localBot as MyAgentBot;
                if (agent != null && agent.BotDefinition.Commandable)
                {
                    ChangeBotBehavior(agent);
                }
            }
        }

        private bool TryGetBotForCharacter(MyCharacter character, out MyAgentBot bot)
        {
            bot = null;
            foreach (var entry in MyAIComponent.Static.Bots.GetAllBots())
            {
                var localBot = entry.Value;
                var agent = localBot as MyAgentBot;
                if (agent != null && agent.AgentEntity == character)
                {
                    bot = agent;
                    return true;
                }
            }
            return false;
        }

        private void ChangeBotBehavior(MyAgentBot bot)
        {
            MyAIComponent.Static.BehaviorTrees.ChangeBehaviorTree(Definition.BehaviorTreeName, bot);
        }
    }
}
