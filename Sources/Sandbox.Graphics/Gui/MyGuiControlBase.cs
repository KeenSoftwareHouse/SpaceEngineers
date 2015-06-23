using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;


namespace Sandbox.Graphics.GUI
{
    public delegate void VisibleChangedDelegate(object sender, bool isVisible);

    public enum MyGuiControlHighlightType
    {
        NEVER,
        WHEN_CURSOR_OVER,
        WHEN_ACTIVE,
        FORCED
    }

    /// <summary>
    /// Structure describing texture that consists of normal and highlight
    /// version. Also holds information about size of the texture before it was
    /// scaled to power of 2 and this size in GUI normalized coordinates.
    /// </summary>
    public struct MyGuiHighlightTexture
    {
        /// <summary>
        /// Normal, non highlight version of the texture;
        /// </summary>
        public string Normal;

        /// <summary>
        /// Highlight version of the texture.
        /// </summary>
        public string Highlight;

        /// <summary>
        /// Size in pixels before texture was scaled to power of 2. This helps
        /// when we have to compute its correct aspect ratio and ideal
        /// resolution for rendering.
        /// </summary>
        public Vector2 SizePx
        {
            get { return m_sizePx; }
            set
            {
                m_sizePx = value;
                SizeGui = m_sizePx / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }
        }
        private Vector2 m_sizePx;

        /// <summary>
        /// Size in pixels converted to normalized gui coordinates. Can be used
        /// as size when drawing.
        /// </summary>
        public Vector2 SizeGui
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Similar to MyGuiHighlightTexture but only contains one texture image.
    /// </summary>
    public struct MyGuiSizedTexture
    {
        private Vector2 m_sizePx;
        private Vector2 m_sizeGui;

        public string Texture;

        public Vector2 SizePx
        {
            get { return m_sizePx; }
            set
            {
                m_sizePx = value;
                SizeGui = m_sizePx / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }
        }

        public Vector2 SizeGui
        {
            get { return m_sizeGui; }
            private set { m_sizeGui = value; }
        }

        public MyGuiSizedTexture(MyGuiPaddedTexture original)
        {
            Texture = original.Texture;
            m_sizePx = original.SizePx;
            m_sizeGui = original.SizeGui;
        }
    }

    /// <summary>
    /// Texture that also contains padding information.
    /// </summary>
    public struct MyGuiPaddedTexture
    {
        public string Texture;

        public Vector2 SizePx
        {
            get { return m_sizePx; }
            set
            {
                m_sizePx = value;
                SizeGui = m_sizePx / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }
        }
        private Vector2 m_sizePx;

        public Vector2 PaddingSizePx
        {
            get { return m_paddingSizePx; }
            set
            {
                m_paddingSizePx = value;
                PaddingSizeGui = m_paddingSizePx / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }
        }
        private Vector2 m_paddingSizePx;

        public Vector2 SizeGui
        {
            get;
            private set;
        }

        public Vector2 PaddingSizeGui
        {
            get;
            private set;
        }

    }

    /// <summary>
    /// Structure specifying thickness of each of the 4 borders of a rectangle.
    /// Can be used for margin and padding specification.
    /// </summary>
    public struct MyGuiBorderThickness
    {
        public float Left, Right, Top, Bottom;

        public MyGuiBorderThickness(float val = 0f)
        {
            Left = Right = Top = Bottom = val;
        }

        public MyGuiBorderThickness(float horizontal, float vertical)
        {
            Left = Right = horizontal;
            Top = Bottom = vertical;
        }

        public float HorizontalSum
        {
            get { return Left + Right; }
        }

        public float VerticalSum
        {
            get { return Top + Bottom; }
        }

        public Vector2 TopLeftOffset
        {
            get { return new Vector2(Left, Top); }
        }

        public Vector2 TopRightOffset
        {
            get { return new Vector2(-Right, Top); }
        }

        public Vector2 BottomLeftOffset
        {
            get { return new Vector2(Left, -Bottom); }
        }

        public Vector2 SizeChange
        {
            get { return new Vector2(HorizontalSum, VerticalSum); }
        }

        public Vector2 MarginStep
        {
            get
            {
                return new Vector2(Math.Max(Left, Right),
                                   Math.Max(Top, Bottom));
            }
        }
    }

    public abstract class MyGuiControlBase : IMyGuiControlsOwner
    {
        public class Friend
        {
            protected static void SetOwner(MyGuiControlBase control, IMyGuiControlsOwner owner)
            {
                control.Owner = owner;
            }
        }

