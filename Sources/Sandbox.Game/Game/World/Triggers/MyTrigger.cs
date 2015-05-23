﻿using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Game.World.Triggers
{
    /*abstract*/ public class MyTrigger : ICloneable
    {
        protected bool m_IsTrue = false;
        public bool IsTrue{get{return m_IsTrue;} set{m_IsTrue=value;}}

        public string Message;

        public MyTrigger() { }
        public MyTrigger(MyTrigger trg)
        {
            m_IsTrue = trg.m_IsTrue;
            if (trg.Message!=null)
                Message = string.Copy(trg.Message);
        }
        public virtual object Clone()
        {
            return new MyTrigger(this);
        }

        public virtual bool Update(MyCharacter me)
        {
            return IsTrue;
        }
        public virtual bool RaiseSignal(Signal signal)
        {
            return IsTrue;
        }

        public void DisplayMessage(bool won)//"won" because trigger does not know if he is in winning or losing set
        {
            MyAPIGateway.Utilities.ShowNotification(Message, 3000, (won ? Sandbox.Common.MyFontEnum.Green : Sandbox.Common.MyFontEnum.Red));
        }
        

        //save/load
        public virtual void Init(MyObjectBuilder_Trigger ob)
        {
            m_IsTrue=ob.IsTrue;
            Message = ob.Message;
        }
        public virtual MyObjectBuilder_Trigger GetObjectBuilder()
        {
            var ob = TriggerFactory.CreateObjectBuilder(this);
            ob.IsTrue = m_IsTrue;
            ob.Message = Message;
            return ob;
        }
        //GUI
        public virtual void DisplayGUI()
        {}
        public static MyStringId GetCaption()
        {
            return MySpaceTexts.MessageBoxCaptionError;
        }
    }
}
