using Sandbox.Common;
using Sandbox.Game.World.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System.Diagnostics;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentMissionTriggers : MySessionComponentBase
    {
        public static MySessionComponentMissionTriggers Static {get; private set;}
        public Dictionary<MyPlayer.PlayerId, MyMissionTriggers> MissionTriggers { get; private set; }

        protected bool m_someoneWon;
        private int m_updateCount = 0;
        public override void UpdateBeforeSimulation()
        {
            if (!(MySession.Static.IsScenario || MySession.Static.Settings.ScenarioEditMode)
                || MyScenarioSystem.Static == null
                || MyScenarioSystem.Static.GameState < Sandbox.Game.GameSystems.MyScenarioSystem.MyState.Running)
                return;

            m_updateCount++;
            if (m_updateCount % 10 == 0)
            {
                UpdateLocal();
                if (!Sync.IsServer)
                    return;

                int lostCount=0;
                int totalCount=0;
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    MyEntity entity=null;
                    if (player.Controller != null && player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity != null)
                        entity = player.Controller.ControlledEntity.Entity;
                    if (Update(player, entity))
                        lostCount++;
                    totalCount++;
                }

                //all others lost?
                if (lostCount + 1 == totalCount && lostCount>0)
                {
                    foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                        RaiseSignal(player, Signal.ALL_OTHERS_LOST);
                }

                //someone else won?
                if (m_someoneWon)
                    foreach (var triggers in MissionTriggers)
                        triggers.Value.RaiseSignal(triggers.Key, Signal.OTHER_WON);

                
            }
        }

        public bool Update(MyPlayer player, MyEntity entity) //returns if lost
        {
            //MySessionComponentMission.Static.TryCreateFromDefault(Id);

            //if (IsLocal(player.Id))
            //    UpdateLocal(player.Id);

            if (!Sync.IsServer)
                return false;

            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(player.Id, out mtrig))
            {
                //Debug.Assert(false,"Bad ID for update in missionTriggers");
                mtrig = TryCreateFromDefault(player.Id, false);
            }
            mtrig.UpdateWin(player, entity);
            if (!mtrig.Won)
                mtrig.UpdateLose(player, entity);
            else
            {
                m_someoneWon = true;
                MyAnalyticsHelper.ReportTutorialEnd();
                //MyTutorialHelper.MissionSuccess();
            }
            return mtrig.Lost;
        }

        public static void PlayerDied(MyPlayer player)
        {
            //if (!Sync.IsServer) server still checks death count trigger "for real", but we need to know lives left on client to display it on screen
            //    return;
            RaiseSignal(player,Signal.PLAYER_DIED);
        }

        public static void RaiseSignal(MyPlayer player, Signal signal)
        {
            MyMissionTriggers mtrig;
            if (!Static.MissionTriggers.TryGetValue(player.Id, out mtrig))
                mtrig = Static.TryCreateFromDefault(player.Id, false);
            mtrig.RaiseSignal(player.Id, signal);
            if (Static.IsLocal(player.Id))
                Static.UpdateLocal(player);
        }

        public static bool CanRespawn(MyPlayer.PlayerId Id)
        {
            if (MySession.Static.Settings.ScenarioEditMode)
                return true;
            //beware, can be unreliable on client - you can call it before newest info from server arrives
            MyMissionTriggers mtrig;
            if (!Static.MissionTriggers.TryGetValue(Id, out mtrig))
            {
                return true;
            }
            return !mtrig.Lost;
        }

        #region displaying win/lose message on local computer
        private void UpdateLocal()
        {
            if (!MySandboxGame.IsDedicated && MySession.Static.LocalHumanPlayer != null)
                UpdateLocal(MySession.Static.LocalHumanPlayer);
        }
        private void UpdateLocal(MyPlayer player)
        {
            MyEntity me = null;
            if (player.Controller != null &&
                player.Controller.ControlledEntity != null &&
                player.Controller.ControlledEntity.Entity != null)
                me = player.Controller.ControlledEntity.Entity;
            UpdateLocal(player, me);
        }
        private void UpdateLocal(MyPlayer player, MyEntity me)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(player.Id, out mtrig))
            {
                //Debug.Fail("Bad ID for UpdateLocal");
                mtrig = TryCreateFromDefault(player.Id, false);
                return;
            }
            mtrig.DisplayMsg();
            mtrig.DisplayHints(player, me);
        }
        #endregion
        #region progress
        public static StringBuilder GetProgress(MyPlayer player)
        {
            MyMissionTriggers mtrig;
            if (!Static.MissionTriggers.TryGetValue(player.Id, out mtrig))
            {
                //Debug.Assert(false,"Bad ID for update in missionTriggers");
                mtrig = Static.TryCreateFromDefault(player.Id, false);
            }
            return mtrig.GetProgress();
        }
        #endregion
        #region network
        public void SetWon(MyPlayer.PlayerId Id, int index)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                Debug.Fail("Bad ID for SetWon");
                return;
            }
            mtrig.SetWon(index);
        }
        public void SetLost(MyPlayer.PlayerId Id, int index)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                Debug.Fail("Bad ID for SetLost");
                return;
            }
            mtrig.SetLost(index);
        }
        #endregion
        private bool IsLocal(MyPlayer.PlayerId Id)
        {
            if (!MySandboxGame.IsDedicated && MySession.Static.LocalHumanPlayer!=null && Id == MySession.Static.LocalHumanPlayer.Id)
                return true;
            return false;
        }

        public MyMissionTriggers TryCreateFromDefault(MyPlayer.PlayerId newId, bool overwrite = false)
        {
            MyMissionTriggers source;
            if (overwrite)
                MissionTriggers.Remove(newId);
            else
                if (MissionTriggers.TryGetValue(newId, out source))//(MissionTriggers.ContainsKey(newId))
                    return source;//already exists, thats ok for us
            MyMissionTriggers mtrig = new MyMissionTriggers();
            MissionTriggers.Add(newId, mtrig);

            MissionTriggers.TryGetValue(MyMissionTriggers.DefaultPlayerId, out source);
            if (source == null)
            {
                //older save which does not have defaults set
                source = new MyMissionTriggers();
                MySessionComponentMissionTriggers.Static.MissionTriggers.Add(MyMissionTriggers.DefaultPlayerId, source);
            }
            mtrig.CopyTriggersFrom(source);
            m_someoneWon = false;
            return mtrig;
        }

        public MySessionComponentMissionTriggers()
        {
            MissionTriggers = new Dictionary<MyPlayer.PlayerId, MyMissionTriggers>();
            Static = this;
        }
        public void Load(MyObjectBuilder_SessionComponentMission obj)
        {
            MissionTriggers.Clear();
            if (obj!=null && obj.Triggers != null)
                foreach (var trigger in obj.Triggers.Dictionary)
                {
                    var id=new MyPlayer.PlayerId(trigger.Key.stm, trigger.Key.ser);
                    var triggers = new MyMissionTriggers(trigger.Value);
                    MissionTriggers.Add(id, triggers);
                }
        }

        public MyObjectBuilder_SessionComponentMission GetObjectBuilder()
        {
            var builder = new MyObjectBuilder_SessionComponentMission();

            if (MissionTriggers!=null)
                foreach (var trigger in MissionTriggers)
                    builder.Triggers.Dictionary.Add(new MyObjectBuilder_SessionComponentMission.pair(trigger.Key.SteamId, trigger.Key.SerialId), trigger.Value.GetObjectBuilder());

            return builder;
        }
    }
}