        public struct NameChangedArgs
        {
            public string OldName;
        }

        private const bool DEBUG_CONTROL_FOCUS = false;

        /// <summary>
        /// Status of mouse over in this update.
        /// </summary>
        private bool m_isMouseOver = false;

        /// <summary>
        /// Status of mouse over in previous update.
        /// </summary>
        private bool m_isMouseOverInPrevious = false;

        private int m_showToolTipDelay;

        private bool m_canHaveFocus;

        private Vector2 m_minSize = Vector2.Zero;
        private Vector2 m_maxSize = Vector2.PositiveInfinity;
        private string m_name;

        protected bool m_mouseButtonPressed = false;

        protected bool m_showToolTip = false;

        protected internal MyToolTips m_toolTip;

        protected Vector2 m_toolTipPosition;

        #region Properties and public fields
        public string Name
        {
            get { return m_name; }
            set
            {
                if (m_name != value)
                {
                    var oldValue = m_name;
                    m_name = value;
                    if (NameChanged != null)
                    {
                        NameChanged(this, new NameChangedArgs()
                        {
                            OldName = oldValue,
                        });
                    }
                }
            }
        }

        public event Action<MyGuiControlBase, NameChangedArgs> NameChanged;

        public IMyGuiControlsOwner Owner
        {
            get;
            private set;
        }

        protected readonly MyGuiControls Elements;

        /// <summary>
        /// Position of control's center (normalized and relative to parent screen center (not left/top corner!!!))
        /// </summary>
        public Vector2 Position
        {
            get { return m_position; }
            set
            {
                if (m_position != value)
                {
                    m_position = value;
                    OnPositionChanged();
                }
            }
        }
        private Vector2 m_position;

        /// <summary>
        /// Size of control (normalized).
        /// </summary>
        public Vector2 Size
        {
            get { return m_size; }
            set
            {
                value = Vector2.Clamp(value, MinSize, MaxSize);
                Debug.Assert(!float.IsNaN(value.X) && !float.IsNaN(value.Y), "Invalid value");
                if (m_size != value)
                {
                    m_size = value;
                    OnSizeChanged();
                }
            }
        }
        private Vector2 m_size;
        public event Action<MyGuiControlBase> SizeChanged;

        public Vector2 MinSize
        {
            get { return m_minSize; }
            protected set
            {
                if (m_minSize != value)
                {
                    m_minSize = value;
                    Size = m_size;
                }
            }
        }

        public Vector2 MaxSize
        {
            get { return m_maxSize; }
            protected set
            {
                if (m_maxSize != value)
                {
                    m_maxSize = value;
                    Size = m_size;
                }
            }
        }

        private Vector4 m_colorMask;
        public Vector4 ColorMask
        {
            get { return m_colorMask; }
            set
            {
                if (m_colorMask != value)
                {
                    m_colorMask = value;
                    OnColorMaskChanged();
                }
            }
        }

        public MyGuiCompositeTexture BackgroundTexture;

        public Vector4 BorderColor;

        public int BorderSize
        {
            get;
            set;
        }

        public bool BorderEnabled;

        public bool DrawWhilePaused;

        /// <summary>
        /// False to disable control, disabled controls are skipped when switching with Tab key etc., look implemented atm. only in MyGuiControlButton.
        /// </summary>
        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                {
                    m_enabled = value;
                    OnEnabledChanged();
                }
            }
        }
        private bool m_enabled;

        public bool ShowTooltipWhenDisabled;

        /// <summary>
        /// There are some controls, that cannot receive any handle input(control panel for example), thus disable them with this.
        /// </summary>
        public bool IsActiveControl;

        public MyGuiDrawAlignEnum OriginAlign
        {
            get { return m_originAlign; }
            set
            {
                if (m_originAlign != value)
                {
                    m_originAlign = value;
                    OnOriginAlignChanged();
                }
            }
        }
        private MyGuiDrawAlignEnum m_originAlign;

        /// <summary>
        /// Says whether control is visible. Note that this is not a constant time operation (checks parents, fires events on set).
        /// </summary>
        public bool Visible
        {
            get { return m_visible; }
            set
            {
                if (m_visible != value)
                {
                    m_visible = value;
                    OnVisibleChanged();
                }
            }
        }
        private bool m_visible;
        public event VisibleChangedDelegate VisibleChanged;

        public MyGuiControlHighlightType HighlightType;

