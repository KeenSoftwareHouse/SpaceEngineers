using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public class MyHudScreenEffects
    {
        #region Fields

        //black screen (fade-in, fade-out)
        private float m_blackScreenCurrent = 1f;
        private float m_blackScreenStart = 0f;
        private float m_blackScreenTimeIncrement = 0f;
        private float m_blackScreenTimeTimer = 0f;
        private float m_blackScreenTarget = 1f;
        private bool m_blackScreenDataSaved = false;
        private Color m_blackScreenDataSavedLightColor = Color.Black;
        private Color m_blackScreenDataSavedDarkColor = Color.Black;
        private float m_blackScreenDataSavedStrength = 0f;
        public float BlackScreenCurrent { get { return m_blackScreenCurrent; } }
        public bool BlackScreenMinimalizeHUD = true;
        public Color BlackScreenColor = Color.Black;

        #endregion

        #region Update

        public void Update()
        {
            UpdateBlackScreen();
        }

        #endregion

        #region BlackScreen

        //start screen fading over given time (0 = instant change).
        public void FadeScreen(float targetAlpha, float time = 0f)
        {
            targetAlpha = MathHelper.Clamp(targetAlpha, 0f, 1f);
            if (time <= 0f)
            {
                m_blackScreenTarget = targetAlpha;
                m_blackScreenCurrent = targetAlpha;
            }
            else
            {
                m_blackScreenTarget = targetAlpha;
                m_blackScreenStart = m_blackScreenCurrent;
                m_blackScreenTimeTimer = 0;
                m_blackScreenTimeIncrement = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / time;
            }
            if (targetAlpha < 1f && !m_blackScreenDataSaved)
            {
                m_blackScreenDataSaved = true;
                m_blackScreenDataSavedLightColor = MyPostprocessSettingsWrapper.Settings.Data.LightColor;
                m_blackScreenDataSavedDarkColor = MyPostprocessSettingsWrapper.Settings.Data.DarkColor;
                m_blackScreenDataSavedStrength = MyPostprocessSettingsWrapper.Settings.Data.SepiaStrength;
            }
        }

        //start screen fading to opposite value
        public void SwitchFadeScreen(float time = 0)
        {
            FadeScreen(1f - m_blackScreenTarget, time);
        }

        //black screen update (can be set to other colors as well)
        private void UpdateBlackScreen()
        {
            if (m_blackScreenTimeTimer < 1f && m_blackScreenCurrent != m_blackScreenTarget)
            {
                m_blackScreenTimeTimer += m_blackScreenTimeIncrement;
                if (m_blackScreenTimeTimer > 1)
                    m_blackScreenTimeTimer = 1;
                m_blackScreenCurrent = MathHelper.Lerp(m_blackScreenStart, m_blackScreenTarget, m_blackScreenTimeTimer);
            }
            if (m_blackScreenCurrent < 1f)
            {
                if (BlackScreenMinimalizeHUD)
                    MyHud.CutsceneHud = true;
                MyPostprocessSettingsWrapper.Settings.Data.LightColor = BlackScreenColor;
                MyPostprocessSettingsWrapper.Settings.Data.DarkColor = BlackScreenColor;
                MyPostprocessSettingsWrapper.Settings.Data.SepiaStrength = 1f - m_blackScreenCurrent;
            }
            else if (m_blackScreenDataSaved)
            {
                m_blackScreenDataSaved = false;
                MyHud.CutsceneHud = MySession.Static.GetComponent<MySessionComponentCutscenes>().IsCutsceneRunning;
                MyPostprocessSettingsWrapper.Settings.Data.LightColor = m_blackScreenDataSavedLightColor;
                MyPostprocessSettingsWrapper.Settings.Data.DarkColor = m_blackScreenDataSavedDarkColor;
                MyPostprocessSettingsWrapper.Settings.Data.SepiaStrength = m_blackScreenDataSavedStrength;
            }
        }

        #endregion
    }
}
