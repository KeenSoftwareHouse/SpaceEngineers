using Sandbox.Game.AI;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Logic
{
    public class MyAgentLogic : MyBotLogic
    {
        public MyAgentBot AgentBot { get { return m_bot as MyAgentBot; } }
        public MyAiTargetBase AiTarget { get; set; }

        public MyAgentLogic(IMyBot bot)
            : base(bot)
        {
        }

        public override void Init()
        {
            base.Init();

            AiTarget = AgentBot.AgentActions.AiTarget as MyAiTargetBase;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            AiTarget.Cleanup();
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
