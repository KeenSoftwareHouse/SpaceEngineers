using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageMath;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;
using CameraControllerSettings = Sandbox.Common.ObjectBuilders.MyObjectBuilder_Player.CameraControllerSettings;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities;
using VRage.Utils;

namespace Sandbox.Game.Multiplayer
{
    public class MyEntityCameraSettings
    {
        public PlayerId PID;
        public bool IsFirstPerson
        {
            get 
            { 
                return m_isFirstPerson || MySession.Static.Settings.Enable3rdPersonView == false;
            }

            set
            {
                m_isFirstPerson = value;
            }
        }
        public double Distance;
        public Vector2? HeadAngle;

        bool m_isFirstPerson;
    }

    public struct MyCameraControllerSettings
    {
        public double Distance;
        public MyCameraControllerEnum Controller;
    }

    [PreloadRequired]
    class MyCameraCollection
    {
        [ProtoContract]
        [MessageId(456, P2PMessageEnum.Reliable)]
        struct PlayerSaveEntityCameraSettingsMsg
        {
            [ProtoMember]
            public int PlayerSerialId;
            [ProtoMember]
            public long EntityId;
            [ProtoMember]
            public double Distance;
            [ProtoMember]
            public bool IsFirstPerson;
            [ProtoMember]
            public float HeadX;
            [ProtoMember]
            public float HeadY;
        }

        static MyCameraCollection()
        {    
            MySyncLayer.RegisterMessage<PlayerSaveEntityCameraSettingsMsg>(OnSaveEntityCameraSettings, MyMessagePermissions.ToServer);
        }

