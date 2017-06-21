using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace SpaceEngineers.Game.Achievements
{
    /*
     * Achievement: Win-Win
     * 
     * Make peace with a faction.
     */
    class MyAchievement_WinWin : MySteamAchievementBase
    {


        public override string AchievementTag { get { return "MyAchievement_WinWin"; } }
        public override bool NeedsUpdate { get { return false; } }

        public override void Init()
        {
            base.Init();

            if (IsAchieved) return;

            MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
        }

        private void Factions_FactionStateChanged(MyFactionCollection.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            if(MySession.Static.LocalHumanPlayer == null) return;
            // get player id
            long localPlayerID = MySession.Static.LocalHumanPlayer.Identity.IdentityId;

            // get player faction
            IMyFaction myFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalHumanPlayer.Identity.IdentityId);

            if (myFaction == null)
                return;

            if ((myFaction.IsLeader(localPlayerID) || myFaction.IsFounder(localPlayerID)) && // is leader or founder
                (myFaction.FactionId == fromFactionId || myFaction.FactionId == toFactionId) && // its our faction in this deal
                action == MyFactionCollection.MyFactionStateChange.AcceptPeace) // is it peace?
            {
                NotifyAchieved();
                MySession.Static.Factions.FactionStateChanged -= Factions_FactionStateChanged;
            }
        }
    }
}
