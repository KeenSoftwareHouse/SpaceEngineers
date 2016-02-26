using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    abstract class MyToolbarItemActions : MyToolbarItem
    {
        protected bool ActionChanged { get; set; }

        private string m_actionId;
        public string ActionId
        {
            get
            {
                return m_actionId;
            }
            set
            {
                if ((m_actionId != null && m_actionId.Equals(value)) ||
                    (m_actionId == null && value == null))
                {}
                else
                {
                    m_actionId = value;
                    ActionChanged = true;
                }
            }
        }

        public abstract ListReader<ITerminalAction> AllActions { get; }
        public abstract ListReader<ITerminalAction> PossibleActions(MyToolbarType toolbarType);

        public ITerminalAction GetCurrentAction()
        {
            return GetActionOrNull(ActionId);
        }

        public ITerminalAction GetActionOrNull(string id)
        {
            foreach (var item in AllActions)
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }

        protected void SetAction(string action)
        {
            ActionId = action;
            if (ActionId == null)
            {
                var actions = AllActions;
                if (actions.Count > 0)
                {
                    ActionId = actions.ItemAt(0).Id;
                }
            }
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            if (ActionId == null)
            {
                var actions = AllActions;
                if (actions.Count > 0)
                {
                    ActionId = actions.ItemAt(0).Id;
                }
            }
            return ChangeInfo.None;
        }
    }
}