        /// <summary>
        /// Says whether control is currently highlighted. When control is highlit depends on HighlightType.
        /// </summary>
        public bool HasHighlight
        {
            get { return m_hasHighlight; }
            private set
            {
                if (m_hasHighlight != value)
                {
                    m_hasHighlight = value;
                    OnHasHighlightChanged();
                }
            }
        }
        private bool m_hasHighlight;

        public bool HasFocus
        {
            get { return MyScreenManager.FocusedControl == this; }
        }

        public bool IsMouseOver
        {
            get { return m_isMouseOver; }
            private set { m_isMouseOver = value; }
        }

        public bool CanHaveFocus
        {
            get { return m_canHaveFocus; }
            set { m_canHaveFocus = value; }
        }

        /// <summary>
        /// Indicates whether elements can have focus or not.
        /// </summary>
        protected bool AllowFocusingElements
        {
            get;
            set;
        }

        /// <summary>
        /// Specific user data for this control.
        /// </summary>
        public object UserData
        {
            get;
            set;
        }

        public string DebugNamePath
        {
            get { return Path.Combine(Owner != null ? Owner.DebugNamePath : "null", Name); }
        }

        /// <summary>
        /// Only use this to turn off mouse handling for child controls when
        /// parent does not contain mouse.
        /// </summary>
        internal bool HandleMouse = true;

        bool IMyGuiControlsOwner.HandleMouse { get { return HandleMouse && (Owner != null ? Owner.HandleMouse : true); } }

        #endregion Properties and public fields

        #region Construction & serialization

        protected MyGuiControlBase(
            Vector2? position = null,
            Vector2? size = null,
            Vector4? colorMask = null,
            String toolTip = null,
            MyGuiCompositeTexture backgroundTexture = null,
            bool isActiveControl = true,
            bool canHaveFocus = false,
            bool allowFocusingElements = false,
            MyGuiControlHighlightType highlightType = MyGuiControlHighlightType.WHEN_ACTIVE,
            MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
        {
            Name = GetType().Name;
            Visible = true;
            m_enabled = true;
            m_position = position ?? Vector2.Zero;
            m_canHaveFocus = canHaveFocus;
            m_size = size ?? Vector2.One;
            m_colorMask = colorMask ?? Vector4.One;
            BackgroundTexture = backgroundTexture;
            IsActiveControl = isActiveControl;
            HighlightType = highlightType;
            m_originAlign = originAlign;
            BorderSize = 1;
            BorderColor = Vector4.One;
            BorderEnabled = false;
            DrawWhilePaused = true;
            Elements = new MyGuiControls(this);
            AllowFocusingElements = allowFocusingElements;

            if (toolTip != null)
                m_toolTip = new MyToolTips(toolTip);

        }

        public virtual void Init(MyObjectBuilder_GuiControlBase builder)
        {
            m_position = builder.Position;
            Size = builder.Size;
            Name = builder.Name;
            if (builder.BackgroundColor != Vector4.One) ColorMask = builder.BackgroundColor;
            if (builder.ControlTexture != null)
                BackgroundTexture = new MyGuiCompositeTexture() { Center = new MyGuiSizedTexture() { Texture = builder.ControlTexture } };
            OriginAlign = builder.OriginAlign;
        }

        public virtual MyObjectBuilder_GuiControlBase GetObjectBuilder()
        {
            var objectBuilder = MyGuiControlsFactory.CreateObjectBuilder(this);

            objectBuilder.Position = m_position;
            objectBuilder.Size = Size;
            objectBuilder.Name = Name;
            objectBuilder.BackgroundColor = ColorMask;
            objectBuilder.ControlTexture = BackgroundTexture != null ? BackgroundTexture.Center.Texture : null;
            objectBuilder.OriginAlign = OriginAlign;

            return objectBuilder;
        }

        public static void ReadIfHasValue<T>(ref T target, T? source) where T : struct
        {
            if (source.HasValue)
                target = source.Value;
        }

        public static void ReadIfHasValue(ref Color target, Vector4? source)
        {
            if (source.HasValue)
                target = new Color(source.Value);
        }
        #endregion

        #region Virtuals

        public virtual void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            DrawBackground(backgroundTransitionAlpha);
            DrawElements(transitionAlpha, backgroundTransitionAlpha);
            DrawBorder(transitionAlpha);
        }

