using Sandbox.Game.AI;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.AI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Logic
{
    public class MyAnimalBotLogic : MyAgentLogic
    {
        public MyAnimalBot AnimalBot { get { return m_bot as MyAnimalBot; } }
        private MyCharacterAvoidance m_characterAvoidance;

        public MyAnimalBotLogic(MyAnimalBot bot)
            : base(bot)
        {
            var navigation = AnimalBot.Navigation;
            navigation.AddSteering(new MyTreeAvoidance(navigation, 0.1f));
            m_characterAvoidance = new MyCharacterAvoidance(navigation, 1f);
            navigation.AddSteering(m_characterAvoidance);
            navigation.MaximumRotationAngle = MathHelper.ToRadians(23);
        }

        public void EnableCharacterAvoidance(bool isTrue)
        {
            var navigation = AnimalBot.Navigation;
            var hasSteering = navigation.HasSteeringOfType(m_characterAvoidance.GetType());
            if (isTrue && !hasSteering)
                navigation.AddSteering(m_characterAvoidance);
            else if (!isTrue && hasSteering)
                navigation.RemoveSteering(m_characterAvoidance);
        }

        public override BotType BotType
        {
            get { return BotType.ANIMAL; }
        }
    }
}
