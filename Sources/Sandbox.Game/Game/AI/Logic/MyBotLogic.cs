using Sandbox.Common.AI;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Logic
{
    public enum BotType
    {
        HUMANOID,
        ANIMAL,
        UNKNOWN,
    }

    public abstract class MyBotLogic
    {
        protected IMyBot m_bot;
        public abstract BotType BotType { get; }

        public MyBotLogic(IMyBot bot)
        {
            m_bot = bot;
        }

        public virtual void Init()
        {
        }

        public virtual void Cleanup()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void OnControlledEntityChanged(IMyControllableEntity newEntity)
        {
        }

        public virtual void DebugDraw()
        {
        }
    }
}
