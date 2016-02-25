using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.SessionComponents
{
    public abstract class MyRespawnComponentBase : MySessionComponentBase
    {
        public abstract void InitFromCheckpoint(MyObjectBuilder_Checkpoint checkpoint);
        public abstract void SaveToCheckpoint(MyObjectBuilder_Checkpoint checkpoint);

        public abstract bool HandleRespawnRequest(bool joinGame, bool newIdentity, long medicalRoom, string respawnShipId, MyPlayer.PlayerId playerId, Vector3D? spawnPosition, VRage.ObjectBuilders.SerializableDefinitionId? botDefinitionId);
        public abstract MyIdentity CreateNewIdentity(string identityName, MyPlayer.PlayerId playerId, string modelName);
        public abstract void AfterRemovePlayer(MyPlayer player);
        public abstract void SetupCharacterDefault(MyPlayer player, MyWorldGenerator.Args args);

        public abstract int CountAvailableSpawns(MyPlayer player);
        public abstract bool IsInRespawnScreen();
        public abstract void CloseRespawnScreen();
        public abstract void SetNoRespawnText(StringBuilder text, int timeSec);
        public abstract void SetupCharacterFromStarts(MyPlayer player, MyWorldGeneratorStartingStateBase[] playerStarts, MyWorldGenerator.Args args);

        /// <summary>
        /// Indicates if permadeath ownership warning should be shown.
        /// </summary>
        protected static bool ShowPermaWarning { get; set; }

        public void ResetPlayerIdentity(MyPlayer player)
        {
            if (player.Identity != null)
            {
                if (MySession.Static.Settings.PermanentDeath.Value)
                {
                    if (!player.Identity.IsDead)
                        Sync.Players.KillPlayer(player);

                    var faction = MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId);
                    if (faction != null)
                        MyFactionCollection.KickMember(faction.FactionId, player.Identity.IdentityId);

                    //Clear chat history
                    if (MySession.Static.ChatSystem != null)
                    {
                        MySession.Static.ChatSystem.ClearChatHistoryForPlayer(player.Identity);
                    }

                    var identity = Sync.Players.CreateNewIdentity(player.DisplayName);
                    player.Identity = identity;
                }
            }
        }
    }
}
