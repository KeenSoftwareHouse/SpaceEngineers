using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Engine.Multiplayer
{
    /// <summary>
    /// Client state, can be defined per-game.
    /// </summary>
    public class MyClientState : MyClientStateBase
    {
        public enum MyContextKind
        {
            None =0,
            Terminal,
            Inventory,
            Production,
        }
        /// <summary>
        /// Client point of interest, used on server to replicate nearby entities
        /// </summary>
        public Vector3D Position { get; private set; }
        public MyContextKind Context { get; private set;}
        public MyEntity ContextEntity { get; private set; }

        public override void Serialize(BitStream stream)
        {
            if (stream.Writing)
                Write(stream);
            else
                Read(stream);
        }

        void Write(BitStream stream)
        {
            // TODO: Make sure sleeping, server controlled entities are not moving locally (or they can be but eventually their position should be corrected)

            stream.WriteBool(MySession.ControlledEntity != null);
            if (MySession.ControlledEntity == null)
            {
                Vector3D pos = MySpectatorCameraController.Static.Position;
                stream.WriteDouble(pos.X);
                stream.WriteDouble(pos.Y);
                stream.WriteDouble(pos.Z);
            }
            else
            {
                var controlledEntity = MySession.ControlledEntity.Entity.GetTopMostParent();

                // Send controlled entity every other frame to server
                if (MyMultiplayer.Static.FrameCounter % 2 == 0)
                {
                    // TODO: Write additional client data, context

                    if (controlledEntity != null && controlledEntity.SyncFlag && ((MySyncEntity)controlledEntity.SyncObject).ResponsibleForUpdate(Sync.Clients.LocalClient))
                    {
                        stream.WriteInt64(controlledEntity.EntityId);
                        switch (MyGuiScreenTerminal.GetCurrentScreen())
                        {
                            case MyTerminalPageEnum.Inventory:
                                stream.WriteInt32((int)MyContextKind.Inventory, 2);
                                break;
                            case MyTerminalPageEnum.ControlPanel:
                                stream.WriteInt32((int)MyContextKind.Terminal, 2);
                                break;
                            case MyTerminalPageEnum.Production:
                                stream.WriteInt32((int)MyContextKind.Production, 2);
                                break;
                            default:
                                stream.WriteInt32((int)MyContextKind.None, 2);
                                break;
                        }

                        if (MyGuiScreenTerminal.InteractedEntity != null)
                        {
                            stream.WriteInt64(MyGuiScreenTerminal.InteractedEntity.EntityId);
                        }
                        else
                        {
                            stream.WriteInt64(0);
                        }

                        ((MySyncEntity)controlledEntity.SyncObject).SerializePhysics(stream, null);
                    }
                }
            }
        }

        void Read(BitStream stream)
        {
            // TODO: Read additional client data, context

            MyNetworkClient sender;
            if (!Sync.Clients.TryGetClient(EndpointId.Value, out sender))
            {
                Debug.Fail("Unknown sender");
                return;
            }

            var hasControlledEntity = stream.ReadBool();
            if (hasControlledEntity == false)
            {
                Vector3D pos = Vector3D.Zero;
                stream.Serialize(ref pos); // 24B
                Position = pos;
            }
            else
            {
                int numEntity = 0;
                if (stream.BytePosition < stream.ByteLength)
                {
                    var entityId = stream.ReadInt64();
                    MyEntity entity;
                    if (!MyEntities.TryGetEntityById(entityId, out entity))
                        return;

                    MySyncEntity syncEntity = entity.SyncObject as MySyncEntity;
                    if (syncEntity == null)
                        return;

                    Context = (MyContextKind)stream.ReadInt32(2);

                    switch (Context)
                    {
                        case MyContextKind.Inventory:
                            entityId = stream.ReadInt64();
                            break;
                        case MyContextKind.Terminal:
                            entityId = stream.ReadInt64();
                            break;
                        case MyContextKind.Production:
                            entityId = stream.ReadInt64();
                            break;
                        default:
                            entityId = stream.ReadInt64();
                            break;
                    }

                    MyEntities.TryGetEntityById(entityId, out entity);
                    ContextEntity = entity;

                    if (!syncEntity.ResponsibleForUpdate(sender))
                    {
                        // Also happens when entering cockpit due to order of operations and responsibility update change
                        //Debug.Fail("Server sending entity update for entity controlled by client, should happen only very rarely (packets out-of-order)");
                        return;
                    }
                    syncEntity.SerializePhysics(stream, sender);

                    if (numEntity == 0)
                    {
                        Position = syncEntity.Entity.WorldMatrix.Translation;
                    }
                    numEntity++;
                }
            }
        }
    }
}
