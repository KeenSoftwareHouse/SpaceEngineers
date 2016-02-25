using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Triggers;
using Sandbox.Game.SessionComponents;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Entity;

namespace Sandbox.Game.World.Triggers
{
    [TriggerType(typeof(MyObjectBuilder_TriggerNoSpawn))]
    class MyTriggerNoSpawn : MyTrigger, ICloneable
    {
        private bool m_isDead = false;

        private bool m_couldRespawn=true;
        private DateTime m_begin;
        private int m_limitInSeconds = 60;
        private TimeSpan m_limit = new TimeSpan(0, 1, 0);
        //private static readonly TimeSpan DELAY_BEFORE_SPAWN = new TimeSpan(0, 0, 5);//countdown between death and respawn window
        public int LimitInSeconds
        {
            get
            {
                return m_limitInSeconds;
            }
            set
            {
                m_limitInSeconds=value;
                m_limit = new TimeSpan(0, 0, value);
            }
        }
        public MyTriggerNoSpawn(){ }

        public MyTriggerNoSpawn(MyTriggerNoSpawn trg)
            : base(trg) 
        {
            LimitInSeconds = trg.LimitInSeconds;
        }

        public override object Clone()
        {
            return new MyTriggerNoSpawn(this);
        }

        public override bool Update(MyPlayer player, MyEntity me)
        {
            if (player.Identity.IsDead)
            {
                Debug.Assert(player != null);
                if (m_begin == DateTime.MinValue)
                    m_begin = DateTime.UtcNow;// + DELAY_BEFORE_SPAWN;

                if (DateTime.UtcNow - m_begin > m_limit)
                {
                    m_IsTrue = true;
                    //m_begin = DateTime.MinValue;
                }
            }
            else
            {
                m_begin = DateTime.MinValue;
            }
            return IsTrue;
        }
        public static int CountAvailable(MyPlayer playerId)
        {
            return Sync.Players.RespawnComponent.CountAvailableSpawns(playerId);
        }

        private int m_lastSeconds;
        private StringBuilder m_guiText = new StringBuilder();
        public override void DisplayHints(MyPlayer player, MyEntity me)
        {
            if (!MySession.Static.IsScenario)
                return;
            if (m_IsTrue)
            {
                //Sync.Players.RespawnComponent.CloseRespawnScreen();//MyGuiScreenMedicals.Close();
                m_begin = DateTime.MinValue;
            }
            else
            {
                if (Sync.Players.RespawnComponent.IsInRespawnScreen())//if (MyGuiScreenMedicals.Static!=null && MyGuiScreenMedicals.Static.State == MyGuiScreenState.OPENED)//~character dead
                {
                    if (m_begin == DateTime.MinValue)
                        m_begin = DateTime.UtcNow;
                }
                else
                {
                    m_begin = DateTime.MinValue;
                }
                if (m_begin == DateTime.MinValue)
                    return;
                TimeSpan difference = m_limit - (DateTime.UtcNow - m_begin);
                var seconds = difference.Seconds;
                if (m_lastSeconds != seconds)
                {
                    m_lastSeconds = seconds;
                    m_guiText.Clear().AppendFormat(MyTexts.GetString(MySpaceTexts.ScreenMedicals_NoRespawnPlace), (int)difference.TotalMinutes, seconds);
                    Sync.Players.RespawnComponent.SetNoRespawnText(m_guiText, (int)difference.TotalSeconds);//MyGuiScreenMedicals.NoRespawnText = m_guiText;
                }
            }
        }

        private StringBuilder m_progress = new StringBuilder();
        public override StringBuilder GetProgress()
        {
            m_progress.Clear().AppendFormat(MySpaceTexts.ScenarioProgressNoSpawn, LimitInSeconds);
            return m_progress;
        }
        //OB:
        public override void Init(MyObjectBuilder_Trigger ob)
        {
            base.Init(ob);
            LimitInSeconds = ((MyObjectBuilder_TriggerNoSpawn)ob).Limit;
        }
        public override MyObjectBuilder_Trigger GetObjectBuilder()
        {
            MyObjectBuilder_TriggerNoSpawn ob = (MyObjectBuilder_TriggerNoSpawn)base.GetObjectBuilder();
            ob.Limit = LimitInSeconds;
            return ob;
        }

        //GUI
        public override void DisplayGUI()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenTriggerNoSpawn(this));
        }
        public new static MyStringId GetCaption()
        {
            return MySpaceTexts.GuiTriggerCaptionNoSpawn;
        }
    }

}
