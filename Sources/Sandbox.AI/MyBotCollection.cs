using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.AI
{
    public class MyBotCollection
    {
        private Dictionary<int, IMyBot> m_allBots;

        private Dictionary<int, ActionCollection> m_botActions;

        public MyBotCollection()
        {
            m_allBots = new Dictionary<int, IMyBot>(8);
            m_botActions = new Dictionary<int, ActionCollection>(8);
        }

        public void Update()
        {
            foreach (var botEntry in m_allBots)
            {
                int botHandler = botEntry.Key;

                var actions = m_botActions[botHandler];
                actions.PerformAction("Test");
            }
        }

        public void AddBot(int botHandler, IMyBot newBot)
        {
            Debug.Assert(!m_allBots.ContainsKey(botHandler), "Bot with the given handler already exists!");
            if (m_allBots.ContainsKey(botHandler)) return;

            ActionCollection botActions = new ActionCollection();
            newBot.GetAvailableActions(botActions);

            m_botActions.Add(botHandler, botActions);
            m_allBots.Add(botHandler, newBot);
        }

        public void RemoveBot(int botHandler)
        {
            Debug.Assert(m_allBots.ContainsKey(botHandler), "Bot with the given handler does not exist!");
            if (!m_allBots.ContainsKey(botHandler)) return;

            m_allBots.Remove(botHandler);
            m_botActions.Remove(botHandler);
        }
    }
}
