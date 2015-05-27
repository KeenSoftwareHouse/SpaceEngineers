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
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]//.BeforeSimulation)]
    public class MySessionComponentMission : MySessionComponentBase
    {
        //public override void UpdateBeforeSimulation()
        public static MySessionComponentMission Static {get; private set;}
        public Dictionary<MyPlayer.PlayerId, MyMissionTriggers> MissionTriggers { get; private set; }

        //first triggered on this local computer:
        private string m_message=null;
        private bool m_won;
        private bool m_wasShown;

        /*public bool Update(MyFaction faction, MyCharacter me)
        {
            bool res = false;
            if (faction != null)
                res = Update(faction.FactionId, me);
            else
                res = Update(me.EntityId, me);

            return res;
        }*/
        /*public bool Update()
        {
            return false;
        }*/

        public bool Update(MyPlayer.PlayerId Id, MyCharacter me)
        {
            if (m_message != null && !m_wasShown)
            {
                MyAPIGateway.Utilities.ShowNotification(m_message, 60000, (m_won ? Sandbox.Common.MyFontEnum.Green : Sandbox.Common.MyFontEnum.Red));
                m_wasShown = true;
            }
            if (!Sync.IsServer)
                return false;

            MySessionComponentMission.Static.TryCreateFromDefault(Id, false);

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
            {
                foreach(var triggers in MissionTriggers)
                    if (triggers.Key!=Id)//MyMissionTriggers.DefaultPlayerId)
                        triggers.Value.RaiseSignal(Id, Signal.OTHER_WON);
            }
            return false;
        }

        #region network
        public void SetWon(MyPlayer.PlayerId Id, int index)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                Debug.Fail("Bad ID for SetWon");
                return;
            }
            if (IsLocal(Id) && m_message == null)
            {
                m_message = mtrig.SetWon(index);
                m_won = true;
            }
        }
        public void SetLost(MyPlayer.PlayerId Id, int index)
        {
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                Debug.Fail("Bad ID for SetLost");
                return;
            }
            if (IsLocal(Id) && m_message == null)
            {
                m_message = mtrig.SetLost(index);
                m_won = false;
            }
        }
        #endregion
        private bool IsLocal(MyPlayer.PlayerId Id)
        {
            if (!MySandboxGame.IsDedicated && Id == MySession.LocalHumanPlayer.Id)
                return true;
            return false;
        }

        public void TryCreateFromDefault(MyPlayer.PlayerId newId, bool overwrite)
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
            m_message = null;
            m_wasShown = false;
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
                    MissionTriggers.Add(new MyPlayer.PlayerId(trigger.Key.stm,trigger.Key.ser), new MyMissionTriggers(trigger.Value));
            //TODO save/load m_message, m_won, m_wasShown
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
