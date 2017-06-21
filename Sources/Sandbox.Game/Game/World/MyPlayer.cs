using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game.Models;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1

namespace Sandbox.Game.World
{
    public partial class MyPlayer
    {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public struct PlayerId : IComparable<PlayerId>
#else // XB1
        public struct PlayerId : IComparable<PlayerId>, IMySetGetMemberDataHelper
#endif // XB1
        {
            public ulong SteamId; // Steam Id that identifies the steam account that the controller belongs to
            public int SerialId;  // Serial Id to differentiate between multiple controllers on one computer

            public bool IsValid { get { return SteamId != 0; } }

            public PlayerId(ulong steamId) : this(steamId, 0) {}

            public PlayerId(ulong steamId, int serialId)
            {
                SteamId = steamId;
                SerialId = serialId;
            }

            #region Equals
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

            public class PlayerIdComparerType : IEqualityComparer<PlayerId>
            {
                public bool Equals(PlayerId left, PlayerId right)
                {
                    return left == right;
                }

                public int GetHashCode(PlayerId playerId)
                {
                    return playerId.GetHashCode();
                }
            }
            public static readonly PlayerIdComparerType Comparer = new PlayerIdComparerType();
            #endregion Equals

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

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            public object GetMemberData(MemberInfo m)
            {
                if (m.Name == "SteamId")
                    return SteamId;
                if (m.Name == "SerialId")
                    return SerialId;

                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return null;
            }
#endif // XB1
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
		public static int SelectedColorSlot { get { return MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.SelectedBuildColorSlot : 0; } }
		public static Vector3 SelectedColor { get { return MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.SelectedBuildColor : m_buildColorDefaults[0]; } }
		public static ListReader<Vector3> ColorSlots { get { return MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.BuildColorSlots : new ListReader<Vector3>(m_buildColorDefaults); } }

		private static readonly List<Vector3> m_buildColorDefaults = new List<Vector3>(m_buildColorSlotCount);

		private List<Vector3> m_buildColorHSVSlots = new List<Vector3>(m_buildColorSlotCount);
		public List<Vector3> BuildColorSlots { get { return m_buildColorHSVSlots; } }

		public bool IsLocalPlayer { get { return m_client == Sync.Clients.LocalClient; } }
		public bool IsRemotePlayer { get { return m_client != Sync.Clients.LocalClient; } }

        public bool IsRealPlayer { get { return Id.SerialId == 0; } }
        public bool IsBot { get { return !IsRealPlayer; } }

        public bool IsAdmin
        {
            get
            {
                if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    return true;
                return MySession.Static.IsUserAdmin( Client.SteamUserId );
            }
        }

        public bool IsSpaceMaster
        {
            get
            {
                if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    return true;
                return MySession.Static.IsUserSpaceMaster( Client.SteamUserId );
            }
        }

        public bool IsScripter
        {
            get
            {
                if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    return true;
                return MySession.Static.IsUserScripter(Client.SteamUserId);
            }
        }

        public bool IsModerator
        {
            get
            {
                if (MySession.Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    return true;
                return MySession.Static.IsUserModerator(Client.SteamUserId);
            }
        }

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

        public List<long> CachedControllerId;

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

        public void SpawnAt(MatrixD worldMatrix, Vector3 velocity, MyEntity spawnedBy, MyBotDefinition botDefinition, bool findFreePlace = true)
        {
            Debug.Assert(Sync.IsServer, "Spawning can be called only on the server!");
            Debug.Assert(Identity != null, "Spawning with empty identity!");
            if (!Sync.IsServer || Identity == null) return;

            var character = MyCharacter.CreateCharacter(worldMatrix, velocity, Identity.DisplayName, Identity.Model,
                Identity.ColorMask, findNearPos: false, useInventory: Id.SerialId == 0, playerSteamId: this.Id.SteamId,
                botDefinition: botDefinition);

            if (findFreePlace)
            {
                float radius;
                MyModel model = character.Render.GetModel();
                radius = model.BoundingBox.Size.Length() / 2;
                const float SPHERE_REDUCTION_RATE = 0.9f;
                radius *= SPHERE_REDUCTION_RATE;

                // Offset from bottom position to center.
                Vector3 up = worldMatrix.Up;
                up.Normalize();
                const float RISE_STEP = 0.01f; // 1cm
                Vector3 offset = up * (radius + RISE_STEP);
                MatrixD matrix = worldMatrix;
                matrix.Translation = worldMatrix.Translation + offset;

                // Attempt first to rotate around the given spawn location (matrix.Up axis rotation)
                // NOTE: A proper orbital search would be better here
                Vector3D? correctedPos = MyEntities.FindFreePlace(ref matrix, matrix.GetDirectionVector(Base6Directions.Direction.Up), radius, 200, 15, 0.2f);
                if (!correctedPos.HasValue)
                {
                    // Attempt secondly to rotate around matrix.Right axis
                    correctedPos = MyEntities.FindFreePlace(ref matrix, matrix.GetDirectionVector(Base6Directions.Direction.Right), radius, 200, 15, 0.2f);
                    if (!correctedPos.HasValue)
                    {
                        // If everything fails attempt the old FindFreePlace
                        correctedPos = MyEntities.FindFreePlace(worldMatrix.Translation + offset, radius, 200, 15, 0.2f);
                    }
                }

                if (correctedPos.HasValue)
                {
                    worldMatrix.Translation = correctedPos.Value - offset;
                    character.PositionComp.SetWorldMatrix(worldMatrix);
                }
            }

            Sync.Players.SetPlayerCharacter(this, character, spawnedBy);
            Sync.Players.RevivePlayer(this);
        }

        public void SpawnIntoCharacter(MyCharacter character)
        {
            Debug.Assert(Sync.IsServer);

            Sync.Players.SetPlayerCharacter(this, character, null);
            Sync.Players.RevivePlayer(this);
        }

        public static VRage.Game.MyRelationsBetweenPlayerAndBlock GetRelationBetweenPlayers(long playerId1, long playerId2)
        {
            if (playerId1 == playerId2) return VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner;

            var faction1 = MySession.Static.Factions.TryGetPlayerFaction(playerId1);
            var faction2 = MySession.Static.Factions.TryGetPlayerFaction(playerId2);

            if (faction1 == null || faction2 == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;

            if (faction1 == faction2)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare;

            MyRelationsBetweenFactions relation = MySession.Static.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId);
            if (relation == MyRelationsBetweenFactions.Neutral)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral;

            return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public VRage.Game.MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId)
        {
            if (Identity == null) return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;

            return GetRelationBetweenPlayers(Identity.IdentityId, playerId);
        }

        public static MyRelationsBetweenPlayers GetRelationsBetweenPlayers(long playerId1, long playerId2)
        {
            if (playerId1 == 0 || playerId2 == 0) return MyRelationsBetweenPlayers.Neutral;
            if (playerId1 == playerId2) return MyRelationsBetweenPlayers.Self;

            var faction1 = MySession.Static.Factions.TryGetPlayerFaction(playerId1);
            var faction2 = MySession.Static.Factions.TryGetPlayerFaction(playerId2);

            if (faction1 == null || faction2 == null)
                return MyRelationsBetweenPlayers.Enemies;

            if (faction1 == faction2)
                return MyRelationsBetweenPlayers.Allies;

            MyRelationsBetweenFactions relation = MySession.Static.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId);
            if (relation == MyRelationsBetweenFactions.Neutral)
                return MyRelationsBetweenPlayers.Neutral;

            return MyRelationsBetweenPlayers.Enemies;
        }

        public void RemoveGrid(long gridEntityId)
        {
            Grids.Remove(gridEntityId);
        }

        public void AddGrid(long gridEntityId)
        {
            Grids.Add(gridEntityId);
        }

        public static MyPlayer GetPlayerFromCharacter(MyCharacter character)
        {
            if (character == null)
            {
                Debug.Fail("Invalid argument");
                return null;
            }

            if (character.ControllerInfo != null && character.ControllerInfo.Controller != null)
            {
                return character.ControllerInfo.Controller.Player;
            }

            return null;
        }

        public static MyPlayer GetPlayerFromWeapon(IMyGunBaseUser gunUser)
        {
            if (gunUser == null)
            {
                Debug.Fail("Invalid argument");
                return null;
            }

            MyCharacter gunHolder = gunUser.Owner as MyCharacter;
            if (gunHolder != null)
            {
                return GetPlayerFromCharacter(gunHolder);
            }

            return null;
        }
    }
}
