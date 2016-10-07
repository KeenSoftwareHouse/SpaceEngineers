using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Planet;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Engine.Multiplayer
{
    /// <summary>
    /// Client state, can be defined per-game.
    /// </summary>
    public abstract class MyClientState : MyClientStateBase
    {
        public readonly Dictionary<long, HashSet<long>> KnownSectors = new Dictionary<long, HashSet<long>>();

        public enum MyContextKind
        {
            None = 0,
            Terminal = 1,
            Inventory = 2,
            Production = 3,
            Building = 4,
        }

        /// <summary>
        /// Client point of interest, used on server to replicate nearby entities
        /// </summary>
        public Vector3D Position { get; protected set; }
        public MyContextKind Context { get; protected set; }
        public MyEntity ContextEntity { get; protected set; }

        uint m_currentServerTimeStamp = 0;

        public override void Serialize(BitStream stream,uint serverTimeStamp)
        {
            if (stream.Writing)
                Write(stream);
            else
                Read(stream, serverTimeStamp);
        }

        protected virtual MyEntity GetControlledEntity()
        {
            if (MySession.Static.HasAdminRights && MySession.Static.CameraController == MySpectatorCameraController.Static
                && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
                return null;

            return MySession.Static.ControlledEntity != null ? MySession.Static.ControlledEntity.Entity.GetTopMostParent() : null;
        }

        private void Write(BitStream stream)
        {
            WritePlanetSectors(stream);

            // TODO: Make sure sleeping, server controlled entities are not moving locally (or they can be but eventually their position should be corrected)
            MyEntity controlledEntity = GetControlledEntity();

            WriteShared(stream, controlledEntity);
            if (controlledEntity != null)
            {
                WriteInternal(stream, controlledEntity);
                WritePhysics(stream, controlledEntity);
            }
        }

        private void Read(BitStream stream, uint serverTimeStamp)
        {
            MyNetworkClient sender;
            if (!Sync.Clients.TryGetClient(EndpointId.Value, out sender))
            {
                Debug.Fail("Unknown sender");
                return;
            }

            ReadPlanetSectors(stream);

            MyEntity controlledEntity;
            ReadShared(stream, sender, out controlledEntity);
            if (controlledEntity != null)
            {
                ReadInternal(stream, sender, controlledEntity);
                ReadPhysics(stream, sender, controlledEntity,serverTimeStamp);
            }
        }

        protected abstract void WriteInternal(BitStream stream, MyEntity controlledEntity);
        protected abstract void ReadInternal(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity);

        /// <summary>
        /// Shared area for SE and ME. So far it writes whether you have a controlled entity or not. In the latter case you get the spectator position
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="validControlledEntity"></param>
        private void WriteShared(BitStream stream, MyEntity controlledEntity)
        {
            stream.WriteUInt32(ClientTimeStamp);
            stream.WriteBool(controlledEntity != null);
            if (controlledEntity == null)
            {
                Vector3D pos = MySpectatorCameraController.Static.Position;
                stream.Serialize(ref pos);
            }
            else
            {
                stream.WriteInt64(controlledEntity.EntityId);
            }
        }

        private void ReadShared(BitStream stream, MyNetworkClient sender, out MyEntity controlledEntity)
        {
            controlledEntity = null;

            ClientTimeStamp = stream.ReadUInt32();
            var hasControlledEntity = stream.ReadBool();
            if (!hasControlledEntity)
            {
                Vector3D pos = Vector3D.Zero;
                stream.Serialize(ref pos); // 24B
                Position = pos;
            }
            else
            {
                var entityId = stream.ReadInt64();
                MyEntity entity;
                if (!MyEntities.TryGetEntityById(entityId, out entity))
                    return;

                Position = entity.WorldMatrix.Translation;

                // TODO: Obsolete check?
                MySyncEntity syncEntity = entity.SyncObject as MySyncEntity;
                if (syncEntity == null)
                    return;
                controlledEntity = entity;
            }
        }

        private void WritePhysics(BitStream stream, MyEntity controlledEntity)
        {
            IMyReplicable controlledReplicable = MyExternalReplicable.FindByObject(controlledEntity);

            stream.WriteBool(controlledReplicable != null);
          
            if (controlledReplicable == null)
            {             
                return;
            }

            IMyStateGroup stateGroup = null;

            bool useCharacterOnServer  = controlledEntity is MyCharacter &&  MyFakes.ENABLE_CHARACTER_CONTROL_ON_SERVER;
            bool useGridOnServer = controlledEntity is MyCubeGrid && MyFakes.ENABLE_SHIP_CONTROL_ON_SERVER;
            MyShipController controller = MySession.Static.ControlledEntity as MyShipController;
            bool hasWheels = controller != null && controller.HasWheels;
            long? supportId = null;

            if (useCharacterOnServer || (useGridOnServer && hasWheels == false))
            {
                MyEntityPositionVerificationStateGroup group = controlledReplicable.FindStateGroup<MyEntityPositionVerificationStateGroup>();
                stateGroup = group;
                supportId = group.GetSupportID();
            }
            else
            {
                stateGroup = controlledReplicable.FindStateGroup<MyEntityPhysicsStateGroup>();
            }

       
            stream.WriteBool(useCharacterOnServer || (useGridOnServer && hasWheels == false));
            stream.WriteBool(stateGroup != null );

            if (stateGroup == null)
            {
                return;
            }

            stream.WriteBool(supportId.HasValue);
            if (supportId.HasValue)
            {
                stream.WriteInt64(supportId.Value);
            }

            bool isResponsible = MyEntityPhysicsStateGroup.ResponsibleForUpdate(controlledEntity,new EndpointId(Sync.MyId));
            stream.WriteBool(isResponsible);
            if (isResponsible)
            {
                stateGroup.Serialize(stream, EndpointId, ClientTimeStamp, 0, 1024*1024);
            }
        }

        private void ReadPhysics(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity,uint serverTimeStamp)
        {
            
            bool hasPhysics = stream.ReadBool();

            m_currentServerTimeStamp = serverTimeStamp;

            if (hasPhysics && MyEntityPhysicsStateGroup.ResponsibleForUpdate(controlledEntity, new EndpointId(sender.SteamUserId)))
            {
                IMyStateGroup stateGroup = null;
                SupportId = null;
                bool enableControlOnServer = stream.ReadBool();
                bool stateGroupFound = stream.ReadBool();
                if (stateGroupFound == false)
                {
                    return;
                }

              
                if (stream.ReadBool())
                {
                    SupportId = stream.ReadInt64();
                }
        
                if (enableControlOnServer)
                {
                    stateGroup = MyExternalReplicable.FindByObject(controlledEntity).FindStateGroup<MyEntityPositionVerificationStateGroup>();
                }
                else
                {
                    stateGroup = MyExternalReplicable.FindByObject(controlledEntity).FindStateGroup<MyEntityPhysicsStateGroup>();
                }

                if (stream.ReadBool())
                {
                    stateGroup.Serialize(stream, new EndpointId(sender.SteamUserId), ClientTimeStamp, 0, 65535);
                }
            }
        }

        private const int PlanetMagic = 0x42424242;

        protected void WritePlanetSectors(BitStream stream)
        {
            stream.WriteInt32(PlanetMagic);

            var planets = MyPlanets.GetPlanets();

            // Planets are not enabled if session component is not loaded.
            if (planets == null)
            {
                stream.WriteInt32(0);
                return;
            }

            stream.WriteInt32(planets.Count);

            foreach (var planet in planets)
            {
                stream.WriteInt64(planet.EntityId);

                MyPlanetEnvironmentComponent env = planet.Components.Get<MyPlanetEnvironmentComponent>();

                var syncLod = env.EnvironmentDefinition.SyncLod;

                foreach (var provider in env.Providers)
                {
                    foreach (var sector in provider.LogicalSectors)
                        if (sector.MinLod <= syncLod)
                        {
                            stream.WriteInt64(sector.Id);
}
                }

                // don't know how many in advance so I will use ~0 termination instead of count.
                stream.WriteInt64(~0);
            }
        }

        protected void ReadPlanetSectors(BitStream stream)
        {
            KnownSectors.Clear();

            if (stream.ReadInt32() != PlanetMagic)
            {
                throw new BitStreamException("Wrong magic when reading planet sectors from client state.");
            }

            int count = stream.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                long p = stream.ReadInt64();
                Debug.Assert(p != ~0);

                HashSet<long> sectids = new HashSet<long>();

                KnownSectors.Add(p, sectids);

                while (true)
                {
                    long sectorid = stream.ReadInt64();
                    if (sectorid == ~0) break;

                    sectids.Add(sectorid);
                }
            }
        }
    }
}
