using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public enum MyGuiControlBlockPropertyLayoutEnum
    {
        Horizontal,
        Vertical
    }

    public class MyGuiControlBlockProperty : MyGuiControlBase
    {
        MyGuiControlBlockPropertyLayoutEnum m_layout;
        MyGuiControlLabel m_title;
        MyGuiControlLabel m_extraInfo;
        MyGuiControlBase m_propertyControl;
        public MyGuiControlBase PropertyControl
        {
            get
            {
                return m_propertyControl;
            }
        }

        float titleHeight;

        public MyGuiControlLabel ExtraInfoLabel { get { return m_extraInfo; } }

        public MyGuiControlBlockProperty(String title, String tooltip, MyGuiControlBase propertyControl,
            MyGuiControlBlockPropertyLayoutEnum layout = MyGuiControlBlockPropertyLayoutEnum.Vertical, bool showExtraInfo = true)
            : base(toolTip: tooltip, canHaveFocus: true, isActiveControl: false, allowFocusingElements: true)
        {
            const float LABEL_TEXT_SCALE = MyGuiConstants.DEFAULT_TEXT_SCALE * 0.95f;
            m_title = new MyGuiControlLabel(text: title, textScale: LABEL_TEXT_SCALE);
            if (title.Length > 0)
            {
                Elements.Add(m_title);
            }
            
            m_extraInfo = new MyGuiControlLabel(textScale: LABEL_TEXT_SCALE);
            if (showExtraInfo)
            {
                Elements.Add(m_extraInfo);
            }

            m_propertyControl = propertyControl;
            Elements.Add(m_propertyControl);

            titleHeight = title.Length > 0 || showExtraInfo ? m_title.Size.Y : 0;

            m_layout = layout;
            switch (layout)
            {
                case MyGuiControlBlockPropertyLayoutEnum.Horizontal:                    
                    MinSize = new Vector2(m_propertyControl.Size.X + m_title.Size.X * 1.1f, Math.Max(m_propertyControl.Size.Y, 2.1f * titleHeight));
                    Size = MinSize;

                    m_title.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                    m_propertyControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
                    m_extraInfo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                    break;
                case MyGuiControlBlockPropertyLayoutEnum.Vertical:
                    MinSize = new Vector2(Math.Max(m_propertyControl.Size.X, m_title.Size.X), m_propertyControl.Size.Y + titleHeight * 1.1f);
                    Size = MinSize;

                    m_title.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                    m_propertyControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                    m_extraInfo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
                    break;
            }

            RefreshPositionsAndSizes();

            m_extraInfo.Text = "";
            m_extraInfo.Visible = false;
        }

        public override void OnRemoving()
        {
            // HACK: we don't allow control reuse
            ClearEvents();
            //base.OnRemoving();
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
        }

        public override MyGuiControlBase HandleInput()
        {
            var captureControl = base.HandleInput();

            if (captureControl == null)
                captureControl = HandleInputElements();

            if (captureControl == null && HasFocus)
                captureControl = m_propertyControl.HandleInput();

            return captureControl;
        }

        protected override void OnSizeChanged()
        {
            RefreshPositionsAndSizes();
            base.OnSizeChanged();
        }

        private void RefreshPositionsAndSizes()
        {
            switch (m_layout)
            {
                case MyGuiControlBlockPropertyLayoutEnum.Horizontal:
                    m_title.Position = Size * -0.5f; // left top
                    m_extraInfo.Position = m_title.Position + new Vector2(0f, titleHeight * 1.05f);
                    m_propertyControl.Position = new Vector2(Size.X * 0.505f, Size.Y * -0.5f);
                    break;

                case MyGuiControlBlockPropertyLayoutEnum.Vertical:
                    m_title.Position = Size * -0.5f;
                    m_extraInfo.Position = Size * new Vector2(0.5f, -0.5f);
                    m_propertyControl.Position = m_title.Position + new Vector2(0f, titleHeight * 1.05f);
                    break;
            }
        }

        public void SetExtraInfo(StringBuilder extraInfoText)
        {
            // TODO: This is totally wrong, rewrite label
            m_extraInfo.Text = extraInfoText.ToString();
            m_extraInfo.Visible = !string.IsNullOrWhiteSpace(m_extraInfo.Text);
        }

    }
}
