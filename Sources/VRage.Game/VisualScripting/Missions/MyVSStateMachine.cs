using VRage.Collections;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder;
using VRage.Generics;
using VRage.Utils;

namespace VRage.Game.VisualScripting.Missions
{
    public class MyVSStateMachine : MyStateMachine
    {
        private MyObjectBuilder_ScriptSM m_objectBuilder;
        private readonly MyConcurrentHashSet<MyStringId> m_cachedActions = new MyConcurrentHashSet<MyStringId>(); 

        public int ActiveCursorCount { get { return m_activeCursors.Count; } }

        private long m_ownerId;
        public long OwnerId
        {
            get { return m_ownerId; }
            set
            {
                // Set ownerId for all existing scripts within SM
                foreach (var node in m_nodes.Values)
                {
                    var scriptNode = node as MyVSStateMachineNode;
                    if(scriptNode == null) continue;
                    if(scriptNode.ScriptInstance == null) continue;
                    
                    scriptNode.ScriptInstance.OwnerId = value;
                }

                m_ownerId = value;
            }
        }

        public MyStateMachineCursor RestoreCursor(string nodeName)
        {
            // check if there isnt cursor already
            foreach (var cursor in m_activeCursorsById.Values)
                if (cursor.Node.Name == nodeName)
                    return null;

            var newCursor = base.CreateCursor(nodeName);
            if (newCursor != null)
            {
                newCursor.OnCursorStateChanged += OnCursorStateChanged;

                var missionNode = newCursor.Node as MyVSStateMachineNode;
                if (missionNode != null)
                    missionNode.ActivateScript(true);
            }

            return newCursor;
        }

        public override MyStateMachineCursor CreateCursor(string nodeName)
        {
            // check if there isnt cursor already
            foreach (var cursor in m_activeCursorsById.Values)
                if(cursor.Node.Name == nodeName)
                    return null;

            var newCursor = base.CreateCursor(nodeName);
            if(newCursor != null)
            {
                newCursor.OnCursorStateChanged += OnCursorStateChanged;

                var missionNode = newCursor.Node as MyVSStateMachineNode;
                if (missionNode != null)
                    missionNode.ActivateScript();
            }

            return newCursor;
        }

        private void OnCursorStateChanged(int transitionId, MyStateMachineNode node, MyStateMachine stateMachine)
        {
            var transition = FindTransitionWithStart(transitionId);
            var startNode = transition.StartNode as MyVSStateMachineNode;
            if (startNode != null)
                startNode.DisposeScript();

            var missionNode = node as MyVSStateMachineNode;
            if (missionNode != null)
                missionNode.ActivateScript();
        }

        public override void Update()
        {
            foreach (var action in m_cachedActions)
                m_enqueuedActions.Add(action);

            m_cachedActions.Clear();
            base.Update();
        }

        public void Init(MyObjectBuilder_ScriptSM ob, long? ownerId = null)
        {
            m_objectBuilder = ob;
            Name = ob.Name;

            if(ob.Nodes != null)
                foreach (var nodeData in ob.Nodes)
                {
                    MyStateMachineNode stateNode;

                    if(nodeData is MyObjectBuilder_ScriptSMFinalNode)
                        stateNode = new MyVSStateMachineFinalNode(nodeData.Name);
                    else if(nodeData is MyObjectBuilder_ScriptSMSpreadNode)
                        stateNode = new MyVSStateMachineSpreadNode(nodeData.Name);
                    else if(nodeData is MyObjectBuilder_ScriptSMBarrierNode)
                        stateNode = new MyVSStateMachineBarrierNode(nodeData.Name);
                    else
                    {
                        var scriptType = MyVSAssemblyProvider.GetType("VisualScripting.CustomScripts." + nodeData.ScriptClassName);
                        var missionNode = new MyVSStateMachineNode(nodeData.Name, scriptType);
                        if (missionNode.ScriptInstance != null)
                        {
                            if (ownerId == null)
                                missionNode.ScriptInstance.OwnerId = ob.OwnerId;
                            else
                                missionNode.ScriptInstance.OwnerId = ownerId.Value;
                        }

                        stateNode = missionNode;
                    }

                    AddNode(stateNode);
                }

            if(ob.Transitions != null)
                foreach (var transitionData in ob.Transitions)
                {
                    AddTransition(transitionData.From, transitionData.To, name: transitionData.Name);
                }

            if(ob.Cursors != null)
                foreach (var cursorData in ob.Cursors)
                {
                    CreateCursor(cursorData.NodeName);
                }
        }

        public MyObjectBuilder_ScriptSM GetObjectBuilder()
        {
            m_objectBuilder.Cursors = new MyObjectBuilder_ScriptSMCursor[m_activeCursors.Count];

            m_objectBuilder.OwnerId = m_ownerId;
            for (var i = 0; i < m_activeCursors.Count; i++)
            {
                m_objectBuilder.Cursors[i] = new MyObjectBuilder_ScriptSMCursor {NodeName = m_activeCursors[i].Node.Name};
            }

            return m_objectBuilder;
        }

        public void Dispose()
        {
            m_activeCursors.ApplyChanges();
            for (var i = 0; i < m_activeCursors.Count; i++)
            {
                var missionNode = m_activeCursors[i].Node as MyVSStateMachineNode;
                if (missionNode != null)
                    missionNode.DisposeScript();

                DeleteCursor(m_activeCursors[i].Id);
            }
            m_activeCursors.ApplyChanges();
            m_activeCursors.Clear();
        }

        // Caches action for next update pass. Mostly for nodes triggering theire own action.
        public void TriggerCachedAction(MyStringId actionName)
        {
            m_cachedActions.Add(actionName);
        }
    }
}
