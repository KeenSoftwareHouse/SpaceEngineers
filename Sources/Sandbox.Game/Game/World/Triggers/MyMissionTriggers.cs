using Sandbox.Game.Entities.Character;
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
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using VRage;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRage.Game.Common;
using VRage.Game.Entity;
using Sandbox.Game.SessionComponents;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI;
using VRage.Library;

namespace Sandbox.Game.World.Triggers
{
    public enum Signal
    {
        NONE=0,
        OTHER_WON,
        ALL_OTHERS_LOST,
        PLAYER_DIED,
    };

    [StaticEventOwner]
    public class MyMissionTriggers
    {
        public static readonly MyPlayer.PlayerId DefaultPlayerId = new MyPlayer.PlayerId(0, 0);

        List<MyTrigger> m_winTriggers=new List<MyTrigger>();
        List<MyTrigger> m_loseTriggers = new List<MyTrigger>();
        IMyHudNotification m_notification;
        public bool Won { get; protected set; }
        public void SetWon(int triggerIndex)
        {
            Won = true;
            m_winTriggers[triggerIndex].SetTrue();
            if (Message == null)
            {
                Message = m_winTriggers[triggerIndex].Message;
                IsMsgWinning = true;
            }
            DoEnd();
        }
        public bool Lost { get; protected set; }
        public void SetLost(int triggerIndex)
        {
            Lost = true;
            m_loseTriggers[triggerIndex].SetTrue();
            if (Message == null)
            {
                Message = m_loseTriggers[triggerIndex].Message;
                IsMsgWinning = false;
            }
            DoEnd();
        }
        protected void DoEnd()
        {
            if (!MySession.Static.Settings.ScenarioEditMode)
                Sync.Players.RespawnComponent.CloseRespawnScreen();
            MyScenarioSystem.Static.GameState = MyScenarioSystem.MyState.Ending;
        }

        public string Message {get; protected set;}
        public bool IsMsgWinning { get; protected set; }
        public bool DisplayMsg()
        {
            if (Message != null && m_notification==null)
            {
                m_notification=MyAPIGateway.Utilities.CreateNotification(Message, 0, (IsMsgWinning ? MyFontEnum.Green : MyFontEnum.Red));
                m_notification.Show();
                return true;
            }
            return false;
        }
        

        public List<MyTrigger> WinTriggers { get { return m_winTriggers; } /*set { m_winTriggers = value; }*/ }
        public List<MyTrigger> LoseTriggers { get { return m_loseTriggers; } /*set { m_winTriggers = value; }*/ }

        public bool UpdateWin(MyPlayer player, MyEntity me)
        {
            if (Won || Lost)
                return true;
            for (int i=0;i<m_winTriggers.Count;i++)
            {
                var trigger=m_winTriggers[i];
                if (trigger.IsTrue || trigger.Update(player, me))
                { //Won!
                    SetPlayerWon(player.Id, i);
                    return true;
                }
            }
            return false;
        }
        public bool UpdateLose(MyPlayer player, MyEntity me)
        {
            if (Won || Lost)
                return true;
            for (int i = 0; i < m_loseTriggers.Count; i++)
            {
                var trigger = m_loseTriggers[i];
                if (trigger.IsTrue || trigger.Update(player, me))
                { //Loser!
                    SetPlayerLost(player.Id, i);
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
                case Signal.PLAYER_DIED:
                case Signal.ALL_OTHERS_LOST:
                    for (int i = 0; i < m_winTriggers.Count; i++)
                    {
                        var trigger = m_winTriggers[i];
                        if (trigger.IsTrue || trigger.RaiseSignal(signal))
                        { //Won!
                            SetPlayerWon(Id, i);
                            return true;
                        }
                    }

                    for (int i = 0; i < m_loseTriggers.Count; i++)
                    {
                        var trigger = m_loseTriggers[i];
                        if (trigger.IsTrue || trigger.RaiseSignal(signal))
                        {//Lost
                            SetPlayerLost(Id, i);
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

        public void DisplayHints(MyPlayer player, MyEntity me)
        {
            for (int i = 0; i < m_winTriggers.Count; i++)
                m_winTriggers[i].DisplayHints(player, me);
            for (int i = 0; i < m_loseTriggers.Count; i++)
                m_loseTriggers[i].DisplayHints(player, me);
        }

        private StringBuilder m_progress=new StringBuilder();
        public StringBuilder GetProgress()
        {
            StringBuilder tempSB;
            m_progress.Clear().Append(MyTexts.Get(MySpaceTexts.ScenarioProgressWinConditions)).Append(MyEnvironment.NewLine);
            for (int i = 0; i < m_winTriggers.Count; i++)
            {
                tempSB = m_winTriggers[i].GetProgress();
                if (tempSB!=null)
                    m_progress.Append(tempSB).Append(MyEnvironment.NewLine);
            }

            m_progress.Append(MyEnvironment.NewLine).Append(MyTexts.Get(MySpaceTexts.ScenarioProgressLoseConditions)).Append(MyEnvironment.NewLine);
            for (int i = 0; i < m_loseTriggers.Count; i++)
            {
                tempSB = m_loseTriggers[i].GetProgress();
                if (tempSB!=null)
                    m_progress.Append(tempSB).Append(MyEnvironment.NewLine);
            }
            return m_progress;
        }


        public MyMissionTriggers(MyObjectBuilder_MissionTriggers builder)
        {
            Init(builder);
        }

        public MyMissionTriggers()
        {
        }

        public void CopyTriggersFrom(MyMissionTriggers source)
        {
            m_winTriggers.Clear();
            foreach (var trigger in source.m_winTriggers)
            {
                var newTrig = (MyTrigger)trigger.Clone();
                newTrig.IsTrue = false;
                m_winTriggers.Add(newTrig);
            }
            m_loseTriggers.Clear();
            foreach (var trigger in source.m_loseTriggers)
            {
                var newTrig = (MyTrigger)trigger.Clone();
                newTrig.IsTrue = false;
                m_loseTriggers.Add(newTrig);
            }
            Won = false;
            Lost = false;
            Message = null;
            HideNotification();
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
        public void HideNotification()
        {
            if (m_notification != null)
            {
                m_notification.Hide();
                m_notification = null;
            }
        }

        static void SetPlayerWon(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetWon(id, triggerIndex);
            if (!Sync.MultiplayerActive || !MySession.Static.IsScenario)
                return;

            MyMultiplayer.RaiseStaticEvent(s => MyMissionTriggers.OnPlayerWon, id, triggerIndex);
        }

        static void SetPlayerLost(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetLost(id, triggerIndex);
            if (!Sync.MultiplayerActive || !MySession.Static.IsScenario)
                return;

            MyMultiplayer.RaiseStaticEvent(s => MyMissionTriggers.OnPlayerLost, id, triggerIndex);
        }

        [Event, Reliable, Broadcast]
        static void OnPlayerWon(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetWon(id, triggerIndex);
        }

        [Event, Reliable, Broadcast]
        static void OnPlayerLost(MyPlayer.PlayerId id, int triggerIndex)
        {
            MySessionComponentMissionTriggers.Static.SetLost(id, triggerIndex);
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
