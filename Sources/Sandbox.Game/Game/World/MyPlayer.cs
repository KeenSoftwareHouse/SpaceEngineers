﻿using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Components;
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

		private MyIdentity m_identity;
        public MyIdentity Identity {
			get
			{
				return m_identity;
			} 

			set
			{
				Debug.Assert(value != null, "Changing an identity of a controller to nobody, which does not make sense");
				m_identity = value;

				if (IdentityChanged != null)
					IdentityChanged(this, value);
			}
		}
        public event Action<MyPlayer, MyIdentity> IdentityChanged;

        /// <summary>
        /// This is created with the creation of the player, so it should never be null
        /// </summary>
        public MyEntityController Controller { get; private set; }

        public string DisplayName { get; private set; }

		// Colors that are applied to blocks when the player builds or paints them
		private const int m_buildColorSlotCount = 14;
		public static int BuildColorSlotCount { get { return m_buildColorSlotCount; } }
		
		private int m_selectedBuildColorSlot = 0;
		public int SelectedBuildColorSlot { get { return m_selectedBuildColorSlot; } set { m_selectedBuildColorSlot = MathHelper.Clamp(value, 0, m_buildColorHSVSlots.Count-1); } }

		public Vector3 SelectedBuildColor { get { return m_buildColorHSVSlots[m_selectedBuildColorSlot]; } set { m_buildColorHSVSlots[m_selectedBuildColorSlot] = value; } }

		// MK: TODO: Remove these static properties for bot colours
		public static int SelectedColorSlot { get { return MySession.LocalHumanPlayer != null ? MySession.LocalHumanPlayer.SelectedBuildColorSlot : 0; } }
		public static Vector3 SelectedColor { get { return MySession.LocalHumanPlayer != null ? MySession.LocalHumanPlayer.SelectedBuildColor : m_buildColorDefaults[0]; } }
		public static ListReader<Vector3> ColorSlots { get { return MySession.LocalHumanPlayer != null ? MySession.LocalHumanPlayer.BuildColorSlots : new ListReader<Vector3>(m_buildColorDefaults); } }

		private static readonly List<Vector3> m_buildColorDefaults = new List<Vector3>(m_buildColorSlotCount);

		private List<Vector3> m_buildColorHSVSlots = new List<Vector3>(m_buildColorSlotCount);
		public List<Vector3> BuildColorSlots { get { return m_buildColorHSVSlots; } }

		public bool IsLocalPlayer { get { return m_client == Sync.Clients.LocalClient; } }
		public bool IsRemotePlayer { get { return m_client != Sync.Clients.LocalClient; } }

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

        /// #warning: This should probably be on the identity. Check whether it's correct
        /// <summary>
        /// Grids in which this player has at least one block
        /// </summary>
        public HashSet<long> Grids = new HashSet<long>();

		static MyPlayer()
		{
			InitDefaultColors();
		}

        public MyPlayer(MyNetworkClient client, PlayerId id)
        {
            m_client = client;
            Id = id;
            Controller = new MyEntityController(this);
        }

		public void Init(MyObjectBuilder_Player objectBuilder)
		{
			DisplayName = objectBuilder.DisplayName;
			Identity = Sync.Players.TryGetIdentity(objectBuilder.IdentityId);

			if (m_buildColorHSVSlots.Count < m_buildColorSlotCount)
			{
				var defaultCount = m_buildColorHSVSlots.Count;
				for (int index = 0; index < m_buildColorSlotCount - defaultCount; ++index)
					m_buildColorHSVSlots.Add(MyRenderComponentBase.OldBlackToHSV);
			}

			if ((objectBuilder.BuildColorSlots == null) || (objectBuilder.BuildColorSlots.Count == 0))
			{
				SetDefaultColors();
			}
			else if (objectBuilder.BuildColorSlots.Count == m_buildColorSlotCount)
			{
				m_buildColorHSVSlots = objectBuilder.BuildColorSlots;
			}
			else if (objectBuilder.BuildColorSlots.Count > m_buildColorSlotCount)
			{
				m_buildColorHSVSlots = new List<Vector3>(m_buildColorSlotCount);
				for (int i = 0; i < m_buildColorSlotCount; i++)
					m_buildColorHSVSlots.Add(objectBuilder.BuildColorSlots[i]);
			}
			else
			{
				m_buildColorHSVSlots = objectBuilder.BuildColorSlots;
				for (int i = m_buildColorHSVSlots.Count - 1; i < m_buildColorSlotCount; i++)
					m_buildColorHSVSlots.Add(MyRenderComponentBase.OldBlackToHSV);
			}

			if (!Sync.IsServer)
				return;

			// Don't care about bot build colours for now
			if (Id.SerialId != 0)
				return;

			if (MyCubeBuilder.AllPlayersColors == null)
				MyCubeBuilder.AllPlayersColors = new Dictionary<PlayerId, List<Vector3>>();

			if (!MyCubeBuilder.AllPlayersColors.ContainsKey(Id))
				MyCubeBuilder.AllPlayersColors.Add(Id, m_buildColorHSVSlots);
			else
				MyCubeBuilder.AllPlayersColors.TryGetValue(Id, out m_buildColorHSVSlots);
		}

		public MyObjectBuilder_Player GetObjectBuilder()
		{
			MyObjectBuilder_Player objectBuilder = new MyObjectBuilder_Player();

			objectBuilder.DisplayName = DisplayName;
			objectBuilder.IdentityId = Identity.IdentityId;
			objectBuilder.Connected = true;

			if (!IsColorsSetToDefaults(m_buildColorHSVSlots))
			{
				objectBuilder.BuildColorSlots = new List<Vector3>();

				foreach (var color in m_buildColorHSVSlots)
				{
					objectBuilder.BuildColorSlots.Add(color);
				}
			}

			return objectBuilder;
		}

		public static bool IsColorsSetToDefaults(List<Vector3> colors)
		{
			for (int index = 0; index < m_buildColorSlotCount; ++index)
			{
				if (colors[index] != m_buildColorDefaults[index])
					return false;
			}

			return true;
		}

		public void SetDefaultColors()
		{
			for (int index = 0; index < m_buildColorSlotCount; ++index)
			{
				m_buildColorHSVSlots[index] = m_buildColorDefaults[index];
			}
		}

		private static void InitDefaultColors()
		{
			if (m_buildColorDefaults.Count < m_buildColorSlotCount)
			{
				var defaultCount = m_buildColorDefaults.Count;
				for (int index = 0; index < m_buildColorSlotCount - defaultCount; ++index)
					m_buildColorDefaults.Add(MyRenderComponentBase.OldBlackToHSV);
			}
			m_buildColorDefaults[0] = (MyRenderComponentBase.OldGrayToHSV);
			m_buildColorDefaults[1] = (MyRenderComponentBase.OldRedToHSV);
			m_buildColorDefaults[2] = (MyRenderComponentBase.OldGreenToHSV);
			m_buildColorDefaults[3] = (MyRenderComponentBase.OldBlueToHSV);
			m_buildColorDefaults[4] = (MyRenderComponentBase.OldYellowToHSV);
			m_buildColorDefaults[5] = (MyRenderComponentBase.OldWhiteToHSV);
			m_buildColorDefaults[6] = (MyRenderComponentBase.OldBlackToHSV);
			for (int index = 7; index < m_buildColorSlotCount; ++index)
				m_buildColorDefaults[index] = (m_buildColorDefaults[index - 7] + new Vector3(0, 0.15f, 0.2f));
		}

		public void ChangeOrSwitchToColor(Vector3 color)
		{
			for (int i = 0; i < m_buildColorSlotCount; i++)
			{
				if (m_buildColorHSVSlots[i] == color)
				{
					m_selectedBuildColorSlot = i;
					return;
				}
			}
			SelectedBuildColor = color;
		}

		public void SetBuildColorSlots(List<Vector3> newColors)
		{
			m_buildColorHSVSlots = newColors;
			if (MyCubeBuilder.AllPlayersColors != null && MyCubeBuilder.AllPlayersColors.Remove(Id))
				MyCubeBuilder.AllPlayersColors.Add(Id, m_buildColorHSVSlots);
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
