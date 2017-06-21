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
using VRage.Library.Utils;

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
        public MyContextKind Context { get; protected set; }
        public MyEntity ContextEntity { get; protected set; }

        MyTimeSpan m_currentServerTimeStamp = MyTimeSpan.Zero;

        private MyEntity m_positionEntityServer;

        public override Vector3D Position
        {
            get
            {
                return m_positionEntityServer != null ? m_positionEntityServer.WorldMatrix.Translation : base.Position;
            }
            protected set { base.Position = value; }
        }

        public override void Serialize(BitStream stream, bool outOfOrder)
        {
            if (stream.Writing)
                Write(stream);
            else
                Read(stream, outOfOrder);
        }

        public override void Update()
        {
            MyEntity controlledEntity;
            bool hasControl;
            GetControlledEntity(out controlledEntity, out hasControl);
            if (hasControl && controlledEntity != null)
                controlledEntity.ApplyLastControls();
        }

        protected virtual void GetControlledEntity(out MyEntity controlledEntity, out bool hasControl)
        {
            controlledEntity = null;
            hasControl = false;
            if (MySession.Static.HasCreativeRights && MySession.Static.CameraController == MySpectatorCameraController.Static
                && (MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled ||
                MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.Orbit))
                return;

            controlledEntity = MySession.Static.TopMostControlledEntity;
            if (controlledEntity == null)
                return;

            var controllingPlayer = Sync.Players.GetControllingPlayer(controlledEntity);
            hasControl = MySession.Static.LocalHumanPlayer == controllingPlayer;
        }

        private void Write(BitStream stream)
        {
            WritePlanetSectors(stream);

            // TODO: Make sure sleeping, server controlled entities are not moving locally (or they can be but eventually their position should be corrected)
            MyEntity controlledEntity;
            bool hasControl;
            GetControlledEntity(out controlledEntity, out hasControl);

            WriteShared(stream, controlledEntity, hasControl);
            if (controlledEntity != null)
            {
                WriteInternal(stream, controlledEntity);
                controlledEntity.SerializeControls(stream);
                //WritePhysics(stream, controlledEntity);
            }
        }

        private void Read(BitStream stream, bool outOfOrder)
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
                controlledEntity.DeserializeControls(stream, outOfOrder);
            }
        }

        protected abstract void WriteInternal(BitStream stream, MyEntity controlledEntity);
        protected abstract void ReadInternal(BitStream stream, MyNetworkClient sender, MyEntity controlledEntity);

        /// <summary>
        /// Shared area for SE and ME. So far it writes whether you have a controlled entity or not. In the latter case you get the spectator position
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="validControlledEntity"></param>
        private void WriteShared(BitStream stream, MyEntity controlledEntity, bool hasControl)
        {
            stream.WriteBool(controlledEntity != null);
            if (controlledEntity == null)
            {
                Vector3D pos = MySpectatorCameraController.Static.Position;
                stream.Serialize(ref pos);
            }
            else
            {
                stream.WriteInt64(controlledEntity.EntityId);
                stream.WriteBool(hasControl);
            }
        }

        private void ReadShared(BitStream stream, MyNetworkClient sender, out MyEntity controlledEntity)
        {
            controlledEntity = null;

            var hasControlledEntity = stream.ReadBool();
            if (!hasControlledEntity)
            {
                Vector3D pos = Vector3D.Zero;
                stream.Serialize(ref pos); // 24B
                m_positionEntityServer = null;
                Position = pos;
            }
            else
            {
                var entityId = stream.ReadInt64();
                bool hasControl = stream.ReadBool();
                MyEntity entity;
                if (!MyEntities.TryGetEntityById(entityId, out entity))
                {
                    m_positionEntityServer = null;
                    return;
                }

                m_positionEntityServer = entity;
                if(!hasControl)
                    return;

                // TODO: Obsolete check?
                MySyncEntity syncEntity = entity.SyncObject as MySyncEntity;
                if (syncEntity == null)
                    return;
                controlledEntity = entity;
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

                if (KnownSectors.ContainsKey(p))
                    KnownSectors[p] = sectids;
                else
                    KnownSectors.Add(p, sectids);

                while (true)
                {
                    long sectorid = stream.ReadInt64();
                    if (sectorid == ~0) break;

                    sectids.Add(sectorid);
                }
            }
        }

        public override IMyReplicable ControlledReplicable
        {
            get 
            {
                MyPlayer player = this.GetPlayer();
                if (player == null) return null;
                MyCharacter controlledCharacter = player.Character;
                return controlledCharacter != null ? MyExternalReplicable.FindByObject(controlledCharacter) : null; 
            }
        }
    }
}
