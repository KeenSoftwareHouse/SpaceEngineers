using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.World.Triggers
{
    /*abstract*/ public class MyTrigger : ICloneable
    {
        protected bool m_IsTrue = false;
        public virtual bool IsTrue{get{return m_IsTrue;} set{m_IsTrue=value;}}
        public void SetTrue()
        {
            IsTrue = true;
            if (WwwLink != null && WwwLink.Length > 0)
                MyGuiSandbox.OpenUrlWithFallback(WwwLink, "Scenario info", true);
            if (NextMission != null && NextMission.Length > 0 && MySession.Static.IsScenario)
                MyScenarioSystem.LoadNextScenario(NextMission);
        }

        public string Message;
        public string WwwLink;
        public string NextMission;

        public MyTrigger() { }
        public MyTrigger(MyTrigger trg)
        {
            m_IsTrue = trg.m_IsTrue;
            if (trg.Message!=null)
                Message = string.Copy(trg.Message);
            if (trg.WwwLink != null)
                WwwLink = string.Copy(trg.WwwLink);
            if (trg.NextMission!= null)
                NextMission = string.Copy(trg.NextMission);

        }
        public virtual object Clone()
        {
            return new MyTrigger(this);
        }

        public virtual bool Update(MyPlayer player, MyEntity me)
        {
            return IsTrue;
        }
        public virtual bool RaiseSignal(Signal signal)
        {
            return IsTrue;
        }
        //hints are to be displayed during gameplay. This is called on both server&clients.
        public virtual void DisplayHints(MyPlayer player, MyEntity me) { }//call this only for triggers belonging to local computer!

        //progress is going to be displayed on custom screen:
        public virtual StringBuilder GetProgress()
        {
            return null;
        }

        //save/load
        public virtual void Init(MyObjectBuilder_Trigger ob)
        {
            m_IsTrue=ob.IsTrue;
            Message = ob.Message;
            WwwLink = ob.WwwLink;
            NextMission = ob.NextMission;
        }
        public virtual MyObjectBuilder_Trigger GetObjectBuilder()
        {
            var ob = TriggerFactory.CreateObjectBuilder(this);
            ob.IsTrue = m_IsTrue;
            ob.Message = Message;
            ob.WwwLink = WwwLink;
            ob.NextMission = NextMission;
            return ob;
        }
        //GUI
        public virtual void DisplayGUI()
        {}
        public static MyStringId GetCaption()
        {
            return MyCommonTexts.MessageBoxCaptionError;
        }
    }
}
