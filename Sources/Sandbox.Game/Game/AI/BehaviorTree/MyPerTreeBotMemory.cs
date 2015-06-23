using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.AI;
using VRage.Utils;
using System.Diagnostics;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.Collections;

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyPerTreeBotMemory
    {
        private List<MyBehaviorTreeNodeMemory> m_nodesMemory;
        private Dictionary<MyStringId, MyBBMemoryValue> m_blackboardMemory;

        public int NodesMemoryCount { get { return m_nodesMemory.Count; } }
        public ListReader<MyBehaviorTreeNodeMemory> NodesMemory { get { return new ListReader<MyBehaviorTreeNodeMemory>(m_nodesMemory); } }

        public IEnumerable<KeyValuePair<MyStringId, MyBBMemoryValue>> BBMemory { get { return m_blackboardMemory; } }

        public MyPerTreeBotMemory()
        {
            m_nodesMemory = new List<MyBehaviorTreeNodeMemory>(20);
            m_blackboardMemory = new Dictionary<MyStringId, MyBBMemoryValue>(20, MyStringId.Comparer);
        }

        public void AddNodeMemory(MyBehaviorTreeNodeMemory nodeMemory)
        {
            m_nodesMemory.Add(nodeMemory);
        }

        public void AddBlackboardMemoryInstance(string name, MyBBMemoryValue obj)
        {
            MyStringId stringId = MyStringId.GetOrCompute(name);
            m_blackboardMemory.Add(stringId, obj);
        }

        public void RemoveBlackboardMemoryInstance(MyStringId name)
        {
            m_blackboardMemory.Remove(name);
        }

        public MyBehaviorTreeNodeMemory GetNodeMemoryByIndex(int index)
        {
            return m_nodesMemory[index];
        }

        public void ClearNodesData()
        {
            foreach (var nodeMemory in m_nodesMemory)
                nodeMemory.ClearNodeState();
        }

        public void Clear()
        {
            m_nodesMemory.Clear();
            m_blackboardMemory.Clear();
        }

        public bool TryGetFromBlackboard<T>(MyStringId id, out T value)
            where T : MyBBMemoryValue
        {
            MyBBMemoryValue output = null;
            bool found = m_blackboardMemory.TryGetValue(id, out output);
            value = output as T;
            return found;
        }

        public void SaveToBlackboard(MyStringId id, MyBBMemoryValue value)
        {
            Debug.Assert(id != MyStringId.NullOrEmpty, "Empty id for memory.");
            if (id != MyStringId.NullOrEmpty)
            {
                m_blackboardMemory[id] = value;
            }
        }

        public MyBBMemoryValue TrySaveToBlackboard(MyStringId id, Type type)
        {
            Debug.Assert(type.IsSubclassOf(typeof(MyBBMemoryValue)) || type == typeof(MyBBMemoryValue), "Invalid type is being saved to blackboard memory");
            if (!(type.IsSubclassOf(typeof(MyBBMemoryValue)) || type == typeof(MyBBMemoryValue)))
                return null;
            Debug.Assert(type.GetConstructor(Type.EmptyTypes) != null, "Invalid type is being saved to blackboard memory");
            if (type.GetConstructor(Type.EmptyTypes) == null)
                return null;
            var newMemoryChunk = Activator.CreateInstance(type) as MyBBMemoryValue;
            m_blackboardMemory[id] = newMemoryChunk;
            return newMemoryChunk;
        }
    }
}
