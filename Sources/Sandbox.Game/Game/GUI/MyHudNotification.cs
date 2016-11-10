using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;

using Sandbox.Common;
using Sandbox.ModAPI;
using VRage;
using Sandbox.Definitions;
using Sandbox.Graphics;
using VRage.Utils;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    public enum MyNotificationLevel
    {
        Normal,
        Control,
        Important,
        Debug
    }

    public abstract class MyHudNotificationBase
    {
        public static readonly int INFINITE = 0;

        #region Private fields
        private int m_formatArgsCount;
        private object[] m_textFormatArguments = new object[20];
        private MyGuiDrawAlignEnum m_actualTextAlign;
        private int m_aliveTime;
        private string m_notificationText;
        private bool m_isTextDirty;
        #endregion

        public int m_lifespanMs;

        public MyNotificationLevel Level = MyNotificationLevel.Normal;

        public bool IsControlsHint { get { return Level == MyNotificationLevel.Control; } }

        public readonly int Priority;

        public string Font;

        public bool Alive
        {
            get;
            private set;
        }

        public MyHudNotificationBase(
            int disapearTimeMs,
            string font = MyFontEnum.White,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            int priority                 = 0,
            MyNotificationLevel level    = MyNotificationLevel.Normal)
        {
            Font = font;
            Priority = priority;
            m_isTextDirty = true;

            m_actualTextAlign = textAlign;
            AssignFormatArgs(null);
            Level = level;

            // timing:
            m_lifespanMs = disapearTimeMs;
            m_aliveTime = 0;
            RefreshAlive();
        }

        public void SetTextDirty()
        {
            m_isTextDirty = true;
        }

        public string GetText()
        {
            if (string.IsNullOrEmpty(m_notificationText) || m_isTextDirty)
            {
                if (m_formatArgsCount > 0)
                {
                    m_notificationText = String.Format(GetOriginalText(), m_textFormatArguments);
                }
                else
                {
                    m_notificationText = GetOriginalText();
                }

                m_isTextDirty = false;
            }
            return m_notificationText;
        }

        public object[] GetTextFormatArguments()
        {
            return m_textFormatArguments;
        }

        public void SetTextFormatArguments(params object[] arguments)
        {
            AssignFormatArgs(arguments);
            m_notificationText = null;
            GetText();
        }

        public void AddAliveTime(int timeStep)
        {
            m_aliveTime += timeStep;
            RefreshAlive();
        }

        public void ResetAliveTime()
        {
            m_aliveTime = 0;
            RefreshAlive();
        }

        protected abstract string GetOriginalText();

        private void RefreshAlive()
        {
            Alive = (m_lifespanMs == INFINITE) ? true
                                                : (m_aliveTime < m_lifespanMs);
        }

        private void AssignFormatArgs(object[] args)
        {
            int i = 0;
            m_formatArgsCount = 0;

            if (args != null)
            {
                if (m_textFormatArguments.Length < args.Length)
                    m_textFormatArguments = new object[args.Length];

                for (; i < args.Length; i++)
                    m_textFormatArguments[i] = args[i];

                m_formatArgsCount = args.Length;
            }

            for (; i < m_textFormatArguments.Length; i++)
                m_textFormatArguments[i] = "<missing>";
        }

        public virtual void BeforeAdd()
        { }

        public virtual void BeforeRemove()
        { }
    }

    public partial class MyHudNotification : MyHudNotificationBase
    {
        private MyStringId m_originalText;
       

        public MyStringId Text 
        {
            get 
            { 
                return m_originalText; 
            } 
            set 
            {
                if (m_originalText != value)
                {
                    m_originalText = value;
                    SetTextDirty();
                }
            } 
        }

        public MyHudNotification(
            MyStringId text              = default(MyStringId),
            int disappearTimeMs          = 2500,
            string font = MyFontEnum.White,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            int priority                 = 0,
            MyNotificationLevel level    = MyNotificationLevel.Normal) :
            base(disappearTimeMs, font, textAlign, priority, level)
        {
            m_originalText = text;
        }


        protected override string GetOriginalText()
        {
            return MyTexts.Get(m_originalText).ToString();
        }

    }

    public class MyHudNotificationDebug : MyHudNotificationBase
    {
        private string m_originalText;

        public MyHudNotificationDebug(string text,
            int disapearTimeMs           = 2500,
            string font = MyFontEnum.White,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            int priority                 = 0,
            MyNotificationLevel level    =  MyNotificationLevel.Debug):
            base(disapearTimeMs, font, textAlign, priority, level)
        {
            m_originalText = text;
        }

        protected override string GetOriginalText()
        {
            return m_originalText;
        }
    }

    public class MyHudMissingComponentNotification : MyHudNotificationBase
    {
        private MyStringId m_originalText;

        public MyHudMissingComponentNotification(MyStringId text,
            int disapearTimeMs           = 2500,
            string font = MyFontEnum.White,
            MyGuiDrawAlignEnum textAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
            int priority                 = 0,
            MyNotificationLevel level    = MyNotificationLevel.Normal) :
            base(disapearTimeMs, font, textAlign, priority, level)
        {
            m_originalText = text;
        }

        protected override string GetOriginalText()
        {
            return MyTexts.GetString(m_originalText);
        }

        public void SetBlockDefinition(MyCubeBlockDefinition definition)
        {
            SetTextFormatArguments(
                definition.Components[0].Definition.DisplayNameText.ToString(),
                definition.DisplayNameText.ToString());
        }

        public override void BeforeAdd()
        {
            MyHud.BlockInfo.MissingComponentIndex = 0;
        }

        public override void BeforeRemove()
        {
            MyHud.BlockInfo.MissingComponentIndex = -1;
        }
    }
}
