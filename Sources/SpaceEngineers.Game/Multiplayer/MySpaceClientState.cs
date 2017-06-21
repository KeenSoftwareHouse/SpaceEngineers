using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Library.Collections;

namespace Multiplayer
{
    class MySpaceClientState : MyClientState
    {
        static MyContextKind GetContextByPage(MyTerminalPageEnum page)
        {
            switch (page)
            {
                case MyTerminalPageEnum.Inventory: return MyContextKind.Inventory;
                case MyTerminalPageEnum.ControlPanel: return MyContextKind.Terminal;
                case MyTerminalPageEnum.Production: return MyContextKind.Production;
                default: return MyContextKind.None;
            }
        }

        protected override void WriteInternal(BitStream stream, MyEntity controlledEntity)
        {
            MyContextKind context = GetContextByPage(MyGuiScreenTerminal.GetCurrentScreen());

            stream.WriteInt32((int)context, 2);
            if (context != MyContextKind.None)
            {
                var entityId = MyGuiScreenTerminal.InteractedEntity != null ? MyGuiScreenTerminal.InteractedEntity.EntityId : 0;
                stream.WriteInt64(entityId);
            }
        }

        protected override void ReadInternal(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity)
        {
            Context = (MyContextKind)stream.ReadInt32(2);
            if (Context != MyContextKind.None)
            {
                long entityId = stream.ReadInt64();
                ContextEntity = MyEntities.GetEntityByIdOrDefault(entityId);
            }
            else
            {
                ContextEntity = null;
            }
        }
    }
}
