
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
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlColor<TBlock> : MyTerminalValueControl<TBlock, Color>
        where TBlock : MyTerminalBlock
    {
        public Func<TBlock, Color> Getter;
        public Action<TBlock, Color> Setter;

        public readonly MyStringId Title;

        private MyGuiControlColor m_color;
        private Action<MyGuiControlColor> m_changeColor;

        public MyTerminalControlColor(string id, MyStringId title)
            : base(id)
        {
            Title = title;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_color = new MyGuiControlColor(MyTexts.Get(Title), 0.95f, Vector2.Zero, Color.White, Color.White, placeSlidersVertically: true);
            m_changeColor = OnChangeColor;
            m_color.OnChange += m_changeColor;
            m_color.Size = new Vector2(PREFERRED_CONTROL_WIDTH, m_color.Size.Y);
            return new MyGuiControlBlockProperty(String.Empty, String.Empty, m_color);
        }

        void OnChangeColor(MyGuiControlColor obj)
        {
            foreach (var item in TargetBlocks)
                Setter(item, obj.GetColor());
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                m_color.OnChange -= m_changeColor;
                m_color.SetColor(Getter(first));
                m_color.OnChange += m_changeColor;
            }
        }

        public override Color GetValue(TBlock block)
        {
            return Getter(block);
        }

        public override void SetValue(TBlock block, Color value)
        {
            Setter(block, new Color(Vector4.Clamp(value.ToVector4(), Vector4.Zero, Vector4.One)));
        }

        public override Color GetDefaultValue(TBlock block)
        {
            return new Color(Vector4.One);
        }

        public override Color GetMininum(TBlock block)
        {
            return new Color(Vector4.Zero);
        }

        public override Color GetMaximum(TBlock block)
        {
            return new Color(Vector4.One);
        }
    }
}
