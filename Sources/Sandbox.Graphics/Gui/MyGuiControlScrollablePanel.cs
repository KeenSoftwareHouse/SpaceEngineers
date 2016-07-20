using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiControlScrollablePanel : MyGuiControlBase, IMyGuiControlsParent
    {
        private MyGuiControls m_controls;
        private MyVScrollbar m_scrollbarV;
        private MyHScrollbar m_scrollbarH;
        private MyGuiControlBase m_scrolledControl;
        private RectangleF m_scrolledArea;
        private MyGuiBorderThickness m_scrolledAreaPadding;

        public event Action<MyGuiControlScrollablePanel> PanelScrolled;

        public Vector2 ScrollBarOffset = Vector2.Zero;
        public float ScrollBarHScale = 1;
        public float ScrollBarVScale = 1;

        public float ScrollbarHSizeX
        {
            get
            {
                if (m_scrollbarH == null) return 0f;
                return m_scrollbarH.Size.X;
            }
        }

        public float ScrollbarHSizeY
        {
            get
            {
                if (m_scrollbarH == null) return 0f;
                return m_scrollbarH.Size.Y;
            }
        }

        public float ScrollbarVSizeX
        {
            get
            {
                if (m_scrollbarV == null) return 0f;
                return m_scrollbarV.Size.X;
            }
        }

        public float ScrollbarVSizeY
        {
            get
            {
                if (m_scrollbarV == null) return 0f;
                return m_scrollbarV.Size.Y;
            }
        }

        public bool ScrollbarHWheel
        {
            get { return m_scrollbarH != null && m_scrollbarH.EnableWheelScroll; }
            set
            {
                if (m_scrollbarH != null)
                {
                    m_scrollbarH.EnableWheelScroll = value;
                }
            }
        }

        public bool ScrollbarHEnabled
        {
            get { return m_scrollbarH != null; }
            set
            {
                if (value && m_scrollbarH == null)
                {
                    m_scrollbarH = new MyHScrollbar(this);
                    m_scrollbarH.ValueChanged += scrollbar_ValueChanged;
                }
                else if (!value)
                    m_scrollbarH = null;
            }
        }

        public bool ScrollbarVEnabled
        {
            get { return m_scrollbarV != null; }
            set
            {
                if (value && m_scrollbarV == null)
                {
                    m_scrollbarV = new MyVScrollbar(this);
                    m_scrollbarV.ValueChanged += scrollbar_ValueChanged;
                }
                else if (!value)
                    m_scrollbarV = null;
            }
        }

        public MyGuiControlBase ScrolledControl
        {
            get { return m_scrolledControl; }
            set
            {
                if (m_scrolledControl != value)
                {
                    if (m_scrolledControl != null)
                    {
                        Elements.Remove(m_scrolledControl);
                        m_scrolledControl.SizeChanged -= scrolledControl_SizeChanged;
                    }

                    m_scrolledControl = value;

                    if (m_scrolledControl != null)
                    {
                        Elements.Add(m_scrolledControl);
                        m_scrolledControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                        m_scrolledControl.SizeChanged += scrolledControl_SizeChanged;
                    }

                    RefreshScrollbar();
                    RefreshScrolledControlPosition();
                }
            }
        }

        public Vector2 ScrolledAreaSize
        {
            get { return m_scrolledArea.Size; }
        }

        public MyGuiBorderThickness ScrolledAreaPadding
        {
            get { return m_scrolledAreaPadding; }
            set
            {
                m_scrolledAreaPadding = value;
                RefreshInternals();
            }
        }

        public float ScrollbarVPosition
        {
            get
            {
                if (m_scrollbarV != null)
                    return m_scrollbarV.Value;
                return 0.0f;
            }
            set
            {
                if (m_scrollbarV != null)
                    m_scrollbarV.Value = value;
            }
        }

        public MyGuiControlScrollablePanel(MyGuiControlBase scrolledControl)
        {
            Name = "ScrollablePanel";
            ScrolledControl = scrolledControl;
            m_controls = new MyGuiControls(this);
            m_controls.Add(ScrolledControl);
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase res = base.HandleInput();

            var scrolledArea = m_scrolledArea;
            scrolledArea.Position += GetPositionAbsoluteTopLeft();
            res = base.HandleInputElements();

            if (m_scrollbarV != null)
            {
                bool capturedByScrollbar = m_scrollbarV.HandleInput();
                if (capturedByScrollbar)
                    res = res ?? this;
            }
            
            if (m_scrollbarH != null)
            {
                bool capturedByScrollbar = m_scrollbarH.HandleInput();
                if (capturedByScrollbar)
                    res = res ?? this;
            }

            return res;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);

            var scrollbarMask = ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha);
            if (m_scrollbarV != null)
            {
                m_scrollbarV.ScrollBarScale = ScrollBarVScale;
                m_scrollbarV.Draw(scrollbarMask);
            }
            if (m_scrollbarH != null)
            {
                m_scrollbarH.ScrollBarScale = ScrollBarHScale;
                m_scrollbarH.Draw(scrollbarMask);
            }


            //DebugDraw();
        }

        protected override void DrawElements(float transitionAlpha, float backgroundTransitionAlpha)
        {
            var scissor = m_scrolledArea;
            scissor.Position += GetPositionAbsoluteTopLeft();
            using (MyGuiManager.UsingScissorRectangle(ref scissor))
            {
                base.DrawElements(transitionAlpha, backgroundTransitionAlpha);
            }
        }

        private void DebugDraw()
        {
            MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft(), Size, Color.White, 2);
            MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft() + m_scrolledArea.Position, m_scrolledArea.Size, Color.Cyan, 1);
        }

        public void FitSizeToScrolledControl()
        {
            if (ScrolledControl == null)
                return;
            m_scrolledArea.Size = ScrolledControl.Size;
            var size = ScrolledControl.Size + m_scrolledAreaPadding.SizeChange;
            if (m_scrollbarV != null)
                size.X += m_scrollbarV.Size.X;
            if (m_scrollbarH != null)
                size.Y += m_scrollbarH.Size.Y;
            Size = size;
        }

        public void SetPageVertical(float pageNumber)
        {
            m_scrollbarV.SetPage(pageNumber);
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            RefreshScrollbar();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshScrolledArea();
            RefreshScrollbar();
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            if (m_scrollbarV != null) m_scrollbarV.HasHighlight = this.HasHighlight;
            if (m_scrollbarH != null) m_scrollbarH.HasHighlight = this.HasHighlight;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
        }

        public override void ShowToolTip()
        {
            if (IsMouseOver)
                base.ShowToolTip();
        }

        private void RefreshScrolledArea()
        {
            m_scrolledArea = new RectangleF(m_scrolledAreaPadding.TopLeftOffset, Size - m_scrolledAreaPadding.SizeChange);
            if (m_scrollbarV != null)
                m_scrolledArea.Size.X -= m_scrollbarV.Size.X;
            if (m_scrollbarH != null)
                m_scrolledArea.Size.Y -= m_scrollbarH.Size.Y;
            if (PanelScrolled != null)
                PanelScrolled(this);
        }

        private void RefreshScrollbar()
        {
            if (ScrolledControl != null)
            {
                if (m_scrollbarV != null)
                {
                    m_scrollbarV.Visible = m_scrolledArea.Size.Y < ScrolledControl.Size.Y;
                    if (m_scrollbarV.Visible)
                    {
                        m_scrollbarV.Init(ScrolledControl.Size.Y, m_scrolledArea.Size.Y);
                        var posTopRight = Size * new Vector2(0.5f, -0.5f);
                        var position = new Vector2(posTopRight.X - m_scrollbarV.Size.X,
                                                   posTopRight.Y);
                        m_scrollbarV.Layout(position + new Vector2(0.0f, m_scrolledAreaPadding.Top), m_scrolledArea.Size.Y);
                    }
                    else
                        m_scrollbarV.Value = 0f;
                }

                if (m_scrollbarH != null)
                {
                    m_scrollbarH.Visible = m_scrolledArea.Size.X < ScrolledControl.Size.X;
                    if (m_scrollbarH.Visible)
                    {
                        m_scrollbarH.Init(ScrolledControl.Size.X, m_scrolledArea.Size.X);
                        var posBottomLeft = Size * new Vector2(-0.5f, 0.5f);
                        var position = new Vector2(posBottomLeft.X,
                                                   posBottomLeft.Y - m_scrollbarH.Size.Y + ScrollBarOffset.Y);
                        m_scrollbarH.Layout(position + new Vector2(m_scrolledAreaPadding.Left), m_scrolledArea.Size.X);
                    }
                    else
                        m_scrollbarH.Value = 0f;
                }
            }
            else
            {
                if (m_scrollbarV != null) m_scrollbarV.Visible = false;
                if (m_scrollbarH != null) m_scrollbarH.Visible = false;
            }

            RefreshScrolledControlPosition();
        }

        private void scrollbar_ValueChanged(MyScrollbar scrollbar)
        {
            RefreshScrolledControlPosition();
        }

        private void scrolledControl_SizeChanged(MyGuiControlBase control)
        {
            RefreshInternals();
        }

        public void RefreshInternals()
        {
            RefreshScrolledArea();
            RefreshScrollbar();
        }

        private void RefreshScrolledControlPosition()
        {
            var position = -0.5f * Size + m_scrolledAreaPadding.TopLeftOffset;
            if (m_scrollbarH != null) position.X -= m_scrollbarH.Value;
            if (m_scrollbarV != null) position.Y -= m_scrollbarV.Value;
            ScrolledControl.Position = position;
            if(PanelScrolled != null)
                PanelScrolled(this);
        }


        public MyGuiControls Controls
        {
            get { return m_controls; }
        }
    }
}
