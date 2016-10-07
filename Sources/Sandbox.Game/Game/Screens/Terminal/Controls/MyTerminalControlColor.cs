using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using VRage;
using VRage.Library.Utils;
using Sandbox.Game.Localization;
using VRage.Library.Collections;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlColor<TBlock> : MyTerminalValueControl<TBlock, Color>, IMyTerminalControlColor
        where TBlock : MyTerminalBlock
    {
        public MyStringId Title;
        public MyStringId Tooltip; // Apparently not actually used

        private MyGuiControlColor m_color;
        private Action<MyGuiControlColor> m_changeColor;

        public MyTerminalControlColor(string id, MyStringId title)
            : base(id)
        {
            Title = title;
            Serializer = delegate(BitStream stream, ref Color value) { stream.Serialize(ref value.PackedValue); };
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_color = new MyGuiControlColor(MyTexts.Get(Title).ToString(), 0.95f, Vector2.Zero, Color.White, Color.White, MyCommonTexts.DialogAmount_SetValueCaption, placeSlidersVertically: true);
            m_changeColor = OnChangeColor;
            m_color.OnChange += m_changeColor;
            m_color.Size = new Vector2(PREFERRED_CONTROL_WIDTH, m_color.Size.Y);
            return new MyGuiControlBlockProperty(String.Empty, String.Empty, m_color);
        }

        void OnChangeColor(MyGuiControlColor obj)
        {
            foreach (var item in TargetBlocks)
                SetValue(item, obj.GetColor());
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                m_color.OnChange -= m_changeColor;
                m_color.SetColor(GetValue(first));
                m_color.OnChange += m_changeColor;
            }
        }

        public override void SetValue(TBlock block, Color value)
        {
            base.SetValue(block, new Color(Vector4.Clamp(value.ToVector4(), Vector4.Zero, Vector4.One)));
        }

        public override Color GetDefaultValue(TBlock block)
        {
            return new Color(Vector4.One);
        }

        public override Color GetMinimum(TBlock block)
        {
            return new Color(Vector4.Zero);
        }

        public override Color GetMaximum(TBlock block)
        {
            return new Color(Vector4.One);
        }

        MyStringId IMyTerminalControlTitleTooltip.Title
        {
            get
            {
                return Title;
            }

            set
            {
                Title = value;
            }
        }

        MyStringId IMyTerminalControlTitleTooltip.Tooltip
        {
            get
            {
                return Tooltip;
            }

            set
            {
                Tooltip = value;
            }
        }
    }
}
