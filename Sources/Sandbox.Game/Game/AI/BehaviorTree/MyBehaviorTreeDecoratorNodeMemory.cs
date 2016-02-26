using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LogicMemoryBuilder = VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNodeMemory.LogicMemoryBuilder;
using TimerMemoryBuilder = VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNodeMemory.TimerLogicMemoryBuilder;
using CounterMemoryBuilder = VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNodeMemory.CounterLogicMemoryBuilder;
using System.Diagnostics;
using VRage.Game;

namespace Sandbox.Game.AI.BehaviorTree
{
    [MyBehaviorTreeNodeMemoryType(typeof(MyObjectBuilder_BehaviorTreeDecoratorNodeMemory))]
    public class MyBehaviorTreeDecoratorNodeMemory : MyBehaviorTreeNodeMemory
    {
        public abstract class LogicMemory
        {
            public abstract void ClearMemory();
            public abstract void Init(LogicMemoryBuilder logicMemoryBuilder);
            public abstract LogicMemoryBuilder GetObjectBuilder();
            public abstract void PostTickMemory();
        }

        public class TimerLogicMemory : LogicMemory
        {
            public bool TimeLimitReached { get; set; }
            public long CurrentTime { get; set; }

            public override void ClearMemory()
            {
                TimeLimitReached = true;
                CurrentTime = Stopwatch.GetTimestamp();
            }

            public override void Init(LogicMemoryBuilder logicMemoryBuilder)
            {
                var builder = logicMemoryBuilder as TimerMemoryBuilder;
                CurrentTime = Stopwatch.GetTimestamp() - builder.CurrentTime;
                TimeLimitReached = builder.TimeLimitReached;
            }

            public override LogicMemoryBuilder GetObjectBuilder()
            {
                var builder = new TimerMemoryBuilder();
                builder.CurrentTime = Stopwatch.GetTimestamp() - CurrentTime;
                builder.TimeLimitReached = TimeLimitReached;
                return builder;
            }

            public override void PostTickMemory()
            {
                TimeLimitReached = false;
                CurrentTime = Stopwatch.GetTimestamp();
            }
        }

        public class CounterLogicMemory : LogicMemory
        {
            public int CurrentCount { get; set; }

            public override void ClearMemory()
            {
                CurrentCount = 0;
            }
        
            public override void Init(LogicMemoryBuilder logicMemoryBuilder)
            {
                var builder = logicMemoryBuilder as CounterMemoryBuilder;
                CurrentCount = builder.CurrentCount;
            }

            public override LogicMemoryBuilder GetObjectBuilder()
            {
                var builder = new CounterMemoryBuilder();
                builder.CurrentCount = CurrentCount;
                return builder;
            }

            public override void PostTickMemory()
            {
                CurrentCount = 0;
            }
        }

        public MyBehaviorTreeState ChildState { get; set; }
        public LogicMemory DecoratorLogicMemory { get; set; }

        public override void Init(MyObjectBuilder_BehaviorTreeNodeMemory builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_BehaviorTreeDecoratorNodeMemory;
            ChildState = ob.ChildState;
            DecoratorLogicMemory = GetLogicMemoryByBuilder(ob.Logic);
        }

        public override MyObjectBuilder_BehaviorTreeNodeMemory GetObjectBuilder()
        {
 	        var builder = base.GetObjectBuilder() as MyObjectBuilder_BehaviorTreeDecoratorNodeMemory;
            builder.ChildState = ChildState;
            builder.Logic = DecoratorLogicMemory.GetObjectBuilder();
            return builder;
        }

        public override void ClearMemory()
        {
            base.ClearMemory();
            ChildState = MyBehaviorTreeState.NOT_TICKED;
            DecoratorLogicMemory.ClearMemory();
        }

        public override void PostTickMemory()
        {
            base.PostTickMemory();
            ChildState = MyBehaviorTreeState.NOT_TICKED;
            DecoratorLogicMemory.PostTickMemory();
        }

        // MW:TODO refactor
        private static LogicMemory GetLogicMemoryByBuilder(LogicMemoryBuilder builder)
        {
            if (builder is TimerMemoryBuilder)
                return new TimerLogicMemory();
            else if (builder is CounterMemoryBuilder)
                return new CounterLogicMemory();
            else
                Debug.Fail("Unknown builder type.");
            return null;
        }
    }
}
