using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.AI.BehaviorTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.AI;
using Sandbox.Game.AI.Logic;

namespace Sandbox.Game.AI
{
    public interface IMyBot
    {
        void Init(MyObjectBuilder_Bot botBuilder);
        void InitActions(ActionCollection actionCollection);
        void InitLogic(MyBotLogic logic);
        void Cleanup();
        void Update();
        void DebugDraw();
        void Reset();

        bool IsValidForUpdate { get; }
        bool CreatedByPlayer { get; }

        MyObjectBuilder_Bot GetBotData();

        MyBehaviorTree BehaviorTree { get; set; }
        string BehaviorSubtypeName { get; }
        ActionCollection ActionCollection { get; }
        MyBotMemory BotMemory { get; }
        MyBotDefinition BotDefinition { get; }
        MyAbstractBotActionProxy BotActions { get; set; }
        MyBotLogic BotLogic { get; }
    }
}
