using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Generics;
using VRage.Generics.StateMachine;
using VRage.Utils;

namespace VRage.Game.VisualScripting.Missions
{
    class MyVSStateMachineNode : MyStateMachineNode
    {
        class VSNodeVariableStorage : IMyVariableStorage<bool>
        {
            MyStringId left;
            MyStringId right;
            bool m_leftValue = false;
            bool m_rightvalue = true;

            public VSNodeVariableStorage()
            {
                left = MyStringId.GetOrCompute("Left");
                right = MyStringId.GetOrCompute("Right");
            }

            public void SetValue(MyStringId key, bool newValue)
            {
                if(key == left)
                    m_leftValue = newValue;
                if(key == right)
                    m_rightvalue = newValue;
            }

            public bool GetValue(MyStringId key, out bool value)
            {
                value = false;

                if (key == left)
                    value = m_leftValue;
                if (key == right)
                    value = m_rightvalue;

                return true;
            }
        }

        private readonly Type m_scriptType;
        private IMyStateMachineScript m_instance;
        private readonly Dictionary<MyStringId, IMyVariableStorage<bool>>  m_transitionNamesToVariableStorages = new Dictionary<MyStringId, IMyVariableStorage<bool>>();

        public IMyStateMachineScript ScriptInstance { get { return m_instance; } }

        public MyVSStateMachineNode(string name, Type script)
            : base(name)
        {
            m_scriptType = script;
        }

        public override void OnUpdate(MyStateMachine stateMachine)
        {
            if(m_instance == null)
            {
                foreach (var transitionNamesToVariableStorage in m_transitionNamesToVariableStorages.Values)
                    transitionNamesToVariableStorage.SetValue(MyStringId.GetOrCompute("Left"), true);
                return;
            }

            if (string.IsNullOrEmpty(m_instance.TransitionTo))
                m_instance.Update();
            // trigger transition when script triggers complete method
            if (!string.IsNullOrEmpty(m_instance.TransitionTo))
            {
                // Correct way to clear a cursor
                if(OutTransitions.Count == 0)
                {
                    // Read the first cursor member
                    var enumerator = Cursors.GetEnumerator();
                    enumerator.MoveNext();
                    var cursor = enumerator.Current;
                    stateMachine.DeleteCursor(cursor.Id);
                }
                else
                {
                    bool found = false;
                    var sId = MyStringId.GetOrCompute(m_instance.TransitionTo);
                    foreach (var outTransition in OutTransitions)
                        if (outTransition.Name == sId)
                        {
                            found = true;
                            break;
                        }

                    Debug.Assert(found, "State with outgoing transitions triggered non existent transition! Fix your scripts (or mission machines)!");
                    IMyVariableStorage<bool> storage;
                    // unblock one of the transitions
                    if(m_transitionNamesToVariableStorages.TryGetValue(MyStringId.GetOrCompute(m_instance.TransitionTo), out storage))
                        storage.SetValue(MyStringId.GetOrCompute("Left"), true);
                    else
                        Debug.Fail("Transition was not found.");

                }
            }
        }

        protected override void TransitionAddedInternal(MyStateMachineTransition transition)
        {
            base.TransitionAddedInternal(transition);
            if(transition.TargetNode != this)
            {
                var variableStorage = new VSNodeVariableStorage();
                transition.Conditions.Add(new MyCondition<bool>(variableStorage, MyCondition<bool>.MyOperation.Equal, "Left", "Right"));
                m_transitionNamesToVariableStorages.Add(transition.Name, variableStorage);
            }
        }

        public void ActivateScript(bool restored = false)
        {
            if(m_scriptType == null || m_instance != null)
                return;

            m_instance = Activator.CreateInstance(m_scriptType) as IMyStateMachineScript;
            Debug.Assert(m_instance != null, "Script instance should never be null.");
            if (restored)
            {
                m_instance.Deserialize();
            }

            m_instance.Init();   

            // reset transitions to blocking state
            foreach (var storage in m_transitionNamesToVariableStorages.Values)
                storage.SetValue(MyStringId.GetOrCompute("Left"), false);
        }

        public void DisposeScript()
        {
            if(m_instance == null) return;

            m_instance.Dispose();
            m_instance = null;
        }

    }
}
