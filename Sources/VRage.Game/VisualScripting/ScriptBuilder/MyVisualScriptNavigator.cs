using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder.Nodes;

namespace VRage.Game.VisualScripting.ScriptBuilder
{
    internal class MyVisualScriptNavigator
    {
        private readonly Dictionary<int, MyVisualSyntaxNode>            m_idToNode          = new Dictionary<int, MyVisualSyntaxNode>();
        private readonly Dictionary<Type, List<MyVisualSyntaxNode>>     m_nodesByType       = new Dictionary<Type, List<MyVisualSyntaxNode>>();
        private readonly Dictionary<string, MyVisualSyntaxVariableNode> m_variablesByName   = new Dictionary<string, MyVisualSyntaxVariableNode>(); 
        private readonly List<MyVisualSyntaxNode>                       m_freshNodes        = new List<MyVisualSyntaxNode>();

        public MyVisualScriptNavigator(MyObjectBuilder_VisualScript scriptOb)
        {
            var scriptBase = string.IsNullOrEmpty(scriptOb.Interface) ? null : MyVisualScriptingProxy.GetType(scriptOb.Interface);

            foreach (var scriptNodeOb in scriptOb.Nodes)
            {
                Debug.Assert(!m_idToNode.ContainsKey(scriptNodeOb.ID));

                MyVisualSyntaxNode node;
                if (scriptNodeOb is MyObjectBuilder_NewListScriptNode)
                    node = new MyVisualSyntaxNewListNode(scriptNodeOb);
                else if(scriptNodeOb is MyObjectBuilder_SwitchScriptNode)
                    node = new MyVisualSyntaxSwitchNode(scriptNodeOb);
                else if(scriptNodeOb is MyObjectBuilder_LocalizationScriptNode)
                    node = new MyVisualSyntaxLocalizationNode(scriptNodeOb);
                else if(scriptNodeOb is MyObjectBuilder_LogicGateScriptNode)
                    node = new MyVisualSyntaxLogicGateNode(scriptNodeOb);
                else if(scriptNodeOb is MyObjectBuilder_ForLoopScriptNode)
                    node = new MyVisualSyntaxForLoopNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_SequenceScriptNode)
                    node = new MyVisualSyntaxSequenceNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_ArithmeticScriptNode)
                    node = new MyVisualSyntaxArithmeticNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_InterfaceMethodNode)
                    node = new MyVisualSyntaxInterfaceMethodNode(scriptNodeOb, scriptBase);
                else if (scriptNodeOb is MyObjectBuilder_KeyEventScriptNode)
                    node = new MyVisualSyntaxKeyEventNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_BranchingScriptNode)
                    node = new MyVisualSyntaxBranchingNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_InputScriptNode)
                    node = new MyVisualSyntaxInputNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_CastScriptNode)
                    node = new MyVisualSyntaxCastNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_EventScriptNode)
                    node = new MyVisualSyntaxEventNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_FunctionScriptNode)
                    node = new MyVisualSyntaxFunctionNode(scriptNodeOb, scriptBase);
                else if (scriptNodeOb is MyObjectBuilder_VariableSetterScriptNode)
                    node = new MyVisualSyntaxSetterNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_TriggerScriptNode)
                    node = new MyVisualSyntaxTriggerNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_VariableScriptNode)
                    node = new MyVisualSyntaxVariableNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_ConstantScriptNode)
                    node = new MyVisualSyntaxConstantNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_GetterScriptNode)
                    node = new MyVisualSyntaxGetterNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_OutputScriptNode)
                    node = new MyVisualSyntaxOutputNode(scriptNodeOb);
                else if (scriptNodeOb is MyObjectBuilder_ScriptScriptNode)
                    node = new MyVisualSyntaxScriptNode(scriptNodeOb);
                else
                    continue;

                node.Navigator = this;

                m_idToNode.Add(scriptNodeOb.ID, node);

                var type = node.GetType();
                if (!m_nodesByType.ContainsKey(type))
                    m_nodesByType.Add(type, new List<MyVisualSyntaxNode>());

                m_nodesByType[type].Add(node);

                if(type == typeof(MyVisualSyntaxVariableNode))
                    m_variablesByName.Add(((MyObjectBuilder_VariableScriptNode)scriptNodeOb).VariableName, (MyVisualSyntaxVariableNode)node);
            }
        }

        public List<MyVisualSyntaxNode> FreshNodes
        {
            get { return m_freshNodes; }
        }

        public MyVisualSyntaxNode GetNodeByID(int id)
        {
            MyVisualSyntaxNode node;

            m_idToNode.TryGetValue(id, out node);
            return node;
        }

        public List<T> OfType<T>() where T : MyVisualSyntaxNode
        {
            List<MyVisualSyntaxNode> list = new List<MyVisualSyntaxNode>();
            foreach (var pair in m_nodesByType)
            {
                if(typeof(T) == pair.Key)
                {
                    list.AddRange(pair.Value);
                }
            }

            return list.ConvertAll(node => (T)node);
        }

        public void ResetNodes()
        {
            foreach (var keyValuePair in m_idToNode)
            {
                keyValuePair.Value.Reset();
            }
        }

        public MyVisualSyntaxVariableNode GetVariable(string name)
        {
            MyVisualSyntaxVariableNode var;
            m_variablesByName.TryGetValue(name, out var);

            return var;
        }
    }
}
