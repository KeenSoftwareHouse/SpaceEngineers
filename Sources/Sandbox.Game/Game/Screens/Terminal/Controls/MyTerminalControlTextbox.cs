
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using VRageMath;
using Sandbox.Game.World;
using VRage.Utils;
using VRage;
using VRage.Library.Utils;
using VRage.Library.Collections;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlTextbox<TBlock> : MyTerminalControl<TBlock>, ITerminalControlSync
        where TBlock : MyTerminalBlock
    {
        public delegate StringBuilder GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, StringBuilder value);
        public delegate void SerializerDelegate(BitStream stream, StringBuilder value);

        char[] m_tmpArray = new char[64];
        MyGuiControlTextbox m_textbox;

        /// <summary>
        /// Returns borrowed string builder instance which won't be stored or overwritten
        /// </summary>
        public GetterDelegate Getter { private get; set; }
        public SetterDelegate Setter { private get; set; }
        public SerializerDelegate Serializer;

        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

        public Expression<Func<TBlock, StringBuilder>> MemberExpression
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }

        private StringBuilder m_tmpText = new StringBuilder(15);
        private Action<MyGuiControlTextbox> m_textChanged;

        public MyTerminalControlTextbox(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
            Serializer = (s, sb) => s.Serialize(sb, ref m_tmpArray, Encoding.UTF8);
        }

        public StringBuilder GetValue(TBlock block)
        {
            return Getter(block);
        }

        public void SetValue(TBlock block, StringBuilder value)
        {
            Setter(block, value);
            block.NotifyTerminalValueChanged(this);
        }

        public void Serialize(BitStream stream, MyTerminalBlock block)
        {
            //if (stream.Reading)
            //{
            //    m_tmpText.Clear();
            //    Serializer(stream, m_tmpText);
            //    SetValue((TBlock)block, m_tmpText);
            //}
            //else
            //{
            //    Serializer(stream, GetValue((TBlock)block));
            //}
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_textbox = new MyGuiControlTextbox();
            m_textbox.Size = new Vector2(PREFERRED_CONTROL_WIDTH, m_textbox.Size.Y);
            m_textChanged = OnTextChanged;
            m_textbox.TextChanged += m_textChanged;

            var propertyControl = new MyGuiControlBlockProperty(MyTexts.GetString(Title), MyTexts.GetString(Tooltip), m_textbox);
            propertyControl.Size = new Vector2(PREFERRED_CONTROL_WIDTH, propertyControl.Size.Y);

            return propertyControl;
        }

        void OnTextChanged(MyGuiControlTextbox obj)
        {
            m_tmpText.Clear();
            obj.GetText(m_tmpText);

            foreach (var item in TargetBlocks)
            {
                SetValue(item, m_tmpText);
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                StringBuilder newText = GetValue(first);
                if (!m_textbox.TextEquals(newText))
                {
                    m_textbox.TextChanged -= m_textChanged;
                    m_textbox.SetText(newText);
                    m_textbox.TextChanged += m_textChanged;
                }
            }
        }
    }
}
