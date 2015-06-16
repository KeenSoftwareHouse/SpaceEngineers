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
            if (!Sync.IsServer)
                return;
            m_updateCount++;
            if (m_updateCount % 10 == 0)
            {
                foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (player.Controller != null && player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity != null)
                    {
                        var entity = player.Controller.ControlledEntity.Entity;
                        Update(player.Id, entity);
                    }
                }

                if (m_someoneWon)
                    foreach (var triggers in MissionTriggers)
                        triggers.Value.RaiseSignal(triggers.Key, Signal.OTHER_WON);
            }
        }

        public bool Update(MyPlayer.PlayerId Id, MyEntity entity)
        {
            //MySessionComponentMission.Static.TryCreateFromDefault(Id);

            if (IsLocal(Id))
                UpdateLocal(Id);

            if (!Sync.IsServer)
                return false;

            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                //Debug.Assert(false,"Bad ID for update in missionTriggers");
                mtrig=TryCreateFromDefault(Id, false);
            }
            mtrig.UpdateWin(Id, entity);
            if (!mtrig.Won)
                mtrig.UpdateLose(Id, entity);
            else
                m_someoneWon = true;
            return false;
        }

        public static void PlayerDied(MyPlayer.PlayerId Id)
        {
            if (!Sync.IsServer)
                return;
            MyMissionTriggers mtrig;
            if (!Static.MissionTriggers.TryGetValue(Id, out mtrig))
                mtrig = Static.TryCreateFromDefault(Id, false);
            mtrig.RaiseSignal(Id, Signal.PLAYER_DIED);
        }

        public static bool CanRespawn(MyPlayer.PlayerId Id)
        {
            //beware, can be unreliable on client - you can call it before newest info from server arrives
            MyMissionTriggers mtrig;
            if (!Static.MissionTriggers.TryGetValue(Id, out mtrig))
            {
                Debug.Fail("Bad ID for CanRespawn");
                return true;
            }
            return !mtrig.Lost;
        }

        #region displaying win/lose message on local computer
        private void UpdateLocal(MyPlayer.PlayerId Id)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                //Debug.Fail("Bad ID for UpdateLocal");
                mtrig = TryCreateFromDefault(Id, false);
                return;
            }
            mtrig.DisplayMsg();
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
            if (!MySandboxGame.IsDedicated && MySession.LocalHumanPlayer!=null && Id == MySession.LocalHumanPlayer.Id)
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
