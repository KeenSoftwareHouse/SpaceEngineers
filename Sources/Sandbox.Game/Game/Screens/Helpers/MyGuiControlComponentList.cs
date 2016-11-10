using Sandbox.Common;

using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;
using System.Globalization;
using Sandbox.Graphics;
using VRage;
using VRage.Game;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{
    class MyGuiControlComponentList : MyGuiControlBase
    {
        private float m_currentOffsetFromTop;
        private MyGuiBorderThickness m_padding;
        private MyGuiControlLabel m_valuesLabel;

        public StringBuilder ValuesText
        {
            get { return new StringBuilder(m_valuesLabel.Text); }
            set { m_valuesLabel.Text = value.ToString(); }
        }

        public MyGuiControlComponentList():
            base(isActiveControl: false)
        {
            m_padding = new MyGuiBorderThickness(0.02f, 0.008f);

            m_valuesLabel = new MyGuiControlLabel()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                Text = "Required / Available",
                TextScale = 0.75f * MyGuiConstants.DEFAULT_TEXT_SCALE,
            };
            Elements.Add(m_valuesLabel);
            UpdatePositions();
        }

        public ComponentControl this[int i]
        {
            get { return (ComponentControl)Elements[i+1]; }
        }

        public int Count
        {
            get { return Elements.Count - 1; }
        }

        public void Add(MyDefinitionId id, double val1, double val2, string font)
        {
            var control = new ComponentControl(id);
            control.Size            = new Vector2(Size.X - m_padding.HorizontalSum, control.Size.Y);
            m_currentOffsetFromTop += control.Size.Y;
            control.Position        = -0.5f * Size + new Vector2(m_padding.Left, m_currentOffsetFromTop);
            control.OriginAlign     = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            control.ValuesFont      = font;
            control.SetValues(val1, val2);
            Elements.Add(control);
        }

        public void Clear()
        {
            Elements.Clear();
            Elements.Add(m_valuesLabel);
            m_currentOffsetFromTop = m_valuesLabel.Size.Y + m_padding.Top;
        }

        protected override void OnSizeChanged()
        {
            UpdatePositions();
            base.OnSizeChanged();
        }

        private void UpdatePositions()
        {
            m_valuesLabel.Position = Size * new Vector2(0.5f, -0.5f) + m_padding.TopRightOffset;
            m_currentOffsetFromTop = m_valuesLabel.Size.Y + m_padding.Top;
            foreach (var control in Elements)
            {
                if (control == m_valuesLabel)
                    continue;

                var controlHeight       = control.Size.Y;
                m_currentOffsetFromTop += controlHeight;
                control.Position        = -0.5f * Size + new Vector2(m_padding.Left, m_currentOffsetFromTop);
                control.Size            = new Vector2(Size.X - m_padding.HorizontalSum, controlHeight);
            }
        }

        class ItemIconControl : MyGuiControlBase
        {
            private static readonly float SCALE = 0.7f;

            internal ItemIconControl(MyPhysicalItemDefinition def)
                : base( size: MyGuiConstants.TEXTURE_GRID_ITEM.SizeGui * SCALE,
                        backgroundTexture: new MyGuiCompositeTexture(MyGuiConstants.TEXTURE_GRID_ITEM.Normal),
                        isActiveControl: false)
            {
                MinSize = MaxSize = Size;
                var padding = new MyGuiBorderThickness(0.0025f, 0.001f);

                for (int i = 0; i < def.Icons.Length; i++)
                    Elements.Add(new MyGuiControlPanel(
                        size: Size - padding.SizeChange,
                        texture: def.Icons[0]));

                if (def.IconSymbol.HasValue)
                {
                    Elements.Add(new MyGuiControlLabel(
                        position: -0.5f * Size + padding.TopLeftOffset,
                        text: MyTexts.GetString(def.IconSymbol.Value),
                        textScale: SCALE,
                        originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));
                }
            }
        }

        internal class ComponentControl : MyGuiControlBase
        {
            public readonly MyDefinitionId Id;
            private ItemIconControl m_iconControl;
            private MyGuiControlLabel m_nameLabel;
            private MyGuiControlLabel m_valuesLabel;

            internal ComponentControl(MyDefinitionId id)
                : base( size: new Vector2(0.2f, MyGuiConstants.TEXTURE_GRID_ITEM.SizeGui.Y*0.75f),
                        isActiveControl: false)
            {
                var definition = (MyPhysicalItemDefinition)MyDefinitionManager.Static.GetDefinition(id);
                m_iconControl = new ItemIconControl(definition)
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                };
                m_nameLabel = new MyGuiControlLabel(
                    text: definition.DisplayNameText,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    textScale: 0.75f * MyGuiConstants.DEFAULT_TEXT_SCALE) {
                    AutoEllipsis = true,};
                m_valuesLabel = new MyGuiControlLabel(
                    text: new StringBuilder("{0} / {1}").ToString(),
                    font: MyFontEnum.White,
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    textScale: 0.75f * MyGuiConstants.DEFAULT_TEXT_SCALE);

                SetValues(99, 99);

                Elements.Add(m_iconControl);
                Elements.Add(m_nameLabel);
                Elements.Add(m_valuesLabel);

                MinSize = new Vector2(m_iconControl.MinSize.X + m_nameLabel.Size.X + m_valuesLabel.Size.X,
                                      m_iconControl.MinSize.Y);
            }

            protected override void OnSizeChanged()
            {
                m_iconControl.Position = Size * new Vector2(-0.5f, 0f);
                m_nameLabel.Position   = m_iconControl.Position + new Vector2(m_iconControl.Size.X, 0f);
                m_valuesLabel.Position = Size * new Vector2(0.5f, 0f);
                UpdateNameLabelSize();
                base.OnSizeChanged();
            }

            public void SetValues(double val1, double val2)
            {
                m_valuesLabel.UpdateFormatParams(val1.ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture),
                                                 val2.ToString(MyInventoryConstants.GUI_DISPLAY_FORMAT, CultureInfo.InvariantCulture));
                UpdateNameLabelSize();
            }

            public string ValuesFont
            {
                set { m_valuesLabel.Font = value; }
            }

            private void UpdateNameLabelSize()
            {
                m_nameLabel.Size = new Vector2(Size.X - (m_iconControl.Size.X + m_valuesLabel.Size.X), m_nameLabel.Size.Y);
            }
        }

    }
}
