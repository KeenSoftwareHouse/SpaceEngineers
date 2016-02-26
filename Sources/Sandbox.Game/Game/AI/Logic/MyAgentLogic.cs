using Sandbox.Game.AI;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Logic
{
    public abstract class MyAgentLogic : MyBotLogic
    {
        protected IMyEntityBot m_entityBot;
        public MyAgentBot AgentBot { get { return m_bot as MyAgentBot; } }
        public MyAiTargetBase AiTarget { get; private set; }

        public MyAgentLogic(IMyBot bot)
            : base(bot)
        {
            m_entityBot = m_bot as IMyEntityBot;
            AiTarget = MyAIComponent.BotFactory.CreateTargetForBot(AgentBot);
            Debug.Assert(AiTarget != null, "Ai target was not created in CreateTargetForBot()!");
        }

        public override void Init()
        {
            base.Init();

            AiTarget = AgentBot.AgentActions.AiTargetBase;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            AiTarget.Cleanup();
        }

        public override void Update()
        {
            base.Update();

            AiTarget.Update();
        }

        public virtual void OnCharacterControlAcquired(MyCharacter character)
        { 
        }

        public override BotType BotType
        {
            get { return BotType.UNKNOWN; }
        }
    }
}
