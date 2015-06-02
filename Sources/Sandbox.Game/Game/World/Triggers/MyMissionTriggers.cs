﻿using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Game.World.Triggers
{
    public enum Signal
    {
        NONE=0,
        OTHER_WON,
        //ALL_OTHERS_LOST,
        //PLAYER_DIED,
        //BLOCK_DESTROYED
    };

    public class MyMissionTriggers
    {
        public static readonly MyPlayer.PlayerId DefaultPlayerId = new MyPlayer.PlayerId(0, 0);

        List<MyTrigger> m_winTriggers=new List<MyTrigger>();
        List<MyTrigger> m_loseTriggers = new List<MyTrigger>();
        public bool Won { get; protected set; }
        public void SetWon(int triggerIndex)
        {
            Won = true;
            m_winTriggers[triggerIndex].IsTrue = true;
            if (Message == null)
            {
                Message = m_winTriggers[triggerIndex].Message;
                IsMsgWinning = true;
            }
        }
        public bool Lost { get; protected set; }
        public void SetLost(int triggerIndex)
        {
            Lost = true;
            m_loseTriggers[triggerIndex].IsTrue = true;
            if (Message == null)
            {
                Message = m_loseTriggers[triggerIndex].Message;
                IsMsgWinning = false;
            }
        }
        public string Message {get; protected set;}
        public bool IsMsgWinning { get; protected set; }
        public bool DisplayMsg()
        {
            if (Message!=null)
            {
                MyAPIGateway.Utilities.ShowNotification(Message, 0, (IsMsgWinning ? Sandbox.Common.MyFontEnum.Green : Sandbox.Common.MyFontEnum.Red));
                return true;
            }
            return false;
        }
        

        public List<MyTrigger> WinTriggers { get { return m_winTriggers; } /*set { m_winTriggers = value; }*/ }
        public List<MyTrigger> LoseTriggers { get { return m_loseTriggers; } /*set { m_winTriggers = value; }*/ }

        public bool UpdateWin(MyCharacter me)
        {
            if (Won || Lost)
                return true;
            for (int i=0;i<m_winTriggers.Count;i++)
            {
                var trigger=m_winTriggers[i];
                if (trigger.IsTrue || trigger.Update(me))
                { //Won!
                    MySyncMissionTriggers.PlayerWon(me.ControllerInfo.Controller.Player.Id, i);
                    return true;
                }
            }
            return false;
        }
        public bool UpdateLose(MyCharacter me)
        {
            if (Won || Lost)
                return true;
            for (int i = 0; i < m_loseTriggers.Count; i++)
            {
                var trigger = m_loseTriggers[i];
                if (trigger.IsTrue || trigger.Update(me))
                { //Loser!
                    MySyncMissionTriggers.PlayerLost(me.ControllerInfo.Controller.Player.Id, i);
                    return true;
                }
            }
            return false;
        }

        public bool RaiseSignal(MyPlayer.PlayerId Id, Signal signal)
        {
            if (Won || Lost)
                return true;
            switch (signal)
            {
                case Signal.OTHER_WON:
                    for (int i = 0; i < m_winTriggers.Count; i++)
                    {
                        var trigger = m_winTriggers[i];
                        if (trigger.IsTrue || trigger.RaiseSignal(signal))
                        { //Won!
                            MySyncMissionTriggers.PlayerWon(Id, i);
                            return true;
                        }
                    }

                    for (int i = 0; i < m_loseTriggers.Count; i++)
                    {
                        var trigger = m_loseTriggers[i];
                        if (trigger.IsTrue || trigger.RaiseSignal(signal))
                        {//Lost
                            MySyncMissionTriggers.PlayerLost(Id, i);
                            return true;
                        }
                    }
                    break;

                default:
                    Debug.Fail("Wrong signal received");
                    break;
            }
            return false;
        }

        public MyMissionTriggers(MyObjectBuilder_MissionTriggers builder)
        {
            Init(builder);
        }

        public MyMissionTriggers()//!!remove this
        {
            // TODO: Complete member initialization
            //m_loseTriggers.Add(new MyTriggerPosition());//!!!
        }

        public void CopyTriggersFrom(MyMissionTriggers source)
        {
            m_winTriggers.Clear();
            foreach (var trigger in source.m_winTriggers)
                m_winTriggers.Add((MyTrigger)trigger.Clone());
            m_loseTriggers.Clear();
            foreach (var trigger in source.m_loseTriggers)
                m_loseTriggers.Add((MyTrigger)trigger.Clone());
            Won = false;
            Lost = false;
        }

        public void Init(MyObjectBuilder_MissionTriggers builder)
        {
            foreach (var triggerBuilder in builder.WinTriggers)
                m_winTriggers.Add(TriggerFactory.CreateInstance(triggerBuilder));
            foreach (var triggerBuilder in builder.LoseTriggers)
                m_loseTriggers.Add(TriggerFactory.CreateInstance(triggerBuilder));
            Message = builder.message;
            Won = builder.Won;
            Lost = builder.Lost;
            Debug.Assert(!(Won && Lost), "Triggers: won&&lost should not happen");
            if (Won)
                IsMsgWinning = true;
        }

        public virtual MyObjectBuilder_MissionTriggers GetObjectBuilder()
        {
            MyObjectBuilder_MissionTriggers ob= new MyObjectBuilder_MissionTriggers();
            foreach (var trigger in m_winTriggers)
                ob.WinTriggers.Add(trigger.GetObjectBuilder());
            foreach (var trigger in m_loseTriggers)
                ob.LoseTriggers.Add(trigger.GetObjectBuilder());
            ob.message = Message;
            ob.Won = Won;
            ob.Lost = Lost;
            return ob;
        }
    }

    public class TriggerTypeAttribute : MyFactoryTagAttribute
    {
        public TriggerTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }

    }

    public static class TriggerFactory
    {
        private static MyObjectFactory<TriggerTypeAttribute, MyTrigger> m_objectFactory;

        static TriggerFactory()
        {
            m_objectFactory = new MyObjectFactory<TriggerTypeAttribute, MyTrigger>();
            m_objectFactory.RegisterFromCreatedObjectAssembly();
            //m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            //m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static MyTrigger CreateInstance(MyObjectBuilder_Trigger builder)
        {
            var instance = m_objectFactory.CreateInstance(builder.TypeId);
            instance.Init(builder);
            return instance;
        }

        public static MyObjectBuilder_Trigger CreateObjectBuilder(MyTrigger instance)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_Trigger>(instance);
        }
    }


    
}
