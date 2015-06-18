#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Graphics.GUI
{
    #region Enums

    public enum MyGuiScreenState
    {
        OPENING,
        OPENED,
        CLOSING,
        CLOSED,
        HIDING,
        UNHIDING,
        HIDDEN
    }

    /// <summary>
    /// Generic screen results
    /// </summary>
    public enum ScreenResult
    {
        /// <summary>
        /// Ok
        /// </summary>
        Ok,
        /// <summary>
        /// Cancel
        /// </summary>
        Cancel,
    }

    #endregion

    public abstract class MyGuiScreenBase : IMyGuiControlsParent
    {
        #region Delegates

        //Delegates
        public delegate void ScreenHandler(MyGuiScreenBase source);

        #endregion

        #region Events

        //Events
        protected Action OnEnterCallback;
        public Action OnLoadingAction;
        public event ScreenHandler Closed;
        public event VisibleChangedDelegate VisibleChanged;

        public event Action<MyGuiScreenBase> DataLoading;
        public event Action<MyGuiScreenBase> DataUnloading;
        #endregion

        #region Fields

        //Fields
        protected float m_transitionAlpha;
        protected float m_backgroundTransition;
        protected float m_guiTransition;
        private MyGuiControls m_controls;
        protected Vector2 m_position;
        protected Color m_backgroundFadeColor;

        //  If true then no other screen can be added above this one. They will be always added below.
        protected bool m_isTopMostScreen = false;
        protected bool m_isAlwaysFirst = false;

        // Similar to topmost, but can be more of them and input is not affected
        protected bool m_isTopScreen = false;

        protected bool m_isFirstForUnload = false;

        //  Background color of this screen. If not specified, background rectangle won't be drawn (this is default option).
        protected Vector4? m_backgroundColor;

        // Background texture. If not specified, default screen background texture is used
        protected string m_backgroundTexture;

        protected bool m_canCloseInCloseAllScreenCalls = true;


        //  Normalized size of this screen. If you don't need size (this is full-screen screen), set it to null.
        protected Vector2? m_size;

        //  Automaticaly closes this screen when ESC is pressed (e.g. for game-play screen we don't want this functionality)
        protected bool m_closeOnEsc = true;

        private bool m_drawMouseCursor = true;    //  Every screen can define if mouse cursor is drawn when this screen has focus. By default true, so who don't want - must set to false.
        protected bool m_joystickAsMouse = true;   //  If you want to support mouse cursor movement with joystick analog, set this value to true.
        protected bool m_defaultJoystickDpadUse = true; // If you want to move around gui controls with dpad, set value of this field to true.
        protected bool m_defaultJoystickCancelUse = true; // If you want to exit from menus with XBox 'B' button, set value of this field true.


        int m_lastTransitionTime;

        //  Don't change 'm_isLoaded' directly - because it can be used by other threads. Instead use property IsLoaded (it's thread-safe)
        bool m_isLoaded = false;
        object m_isLoadedLock = new object();

        //  Server to check if this screen is before or after first update, so we can do some sort of initialization
        bool m_firstUpdateServed = false;

        protected bool m_drawEvenWithoutFocus = false;  // Some screens must be drawn even if they do not have focus(gameplay screen for example)
        protected bool m_canShareInput = false; //If input is going to be shared, screen can refuse it

        protected bool m_allowUnhidePreviousScreen;

        protected GuiSounds? m_openingCueEnum;
        protected GuiSounds? m_closingCueEnum;

        private MyGuiControlBase m_draggingControl;
        private Vector2 m_draggingControlOffset;
        private StringBuilder m_drawPositionSb = new StringBuilder();

        private MyGuiControlGridDragAndDrop m_listboxDragAndDropHandlingNow;
        private MyGuiControlBase m_comboboxHandlingNow;
        private MyGuiControlBase m_lastHandlingControl;

        private MyGuiControlButton m_closeButton;

        protected readonly MyGuiControls Elements;

        public Color BackgroundFadeColor
        {
            get
            {
                var tmp = m_backgroundFadeColor;
                tmp.A = (byte)(tmp.A * m_transitionAlpha);
                return tmp;
            }
        }

        #endregion

        #region Init

        //  Prohibited!
        private MyGuiScreenBase() { }

        protected MyGuiScreenBase(
            Vector2? position = null,
            Vector4? backgroundColor = null,
            Vector2? size = null,
            bool isTopMostScreen = false,
            string backgroundTexture = null, 
            float backgroundTransition = 0.0f,
            float guiTransition = 0.0f)
        {
            m_controls = new MyGuiControls(this);
            m_backgroundFadeColor = Color.White;
            m_backgroundColor = backgroundColor;
            m_size = size;
            m_isTopMostScreen = isTopMostScreen;
            m_allowUnhidePreviousScreen = true;

            State = MyGuiScreenState.OPENING;
            m_lastTransitionTime = MyGuiManager.TotalTimeInMilliseconds;
            m_position = position ?? new Vector2(0.5f, 0.5f);

            m_backgroundTexture = backgroundTexture;

            Elements = new MyGuiControls(this);
            m_backgroundTransition = backgroundTransition;
            m_guiTransition = guiTransition;
            CreateCloseButton();
            SetDefaultCloseButtonOffset();
        }

        public MyObjectBuilder_GuiScreen GetObjectBuilder()
        {
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_GuiScreen>();

            objectBuilder.Controls = Controls.GetObjectBuilder();
            objectBuilder.BackgroundColor = m_backgroundColor;
            objectBuilder.BackgroundTexture = m_backgroundTexture;
            objectBuilder.Size = m_size;
            objectBuilder.CloseButtonEnabled = CloseButtonEnabled;
            objectBuilder.CloseButtonOffset = CloseButtonOffset;

            return objectBuilder;
        }

        public void Init(MyObjectBuilder_GuiScreen objectBuilder)
        {
            m_backgroundColor = objectBuilder.BackgroundColor;
            m_backgroundTexture = objectBuilder.BackgroundTexture;
            m_size = objectBuilder.Size;
            Controls.Init(objectBuilder.Controls);

            CloseButtonOffset = objectBuilder.CloseButtonOffset;
            CloseButtonEnabled = objectBuilder.CloseButtonEnabled;
        }

        #endregion

        #region Load

        //  In all methods overiding this one, call this base LoadContent as last, after child's
        public virtual void LoadContent()
        {
            IsLoaded = true;
            m_lastTransitionTime = MyGuiManager.TotalTimeInMilliseconds;
        }

        public virtual void LoadData()
        {
            if (DataLoading != null)
                DataLoading(this);
        }

        public virtual void UnloadContent()
        {
            MyLog.Default.WriteLine("MyGuiScreenBase.UnloadContent - START");
            MyLog.Default.IncreaseIndent();

            IsLoaded = false;

            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyGuiScreenBase.UnloadContent - END");
        }

        public virtual void UnloadData()
        {
            if (DataUnloading != null)
                DataUnloading(this);
        }

        //  This method cab be overrided and used in background thread for loading/unloading the screeen
        public virtual void RunLoadingAction()
        {
            if (OnLoadingAction != null)
                OnLoadingAction();
        }

        #endregion

        #region Mouse & Input

        bool IMyGuiControlsOwner.HandleMouse { get { return true; } }

        public bool IsMouseOverAnyControl()
        {
            //  Update screen controls
            for (int i = Controls.GetVisibleControls().Count - 1; i >= 0; i--)
            {
                if (Controls.GetVisibleControls()[i].IsMouseOverAnyControl()) return true;
            }

            return false;
        }

        // Returns first control, which has mouse over
        public MyGuiControlBase GetMouseOverControl()
        {
            //  Update screen controls
            for (int i = Controls.GetVisibleControls().Count - 1; i >= 0; i--)
            {
                var control = Controls.GetVisibleControls()[i];

                var mouseOverControl = control.GetMouseOverControl();

                if (mouseOverControl != null)
                    return mouseOverControl;
            }

            return null;
        }

        public virtual void GetControlsUnderMouseCursor(Vector2 position, List<MyGuiControlBase> controls, bool visibleOnly)
        {
            GetControlsUnderMouseCursor(this, position, controls, visibleOnly);
        }

        static void GetControlsUnderMouseCursor(IMyGuiControlsParent parent, Vector2 position, List<MyGuiControlBase> controls, bool visibleOnly)
        {
            // TODO: Temporary workaround until GetVisibleControls removed
            if (visibleOnly)
            {
                foreach (var control in parent.Controls.GetVisibleControls())
                {
                    if (IsControlUnderCursor(position, control))
                    {
                        controls.Add(control);
                        control.GetElementsUnderCursor(position, visibleOnly, controls);

                        var controlParent = control as IMyGuiControlsParent;
                        if (controlParent != null)
                        {
                            GetControlsUnderMouseCursor(controlParent, position, controls, visibleOnly);
                        }
                    }
                }
            }
            else
            {
                foreach (var control in parent.Controls)
                {
                    if (IsControlUnderCursor(position, control))
                    {
                        controls.Add(control);
                        control.GetElementsUnderCursor(position, visibleOnly, controls);

                        var controlParent = control as IMyGuiControlsParent;
                        if (controlParent != null)
                        {
                            GetControlsUnderMouseCursor(controlParent, position, controls, visibleOnly);
                        }
                    }
                }
            }
        }

        //public MyGuiControlBase GetControlUnderMouseCursor()
        //{
        //    //  Update screen controls
        //    for (int i = m_controlsVisible.Count - 1; i >= 0; i--)
        //    {
        //        if (IsControlUnderCursor(MyGuiManager2.MouseCursorPosition - GetPosition(), m_controlsVisible[i])) return m_controlsVisible[i];
        //    }

        //    return null;
        //}



        MyGuiControlGridDragAndDrop GetDragAndDropHandlingNow()
        {
            for (int i = 0; i < Controls.GetVisibleControls().Count; i++)
            {
                MyGuiControlBase control = Controls.GetVisibleControls()[i];

                MyGuiControlGridDragAndDrop dragAndDrop = control.GetDragAndDropHandlingNow() as MyGuiControlGridDragAndDrop;

                if (dragAndDrop != null)
                    return dragAndDrop;
            }

            //  Not found
            return null;
        }

        MyGuiControlBase GetExclusiveInputHandler()
        {
            foreach (MyGuiControlBase control in Controls.GetVisibleControls())
            {
                MyGuiControlBase exclusiveInputHandler = control.GetExclusiveInputHandler();
                if (exclusiveInputHandler != null)
                    return exclusiveInputHandler;
            }

            return null;


            //for (int i = 0; i < m_controlsVisible.Count; i++)
            //{
            //    MyGuiControlBase control = m_controlsVisible[i];

            //    if (control is MyGuiControlCombobox)
            //    {
            //        MyGuiControlCombobox tempCombobox = (MyGuiControlCombobox)control;
            //        if (tempCombobox.GetExclusiveInputHandler() == true)
            //        {
            //            foreach (MyGuiControlBase c in m_controlsAll)
            //            {
            //                if (c is MyGuiControlCombobox && c != control)
            //                {
            //                    ((MyGuiControlCombobox)c).SetKeyboardActiveControl(false);
            //                }
            //            }
            //            return tempCombobox;
            //        }
            //    }
            //}

            //  Not found
            //return null;
        }


        private bool HandleControlsInput(bool receivedFocusInThisUpdate)
        {
            MyGuiControlBase inputHandledBySomeControl = null;

            if (m_lastHandlingControl != null && m_lastHandlingControl.Visible)
            {
                if (m_lastHandlingControl.HandleInput() != null)
                {
                    inputHandledBySomeControl = m_lastHandlingControl;
                }
            }

            if (inputHandledBySomeControl == null && m_listboxDragAndDropHandlingNow != null)
            {
                if (m_listboxDragAndDropHandlingNow.HandleInput() != null)
                {
                    inputHandledBySomeControl = m_listboxDragAndDropHandlingNow;
                }
            }

            if (inputHandledBySomeControl == null && m_comboboxHandlingNow != null)
            {
                if (m_comboboxHandlingNow.HandleInput() != null)
                {
                    inputHandledBySomeControl = m_comboboxHandlingNow;
                }
            }

            // Check focused screen first
            MyGuiControlBase mouseOverControl = null;
            if (inputHandledBySomeControl == null)
            {
                var visibleControls = Controls.GetVisibleControls();
                for (int i = 0; i < visibleControls.Count; i++)
                {
                    MyGuiControlBase control = visibleControls[i];
                    if (control != m_comboboxHandlingNow && control != m_listboxDragAndDropHandlingNow && control.CheckMouseOver())
                    {
                        mouseOverControl = control;
                        inputHandledBySomeControl = control.HandleInput();
                        break;
                    }
                }
            }

            //  If opened combobox didn't capture the input, we will try to handle it in remaining controls
            if (inputHandledBySomeControl == null)
            {
                var visibleControls = Controls.GetVisibleControls();
                for (int i = visibleControls.Count-1; i >= 0; --i)
                {
                    MyGuiControlBase control = visibleControls[i];
                    if (control != m_comboboxHandlingNow && control != m_listboxDragAndDropHandlingNow && control != mouseOverControl)
                    {
                        inputHandledBySomeControl = control.HandleInput();
                        if (inputHandledBySomeControl != null)
                            break;
                    }
                }
            }

            if (inputHandledBySomeControl == null)
            {
                foreach (var element in Elements)
                {
                    if (!element.Visible || !element.CanHaveFocus)
                        continue;

                    inputHandledBySomeControl = element.HandleInput();
                    if (inputHandledBySomeControl != null)
                        break;
                }
            }

            if (inputHandledBySomeControl != null)
                FocusedControl = inputHandledBySomeControl;

            m_lastHandlingControl = inputHandledBySomeControl;

            return inputHandledBySomeControl != null;
        }

        private MyGuiControlBase GetFirstFocusableControl()
        {
            var visibleControls = Controls.GetVisibleControls();
            foreach (var control in visibleControls)
                if (CanHaveFocusRightNow(control))
                    return control;

            foreach (var element in Elements)
                if (CanHaveFocusRightNow(element))
                    return element;

            return null;
        }

        internal static bool CanHaveFocusRightNow(MyGuiControlBase control)
        {
            return control.Enabled && control.Visible && control.CanHaveFocus;
        }

        public MyGuiControlBase GetNextFocusControl(MyGuiControlBase currentFocusControl, bool forwardMovement)
        {
            Debug.Assert(currentFocusControl.Owner == this);

            var visibleControls = Controls.GetVisibleControls();

            // Pretend that both Controls and Elements are in single array.
            int totalCount = visibleControls.Count + Elements.Count;

            int idxCurrent = visibleControls.IndexOf(currentFocusControl);
            if (idxCurrent == -1)
            {
                idxCurrent = Elements.IndexOf(currentFocusControl);
                Debug.Assert(idxCurrent != -1);
                idxCurrent += visibleControls.Count;
            }

            int sign = (forwardMovement) ? +1 : -1;
            for (int i = 1; i < totalCount; ++i)
            {
                int idx = idxCurrent + (sign * i);
                if (forwardMovement)
                    idx %= totalCount;
                else if (idx < 0)
                    idx += totalCount;

                if (idx < visibleControls.Count)
                {
                    if (CanHaveFocusRightNow(visibleControls[idx]))
                    {
                        if (visibleControls[idx] is MyGuiControlParent)
                            return visibleControls[idx].GetFocusControl(forwardMovement);
                        else
                            return visibleControls[idx];
                    }
                }
                else
                {
                    idx -= visibleControls.Count;
                    if (CanHaveFocusRightNow(Elements[idx]))
                        return Elements[idx];
                }
            }

            return null;
        }

        //  Moves active keyboard index to the next control, or previous control, or first control on the screen that can accept it
        //  forwardMovement -> set to TRUE when you want forward movement, set to FALSE when you wasnt backward
        protected bool HandleKeyboardActiveIndex(bool forwardMovement)
        {
            if (FocusedControl == null)
                return false;

            var nextFocusControl = FocusedControl.GetFocusControl(forwardMovement);
            FocusedControl = nextFocusControl;
            return true;
        }

        public virtual void HandleInput(bool receivedFocusInThisUpdate)
        {
            //  Here we can make some one-time initialization hidden in update
            bool isThisFirstHandleInput = !m_firstUpdateServed;

            if (m_firstUpdateServed == false && FocusedControl == null) //m_keyboardControlIndex could be set from constructor
            {
                FocusedControl = GetFirstFocusableControl();

                //  Never again call this update-initialization (except if RecreateControls() is called, which resets this)
                m_firstUpdateServed = true;
            }

            if (!HandleControlsInput(receivedFocusInThisUpdate))
            {
                bool handled = false;
                //  If input wasn't completely handled or captured by some control, only then we can handle screen's input
                if ((MyInput.Static.IsKeyPress(MyKeys.LeftShift) && MyInput.Static.IsNewKeyPressed(MyKeys.Tab)) ||
                    MyInput.Static.IsNewKeyPressed(MyKeys.Up) ||
                    (MyInput.Static.IsNewKeyPressed(MyKeys.Left) && !(FocusedControl is MyGuiControlSlider)) ||
                    (MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_UP) && m_defaultJoystickDpadUse))
                {
                    handled = HandleKeyboardActiveIndex(false);
                }
                else if (MyInput.Static.IsNewKeyPressed(MyKeys.Tab) ||
                    MyInput.Static.IsNewKeyPressed(MyKeys.Down) ||
                    (MyInput.Static.IsNewKeyPressed(MyKeys.Right) && !(FocusedControl is MyGuiControlSlider))||
                    (MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_DOWN) && m_defaultJoystickDpadUse))
                {
                    handled = HandleKeyboardActiveIndex(true);
                }
                else if ((m_closeOnEsc == true) && ((MyInput.Static.IsNewKeyPressed(MyKeys.Escape) || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MAIN_MENU))
                    || (m_defaultJoystickCancelUse && MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.CANCEL))))
                {
                    Canceling();
                }

                if (!handled)
                {
                    HandleUnhandledInput(receivedFocusInThisUpdate);
                }
                else if (m_defaultJoystickDpadUse && FocusedControl != null)
                {
                    if (MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_UP)
                        || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MOVE_DOWN))
                    {
                        var coords = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(FocusedControl.GetPositionAbsoluteCenter());
                        MyInput.Static.SetMousePosition((int)coords.X, (int)coords.Y);
                    }
                }
            }
        }

        public virtual void InputLost()
        {
        }

        static bool IsControlUnderCursor(Vector2 mousePosition, MyGuiControlBase control)
        {
            //TODO: Visibility
            Vector2? size = control.GetSize();
            if (size != null)
            {
                Vector2 pos = MyUtils.GetCoordCenterFromAligned(control.GetPositionAbsolute(), size.Value, control.OriginAlign);

                Vector2 min = pos - size.Value / 2;
                Vector2 max = pos + size.Value / 2;

                return (mousePosition.X >= min.X && mousePosition.X <= max.X &&
                        mousePosition.Y >= min.Y && mousePosition.Y <= max.Y);
            }
            else
            {
                return false;
            }
        }

        protected bool IsMouseOver()
        {
            Vector2 borderOffsetTopLeft = new Vector2(0.07f, 0.05f);
            Vector2 borderOffsetBottomRight = new Vector2(0.07f, 0.1f);
            Vector2 min = m_position - m_size.Value / 2 + borderOffsetTopLeft;
            Vector2 max = m_position + m_size.Value / 2 - borderOffsetBottomRight;

            return (MyGuiManager.MouseCursorPosition.X >= min.X && MyGuiManager.MouseCursorPosition.X <= max.X &&
                MyGuiManager.MouseCursorPosition.Y >= min.Y && MyGuiManager.MouseCursorPosition.Y <= max.Y);
        }

        public virtual void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            if (OnEnterCallback != null && MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
            {
                OnEnterCallback();
            }
        }

        #endregion

        #region AddCaption

        protected MyGuiControlLabel AddCaption(MyStringId textEnum, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            return AddCaption(MyTexts.GetString(textEnum), captionTextColor: captionTextColor, captionOffset: captionOffset, captionScale: captionScale);
        }

        protected MyGuiControlLabel AddCaption(String text, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            var caption = new MyGuiControlLabel(
                position: new Vector2(0, -m_size.Value.Y / 2.0f + MyGuiConstants.SCREEN_CAPTION_DELTA_Y) + (captionOffset != null ? captionOffset.Value : Vector2.Zero),
                text: text,
                colorMask: captionTextColor ?? Vector4.One,
                textScale: captionScale,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            caption.Name = "CaptionLabel";
            caption.Font = MyFontEnum.ScreenCaption;
            Elements.Add(caption);
            return caption;
        }

        #endregion

        #region Hide & Close

        /// <summary>
        /// Called when user presses ESC or clicks on CANCEL - hook to this method so you can do gui-screen-specific event
        /// </summary>
        protected virtual void Canceling()
        {
            Cancelled = true;
            if (m_closingCueEnum.HasValue)
                    MyGuiSoundManager.PlaySound(m_closingCueEnum.Value);
            else
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);
            CloseScreen();
        }

        //  Use this for closing/quiting/existing screens
        //  Returns true or false to let child implementation know if it has to run its own version of close. It's because this method
        //  should be called only once (when screen starts closing) and then never.
        //  This will close screen with fade-out effect
        public virtual bool CloseScreen()
        {
            if ((State == MyGuiScreenState.CLOSING) || (State == MyGuiScreenState.CLOSED))
            {
                return false;
            }
            else
            {
                State = MyGuiScreenState.CLOSING;
                m_lastTransitionTime = MyGuiManager.TotalTimeInMilliseconds;
                return true;
            }
        }

        // Used in case, when screen is not closing, but needs to be transitioned out
        public virtual bool HideScreen()
        {
            if ((State == MyGuiScreenState.HIDING) || (State == MyGuiScreenState.HIDDEN) || (State == MyGuiScreenState.OPENING))
            {
                return false;
            }
            else
            {
                State = MyGuiScreenState.HIDING;
                m_lastTransitionTime = MyGuiManager.TotalTimeInMilliseconds;
                return true;
            }
        }

        // Used in case, when screen is hidden, and needs to be transitioned in
        public virtual bool UnhideScreen()
        {
            if ((State == MyGuiScreenState.UNHIDING) || (State == MyGuiScreenState.OPENED))
            {
                return false;
            }
            else
            {
                State = MyGuiScreenState.UNHIDING;
                m_lastTransitionTime = MyGuiManager.TotalTimeInMilliseconds;
                return true;
            }
        }

        //  This will close/remove screen instantly, without fade-out effect
        public virtual void CloseScreenNow()
        {
            if (State == MyGuiScreenState.CLOSED)
            {
                return;
            }

            State = MyGuiScreenState.CLOSED;

            //  Notify GUI manager that this screen should be removed from the list
            //RemoveScreen(this);

            OnClosed();

            if (Closed != null)
            {
                Closed(this);
                Closed = null;
            }
        }

        #endregion

        #region Update

        private void UpdateControls()
        {
            //  Update screen controls
            foreach (var control in Controls.GetVisibleControls())
            {
                control.Update();
            }

            foreach (var element in Elements)
                element.Update();

            m_comboboxHandlingNow = GetExclusiveInputHandler();
            m_listboxDragAndDropHandlingNow = GetDragAndDropHandlingNow();
        }


        //  Returns true or false to let child implementation know if it has to run its own version of update.
        public virtual bool Update(bool hasFocus)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiScreenBase");

            if (UpdateTransition())
            {
                //FillControlsResurcive();
                UpdateControls();

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return true;
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            return false;
        }

        bool UpdateTransition()
        {
            if (State == MyGuiScreenState.OPENING || State == MyGuiScreenState.UNHIDING)
            {
                int deltaTime = MyGuiManager.TotalTimeInMilliseconds - m_lastTransitionTime;

                // Play opening sound
                if ((State == MyGuiScreenState.OPENING) && (m_openingCueEnum != null))// && (m_openingCue == null))
                {
                    MyGuiSoundManager.PlaySound(m_openingCueEnum.Value);
                }

                if (deltaTime >= GetTransitionOpeningTime())
                {
                    //  Opening phase finished, we are now in active state
                    State = MyGuiScreenState.OPENED;
                    m_transitionAlpha = MyGuiConstants.TRANSITION_ALPHA_MAX;
                    OnShow();
                }
                else
                {
                    m_transitionAlpha = MathHelper.Lerp(MyGuiConstants.TRANSITION_ALPHA_MIN, MyGuiConstants.TRANSITION_ALPHA_MAX, MathHelper.Clamp((float)deltaTime / (float)GetTransitionOpeningTime(), 0, 1));
                }
            }
            else if (State == MyGuiScreenState.CLOSING || State == MyGuiScreenState.HIDING)
            {
                int deltaTime = MyGuiManager.TotalTimeInMilliseconds - m_lastTransitionTime;

                if (deltaTime >= GetTransitionClosingTime())
                {
                    m_transitionAlpha = MyGuiConstants.TRANSITION_ALPHA_MIN;

                    //  Closing phase finished, we are now in close state
                    if (State == MyGuiScreenState.CLOSING)
                    {
                        CloseScreenNow();
                        return false;
                    }
                    else if (State == MyGuiScreenState.HIDING)
                    {
                        State = MyGuiScreenState.HIDDEN;

                        OnHide();
                    }
                }
                else
                {
                    m_transitionAlpha = MathHelper.Lerp(MyGuiConstants.TRANSITION_ALPHA_MAX, MyGuiConstants.TRANSITION_ALPHA_MIN, MathHelper.Clamp((float)deltaTime / (float)GetTransitionClosingTime(), 0, 1));
                }
            }

            return true;
        }

        #endregion

        #region Draw

        //  Returns true or false to let child implementation know if it has to run its own version of draw.
        public virtual bool Draw()
        {
            //  This is just background of the screen rectangle
            if ((m_backgroundColor.HasValue) && (m_size.HasValue))
            {
                //  Background texture
                if (m_backgroundTexture == null)
                {
                    if (m_size.HasValue)
                    {
                        //  If this screen doesn't have custom texture, we will use one of the default - but with respect to screen's aspect ratio
                        m_backgroundTexture = MyGuiManager.GetBackgroundTextureFilenameByAspectRatio(m_size.Value);
                    }
                }

                MyGuiManager.DrawSpriteBatch(m_backgroundTexture, m_position, m_size.Value, ApplyTransitionAlpha(m_backgroundColor.Value, m_guiTransition != 0 ? m_backgroundTransition : m_transitionAlpha), MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                //if (MyFakes.DRAW_GUI_SCREEN_BORDERS && MyFinalBuildConstants.IS_DEBUG)
                //{
                //    MyGuiManager2.DrawBorders(GetPositionAbsoluteTopLeft(), m_size.Value, Color.White, 1);
                //}
            }
            if (m_guiTransition != 0)
            {
                DrawElements(m_guiTransition, m_backgroundTransition);
                DrawControls(m_guiTransition, m_backgroundTransition);
            }
            else
            {
                DrawElements(m_transitionAlpha, m_transitionAlpha);
                DrawControls(m_transitionAlpha, m_transitionAlpha);
            }
            return true;
        }

        private void DrawElements(float transitionAlpha, float backgroundTransitionAlpha)
        {
            foreach (var element in Elements)
            {
                if (element.Visible)
                    element.Draw(transitionAlpha, backgroundTransitionAlpha);
            }
        }

        private void DrawControls(float transitionAlpha, float backgroundTransitionAlpha)
        {
            //  Then draw all screen controls, except opened combobox and drag and drop - must be drawn as last
            // foreach (MyGuiControlBase control in Controls.GetVisibleControls())  //dont use this - allocations
            List<MyGuiControlBase> visibleControls = Controls.GetVisibleControls();
            for (int i = 0; i < visibleControls.Count; i++)
            {
                MyGuiControlBase control = visibleControls[i];
                if (control != m_comboboxHandlingNow && control != m_listboxDragAndDropHandlingNow)
                {
                    //if (MySandboxGame.IsPaused && !control.DrawWhilePaused) continue;
                    control.Draw(transitionAlpha, backgroundTransitionAlpha);
                }
            }

            //  Finaly draw opened combobox and dragAndDrop, so it will overdraw all other controls

            if (m_comboboxHandlingNow != null)
            {
                m_comboboxHandlingNow.Draw(transitionAlpha, backgroundTransitionAlpha);
            }

            if (m_listboxDragAndDropHandlingNow != null)
            {
                m_listboxDragAndDropHandlingNow.Draw(transitionAlpha, backgroundTransitionAlpha);
            }

            // draw tooltips only when screen has focus
            if (this == MyScreenManager.GetScreenWithFocus())
            {
                //  Draw tooltips
                foreach (var control in Controls.GetVisibleControls())
                {
                    control.ShowToolTip();
                }
            }
        }

        // Used in case, when new screen is added to hide tooltips of other screens
        public void HideTooltips()
        {
            foreach (var control in Controls)
            {
                control.HideToolTip();
            }
        }


        #endregion

        #region Properties

        //Properties
        public bool SkipTransition { get; set; }

        public bool Cancelled { get; private set; }

        protected bool DrawMouseCursor
        {
            set
            {
                /*
                if (value == false && MyGuiManager2.GetScreenWithFocus() == this)
                    MyGuiInput.SetMouseToScreenCenter();//preclear this
                */
                m_drawMouseCursor = value;
            }
            get
            {
                return m_drawMouseCursor;
            }
        }

        public bool JoystickAsMouse
        {
            get { return m_joystickAsMouse; }
            set { m_joystickAsMouse = value; }
        }

        public Vector2 GetPositionAbsolute()
        {
            return m_position;
        }

        public Vector2 GetPositionAbsoluteCenter()
        {
            return GetPositionAbsolute();
        }

        public Vector2 GetPositionAbsoluteTopLeft()
        {
            if (Size.HasValue)
                return GetPositionAbsolute() - Size.Value * 0.5f;
            else
                return GetPositionAbsolute();
        }

        private MyGuiScreenState m_state;
        public MyGuiScreenState State
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    bool visibleBefore = Visible;
                    m_state = value;
                    if (VisibleChanged != null && Visible != visibleBefore)
                        VisibleChanged(this, Visible);
                }
            }
        }

        public bool GetDrawMouseCursor()
        {
            return m_drawMouseCursor;
        }

        //  If true then no other screen can be added above this one. They will be always added below.
        public bool IsTopMostScreen()
        {
            return m_isTopMostScreen;
        }

        public bool IsAlwaysFirst()
        {
            return m_isAlwaysFirst;
        }

        public bool IsTopScreen()
        {
            return m_isTopScreen;
        }

        public bool IsFirstForUnload()
        {
            return m_isFirstForUnload;
        }

        public bool GetDrawScreenEvenWithoutFocus()
        {
            return m_drawEvenWithoutFocus;
        }

        //  This will tell us if LoadContent was finished so we can start using this screen
        //  It's used only for screens that have huge LoadContent - e.g. game play screen. Other don't have to use it.
        public bool IsLoaded
        {
            set
            {
                lock (m_isLoadedLock)
                {
                    m_isLoaded = value;
                }
            }

            get
            {
                bool ret;
                lock (m_isLoadedLock)
                {
                    ret = m_isLoaded;
                }
                return ret;
            }
        }

        public Vector2 GetPosition()
        {
            return m_position;
        }

        //  Changes color according to transition alpha, this when opening the screen - it from 100% transparent to 100% opaque. When closing it's opposite.
        protected Color ApplyTransitionAlpha(Vector4 color, float transition)
        {
            Vector4 ret = color;
            ret.W *= transition;
            return new Color(ret);
        }

        /// <summary>
        /// Draw fade rectangle under top-most screen?
        /// </summary>
        public bool EnabledBackgroundFade
        {
            get { return m_enabledBackgroundFade; }
            protected set { m_enabledBackgroundFade = value; }
        }
        private bool m_enabledBackgroundFade = false;

        public Vector2? GetSize()
        {
            return m_size;
        }

        public bool CanShareInput()
        {
            return m_canShareInput;
        }

        /// <summary>
        /// Indicates whether screen can be hidden when another screen is on
        /// top of it (assuming that screen hides screens below it).
        /// </summary>
        public bool CanBeHidden
        {
            get { return m_canBeHidden; }
            protected set { m_canBeHidden = value; }
        }
        private bool m_canBeHidden = true;

        /// <summary>
        /// Indicates whether screen can hide screens below it. This will not 
        /// hide screens that cannot be hidden.
        /// </summary>
        public bool CanHideOthers
        {
            get { return m_canHideOthers; }
            protected set { m_canHideOthers = value; }
        }
        private bool m_canHideOthers = true;

        /// <summary>
        /// Without focus, this screen will not steal input.
        /// </summary>
        public bool CanHaveFocus
        {
            get { return m_canHaveFocus; }
            protected set { m_canHaveFocus = value; }
        }
        private bool m_canHaveFocus = true;

        public bool CanCloseInCloseAllScreenCalls()
        {
            return m_canCloseInCloseAllScreenCalls;
        }


        public MyGuiControls Controls
        {
            get { return m_controls; }
        }

        public Vector4? BackgroundColor
        {
            get { return m_backgroundColor; }
            set { m_backgroundColor = value; }
        }

        public Vector2? Size
        {
            get { return m_size; }
            set { m_size = value; }
        }

        public bool Visible { get { return State != MyGuiScreenState.HIDDEN; } }

        private Vector2 m_closeButtonOffset;
        public Vector2 CloseButtonOffset
        {
            get { return m_closeButtonOffset; }
            set
            {
                if (m_closeButtonOffset != value)
                {
                    m_closeButtonOffset = value;
                    if (m_closeButton != null)
                        m_closeButton.Position = CalcCloseButtonPosition();
                }
            }
        }

        private bool m_closeButtonEnabled = false;
        public bool CloseButtonEnabled
        {
            get { return m_closeButtonEnabled; }
            set
            {
                m_closeButtonEnabled = value;
                if (m_closeButton != null)
                {
                    m_closeButton.Visible = value;
                    m_closeButton.Position = CalcCloseButtonPosition();
                }
            }
        }

        private MyGuiControlBase m_focusedControl;
        public MyGuiControlBase FocusedControl
        {
            get { return m_focusedControl; }
            set
            {
                m_focusedControl = value;
            }
        }

        public string DebugNamePath
        {
            get { return GetFriendlyName(); }
        }

        #endregion

        #region Virtuals

        /// <summary>
        /// For displaying in the list in the debug screen.
        /// </summary>
        /// <returns></returns>
        public abstract string GetFriendlyName();

        public virtual void RecreateControls(bool constructor)
        {
            Controls.Clear();
            Elements.Clear();
            Elements.Add(m_closeButton);
            FocusedControl = null;
            m_firstUpdateServed = false;
        }

        public virtual int GetTransitionOpeningTime()
        {
            if (SkipTransition) return 0;
            return MyGuiConstants.TRANSITION_OPENING_TIME;
        }

        public virtual int GetTransitionClosingTime()
        {
            if (SkipTransition) return 0;
            return MyGuiConstants.TRANSITION_CLOSING_TIME;
        }

        /// <summary>
        /// Called when [show].
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called when [show].
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// Called when [show].
        /// </summary>
        public virtual void OnRemoved() { }

        /// <summary>
        /// Called when [show].
        /// </summary>
        protected virtual void OnClosed()
        {
            ProfilerShort.Begin("MyGuiScreenBase.OnClosed()");
            Controls.Clear();
            foreach (var element in Elements)
                element.OnRemoving();
            Elements.Clear();
            ProfilerShort.End();
        }

        #endregion

        #region Helpers

        protected static string MakeScreenFilepath(string name)
        {
            return Path.Combine("Data", "Screens", name + ".gsc");
        }

        private void closeButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }

        private void CreateCloseButton()
        {
            m_closeButton = new MyGuiControlButton()
            {
                Name = "CloseButton",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                VisualStyle = MyGuiControlButtonStyleEnum.Close,
                TextScale = 0,
                Position = CalcCloseButtonPosition(),
                Visible = CloseButtonEnabled,
            };
            m_closeButton.ButtonClicked += closeButton_OnButtonClick;
            Elements.Add(m_closeButton);
        }

        private Vector2 CalcCloseButtonPosition()
        {
            return (Size ?? Vector2.One) * new Vector2(0.5f, -0.5f) + CloseButtonOffset;
        }

        /// <summary>
        /// Computes ratio of width and height in GUI coordinates to make X and Y of
        /// equal value represent same distance on screen as well. Then multiplies Y using this ratio.
        /// </summary>
        protected void MakeXAndYEqual(ref Vector2 val)
        {
            var size = Size ?? Vector2.One;
            var ratio = (size.Y / size.X) * (4f / 3f);
            val.Y *= ratio;
        }

        protected Vector2 MakeXAndYEqual(Vector2 val)
        {
            MakeXAndYEqual(ref val);
            return val;
        }

        protected void SetDefaultCloseButtonOffset()
        {
            CloseButtonOffset = MakeXAndYEqual(new Vector2(-0.010f, 0.010f));
        }
        #endregion

    }
}
