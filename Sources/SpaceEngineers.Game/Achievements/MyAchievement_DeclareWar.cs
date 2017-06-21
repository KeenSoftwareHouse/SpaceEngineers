using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Declare War
     * 
     * Declare war with different faction.
     */
    class MyAchievement_DeclareWar : MySteamAchievementBase
    {

        // Name identifier of the achievement within the steam systems 
        public override string AchievementTag { get { return "MyAchievment_DeclareWar"; } }
        public override bool NeedsUpdate { get { return false; } }

        public override void Init()
        {
            base.Init();

            // No need to register if already achieved
            if (IsAchieved) return;

            MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
        }

        private void Factions_FactionStateChanged(MyFactionCollection.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            if(MySession.Static.LocalHumanPlayer == null) return;
            // get player id
            long localPlayerID = MySession.Static.LocalHumanPlayer.Identity.IdentityId;

            // get player faction
            IMyFaction myFaction = MySession.Static.Factions.TryGetPlayerFaction(localPlayerID);

            if (myFaction == null)
                return;

            // Player declaring war
            if ((myFaction.IsFounder(localPlayerID) || myFaction.IsLeader(localPlayerID)) && // is player leader/fouder of faction
                myFaction.FactionId == fromFactionId && // is sending war not recieving
                action == MyFactionCollection.MyFactionStateChange.DeclareWar) // is it war?
            {
                NotifyAchieved();
                MySession.Static.Factions.FactionStateChanged -= Factions_FactionStateChanged;
            }
        }
    }
}
