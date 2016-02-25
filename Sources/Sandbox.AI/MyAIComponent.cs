using Sandbox.Common;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.AI
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, 500)]
    public class MyAIComponent : MySessionComponentBase
    {
        private MyAIThread m_thread;
        private MyBotCollection m_botCollection;

        public MyAIComponent Static;

        public MyAIComponent()
        {
            Static = this;
        }

        public override void LoadData()
        {
            base.LoadData();

            Sync.Players.NewPlayerRequestSucceeded += PlayerCreated;
            Sync.Players.LocalPlayerRemoved += PlayerRemoved;

            m_botCollection = new MyBotCollection();

            m_thread = new MyAIThread(m_botCollection);
            m_thread.Start();
        }

        public override void Simulate()
        {
            base.Simulate();

            // TODO: MyAIProxy, etc...
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            m_thread.StopAndJoin();
            m_thread = null;

            m_botCollection = null;
        }

        void PlayerCreated(int playerNumber)
        {
            if (playerNumber == 0) return;

            var newPlayer = Sync.Clients.LocalClient.GetPlayer(playerNumber);
            var bot = new MySandboxBot(newPlayer);

            // TODO: Via proxy
            m_botCollection.AddBot(playerNumber, bot);
        }

        void PlayerRemoved(int playerNumber)
        {
            if (playerNumber == 0) return;

            // TODO: Via proxy
            m_botCollection.RemoveBot(playerNumber);
        }
    }
}
