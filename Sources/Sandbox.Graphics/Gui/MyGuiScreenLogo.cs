using VRage.Input;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public class MyGuiScreenLogo: MyGuiScreenBase
    {
        private int? m_startTime;
        private string m_textureName;

        private int m_fadeIn, m_fadeOut, m_openTime;
        private float m_scale;

        /// <summary>
        /// Time in ms
        /// </summary>
        public MyGuiScreenLogo(string texture, float scale = 0.66f, int fadeIn = 300, int fadeOut = 300, int openTime = 300)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, Vector2.One)
        {
            this.m_scale = scale;
            this.m_fadeIn = fadeIn;
            this.m_fadeOut = fadeOut;
            this.m_openTime = openTime;
            DrawMouseCursor = false;
            m_textureName = texture;
            m_closeOnEsc = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            VRageRender.MyRenderProxy.UnloadTexture(m_textureName);

            base.UnloadContent();
        }

        protected override void Canceling()
        {
            this.m_fadeOut = 0;
            base.Canceling();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewLeftMousePressed() || MyInput.Static.IsNewRightMousePressed() || MyInput.Static.IsNewKeyPressed(MyKeys.Space) || MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
                Canceling();
        }

        public override string GetFriendlyName()
        {
            return "Logo screen";
        }

        public override int GetTransitionOpeningTime()
        {
            return m_fadeIn;
        }

        public override int GetTransitionClosingTime()
        {
            return m_fadeOut;
        }

        public override bool Update(bool hasFocus)
        {
            if (base.Update(hasFocus) == false) return false;

            if(State == MyGuiScreenState.OPENED && !m_startTime.HasValue)
                m_startTime = MyGuiManager.TotalTimeInMilliseconds;

            if (m_startTime.HasValue && MyGuiManager.TotalTimeInMilliseconds > (m_startTime + m_openTime))
                this.CloseScreen();
                          
            return true;
        }

        public override bool Draw()
        {
            //Rectangle backgroundRectangle;
         
            //TODO:
            //MyGuiManager2.GetSafeAspectRatioFullScreenPictureSize(MyGuiConstants.LOADING_BACKGROUND_TEXTURE_REAL_SIZE, out backgroundRectangle);
            //backgroundRectangle.Inflate(-(int)(backgroundRectangle.Width * (1 - m_scale) / 2), -((int)(backgroundRectangle.Height * (1 - m_scale) / 2)));

            //MyGuiManager2.DrawSpriteBatch(m_textureName, backgroundRectangle, new Color(new Vector4(0.95f, 0.95f, 0.95f, m_transitionAlpha)));

            return true;
        }
    }
}
