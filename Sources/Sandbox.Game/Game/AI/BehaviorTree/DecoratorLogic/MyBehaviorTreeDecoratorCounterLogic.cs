using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyBehaviorTreeDecoratorCounterLogic : IMyDecoratorLogic
    {
        public int CounterLimit { get; private set; }

        public MyBehaviorTreeDecoratorCounterLogic()
        {
            CounterLimit = 0;
        }

        public void Construct(MyObjectBuilder_BehaviorTreeDecoratorNode.Logic logicData)
        {
            var counterLogic = logicData as MyObjectBuilder_BehaviorTreeDecoratorNode.CounterLogic;
            CounterLimit = counterLogic.Count;
        }

        public void Update(MyBehaviorTreeDecoratorNodeMemory.LogicMemory logicMemory)
        {
            var memory = logicMemory as MyBehaviorTreeDecoratorNodeMemory.CounterLogicMemory;
            if (memory.CurrentCount == CounterLimit)
                memory.CurrentCount = 0;
            else
                ++memory.CurrentCount;
        }

        public bool CanRun(MyBehaviorTreeDecoratorNodeMemory.LogicMemory logicMemory)
        {
            var memory = logicMemory as MyBehaviorTreeDecoratorNodeMemory.CounterLogicMemory;
            return memory.CurrentCount == CounterLimit;
        }

        public MyBehaviorTreeDecoratorNodeMemory.LogicMemory GetNewMemoryObject()
        {
            return new MyBehaviorTreeDecoratorNodeMemory.CounterLogicMemory();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = CounterLimit.GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return "Counter";
        }
    }
}
