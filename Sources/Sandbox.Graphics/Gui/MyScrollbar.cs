using System;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public abstract class MyScrollbar
    {
        protected enum StateEnum
        {
            Ready,
            Drag
        }

        private bool m_hasHighlight;
        private float m_value;
        private MyGuiCompositeTexture m_normalTexture;
        private MyGuiCompositeTexture m_highlightTexture;
        protected MyGuiCompositeTexture m_backgroundTexture;

        protected MyGuiControlBase OwnerControl;
        protected Vector2 Position;
        protected Vector2 CaretSize;
        protected float Max;
        protected float Page;
        protected StateEnum State;
        protected MyGuiCompositeTexture Texture;

        public float ScrollBarScale = 1;

        public Vector2 Size
        {
            get;
            protected set;
        }

        public bool Visible;

        public bool HasHighlight
        {
            get { return m_hasHighlight; }
            set
            {
                if (m_hasHighlight != value)
                {
                    m_hasHighlight = value;
                    RefreshInternals();
                }
            }
        }

        public float Value
        {
            get { return m_value; }
            set
            {
                value = MathHelper.Clamp(value, 0, Max - Page);
                if (m_value != value)
                {
                    m_value = value;
                    if (ValueChanged != null)
                        ValueChanged(this);
                }
            }
        }

        public bool IsOverCaret { get; protected set; }

        public event Action<MyScrollbar> ValueChanged;

        protected MyScrollbar(MyGuiControlBase control,
            MyGuiCompositeTexture normalTexture,
            MyGuiCompositeTexture highlightTexture,
            MyGuiCompositeTexture backgroundTexture)
        {
            OwnerControl       = control;
            m_normalTexture    = normalTexture;
            m_highlightTexture = highlightTexture;
            m_backgroundTexture = backgroundTexture;
            RefreshInternals();
        }

        protected bool CanScroll()
        {
            return Max > 0 && Max > Page;
        }

        public void Init(float max, float page)
        {
            Max = max;
            Page = page;

            //GR: When refreshing on each control you may need different behavior. So handle from there
            //ChangeValue(0);
        }

        public void ChangeValue(float amount)
        {
            Value = Value + amount;
        }

        public void PageDown()
        {
            ChangeValue(Page);
        }

        public void PageUp()
        {
            ChangeValue(-Page);
        }

        public void SetPage(float pageNumber)
        {
            Value = pageNumber * Page;
        }

        public abstract void Layout(Vector2 position, float length);

        public abstract void Draw(Color colorMask);

        public void DebugDraw()
        {
            MyGuiManager.DrawBorders(OwnerControl.GetPositionAbsoluteCenter() + Position, Size, Color.White, 1);
        }

        public abstract bool HandleInput();

        protected virtual void RefreshInternals()
        {
            Texture = (HasHighlight) ? m_highlightTexture : m_normalTexture;
            if (HasHighlight)
                Texture = m_highlightTexture;
            else
                Texture = m_normalTexture;
        }

    }

    public class MyVScrollbar : MyScrollbar
    {
        private Vector2 m_dragClick;

        public MyVScrollbar(MyGuiControlBase control):
            base(control,
                 MyGuiConstants.TEXTURE_SCROLLBAR_V_THUMB,
                 MyGuiConstants.TEXTURE_SCROLLBAR_V_THUMB_HIGHLIGHT,
                 MyGuiConstants.TEXTURE_SCROLLBAR_V_BACKGROUND)
        {

        }

        private Vector2 GetCarretPosition()
        {
            return new Vector2(0, Value * (Size.Y - CaretSize.Y) / (Max - Page));
        }

        public override void Layout(Vector2 positionTopLeft, float length)
        {
            Position = positionTopLeft;
            Size = new Vector2(Texture.MinSizeGui.X, length);

            if (CanScroll())
            {
                CaretSize = new Vector2(
                    Texture.MinSizeGui.X,
                    MathHelper.Clamp((Page / Max) * length, Texture.MinSizeGui.Y, Texture.MaxSizeGui.Y));
            }
        }

        public override void Draw(Color colorMask)
        {
            if (!Visible)
            {
                return;
            }

            var leftTopPosition = OwnerControl.GetPositionAbsoluteCenter() + Position;
            m_backgroundTexture.Draw(leftTopPosition, Size, colorMask);

            if (CanScroll())
            {
                var carretPosition = GetCarretPosition();
                Texture.Draw(leftTopPosition + carretPosition,
                    CaretSize,
                    colorMask);
            }
        }

        public override bool HandleInput()
        {
            bool captured = false;
            if (!Visible || !CanScroll())
                return captured;

            var absolutePosition = OwnerControl.GetPositionAbsoluteCenter() + Position;
            bool mouseOverThis = MyGuiControlBase.CheckMouseOver(Size, absolutePosition, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            bool mouseOverOwner = OwnerControl.IsMouseOver;
            IsOverCaret = MyGuiControlBase.CheckMouseOver(CaretSize, absolutePosition + GetCarretPosition(), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            switch (State)
            {
                case StateEnum.Ready:
                    if (MyInput.Static.IsNewPrimaryButtonPressed() && IsOverCaret)
                    {
                        captured = true;
                        State = StateEnum.Drag;
                        m_dragClick = MyGuiManager.MouseCursorPosition;
                    }
                    break;
                case StateEnum.Drag:
                    if (!MyInput.Static.IsPrimaryButtonPressed())
                    {
                        State = StateEnum.Ready;
                    }
                    else
                    {
                        ChangeValue((MyGuiManager.MouseCursorPosition.Y - m_dragClick.Y) * (Max - Page) / (Size.Y - CaretSize.Y));
                        m_dragClick = MyGuiManager.MouseCursorPosition;
                    }
                    captured = true;
                    break;
            }

            if (mouseOverThis || mouseOverOwner)
            {
                var scrolled = MyInput.Static.DeltaMouseScrollWheelValue();
                if (scrolled != 0 && scrolled != -MyInput.Static.PreviousMouseScrollWheelValue())
                {
                    captured = true;
                    ChangeValue((scrolled / -120f) * Page * 0.25f);
                }
            }

            return captured;
        }

        protected override void RefreshInternals()
        {
            base.RefreshInternals();
            Size = new Vector2(Texture.MinSizeGui.X, Size.Y);
        }
    }

    class MyHScrollbar : MyScrollbar
    {
        private Vector2 m_dragClick;

        public bool EnableWheelScroll = false;

        public MyHScrollbar(MyGuiControlBase control):
            base(control,
                 MyGuiConstants.TEXTURE_SCROLLBAR_H_THUMB,
                 MyGuiConstants.TEXTURE_SCROLLBAR_H_THUMB_HIGHLIGHT,
                 MyGuiConstants.TEXTURE_SCROLLBAR_V_BACKGROUND)
        {

        }

        private Vector2 GetCarretPosition()
        {
            return new Vector2(Value * (Size.X - CaretSize.X) / (Max - Page), 0);
        }

        public override void Layout(Vector2 positionTopLeft, float length)
        {
            Position = positionTopLeft;
            Size = new Vector2(length, Texture.MinSizeGui.Y);

            if (CanScroll())
            {
                CaretSize = new Vector2(
                    MathHelper.Clamp((Page / Max) * length, Texture.MinSizeGui.X, Texture.MaxSizeGui.X),
                    Texture.MinSizeGui.Y);
            }
        }

        public override void Draw(Color colorMask)
        {
            if (!Visible)
            {
                return;
            }

            var leftTopPosition = OwnerControl.GetPositionAbsoluteCenter() + Position;
            m_backgroundTexture.Draw(leftTopPosition, Size, colorMask);

            if (CanScroll())
            {
                var carretPosition = GetCarretPosition();
                Texture.Draw(leftTopPosition + carretPosition,
                    CaretSize,
                    colorMask,
                    ScrollBarScale);
            }
        }

        public override bool HandleInput()
        {
            bool captured = false;
            if (!Visible || !CanScroll())
                return captured;

            var absolutePosition = OwnerControl.GetPositionAbsoluteCenter() + Position;
            IsOverCaret = MyGuiControlBase.CheckMouseOver(CaretSize, absolutePosition + GetCarretPosition(), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            switch (State)
            {
                case StateEnum.Ready:
                    if (MyInput.Static.IsNewLeftMousePressed() && IsOverCaret)
                    {
                        captured = true;
                        State = StateEnum.Drag;
                        m_dragClick = MyGuiManager.MouseCursorPosition;
                    }
                    break;
                case StateEnum.Drag:
                    if (!MyInput.Static.IsLeftMousePressed())
                    {
                        State = StateEnum.Ready;
                    }
                    else
                    {
                        ChangeValue((MyGuiManager.MouseCursorPosition.X - m_dragClick.X) * (Max - Page) / (Size.X - CaretSize.X));
                        m_dragClick = MyGuiManager.MouseCursorPosition;
                    }
                    captured = true;
                    break;
            }

            if (EnableWheelScroll)
            {
                bool mouseOverThis = MyGuiControlBase.CheckMouseOver(Size, absolutePosition, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
                bool mouseOverOwner = OwnerControl.IsMouseOver;
                if (mouseOverThis || mouseOverOwner)
                {
                    Value += (MyInput.Static.DeltaMouseScrollWheelValue() / -120f) * Page * 0.25f;
                }
            }

            return captured;
        }

        protected override void RefreshInternals()
        {
            base.RefreshInternals();
            Size = new Vector2(Size.X, Texture.MinSizeGui.Y);
        }
    }
}
