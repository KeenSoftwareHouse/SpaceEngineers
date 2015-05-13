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

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]//.BeforeSimulation)]
    public class MySessionComponentMission : MySessionComponentBase
    {
        //public override void UpdateBeforeSimulation()
        public static MySessionComponentMission Static {get; private set;}
        public Dictionary<MyPlayer.PlayerId, MyMissionTriggers> MissionTriggers { get; private set; }

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
            MyMissionTriggers mtrig;
            if (!MissionTriggers.TryGetValue(Id, out mtrig))
            {
                //Debug.Assert(false,"Bad ID for update in missionTriggers");
                return false;
            }
            mtrig.UpdateWin(me);

            mtrig.UpdateLose(me);//!!
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
            MissionTriggers.TryGetValue(new MyPlayer.PlayerId(0,0), out source);
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
                    MissionTriggers.Add(new MyPlayer.PlayerId(trigger.Key.stm,trigger.Key.ser), new MyMissionTriggers(trigger.Value));
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
