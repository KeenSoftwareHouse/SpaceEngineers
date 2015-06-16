
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;

namespace Sandbox.Game.Gui
{
    public partial class MyTerminalAction<TBlock> : ITerminalAction
        where TBlock : MyTerminalBlock
    {
        private readonly string m_id;
        private readonly string m_icon;
        private readonly StringBuilder m_name;
        private List<TerminalActionParameter> m_parameterDefinitions = new List<TerminalActionParameter>();
        private Action<TBlock> m_action;
        private Action<TBlock, ListReader<TerminalActionParameter>> m_actionWithParameters;
        
        public Func<TBlock, bool> Enabled = (b) => true;
        public List<MyToolbarType> InvalidToolbarTypes = null;
        public bool ValidForGroups = true;
        public MyTerminalControl<TBlock>.WriterDelegate Writer;
        
        /// <summary>
        /// Replace this callback to allow your block to display a custom dialog to fill action parameters.
        /// </summary>
        public Action<IList<TerminalActionParameter>, Action<bool>> DoUserParameterRequest;

        public MyTerminalAction(string id, StringBuilder name, string icon)
        {
            m_id = id;
            m_name = name;
            m_icon = icon;
        }

        public MyTerminalAction(string id, StringBuilder name, Action<TBlock> action, string icon)
        {
            m_id = id;
            m_name = name;
            Action = action;
            this.m_icon = icon;
        }

        public MyTerminalAction(string id, StringBuilder name, Action<TBlock, ListReader<TerminalActionParameter>> action, string icon)
        {
            m_id = id;
            m_name = name;
            ActionWithParameters = action;
            this.m_icon = icon;
        }

        public MyTerminalAction(string id, StringBuilder name, Action<TBlock> action, MyTerminalControl<TBlock>.WriterDelegate valueWriter, string icon)
        {
            m_id = id;
            m_name = name;
            Action = action;
            m_icon = icon;
            Writer = valueWriter;
        }

        public MyTerminalAction(string id, StringBuilder name, Action<TBlock, ListReader<TerminalActionParameter>> action, MyTerminalControl<TBlock>.WriterDelegate valueWriter, string icon)
        {
            m_id = id;
            m_name = name;
            ActionWithParameters = action;
            m_icon = icon;
            Writer = valueWriter;
        }

        public Action<TBlock> Action
        {
            get { return m_action; }
            set
            {
                m_action = value;
                m_actionWithParameters = (block, parameters) => m_action(block);
            }
        }
        
        public Action<TBlock, ListReader<TerminalActionParameter>> ActionWithParameters
        {
            get { return m_actionWithParameters; }
            set
            {
                m_actionWithParameters = value;
                m_action = block => m_actionWithParameters(block, ListReader<TerminalActionParameter>.Empty);
            }
        }
        
        public string Id
        {
            get { return m_id; }
        }

        public string Icon
        {
            get { return m_icon; }
        }

        public StringBuilder Name
        {
            get { return m_name; }
        }

        public void Apply(MyTerminalBlock block, ListReader<TerminalActionParameter> parameters)
        {
            var b = (TBlock)block;
            if (Enabled(b))
                m_actionWithParameters(b, parameters);
        }
        
        public void Apply(MyTerminalBlock block)
        {
            var b = (TBlock)block;
            if (Enabled(b))
                m_action(b);
        }

        public bool IsEnabled(MyTerminalBlock block)
        {
            return Enabled((TBlock)block);
        }

        public bool IsValidForToolbarType(MyToolbarType type)
        {
            if (InvalidToolbarTypes == null)
            {
                return true;
            }
            return !InvalidToolbarTypes.Contains(type);
        }

        public bool IsValidForGroups()
        {
            return ValidForGroups;
        }

        ListReader<TerminalActionParameter> ITerminalAction.GetParameterDefinitions()
        {
            return m_parameterDefinitions;
        }

        public void WriteValue(MyTerminalBlock block, StringBuilder appendTo)
        {
            if(Writer != null)
                Writer((TBlock)block, appendTo);
        }

        string ModAPI.Interfaces.ITerminalAction.Id
        {
            get { return Id; }
        }

        string ModAPI.Interfaces.ITerminalAction.Icon
        {
            get { return Icon; }
        }

        StringBuilder ModAPI.Interfaces.ITerminalAction.Name
        {
            get { return Name; }
        }

        public List<TerminalActionParameter> ParameterDefinitions
        {
            get { return this.m_parameterDefinitions; }
        }

        public void RequestParameterCollection(IList<TerminalActionParameter> parameters, Action<bool> callback)
        {
            if (parameters == null)
            {
                throw new ArgumentException("parameters");
            }
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }
            var blockCallback = DoUserParameterRequest;
            var myParameters = ParameterDefinitions;
            parameters.Clear();
            // Fill the provided parameters list with available parameters and their default values.
            foreach (var parameter in myParameters)
                parameters.Add(parameter);
            
            if (blockCallback == null)
            {
                // We have no callback, so we simply call back with the default values. 
                // This is not considered a cancel.
                callback(true);
                return;
            }

            blockCallback(parameters, callback);
        }
    }
}
