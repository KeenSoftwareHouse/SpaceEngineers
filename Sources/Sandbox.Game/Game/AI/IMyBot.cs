using Sandbox.Definitions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.AI.Actions;
using VRage.Game;

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

        MyObjectBuilder_Bot GetObjectBuilder();

       // MyBehaviorTree BehaviorTree { get; set; }
        string BehaviorSubtypeName { get; }
        ActionCollection ActionCollection { get; }
        MyBotMemory BotMemory { get; }
        MyBotMemory LastBotMemory { get; set; }
        void ReturnToLastMemory(); // for debugging
        MyBotDefinition BotDefinition { get; }
        MyBotActionsBase BotActions { get; set; }
        MyBotLogic BotLogic { get; }
    }
}
