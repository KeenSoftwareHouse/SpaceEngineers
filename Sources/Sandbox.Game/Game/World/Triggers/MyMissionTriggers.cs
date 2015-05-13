using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;

namespace Sandbox.Game.World.Triggers
{
    public enum Signal
    {
        NONE=0,
        SOMEONE_WON,
        SOMEONE_LOST,
        PLAYER_DIED,
        BLOCK_DESTROYED
    };

    public class MyMissionTriggers
    {
        List<MyTrigger> m_winTriggers=new List<MyTrigger>();
        List<MyTrigger> m_loseTriggers = new List<MyTrigger>();
        public bool Won { get; protected set; }
        public bool Lost { get; protected set; }


        public List<MyTrigger> WinTriggers { get { return m_winTriggers; } /*set { m_winTriggers = value; }*/ }
        public List<MyTrigger> LoseTriggers { get { return m_loseTriggers; } /*set { m_winTriggers = value; }*/ }

        public bool UpdateWin(MyCharacter me)
        {
            foreach (var trigger in m_winTriggers)
                if (trigger.IsTrue || trigger.Update(me))
                { //Won!
                    if (IsLocal(me))
                        trigger.DisplayMessage(true);
                    Won = true;
                    return true;
                }
            return false;
        }
        public bool UpdateLose(MyCharacter me)
        {
            foreach (var trigger in m_loseTriggers) 
                if (trigger.IsTrue || trigger.Update(me))
                {//Lost
                    if (IsLocal(me))
                        trigger.DisplayMessage(false);
                    Lost = true;
                    return true;
                }
            return false;
        }
        private bool IsLocal(MyCharacter me)
        {
            if (!MySandboxGame.IsDedicated && me.ControllerInfo.ControllingIdentityId == MySession.LocalPlayerId)
                return true;
            return false;
        }
        public bool RaiseSignal(Signal signal, long? id)
        {
            switch (signal)
            {
                case Signal.SOMEONE_WON:
                    foreach (var trigger in m_winTriggers)
                        if (trigger.IsTrue || trigger.RaiseSignal(signal, id))
                        { //Won!
                            trigger.DisplayMessage(true);
                            Won = true;
                            return true;
                        }

                    foreach (var trigger in m_loseTriggers)
                        if (trigger.IsTrue || trigger.RaiseSignal(signal, id))
                        {//Lost
                            trigger.DisplayMessage(false);
                            Lost = true;
                            return true;
                        }
                    return false;
                    break;

                default:
                    Debug.Assert(false,"Wrong signal received");
                    return false;
                    break;
            }
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
                m_winTriggers.Add(new MyTrigger(trigger));
            m_loseTriggers.Clear();
            foreach (var trigger in source.m_loseTriggers)
                m_loseTriggers.Add(new MyTrigger(trigger));
        }

        public void Init(MyObjectBuilder_MissionTriggers builder)
        {
            foreach (var triggerBuilder in builder.WinTriggers)
                m_winTriggers.Add(TriggerFactory.CreateInstance(triggerBuilder));
            foreach (var triggerBuilder in builder.LoseTriggers)
                m_loseTriggers.Add(TriggerFactory.CreateInstance(triggerBuilder));
        }

        public virtual MyObjectBuilder_MissionTriggers GetObjectBuilder()
        {
            MyObjectBuilder_MissionTriggers ob= new MyObjectBuilder_MissionTriggers();
            foreach (var trigger in m_winTriggers)
                ob.WinTriggers.Add(trigger.GetObjectBuilder());
            foreach (var trigger in m_loseTriggers)
                ob.LoseTriggers.Add(trigger.GetObjectBuilder());
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
