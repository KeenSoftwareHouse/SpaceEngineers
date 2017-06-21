using System;
using VRage;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public abstract class MyGuiScreenProgressBase : MyGuiScreenBase
    {
        bool m_controlsCreated = false;
        bool m_loaded = false;
        MyStringId m_progressText;
        string m_progressTextString;
        MyStringId? m_cancelText;
        protected MyGuiControlLabel m_progressTextLabel;
        protected MyGuiControlRotatingWheel m_rotatingWheel;

        public MyGuiControlRotatingWheel RotatingWheel
        {
            get { return m_rotatingWheel; }
        }

        string m_wheelTexture;

        public event Action ProgressCancelled;

        public MyStringId ProgressText
        {
            get { return m_progressText; }
            set
            {
                if (m_progressText != value)
                {
                    m_progressText = value;
                    m_progressTextLabel.TextEnum = value;
                }
            }
        }

        public String ProgressTextString
        {
            get { return m_progressTextString; }
            set
            {
                if (m_progressTextString != value)
                {
                    m_progressTextString = value;
                    m_progressTextLabel.Text = value;
                }
            }
        }

        public MyGuiScreenProgressBase(MyStringId progressText, MyStringId? cancelText = null) :
            base(position: new Vector2(0.5f, 0.5f),
                 backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                 isTopMostScreen: true)
        {
            m_progressText = progressText;
            m_cancelText = cancelText;

            EnabledBackgroundFade = true;
            DrawMouseCursor = m_cancelText.HasValue;
            m_closeOnEsc = m_cancelText.HasValue;
           
            m_drawEvenWithoutFocus = true;
            CanHideOthers = false;
            
            // There is no reason for hiding progress screens!
            CanBeHidden = false;
            RecreateControls(true);
        }

        protected bool ReturnToMainMenuOnError = false;

        protected virtual void OnCancelClick(MyGuiControlButton sender)
        {
            Canceling();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            m_controlsCreated = false;
            LoadControls();
        }

        void LoadControls()
        {
            // Background texture unloaded in base
            m_wheelTexture = MyGuiConstants.LOADING_TEXTURE;

            m_size = new Vector2(598 / 1600f, 368 / 1200f);
            m_progressTextLabel = new MyGuiControlLabel(
                position: new Vector2(0.0f, -0.07f),
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE * 0.86f,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_progressTextLabel.TextEnum = m_progressText;
            Controls.Add(m_progressTextLabel);

            float deltaX = 0;// (m_enableCancel) ? 0.08f : 0.0f;
            float deltaY = 0.015f;

            m_rotatingWheel = new MyGuiControlRotatingWheel(new Vector2(-deltaX, deltaY), MyGuiConstants.ROTATING_WHEEL_COLOR, MyGuiConstants.ROTATING_WHEEL_DEFAULT_SCALE,
                MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, m_wheelTexture);

            Controls.Add(m_rotatingWheel);

            //  Sometimes we don't want to allow user to cancel pending progress screen
            if (m_cancelText.HasValue)
            {
                var cancelButton = new MyGuiControlButton(
                    position: new Vector2(deltaX, deltaY + 0.09f),
                    size: MyGuiConstants.BACK_BUTTON_SIZE,
                    text: MyTexts.Get(m_cancelText.Value),
                    visualStyle: MyGuiControlButtonStyleEnum.ControlSetting,
                    onButtonClick: OnCancelClick);
                Controls.Add(cancelButton);
            }
            m_controlsCreated = true;
        }

        public void Cancel()
        {
            Canceling();
        }

        #region Overrides of MyGuiScreenBase

        protected override void Canceling()
        {
            base.Canceling();
            var handler = ProgressCancelled;
            if (handler != null) handler();

            handler = null; // Clear it to prevent GC holding references
        }

        public override bool Draw()
        {
            // Load in draw, because sometimes screen is invisible and saving is on background
            if (!m_controlsCreated)
            {
                LoadControls();
            }

            return base.Draw();
        }

        public override void LoadContent()
        {
            if (!m_loaded)
            {
                m_loaded = true;
                ProgressStart();
            }
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            m_loaded = false;

            base.UnloadContent();
        }

        protected abstract void ProgressStart();

        #endregion
    }
}
