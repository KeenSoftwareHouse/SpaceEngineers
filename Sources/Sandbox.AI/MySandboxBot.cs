using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.AI
{
    public class MySandboxBot : IMyBot
    {
        MyPlayer m_player;

        public MySandboxBot(MyPlayer botPlayer)
        {
            m_player = botPlayer;
        }

        public void Cleanup()
        {
            throw new NotImplementedException();
        }

        public void GetAvailableActions(ActionCollection actions)
        {
            // TODO: Do this using reflection
            actions.AddAction("Test", Action_Test);
        }

        private void Action_Test()
        {

        }
    }
}
