using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public abstract class  MyMultiDebugInputComponent : MyDebugComponent
    {
        private int m_activeMode;

        private List<MyKeys> m_keys = new List<MyKeys>();

        public abstract MyDebugComponent[] Components
        {
            get;
        }

        public MyDebugComponent ActiveComponent
        {
            get { return Components != null && Components.Length > 0 ? Components[m_activeMode] : null; }
        }

        [Serializable]
        public struct MultidebugData
        {
            public int ActiveDebug;
            public object[] ChildDatas;
        }

        public override object InputData
        {
            get
            {
                var comps = Components;

                object[] datas = new object[comps.Length];

                for (int i = 0; i < comps.Length; ++i)
                {
                    datas[i] = comps[i].InputData;
                }

                MultidebugData? data = new MultidebugData()
                {
                    ActiveDebug = m_activeMode,
                    ChildDatas = datas
                };

                return data;
            }
            set
            {
                MultidebugData? data = value as MultidebugData?;
                if (data.HasValue)
                {
                    m_activeMode = data.Value.ActiveDebug;

                    var comps = Components;

                    if (comps.Length != data.Value.ChildDatas.Length) return;

                    for (int i = 0; i < comps.Length; ++i)
                    {
                        comps[i].InputData = data.Value.ChildDatas[i];
                    }
                }
                else
                {
                    m_activeMode = 0;
                }
            }
        }

        public override void Draw()
        {
            var comps = Components;
            if (comps == null || comps.Length == 0)
            {
                Text(Color.Red, 1.5f, "{0} Debug Input - NO COMPONENTS", GetName());
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (comps.Length > 0)
                {
                    sb.Append(FormatComponentName(0));
                }
                for (int i = 1; i < comps.Length; ++i)
                {
                    sb.Append(" ");
                    sb.Append(FormatComponentName(i));
                }

                Text(Color.Yellow, 1.5f, "{0} Debug Input: {1}", GetName(), sb.ToString());

                if (MySandboxGame.Config.DebugComponentsInfo == MyDebugComponentInfoState.FullInfo)
                {
                    Text(Color.White, 1.2f, "Select Tab: Left WinKey + Tab Number");
                }

                VSpace(5f);

                DrawInternal();

                comps[m_activeMode].Draw();
            }
        }

        public virtual void DrawInternal() {}

        public override void Update10()
        {
            base.Update10();
            if (ActiveComponent != null)
                ActiveComponent.Update10();
        }

        public override void Update100()
        {
            base.Update100();
            if (ActiveComponent != null)
                ActiveComponent.Update100();
        }

        private string FormatComponentName(int index)
        {
            var name = Components[index].GetName();
            return index == m_activeMode ? String.Format("{0}({1})", name.ToUpper(), index) : String.Format("{0}({1})", name, index);
        }

        public override bool HandleInput()
        {
            if (Components == null || Components.Length == 0)
            {
                return false;
            }

            if (MyInput.Static.IsKeyPress(MyKeys.LeftWindows) || MyInput.Static.IsKeyPress(MyKeys.RightWindows))
            {
                MyInput.Static.GetPressedKeys(m_keys);

                int newMode = m_activeMode;
                foreach (var key in m_keys)
                {
                    int kk = (int)key;
                    if (kk >= (int)MyKeys.NumPad0 && kk <= (int)MyKeys.NumPad9)
                    {
                        var val = kk - (int)MyKeys.NumPad0;

                        if (val < Components.Length)
                        {
                            newMode = val;
                        }
                    }
                }

                if (m_activeMode != newMode)
                {
                    m_activeMode = newMode;
                    Save();
                    return true;
                }
            }

            return Components[m_activeMode].HandleInput();
        }

    }
}
