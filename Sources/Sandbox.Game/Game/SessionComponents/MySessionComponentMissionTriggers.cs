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

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentMission : MySessionComponentBase
    {
        public static MySessionComponentMission Static {get; private set;}
        public Dictionary<MyPlayer.PlayerId, MyMissionTriggers> MissionTriggers { get; private set; }

        protected bool m_someoneWon;
        private int m_updateCount = 0;
        public override void UpdateBeforeSimulation()
        {
            if (!Sync.IsServer)
                return;
            if (m_someoneWon)
                if (++m_updateCount % 100 == 0)
                    foreach (var triggers in MissionTriggers)
                        triggers.Value.RaiseSignal(triggers.Key, Signal.OTHER_WON);
        }

        public bool Update(MyPlayer.PlayerId Id, MyCharacter me)
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
                return false;
            }
            mtrig.UpdateWin(me);
            if (!mtrig.Won)
                mtrig.UpdateLose(me);
            else
                m_someoneWon = true;
            return false;
        }

        #region displaying win/lose message on local computer
        private bool m_LocalMsgShown;
        private void UpdateLocal(MyPlayer.PlayerId Id)
        {
            if (!m_LocalMsgShown)
            {
                MyMissionTriggers mtrig;
                if (!MissionTriggers.TryGetValue(Id, out mtrig))
                {
                    //Debug.Fail("Bad ID for UpdateLocal");
                    return;
                }
                m_LocalMsgShown = mtrig.DisplayMsg();
            }
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

        public void TryCreateFromDefault(MyPlayer.PlayerId newId, bool overwrite = false)
        {
            if (overwrite)
                MissionTriggers.Remove(newId);
            else
                if (MissionTriggers.ContainsKey(newId))
                    return;//already exists, thats ok for us
            MyMissionTriggers mtrig = new MyMissionTriggers();
            MissionTriggers.Add(newId, mtrig);

            MyMissionTriggers source;
            MissionTriggers.TryGetValue(MyMissionTriggers.DefaultPlayerId, out source);
            if (source == null)
                //older save which does not have defaults set
                return;
            mtrig.CopyTriggersFrom(source);
        }

        public MySessionComponentMission()
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
