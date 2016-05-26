using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.ModAPI
{
    public class MyTerminalControlFactoryHelper : IMyTerminalActionsHelper
    {
        private static MyTerminalControlFactoryHelper m_instance;
        public static MyTerminalControlFactoryHelper Static
        {
            get
            {
                if (m_instance == null)
                    m_instance = new MyTerminalControlFactoryHelper();
                return m_instance;
            }
        }

        List<Sandbox.Game.Gui.ITerminalAction> m_actionList = new List<Sandbox.Game.Gui.ITerminalAction>();
        List<ITerminalProperty> m_valueControls = new List<ITerminalProperty>();
        void IMyTerminalActionsHelper.GetActions(Type blockType, List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect)
        {
            if (!typeof(MyTerminalBlock).IsAssignableFrom(blockType))
                return;
            MyTerminalControlFactory.GetActions(blockType, m_actionList);
            foreach (var action in m_actionList)
            {
                if ((collect == null || collect(action)) && action.IsValidForToolbarType(MyToolbarType.ButtonPanel))
                {
                    resultList.Add(action);
                }
            }
            m_actionList.Clear();
        }
        void IMyTerminalActionsHelper.SearchActionsOfName(string name, Type blockType, List<Sandbox.ModAPI.Interfaces.ITerminalAction> resultList, Func<Sandbox.ModAPI.Interfaces.ITerminalAction, bool> collect = null)
        {
            if (!typeof(MyTerminalBlock).IsAssignableFrom(blockType))
                return;
            MyTerminalControlFactory.GetActions(blockType, m_actionList);
            foreach (var action in m_actionList)
            {
                if ((collect == null || collect(action)) && action.Id.ToString().Contains(name) && action.IsValidForToolbarType(MyToolbarType.ButtonPanel))
                {
                    resultList.Add(action);
                }
            }
            m_actionList.Clear();
        }
        Sandbox.ModAPI.Interfaces.ITerminalAction IMyTerminalActionsHelper.GetActionWithName(string name, Type blockType)
        {
            if (!typeof(MyTerminalBlock).IsAssignableFrom(blockType))
            {
                return null;
            }
            MyTerminalControlFactory.GetActions(blockType, m_actionList);
            foreach (var action in m_actionList)
            {
                if (action.Id.ToString() == name && action.IsValidForToolbarType(MyToolbarType.ButtonPanel))
                {
                    m_actionList.Clear();
                    return action;
                }
            }
            m_actionList.Clear();
            return null;
        }

        public Sandbox.ModAPI.Interfaces.ITerminalProperty GetProperty(string id, Type blockType)
        {
            if (!typeof(MyTerminalBlock).IsAssignableFrom(blockType))
            {
                return null;
            }
            MyTerminalControlFactory.GetValueControls(blockType, m_valueControls);
            foreach (var property in m_valueControls)
            {
                if (property.Id == id)
                {
                    m_valueControls.Clear();
                    return property;
                }
            }
            m_valueControls.Clear();
            return null;
        }

        public void GetProperties(Type blockType, List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect = null)
        {
            if (!typeof(MyTerminalBlock).IsAssignableFrom(blockType))
            {
                return;
            }
            MyTerminalControlFactory.GetValueControls(blockType, m_valueControls);
            foreach (var property in m_valueControls)
            {
                if (collect == null || collect(property))
                {
                    resultList.Add(property);
                }
            }
            m_valueControls.Clear();
        }

        IMyGridTerminalSystem IMyTerminalActionsHelper.GetTerminalSystemForGrid(VRage.Game.ModAPI.IMyCubeGrid grid)
        {
            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(grid as MyCubeGrid);
            if (gridGroup != null && gridGroup.GroupData != null)
            {
                return gridGroup.GroupData.TerminalSystem;
            }
            return null;
        }
    }
}