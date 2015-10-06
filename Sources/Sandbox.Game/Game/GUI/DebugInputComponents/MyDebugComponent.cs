#region Using

using Sandbox.Common;
using System;
using System.Collections.Generic;
using VRage.Input;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public abstract class MyDebugComponent : IMyNamedComponent
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
            public Func<bool> Action;

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

        #endregion

        #region Fields

        public enum MyDebugComponentInfoState
        {
            NoInfo,
            EnabledInfo,
            FullInfo
        }

        static float m_shortcutsOffset = 0;

        public static float VerticalTextOffset
        {
            get { return m_shortcutsOffset; }
        }

        static HashSet<UInt16> m_enabledShortcutKeys = new HashSet<ushort>();

        SortedSet<MyShortcut> m_shortCuts = new SortedSet<MyShortcut>(MyShortcutComparer.Static);
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

                if (stateActive && shortcut.Action != null)
                {
                    return shortcut.Action();
                }
            }

            return false;
        }

        public abstract string GetName();

        public static void ResetFrame()
        {
            m_shortcutsOffset = 0;
            m_enabledShortcutKeys.Clear();
        }

        public virtual void Draw() 
        {
            if (m_shortCuts.Count == 0)
                return;

            if (MySandboxGame.Config.DebugComponentsInfo == MyDebugComponentInfoState.FullInfo)
            {
                float scale = 0.6f;
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, m_shortcutsOffset), GetName() + " debug input:", Color.Gold, scale);
                m_shortcutsOffset += 15;

                foreach (var shortcut in m_shortCuts)
                {
                    string shortcutName = shortcut.GetKeysString();
                    string description = shortcut.Description();

                    Color shortcutColor = m_enabledShortcutKeys.Contains(shortcut.GetId()) ? Color.Red : Color.White;
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(100, m_shortcutsOffset), shortcutName + ":", shortcutColor, scale, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(105, m_shortcutsOffset), description, Color.White, scale);

                    m_enabledShortcutKeys.Add(shortcut.GetId());

                    m_shortcutsOffset += 15;
                }

                m_shortcutsOffset += 5;
            }
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
                Action = action,                
            });
        }
    }
}
