#region Using

using Sandbox.Common;
using System;
using System.Collections.Generic;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.Game.Gui
{
    public abstract class MyDebugComponent
    {
        #region Nested classes

        private class MyShortcutComparer : IComparer<MyShortcut>
        {
            public static MyShortcutComparer Static = new MyShortcutComparer();

            public int Compare(MyShortcut x, MyShortcut y)
            {
                return x.GetId().CompareTo(y.GetId());
            }
        }

        struct MyShortcut
        {
            public MyKeys Key;
            public bool NewPress;
            public bool Control;
            public bool Shift;
            public bool Alt;
            
            public Func<string> Description;
            public Func<bool> _Action;

            public string GetKeysString()
            {
                string str = "";
                if (Control)
                    str += "Ctrl";
                if (Shift)
                    str += string.IsNullOrEmpty(str) ? "Shift" : "+Shift";
                if (Alt)
                    str += string.IsNullOrEmpty(str) ? "Alt" : "+Alt";

                str += string.IsNullOrEmpty(str) ? MyInput.Static.GetKeyName(Key)
                                                 : ("+" + MyInput.Static.GetKeyName(Key));

                return str;
            }

            public UInt16 GetId()
            {
                UInt16 id = (UInt16)((int)Key << 8);
                id += (UInt16)(Control ? 4 : 0);
                id += (UInt16)(Shift ? 2 : 0);
                id += (UInt16)(Alt ? 1 : 0);
                return id;
            }
        }

        // class for referencing of a value
        public class MyRef<T>
        {
            Action<T> modify;
            private Func<T> getter;

            public MyRef(Func<T> getter, Action<T> modify)
            {
                this.modify = modify;
                this.getter = getter;
            }

            public T Value
            {
                get { return getter(); }
                set { modify(value); }
            }
        }

        class MySwitch
        {
            public MySwitch(MyKeys key, Func<MyKeys, bool> action, string note = "")
            {
                Key = key;
                Action = action;
                Note = note;
            }
            public MySwitch(MyKeys key, Func<MyKeys, bool> action, string note = "", bool defaultValue=false )
            {
                Key = key;
                Action = action;
                Note = note;
                IsSet = defaultValue;
            }
            public MySwitch(MyKeys key, Func<MyKeys, bool> action, MyRef<bool> field, string note = "")
            {
                m_boolReference = field;
                Key = key;
                Action = action;
                Note = note;
            }

            public MyKeys Key;
            public Func<MyKeys, bool> Action;
            public bool IsSet{
                get{
                    if (m_boolReference!=null)
                        return m_boolReference.Value;
                    return m_value;
                }
                set{
                    if (m_boolReference!=null)
                        m_boolReference.Value = value;
                    else
                        m_value = value;
                }
            }
            public string Note;
            public UInt16 GetId()
            {
                UInt16 id = (UInt16)((int)Key << 8);
                return id;
            }

            MyRef<bool> m_boolReference;  // reference to an external bool variable
            private bool m_value;           // or we switch internal variable
        }

        #endregion

        #region Fields

        public enum MyDebugComponentInfoState
        {
            NoInfo,
            EnabledInfo,
            FullInfo
        }

        #endregion

        #region Text Rendering

        static float m_textOffset = 0;

        private const int LINE_OFFSET = 15;
        private const int LINE_BREAK_OFFSET = 17;

        public static float VerticalTextOffset
        {
            get { return m_textOffset; }
        }

        protected static float NextVerticalOffset
        {
            get
            {
                var val = m_textOffset;
                m_textOffset += LINE_OFFSET;
                return val;
            }
        }

        public static float NextTextOffset(float scale)
        {
            var val = m_textOffset;
            m_textOffset += LINE_OFFSET * scale;
            return val;
        }

        protected void Text(string message, params object[] arguments)
        {
            Text(Color.White, 1f, message, arguments);
        }

        protected void Text(Color color, string message, params object[] arguments)
        {
            Text(color, 1f, message, arguments);
        }

        protected void Text(Color color, float scale, string message, params object[] arguments)
        {
            if (arguments.Length > 0)
                message = String.Format(message, arguments);
            MyRenderProxy.DebugDrawText2D(new Vector2(0, NextTextOffset(scale)), message, color, .6f * scale);
        }

        protected void MultilineText(string message, params object[] arguments)
        {
            MultilineText(Color.White, 1f, message, arguments);
        }

        protected void MultilineText(Color color, string message, params object[] arguments)
        {
            MultilineText(color, 1f, message, arguments);
        }

        protected void MultilineText(Color color, float scale, string message, params object[] arguments)
        {
            if (arguments.Length > 0)
                message = String.Format(message, arguments);

            int lines = 0; // we want lines - 1
            foreach (var c in message)
            {
                if (c == '\n') lines++;
            }

            message = message.Replace("\t", "    ");

            float offset = LINE_OFFSET + LINE_BREAK_OFFSET * lines;
            offset *= scale;

            MyRenderProxy.DebugDrawText2D(new Vector2(0, m_textOffset), message, color, .6f * scale);
            m_textOffset += offset;
        }

        public void Section(string text, params object[] formatArgs)
        {
            VSpace(5);

            Text(Color.Yellow, 1.5f, text, formatArgs);

            VSpace(5);
        }

        protected void VSpace(float space)
        {
            m_textOffset += space;
        }

        static HashSet<UInt16> m_enabledShortcutKeys = new HashSet<ushort>();

        SortedSet<MyShortcut> m_shortCuts = new SortedSet<MyShortcut>(MyShortcutComparer.Static);
        HashSet<MySwitch> m_switches = new HashSet<MySwitch>();//(MySwitchComparer.Static);
        bool m_enabled = true;

        public MyDebugComponent()
            : this(false)
        {
        }

        #endregion

        public MyDebugComponent(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled
        {
            get { return m_enabled; }
            set { m_enabled = value; }
        }

        public virtual object InputData
        {
            get { return null; }
            set { }
        }

        protected void Save()
        {
            var inputs = MySandboxGame.Config.DebugInputComponents;

            string name = GetName();

            var data = inputs[name];
            data.Enabled = Enabled;
            data.Data = InputData;
            inputs[name] = data;

            MySandboxGame.Config.Save();
        }

        public virtual bool HandleInput()
        {
            foreach (var shortcut in m_shortCuts)
            { 
                bool stateActive = true;
                stateActive &= shortcut.Control == MyInput.Static.IsAnyCtrlKeyPressed();
                stateActive &= shortcut.Shift == MyInput.Static.IsAnyShiftKeyPressed();
                stateActive &= shortcut.Alt == MyInput.Static.IsAnyAltKeyPressed();

                if (stateActive)
                {
                    if (shortcut.NewPress)
                        stateActive &= MyInput.Static.IsNewKeyPressed(shortcut.Key);
                    else
                        stateActive &= MyInput.Static.IsKeyPress(shortcut.Key);
                }

                if (stateActive && shortcut._Action != null)
                {
                    return shortcut._Action();
                }
            }

            foreach (var sw in m_switches)
            {
                bool stateActive = true;
                stateActive &= MyInput.Static.IsNewKeyPressed(sw.Key);

                if (stateActive && sw.Action != null)
                {
                    return sw.Action(sw.Key);
                }
            }

            return false;
        }

        public abstract string GetName();

        public static void ResetFrame()
        {
            m_textOffset = 0;
            m_enabledShortcutKeys.Clear();
        }

        public int m_frameCounter = 0;

        public virtual void DispatchUpdate()
        {
            if (m_frameCounter % 10 == 0)
                Update10();
            if (m_frameCounter >= 100)
            {
                Update100();
                m_frameCounter = 0;
            }
            m_frameCounter++;
        }

        public virtual void Draw() 
        {
            if (MySandboxGame.Config.DebugComponentsInfo == MyDebugComponentInfoState.FullInfo)
            {
                float scale = 0.6f;
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, m_textOffset), GetName() + " debug input:", Color.Gold, scale);
                m_textOffset += 15;

                foreach (var shortcut in m_shortCuts)
                {
                    string shortcutName = shortcut.GetKeysString();
                    string description = shortcut.Description();

                    Color shortcutColor = m_enabledShortcutKeys.Contains(shortcut.GetId()) ? Color.Red : Color.White;
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(100, m_textOffset), shortcutName + ":", shortcutColor, scale, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(105, m_textOffset), description, Color.White, scale);

                    m_enabledShortcutKeys.Add(shortcut.GetId());

                    m_textOffset += 15;
                }

                foreach (var sw in m_switches)
                {
                    Color color = GetSwitchValue(sw.Key)? Color.Red : Color.White;
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(30, m_textOffset), "switch " + sw.Key + (sw.Note.Length==0?"":(" "+sw.Note))+" is " + (GetSwitchValue(sw.Key)? "On":"Off"), color, scale);
                    m_textOffset += 15;
                }

                m_textOffset += 5;
            }
        }

        public virtual void Update10()
        {
        }

        public virtual void Update100()
        {
        }


        protected void AddShortcut(MyKeys key, bool newPress, bool control, bool shift, bool alt, Func<string> description, Func<bool> action)
        {
            m_shortCuts.Add(new MyShortcut()
            {
                Key = key,
                NewPress = newPress,
                Control = control,
                Shift = shift,
                Alt = alt,
                Description = description,
                _Action = action,                
            });
        }

        protected void AddSwitch(MyKeys key, Func<MyKeys, bool> action, string note="", bool defaultValue=false )
        {
            MySwitch newSwitch = new MySwitch(key, action, note, defaultValue);
            m_switches.Add(newSwitch);            
        }

        protected void AddSwitch(MyKeys key, Func<MyKeys, bool> action, MyRef<bool> boolRef, string note = "")
        {
            MySwitch newSwitch = new MySwitch(key, action, boolRef, note);
            m_switches.Add(newSwitch);            
        }

        protected void SetSwitch(MyKeys key, bool value)
        {
            foreach (var sw in m_switches)
            {
                if (sw.Key == key)
                {
                    sw.IsSet = value;
                    return;
                }
            }
        }

        public bool GetSwitchValue(MyKeys key)
        {
            foreach (var sw in m_switches)
            {
                if (sw.Key == key)
                    return sw.IsSet;
            }
            return false;
        }

        public bool GetSwitchValue(string note)
        {
            foreach (var sw in m_switches)
            {
                if (sw.Note == note)
                    return sw.IsSet;
            }
            return false;
        }
    
    }
}
