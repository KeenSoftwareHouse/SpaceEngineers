﻿using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.World
{
    public partial class MyPlayer
    {
        public struct PlayerId : IComparable<PlayerId>
        {
            public ulong SteamId; // Steam Id that identifies the steam account that the controller belongs to
            public int SerialId;  // Serial Id to differentiate between multiple controllers on one computer

            public PlayerId(ulong steamId) : this(steamId, 0) {}

            public PlayerId(ulong steamId, int serialId)
            {
                SteamId = steamId;
                SerialId = serialId;
            }

            public static bool operator ==(PlayerId a, PlayerId b)
            {
                return a.SteamId == b.SteamId && a.SerialId == b.SerialId;
            }
            
            public static bool operator !=(PlayerId a, PlayerId b)
            {
                return !(a == b);
            }

            public override string ToString()
            {
                return SteamId.ToString() + ":" + SerialId.ToString();
            }

            public override bool Equals(object obj)
            {
                return (obj is PlayerId) && ((PlayerId)obj == this);
            }

            public override int GetHashCode()
            {
                return SteamId.GetHashCode() * 571 ^ SerialId.GetHashCode();
            }

            public int CompareTo(PlayerId other)
            {
                if (SteamId < other.SteamId) return -1;
                else if (SteamId > other.SteamId) return 1;
                else if (SerialId < other.SerialId) return -1;
                else if (SerialId > other.SerialId) return 1;
                else return 0;
            }

            public static PlayerId operator ++(PlayerId id)
            {
                id.SerialId++;
                return id;
            }

            public static PlayerId operator --(PlayerId id)
            {
                id.SerialId--;
                return id;
            }
        }

        private MyNetworkClient m_client;
        public MyNetworkClient Client { get { return m_client; } }

        public MyIdentity Identity { get; private set; }
        public event Action<MyPlayer, MyIdentity> IdentityChanged;

        /// <summary>
        /// This is created with the creation of the player, so it should never be null
        /// </summary>
        public MyEntityController Controller { get; private set; }

        public string DisplayName { get; private set; }

        public MyCharacter Character
        {
            get
            {
                Debug.Assert(Identity != null, "Identity was null for a player!");
                return Identity.Character;
            }
        }

        public PlayerId Id { get; protected set; }

        public List<long> RespawnShip = new List<long>();

        #warning: This should probably be on the identity. Check whether it's correct
        /// <summary>
        /// Grids in which this player has at least one block
        /// </summary>
        public HashSet<long> Grids = new HashSet<long>();

        public MyPlayer(MyNetworkClient client, string displayName, PlayerId id)
        {
            m_client = client;
            Id = id;
            DisplayName = displayName;
            Controller = new MyEntityController(this);
        }

        public void ChangeIdentity(MyIdentity newIdentity)
        {
            Debug.Assert(newIdentity != null, "Changing an identity of a controller to nobody, which does not make sense");
            Identity = newIdentity;

            if (IdentityChanged != null)
                IdentityChanged(this, newIdentity);
        }

        public bool IsLocalPlayer()
        {
            return m_client == Sync.Clients.LocalClient;
        }

        public bool IsRemotePlayer()
        {
            return m_client != Sync.Clients.LocalClient;
        }

        public Vector3D GetPosition()
        {
            if (Controller.ControlledEntity != null && Controller.ControlledEntity.Entity != null)
            {
                return Controller.ControlledEntity.Entity.PositionComp.GetPosition();
            }
            else return Vector3D.Zero;
        }

        public void SpawnAt(MatrixD worldMatrix, Vector3 velocity, bool findFreePlace = true)
        {
            Debug.Assert(Sync.IsServer, "Spawning can be called only on the server!");
            Debug.Assert(Identity != null, "Spawning with empty identity!");
            if (!Sync.IsServer || Identity == null) return;

            if (findFreePlace)
            {
                Vector3D? correctedPos = MyEntities.FindFreePlace(worldMatrix.Translation, 0.5f, 200);
                if (correctedPos.HasValue)
                    worldMatrix.Translation = correctedPos.Value;
            }

            var character = MyCharacter.CreateCharacter(worldMatrix, velocity, Identity.DisplayName, Identity.Model, Identity.ColorMask, findNearPos: false, useInventory: Id.SerialId == 0);
            Sync.Players.SetPlayerCharacter(this, character);
            Sync.Players.RevivePlayer(this);
        }

        public void SpawnAtRelative(MyEntity parentEntity, Matrix relativeMatrix, Vector3 relativeVelocity)
        {
            Debug.Assert(Sync.IsServer, "Spawning can be called only on the server!");
            Debug.Assert(Identity != null, "Spawning with empty identity!");
            if (!Sync.IsServer || Identity == null) return;

            var character = MyCharacter.CreateCharacterRelative(parentEntity, relativeMatrix, relativeVelocity, Identity.DisplayName, Identity.Model, Identity.ColorMask, false);
            Sync.Players.SetPlayerCharacter(this, character);
            Sync.Players.RevivePlayer(this);
        }

        public void SpawnIntoCharacter(MyCharacter character)
        {
            Debug.Assert(Sync.IsServer);

            Sync.Players.SetPlayerCharacter(this, character);
            Sync.Players.RevivePlayer(this);
        }

        public static MyRelationsBetweenPlayerAndBlock GetRelationBetweenPlayers(long playerId1, long playerId2)
        {
            if (playerId1 == playerId2) return MyRelationsBetweenPlayerAndBlock.Owner;

            var faction1 = MySession.Static.Factions.TryGetPlayerFaction(playerId1);
            var faction2 = MySession.Static.Factions.TryGetPlayerFaction(playerId2);

            if (faction1 == null || faction2 == null)
                return MyRelationsBetweenPlayerAndBlock.Enemies;

            if (faction1 == faction2)
                return MyRelationsBetweenPlayerAndBlock.FactionShare;

            MyRelationsBetweenFactions relation = MySession.Static.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId);
            if (relation == MyRelationsBetweenFactions.Neutral)
                return MyRelationsBetweenPlayerAndBlock.Neutral;

            return MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId)
        {
            if (Identity == null) return MyRelationsBetweenPlayerAndBlock.Enemies;

            return GetRelationBetweenPlayers(Identity.IdentityId, playerId);
        }

        public void RemoveGrid(long gridEntityId)
        {
            Grids.Remove(gridEntityId);
        }

        public void AddGrid(long gridEntityId)
        {
            Grids.Add(gridEntityId);
        }
    }
}