        protected void DrawBackground(float transitionAlpha)
        {
            // Draw background texture if there is one and background is not completely transparent.
            if (BackgroundTexture != null && ColorMask.W > 0.0f)
            {
                BackgroundTexture.Draw(GetPositionAbsoluteTopLeft(), Size, ApplyColorMaskModifiers(ColorMask, Enabled, transitionAlpha));
            }
        }

        protected void DrawBorder(float transitionAlpha)
        {
            if (BorderEnabled || (DEBUG_CONTROL_FOCUS && HasFocus))
            {
                var color = ApplyColorMaskModifiers((DEBUG_CONTROL_FOCUS && HasFocus) ? Vector4.One : (BorderColor * ColorMask), Enabled, transitionAlpha);
                MyGuiManager.DrawBorders(GetPositionAbsoluteTopLeft(), Size, color, BorderSize);
            }
        }

        public virtual MyGuiControlGridDragAndDrop GetDragAndDropHandlingNow()
        {
            return null;
        }

        public virtual MyGuiControlBase GetExclusiveInputHandler()
        {
            return GetExclusiveInputHandler(Elements);
        }

        public static MyGuiControlBase GetExclusiveInputHandler(MyGuiControls controls)
        {
            foreach (var control in controls.GetVisibleControls())
            {
                var exclusiveInputHandler = control.GetExclusiveInputHandler();
                if (exclusiveInputHandler != null)
                    return exclusiveInputHandler;
            }

            return null;
        }

        /// <summary>
        /// Returns first control, which has mouse over.
        /// </summary>
        public virtual MyGuiControlBase GetMouseOverControl()
        {
            if (IsMouseOver)
                return this;
            return null;
        }

        /// <summary>
        /// Method returns true if input was captured by control, so no other controls, nor screen can use input in this update.
        /// </summary>
        public virtual MyGuiControlBase HandleInput()
        {
            bool isMouseOverOld = IsMouseOver;
            IsMouseOver = CheckMouseOver();
            if (IsActiveControl)
            {
                m_mouseButtonPressed = IsMouseOver && MyInput.Static.IsPrimaryButtonPressed();

                if (IsMouseOver && !isMouseOverOld && Enabled)
                {
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseOver);
                }
            }

            // if mouseover this control longer than specified period, show tooltip for the control
            if (IsMouseOver && isMouseOverOld)
            {
                if (m_showToolTip == false)
                {
                    m_showToolTipDelay = MyGuiManager.TotalTimeInMilliseconds + MyGuiConstants.SHOW_CONTROL_TOOLTIP_DELAY;
                    m_showToolTip = true;
                }
            }
            else
            {
                m_showToolTip = false;
            }

