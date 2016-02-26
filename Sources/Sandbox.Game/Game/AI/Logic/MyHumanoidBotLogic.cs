using Sandbox.Game.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Logic
{
	public enum MyReservationStatus
	{
		NONE,
		WAITING,
		SUCCESS,
		FAILURE
	}

    public abstract class MyHumanoidBotLogic : MyAgentLogic
    {
        public MyHumanoidBot HumanoidBot { get { return m_bot as MyHumanoidBot; } }

		public MyReservationStatus ReservationStatus;
		public MyAiTargetManager.ReservedEntityData ReservationEntityData;
        public MyAiTargetManager.ReservedAreaData ReservationAreaData;

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
