using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyBehaviorTreeDecoratorTimerLogic : IMyDecoratorLogic
    {
        public long TimeInMs { get; private set; }

        public MyBehaviorTreeDecoratorTimerLogic()
        {
            TimeInMs = 0;
        }

        public void Construct(MyObjectBuilder_BehaviorTreeDecoratorNode.Logic logicData)
        {
            var timerLogic = logicData as MyObjectBuilder_BehaviorTreeDecoratorNode.TimerLogic;
            TimeInMs = timerLogic.TimeInMs;
        }

        public void Update(MyBehaviorTreeDecoratorNodeMemory.LogicMemory logicMemory)
        {
            var memory = logicMemory as MyBehaviorTreeDecoratorNodeMemory.TimerLogicMemory;
            if (((Stopwatch.GetTimestamp() - memory.CurrentTime) / Stopwatch.Frequency) * 1000 > TimeInMs)
            {
                memory.CurrentTime = Stopwatch.GetTimestamp();
                memory.TimeLimitReached = true;
            }
            else
                memory.TimeLimitReached = false;
        }

        public bool CanRun(MyBehaviorTreeDecoratorNodeMemory.LogicMemory logicMemory)
        {
            var memory = logicMemory as MyBehaviorTreeDecoratorNodeMemory.TimerLogicMemory;
            return memory.TimeLimitReached;
        }

        public MyBehaviorTreeDecoratorNodeMemory.LogicMemory GetNewMemoryObject()
        {
            return new MyBehaviorTreeDecoratorNodeMemory.TimerLogicMemory();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = ((int)TimeInMs).GetHashCode();
                return result;
            }
        }

        public override string ToString()
        {
            return "Timer";
        }
    }
}