            return null;
        }

        public virtual void HideToolTip()
        {
            m_showToolTip = false;
        }

        public virtual bool IsMouseOverAnyControl()
        {
            return false;
        }

        public virtual void ShowToolTip()
        {
            foreach (var element in Elements.GetVisibleControls())
                element.ShowToolTip();

            //show tooltip
            if (m_showToolTip && (Enabled || ShowTooltipWhenDisabled))
            {
                if ((MyGuiManager.TotalTimeInMilliseconds > m_showToolTipDelay) && (m_toolTip != null) && (m_toolTip.ToolTips.Count > 0))
                {
                    m_toolTipPosition = MyGuiManager.MouseCursorPosition;
                    m_toolTip.Draw(m_toolTipPosition);
                }
            }
        }

        public virtual void Update()
        {
            HasHighlight = ShouldHaveHighlight();
            foreach (var element in Elements)
                element.Update();
        }

        protected virtual bool ShouldHaveHighlight()
        {
            if (Enabled && HighlightType != MyGuiControlHighlightType.NEVER && IsMouseOverOrKeyboardActive())
            {
                return (HighlightType == MyGuiControlHighlightType.WHEN_ACTIVE) ||
                       (HighlightType == MyGuiControlHighlightType.WHEN_CURSOR_OVER && IsMouseOver) ||
                       (HasFocus);
            }
            else
                return false;
        }

        /// <summary>
        /// Checks if mouse cursor is over control.
        /// </summary>
        public virtual bool CheckMouseOver()
        {
            return CheckMouseOver(Size, GetPositionAbsolute(), OriginAlign);
        }

        protected virtual void OnHasHighlightChanged()
        {
            foreach (var element in Elements)
                element.HasHighlight = this.HasHighlight;
        }

        protected virtual void OnPositionChanged()
        {
        }

        protected virtual void OnSizeChanged()
        {
            if (SizeChanged != null)
                SizeChanged(this);
        }

        protected virtual void OnVisibleChanged()
        {
            if (VisibleChanged != null /* && Parent.Visible*/)
                VisibleChanged(this, m_visible);
        }

        protected virtual void OnOriginAlignChanged()
        {
        }

        protected virtual void OnEnabledChanged()
        {
            foreach (var e in Elements)
                e.Enabled = m_enabled;
        }

        protected virtual void OnColorMaskChanged()
        {
            foreach (var element in Elements)
                element.ColorMask = ColorMask;
        }

        #endregion

        /// <summary>
        /// Modifies source color mask using transition alpha and color multiplier in case a control is disabled.
        /// </summary>
        /// <param name="sourceColorMask">Original color mask of the control.</param>
        /// <param name="enabled">Indicates whether disabled color mask should be applied.</param>
        /// <param name="transitionAlpha">Alpha value modified during transition.</param>
        /// <returns></returns>
        public static Color ApplyColorMaskModifiers(Vector4 sourceColorMask, bool enabled, float transitionAlpha)
        {
            Vector4 ret = sourceColorMask;
            if (!enabled)
            {
                ret.X *= MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER.X;
                ret.Y *= MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER.Y;
                ret.Z *= MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER.Z;
                ret.W *= MyGuiConstants.DISABLED_CONTROL_COLOR_MASK_MULTIPLIER.W;
            }
            ret.W *= transitionAlpha;
            return new Color(ret);
        }

        public virtual string GetMouseCursorTexture()
        {
            // this is default mouse cursor texture
            string mouseCursorTexture = MyGuiManager.GetMouseCursorTexture();//null;
            if (IsMouseOver)
            {
                //// when mouse button pressed and mouse cursor texture for pressed is not null
                //if (m_mouseButtonPressed && MouseCursorPressedTexture != null)
                //{
                //    mouseCursorTexture = MouseCursorPressedTexture;
                //}
                //// when mouse over control and mouse cursor texture for hover is not null
                //else if(MouseCursorHoverTexture != null)
                //{
                //    mouseCursorTexture = MouseCursorHoverTexture;
                //}
            }
            return mouseCursorTexture;
        }

        public Vector2 GetPositionAbsolute()
        {
            if (Owner != null)
                return Owner.GetPositionAbsoluteCenter() + m_position;

            return m_position;
        }

        public Vector2 GetPositionAbsoluteBottomLeft()
        {
            return GetPositionAbsoluteTopLeft() + new Vector2(0.0f, Size.Y);
        }

        public Vector2 GetPositionAbsoluteBottomRight()
        {
            return GetPositionAbsoluteTopLeft() + Size;
        }

        public Vector2 GetPositionAbsoluteCenterLeft()
        {
            return GetPositionAbsoluteTopLeft() + new Vector2(0.0f, Size.Y * 0.5f);
        }

        public Vector2 GetPositionAbsoluteCenter()
        {
            return MyUtils.GetCoordCenterFromAligned(GetPositionAbsolute(), Size, OriginAlign);
        }

        public Vector2 GetPositionAbsoluteTopLeft()
        {
            return MyUtils.GetCoordTopLeftFromAligned(GetPositionAbsolute(), Size, OriginAlign);
        }

        public Vector2 GetPositionAbsoluteTopRight()
        {
            return GetPositionAbsoluteTopLeft() + new Vector2(Size.X, 0f);
        }

        public Vector2? GetSize()
        {
            return Size;
        }

        public void SetToolTip(MyToolTips toolTip)
        {
            m_toolTip = toolTip;
        }

        public void SetToolTip(String text)
        {
            SetToolTip(new MyToolTips(text));
        }

        public void SetToolTip(MyStringId text)
        {
            SetToolTip(MyTexts.GetString(text));
        }

        public static bool CheckMouseOver(Vector2 size, Vector2 position, MyGuiDrawAlignEnum originAlign)
        {
            return IsPointInside(MyGuiManager.MouseCursorPosition, size, position, originAlign);
        }

        public static bool IsPointInside(Vector2 queryPoint, Vector2 size, Vector2 position, MyGuiDrawAlignEnum originAlign)
        {
            Vector2 pos = MyUtils.GetCoordCenterFromAligned(position, size, originAlign);

            Vector2 min = pos - size / 2;
            Vector2 max = pos + size / 2;

            return ((queryPoint.X >= min.X) && (queryPoint.X <= max.X) && (queryPoint.Y >= min.Y) && (queryPoint.Y <= max.Y));
        }

        protected MyGuiScreenBase GetTopMostOwnerScreen()
        {
            try
            {
                IMyGuiControlsOwner currentOwner = Owner;
                while (!(currentOwner is MyGuiScreenBase))
                {
                    currentOwner = ((MyGuiControlBase)currentOwner).Owner;
                }
                return currentOwner as MyGuiScreenBase;
            }
            catch (NullReferenceException)
            {
                MyLog.Default.WriteLine("NullReferenceException in " + DebugNamePath + " trying to reach top most owner.");
                return null;
            }
        }

        protected bool IsMouseOverOrKeyboardActive()
        {
            MyGuiScreenBase topMostParentScreen = GetTopMostOwnerScreen();
            if (topMostParentScreen != null)
            {
                switch (topMostParentScreen.State)
                {
                    case MyGuiScreenState.OPENED:
                    case MyGuiScreenState.OPENING:
                    case MyGuiScreenState.UNHIDING:
                        return IsMouseOver || HasFocus;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        protected virtual void DrawElements(float transitionAlpha, float backgroundTransitionAlpha)
        {
            foreach (MyGuiControlBase element in Elements.GetVisibleControls())
            {
                if (element.GetExclusiveInputHandler() == element)
                    continue;

                element.Draw(transitionAlpha, backgroundTransitionAlpha);
            }
        }

        protected MyGuiControlBase HandleInputElements()
        {
            MyGuiControlBase captured = null;

            var elements = Elements.GetVisibleControls().ToArray();
            for (int i = elements.Length - 1; i >= 0; --i)
            {
                captured = elements[i].HandleInput();
                if (captured != null)
                    break;
            }

            return captured;
        }

        protected virtual void ClearEvents()
        {
            SizeChanged = null;
            VisibleChanged = null;
            NameChanged = null;
        }

        /// <summary>
        /// Removes various references and clears event handlers.
        /// </summary>
        public virtual void OnRemoving()
        {
            if (HasFocus)
            {
                Debug.Assert(GetTopMostOwnerScreen().FocusedControl == this);
                GetTopMostOwnerScreen().FocusedControl = null;
            }

            Elements.Clear();
            Owner = null;
            ClearEvents();
        }

        public void GetElementsUnderCursor(Vector2 position, bool visibleOnly, List<MyGuiControlBase> controls)
        {
            // TODO: Temporary workaround until GetVisibleControls removed
            if (visibleOnly)
            {
                foreach (var element in Elements.GetVisibleControls())
                {
                    if (IsPointInside(position, element.Size, element.GetPositionAbsolute(), element.OriginAlign))
                    {
                        element.GetElementsUnderCursor(position, visibleOnly, controls);
                        controls.Add(element);
                    }
                }
            }
            else
            {
                foreach (var element in Elements)
                {
                    if (IsPointInside(position, element.Size, element.GetPositionAbsolute(), element.OriginAlign))
                    {
                        element.GetElementsUnderCursor(position, visibleOnly, controls);
                        controls.Add(element);
                    }
                }
            }
        }

        internal virtual MyGuiControlBase GetFocusControl(bool forwardMovement)
        {
            Debug.Assert(Owner != null, "Possibly removed control is still referenced. Screen: " + DebugNamePath);

            if (AllowFocusingElements)
                return this.GetNextFocusControl(this, forwardMovement);
            else if (Owner != null)
                return Owner.GetNextFocusControl(this, forwardMovement);
            else
                return this;
        }

        public virtual MyGuiControlBase GetNextFocusControl(MyGuiControlBase currentFocusControl, bool forwardMovement)
        {
            int currentIdx = Elements.IndexOf(currentFocusControl);
            if (currentIdx == -1 && !forwardMovement)
                currentIdx = Elements.Count;

            int i = (forwardMovement) ? (currentIdx + 1)
                                      : (currentIdx - 1);
            int step = (forwardMovement) ? +1 : -1;
            while ((forwardMovement && i < Elements.Count) ||
                   (!forwardMovement && i >= 0))
            {
                if (MyGuiScreenBase.CanHaveFocusRightNow(Elements[i]))
                    return Elements[i];

                i += step;
            }

            return Owner.GetNextFocusControl(this, forwardMovement);
        }

        public override string ToString()
        {
            return DebugNamePath;
        }
    }
}
