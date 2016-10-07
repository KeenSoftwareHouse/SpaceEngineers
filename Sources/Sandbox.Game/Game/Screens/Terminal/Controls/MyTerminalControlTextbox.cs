
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
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Screens.Terminal.Controls;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlTextbox<TBlock> : MyTerminalValueControl<TBlock, StringBuilder>, ITerminalControlSync, IMyTerminalControlTextbox
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

        public MyStringId Title;
        public MyStringId Tooltip;

#if !XB1
        public Expression<Func<TBlock, StringBuilder>> MemberExpression
        {
            set
            {
                Getter = new GetterDelegate(value.CreateGetter());
                Setter = new SetterDelegate(value.CreateSetter());
            }
        }
#endif
        private StringBuilder m_tmpText = new StringBuilder(15);
        private Action<MyGuiControlTextbox> m_textChanged;

        public MyTerminalControlTextbox(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
            Serializer = (s, sb) => s.Serialize(sb, ref m_tmpArray, Encoding.UTF8);
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

        public override StringBuilder GetValue(TBlock block)
        {
            return Getter(block);
        }

        public override void SetValue(TBlock block, StringBuilder value)
        {
            Setter(block, new StringBuilder(value.ToString()));
            block.NotifyTerminalValueChanged(this);
        }

        public override StringBuilder GetDefaultValue(TBlock block)
        {
            return new StringBuilder();
        }

        public override StringBuilder GetMinimum(TBlock block)
        {
            return new StringBuilder();
        }

        public override StringBuilder GetMaximum(TBlock block)
        {
            return new StringBuilder();
        }

        /// <summary>
        /// Implements IMyTerminalControlTextbox for ModAPI
        /// </summary>
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

        /// <summary>
        /// Implements IMyTerminalValueControl for Mods
        /// </summary>
        Func<IMyTerminalBlock, StringBuilder> IMyTerminalValueControl<StringBuilder>.Getter
        {
            get
            {
                GetterDelegate oldGetter = Getter;
                Func<IMyTerminalBlock, StringBuilder> func = (x) =>
                {
                    return oldGetter((TBlock)x);
                };

                return func;
            }

            set
            {
                Getter = new GetterDelegate(value);
            }
        }

        Action<IMyTerminalBlock, StringBuilder> IMyTerminalValueControl<StringBuilder>.Setter
        {
            get
            {
                SetterDelegate oldSetter = Setter;
                Action<IMyTerminalBlock, StringBuilder> action = (x, y) =>
                {
                    oldSetter((TBlock)x, y);
                };

                return action;
            }

            set
            {
                Setter = new SetterDelegate(value);
            }
        }
    }
}
