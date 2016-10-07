using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    [MyGuiControlType(typeof(MyObjectBuilder_GuiControlList))]
    public class MyGuiControlList : MyGuiControlParent
    {
        #region Styles
        public class StyleDefinition
        {
            public MyGuiCompositeTexture Texture = new MyGuiCompositeTexture();
            public MyGuiBorderThickness ScrollbarMargin;
            public MyGuiBorderThickness ItemMargin;
            public bool ScrollbarEnabled;
        }

        private static StyleDefinition[] m_styles;

        static MyGuiControlList()
        {
            m_styles = new StyleDefinition[MyUtils.GetMaxValueFromEnum<MyGuiControlListStyleEnum>() + 1];
            m_styles[(int)MyGuiControlListStyleEnum.Default] = new StyleDefinition()
            {
                Texture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ScrollbarMargin = new MyGuiBorderThickness()
                {
                    Left   = 2f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Right  = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                    Top    = 3f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                    Bottom = 1f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y,
                },
                ItemMargin = new MyGuiBorderThickness(horizontal: 12f / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                                                      vertical: 12f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y),
                ScrollbarEnabled = true,
            };
            m_styles[(int)MyGuiControlListStyleEnum.Simple] = new StyleDefinition()
            {
                ScrollbarEnabled = true,
            };
        }

        public static StyleDefinition GetVisualStyle(MyGuiControlListStyleEnum style)
        {
            return m_styles[(int)style];
        }
        #endregion Styles

        private MyScrollbar m_scrollBar;
        private Vector2 m_realSize;
        private bool m_showScrollbar;
        private RectangleF m_itemsRectangle;
        private MyGuiBorderThickness m_itemMargin;

        public MyGuiControlListStyleEnum VisualStyle
        {
            get { return m_visualStyle; }
            set
            {
                m_visualStyle = value;
                RefreshVisualStyle();
            }
        }
        private MyGuiControlListStyleEnum m_visualStyle;
        private StyleDefinition m_styleDef;

        #region Construction and serialization
        public MyGuiControlList() : this(position: null) { }

        public MyGuiControlList(
            Vector2? position                     = null,
            Vector2? size                         = null,
            Vector4? backgroundColor              = null,
            String toolTip                        = null,
            MyGuiControlListStyleEnum visualStyle = MyGuiControlListStyleEnum.Default)
            : base( position: position,
                    size: size,
                    backgroundColor:  backgroundColor,
                    toolTip: toolTip)
        {
            Name = "ControlList";

            m_realSize = size ?? Vector2.One;
            m_scrollBar = new MyVScrollbar(this);
            m_scrollBar.ValueChanged += ValueChanged;

            VisualStyle = visualStyle;
            RecalculateScrollbar();

            Controls.CollectionChanged += OnVisibleControlsChanged;
            Controls.CollectionMembersVisibleChanged += OnVisibleControlsChanged;
        }

        public override void Init(MyObjectBuilder_GuiControlBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_GuiControlList;
            MyDebug.AssertDebug(ob != null);
            VisualStyle = ob.VisualStyle;
        }

        public void InitControls(IEnumerable<MyGuiControlBase> controls)
        {
            Controls.CollectionMembersVisibleChanged -= OnVisibleControlsChanged;
            Controls.CollectionChanged -= OnVisibleControlsChanged;
            Controls.Clear();
            foreach (MyGuiControlBase control in controls)
            {
                if (control != null)
                {
                    Controls.Add(control);
                }
            }
            Controls.CollectionChanged += OnVisibleControlsChanged;
            Controls.CollectionMembersVisibleChanged += OnVisibleControlsChanged;
            Recalculate();
        }

        public override MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_GuiControlList;
            MyDebug.AssertDebug(ob != null);

            ob.VisualStyle = VisualStyle;
            return ob;
        }
        #endregion

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            var topLeft = GetPositionAbsoluteTopLeft();
            m_styleDef.Texture.Draw(topLeft, Size,
                ApplyColorMaskModifiers(ColorMask, Enabled, backgroundTransitionAlpha));

            var scissor = m_itemsRectangle;
            scissor.Position += topLeft;
            using (MyGuiManager.UsingScissorRectangle(ref scissor))
            {
                base.Draw(transitionAlpha, backgroundTransitionAlpha);
            }

            if (m_showScrollbar)
                m_scrollBar.Draw(ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));

            //DebugDraw();
        }

        private void DebugDraw()
        {
            MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft() + m_itemsRectangle.Position, m_itemsRectangle.Size, Color.White, 1);
        }

        public override MyGuiControlBase HandleInput()
        {
            MyGuiControlBase baseResult = base.HandleInput();

            if (m_showScrollbar && m_scrollBar.Visible)
            {
                bool capturedScrollbar = m_scrollBar.HandleInput();

                if (capturedScrollbar)
                    return this;

                return baseResult;
            }

            return baseResult;
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();
            RecalculateScrollbar();
            CalculateNewPositionsForControls(m_scrollBar != null ? m_scrollBar.Value : 0);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            RefreshInternals();
        }

        public void Recalculate()
        {
            Vector2 oldRealSize = m_realSize;
            CalculateRealSize();
            m_itemsRectangle.Position = m_itemMargin.TopLeftOffset;
            m_itemsRectangle.Size = Size - (m_itemMargin.SizeChange + new Vector2(m_styleDef.ScrollbarMargin.HorizontalSum + ((m_showScrollbar) ? m_scrollBar.Size.X : 0f), 0.0f));

            RecalculateScrollbar();
            CalculateNewPositionsForControls(m_scrollBar!= null ? m_scrollBar.Value : 0);
        }

        private void RecalculateScrollbar()
        {
            if (m_showScrollbar)
            {
                m_scrollBar.Visible = (Size.Y < m_realSize.Y);
                m_scrollBar.Init(m_realSize.Y, m_itemsRectangle.Size.Y);

                var posTopRight = Size * new Vector2(0.5f, -0.5f);
                var margin = m_styleDef.ScrollbarMargin;
                var position = new Vector2(posTopRight.X - (margin.Right + m_scrollBar.Size.X),
                                           posTopRight.Y + margin.Top);
                m_scrollBar.Layout(position, Size.Y - margin.VerticalSum);
            }
        }

        private void ValueChanged(MyScrollbar scrollbar)
        {
            CalculateNewPositionsForControls(scrollbar.Value);
        }

        private void CalculateNewPositionsForControls(float offset)
        {
            var marginStep = m_itemMargin.MarginStep;
            var tmpControlPosTopLeft = (-0.5f * Size) + m_itemMargin.TopLeftOffset - new Vector2(0f, offset);
            foreach (MyGuiControlBase control in Controls.GetVisibleControls())
            {
                Debug.Assert(control.GetSize() != null);
                Vector2 controlSize = control.Size;
                controlSize.X = m_itemsRectangle.Size.X;
                control.Position = MyUtils.GetCoordAlignedFromTopLeft(tmpControlPosTopLeft, controlSize, control.OriginAlign);
                tmpControlPosTopLeft.Y += controlSize.Y + marginStep.Y;
            }
        }

        private void CalculateRealSize()
        {
            Vector2 newSize = Vector2.Zero;
            float marginHeightStep = m_itemMargin.MarginStep.Y;
            foreach (MyGuiControlBase control in Controls.GetVisibleControls())
            {
                Debug.Assert(control.GetSize() != null);
                Vector2 controlSize = control.GetSize().Value;
                newSize.Y += controlSize.Y + marginHeightStep;
                newSize.X = Math.Max(newSize.X, controlSize.X);
            }

            newSize.Y -= marginHeightStep;

            m_realSize.X = Math.Max(Size.X, newSize.X);
            m_realSize.Y = Math.Max(Size.Y, newSize.Y);
        }

        private void RefreshVisualStyle()
        {
            m_styleDef = GetVisualStyle(VisualStyle);
            m_itemMargin = m_styleDef.ItemMargin;
            m_showScrollbar = m_styleDef.ScrollbarEnabled;
            MinSize = m_styleDef.Texture.MinSizeGui;
            MaxSize = m_styleDef.Texture.MaxSizeGui;
            RefreshInternals();
        }

        private void RefreshInternals()
        {
            Recalculate();
        }

        private void OnVisibleControlsChanged(MyGuiControls sender)
        {
            Recalculate();
        }

        protected override void OnHasHighlightChanged()
        {
            base.OnHasHighlightChanged();
            m_scrollBar.HasHighlight = this.HasHighlight;
        }

        public override void ShowToolTip()
        {
            if (m_itemsRectangle.Contains(MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft()))
                base.ShowToolTip();
        }

        /// <summary>
        /// Set scroll to desired page. Default 0 mean start of the list.
        /// </summary>
        /// <param name="page"></param>
        public void SetScrollBarPage(float page = 0)
        {
            m_scrollBar.SetPage(page);
        }

    }
}
