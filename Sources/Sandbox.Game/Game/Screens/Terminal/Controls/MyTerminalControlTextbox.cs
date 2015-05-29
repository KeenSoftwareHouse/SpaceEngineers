﻿
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
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlTextbox<TBlock> : MyTerminalControl<TBlock>
        where TBlock : MyTerminalBlock
    {
        public delegate StringBuilder GetterDelegate(TBlock block);
        public delegate void SetterDelegate(TBlock block, StringBuilder value);

        MyGuiControlTextbox m_textbox;

        /// <summary>
        /// Returns borrowed string builder instance which won't be stored or overwritten
        /// </summary>
        public GetterDelegate Getter;
        public SetterDelegate Setter;
        public Action<TBlock> EnterPressed;

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
        private Action<MyGuiControlTextbox> m_enterPressed;

        public MyTerminalControlTextbox(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_textbox = new MyGuiControlTextbox();
            m_textbox.Size = new Vector2(PREFERRED_CONTROL_WIDTH, m_textbox.Size.Y);
            m_enterPressed = OnEnterPressed;
            m_textChanged = OnTextChanged;
            m_textbox.TextChanged += m_textChanged;
            m_textbox.EnterPressed += m_enterPressed;

            var propertyControl = new MyGuiControlBlockProperty(MyTexts.GetString(Title), MyTexts.GetString(Tooltip), m_textbox);
            propertyControl.Size = new Vector2(PREFERRED_CONTROL_WIDTH, propertyControl.Size.Y);

            return propertyControl;
        }

        void OnEnterPressed(MyGuiControlTextbox obj)
        {
            foreach (var item in TargetBlocks)
            {
                EnterPressed(item);
            }
        }
        
        void OnTextChanged(MyGuiControlTextbox obj)
        {
            m_tmpText.Clear();
            obj.GetText(m_tmpText);

            foreach (var item in TargetBlocks)
            {
                Setter(item, m_tmpText);
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();
            var first = FirstBlock;
            if (first != null)
            {
                StringBuilder newText = Getter(first);
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
