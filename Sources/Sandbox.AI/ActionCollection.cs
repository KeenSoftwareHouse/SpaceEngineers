using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.AI
{
    public class ActionCollection
    {
        private Dictionary<string, Action> m_actions;

        public void AddAction(string actionName, Action action)
        {
            Debug.Assert(!m_actions.ContainsKey(actionName), "Adding a duplicite bot action!");
            if (m_actions.ContainsKey(actionName))
                return;

            m_actions.Add(actionName, action);
        }

        public void PerformAction(string actionName)
        {
            Debug.Assert(m_actions.ContainsKey(actionName), "Given bot action does not exist!");

            var action = m_actions[actionName];
            if (action == null) return;

            action();
        }
    }
}