        public void RequestSaveEntityCameraSettings(PlayerId pid, long entityId, bool isFirstPerson, double distance, float headAngleX, float headAngleY)
        {
            if (MyEntities.CloseAllowed)
                return;

            var msg = new PlayerSaveEntityCameraSettingsMsg();
            msg.PlayerSerialId = pid.SerialId;
            msg.EntityId = entityId;
            msg.Distance = distance;
            msg.IsFirstPerson = isFirstPerson;
            msg.HeadX = headAngleX;
            msg.HeadY = headAngleY;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        static void OnSaveEntityCameraSettings(ref PlayerSaveEntityCameraSettingsMsg msg, MyNetworkClient sender)
        {
            PlayerId pid = new PlayerId(sender.SteamUserId, msg.PlayerSerialId);
            Vector2 headAngle = new Vector2(msg.HeadX, msg.HeadY);
            MyPlayer player = MySession.Static.Players.TryGetPlayerById(pid);
            if (player != null && player.Character != null && player.Character.EntityId == msg.EntityId)
                MySession.Static.Cameras.AddCharacterCameraData(pid, msg.IsFirstPerson, msg.Distance, headAngle);
            else
                MySession.Static.Cameras.AddCameraData(pid, msg.EntityId, msg.IsFirstPerson, msg.Distance, headAngle);
        }

        private Dictionary<PlayerId, Dictionary<long, MyEntityCameraSettings>> m_entityCameraSettings = new Dictionary<PlayerId, Dictionary<long, MyEntityCameraSettings>>();
        private MyEntityCameraSettings m_characterCameraSettings;
        private List<long> m_entitiesToRemove = new List<long>();

        public bool ContainsPlayer(PlayerId pid)
        {
            return m_entityCameraSettings.ContainsKey(pid);
        }

        private void AddCameraData(PlayerId pid, long entityId, bool isFirstPerson, double distance, Vector2 headAngle)
        {
            MyEntityCameraSettings cameraSettings = null;
            if (TryGetCameraSettings(pid, entityId, out cameraSettings))
            {
                cameraSettings.IsFirstPerson = isFirstPerson;
                if (!isFirstPerson)
                {
                    cameraSettings.Distance = distance;
                    cameraSettings.HeadAngle = headAngle;
                }
            }
            else
            {
                cameraSettings = new MyEntityCameraSettings()
                {
                    Distance = distance,
                    IsFirstPerson = isFirstPerson,
                    HeadAngle = headAngle,
                };
                AddCameraData(pid, entityId, cameraSettings);
            }
        }

        private void AddCharacterCameraData(PlayerId pid, bool isFirstPerson, double distance, Vector2 headAngle)
        {
            if (m_characterCameraSettings == null)
                m_characterCameraSettings = new MyEntityCameraSettings();
            m_characterCameraSettings.IsFirstPerson = isFirstPerson;
            if (!isFirstPerson)
            {
                m_characterCameraSettings.Distance = distance;
                m_characterCameraSettings.HeadAngle = headAngle;
            }
        }

        private void AddCameraData(PlayerId pid, long entityId, MyEntityCameraSettings data)
        {
            if (!ContainsPlayer(pid))
            {
                m_entityCameraSettings[pid] = new Dictionary<long, MyEntityCameraSettings>();
            }

            if (m_entityCameraSettings[pid].ContainsKey(entityId))
                m_entityCameraSettings[pid][entityId] = data;
            else
                m_entityCameraSettings[pid].Add(entityId, data);
        }

        public bool TryGetCameraSettings(PlayerId pid, long entityId, out MyEntityCameraSettings cameraSettings)
        {
            MyPlayer client = null;
            if (MySandboxGame.IsDedicated)
                client = MySession.Static.Players.TryGetPlayerById(pid);
            else
                client = MySession.LocalHumanPlayer;

            if (m_characterCameraSettings != null && client != null && client.Character != null && client.Character.EntityId == entityId)
            {
                cameraSettings = m_characterCameraSettings;
                return true;
            }
            if (ContainsPlayer(pid) && m_entityCameraSettings[pid].ContainsKey(entityId))
            {
                return m_entityCameraSettings[pid].TryGetValue(entityId, out cameraSettings);
            }

            cameraSettings = null;
            return false;
        }

        public void LoadCameraCollection(MyObjectBuilder_Checkpoint checkpoint)
        {
            m_entityCameraSettings = new Dictionary<PlayerId, Dictionary<long, MyEntityCameraSettings>>();
            
            var allPlayers = checkpoint.AllPlayersData;
            if (allPlayers != null)
            {
                foreach (var playerData in allPlayers.Dictionary)
                {
                    PlayerId pid = new PlayerId(playerData.Key.ClientId, playerData.Key.SerialId);
                    m_entityCameraSettings[pid] = new Dictionary<long, MyEntityCameraSettings>();
                    foreach (var cameraSettings in playerData.Value.EntityCameraData)
                    {
                        MyEntityCameraSettings data = new MyEntityCameraSettings()
                        {
                            Distance = cameraSettings.Distance,
                            HeadAngle = (Vector2?)cameraSettings.HeadAngle,
                            IsFirstPerson = cameraSettings.IsFirstPerson
                        };

                        m_entityCameraSettings[pid][cameraSettings.EntityId] = data;
                    }

                    if (playerData.Value.CharacterCameraData != null)
                    {
                        m_characterCameraSettings = new MyEntityCameraSettings()
                        {
                           Distance =  playerData.Value.CharacterCameraData.Distance,
                            HeadAngle = playerData.Value.CharacterCameraData.HeadAngle,
                            IsFirstPerson = playerData.Value.CharacterCameraData.IsFirstPerson
                        };
                    }
                }
            }
        }

        public void SaveCameraCollection(MyObjectBuilder_Checkpoint checkpoint)
        {
            MyDebug.AssertDebug(checkpoint.AllPlayersData != null, "Players data not initialized!");
            if (checkpoint.AllPlayersData == null)
                return;

            foreach (var playerData in checkpoint.AllPlayersData.Dictionary)
            {
                PlayerId pid = new PlayerId(playerData.Key.ClientId, playerData.Key.SerialId);
                playerData.Value.EntityCameraData = new List<CameraControllerSettings>();
                
                if (!m_entityCameraSettings.ContainsKey(pid))
                    continue;

                m_entitiesToRemove.Clear();
                foreach (var cameraSetting in m_entityCameraSettings[pid])
                {
                    if (MyEntities.EntityExists(cameraSetting.Key))
                    {
                        CameraControllerSettings settings = new CameraControllerSettings()
                        {
                            Distance = cameraSetting.Value.Distance,
                            IsFirstPerson = cameraSetting.Value.IsFirstPerson,
                            HeadAngle = cameraSetting.Value.HeadAngle,
                            EntityId = cameraSetting.Key,
                        };
                        playerData.Value.EntityCameraData.Add(settings);
                    }
                    else
                    {
                        m_entitiesToRemove.Add(cameraSetting.Key);
                    }
                }

                foreach (long entityId in m_entitiesToRemove)
                    m_entityCameraSettings[pid].Remove(entityId);

                if (m_characterCameraSettings != null)
                {
                    playerData.Value.CharacterCameraData = new CameraControllerSettings()
                    {
                        Distance = m_characterCameraSettings.Distance,
                        IsFirstPerson = m_characterCameraSettings.IsFirstPerson,
                        HeadAngle = m_characterCameraSettings.HeadAngle,
                    };
                }
            }
        }

        public void SaveEntityCameraSettings(PlayerId pid, long entityId, bool isFirstPerson, double distance, float headAngleX, float headAngleY, bool sync = true)
        {
            if (!Sync.IsServer && sync)
            {
                RequestSaveEntityCameraSettings(pid, entityId, isFirstPerson, distance, headAngleX, headAngleY);
            }

            Vector2 headAngle = new Vector2(headAngleX, headAngleY);
            if (MySession.ControlledEntity is MyCharacter || (MySession.LocalCharacter != null && MySession.LocalCharacter.EntityId == entityId))
                AddCharacterCameraData(pid, isFirstPerson, distance, headAngle);
            else
                AddCameraData(pid, entityId, isFirstPerson, distance, headAngle);
        }

        public void SaveEntityCameraSettingsLocally(PlayerId pid, long entityId, bool isFirstPerson, double distance, float headAngleX, float headAngleY)
        {
            SaveEntityCameraSettings(pid, entityId, isFirstPerson, distance, headAngleX, headAngleY, false);
        }
    }
}
