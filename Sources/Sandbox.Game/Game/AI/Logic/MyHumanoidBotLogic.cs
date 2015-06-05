using Sandbox.Game.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Logic
{
	public enum MyEntityReservationStatus
	{
		NONE,
		WAITING,
		SUCCESS,
		FAILURE
	}
    public class MyHumanoidBotLogic : MyAgentLogic
    {
        public MyHumanoidBot HumanoidBot { get { return m_bot as MyHumanoidBot; } }

		public MyEntityReservationStatus EntityReservationStatus;
		public MyAiTargetManager.ReservedEntityData ReservationEntityData;

        public MyHumanoidBotLogic(IMyBot bot)
            : base(bot)
        {
        }

        public override BotType BotType
        {
            get { return BotType.HUMANOID; }
        }
    }
}
