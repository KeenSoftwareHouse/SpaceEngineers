using System.Text;
using VRageMath;
using Sandbox;

using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;

using Sandbox.Common;
using VRage;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using VRage;
using VRage.Audio;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenOptionsAudio : MyGuiScreenBase
    {
        class MyGuiScreenOptionsAudioSettings
        {
            public float GameVolume;
            public float MusicVolume;
            public bool HudWarnings;          
            public bool EnableVoiceChat;
        }

        MyGuiControlSlider m_gameVolumeSlider;
        MyGuiControlSlider m_musicVolumeSlider;
        MyGuiControlCheckbox m_hudWarnings;
        MyGuiControlCheckbox m_enableVoiceChat;
        MyGuiScreenOptionsAudioSettings m_settingsOld = new MyGuiScreenOptionsAudioSettings();
        MyGuiScreenOptionsAudioSettings m_settingsNew = new MyGuiScreenOptionsAudioSettings();

        private bool m_gameAudioPausedWhenOpen;
        
        public MyGuiScreenOptionsAudio()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(1030f , 572f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            EnabledBackgroundFade = true;

            AddCaption(MySpaceTexts.ScreenCaptionAudioOptions);

            var topLeft = m_size.Value * -0.5f;
            var topCenter = m_size.Value * new Vector2(0f, -0.5f);
            var bottomCenter = m_size.Value * new Vector2(0f, 0.6f);

            Vector2 controlsOriginLeft = topLeft + new Vector2(110f, 170f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsOriginRight = topCenter + new Vector2(-25f, 170f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsDelta = new Vector2(0f, 60f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            //  Game Volume
            Controls.Add(new MyGuiControlLabel(
                position: controlsOriginLeft + 0 * controlsDelta,
                text: MyTexts.GetString(MySpaceTexts.GameVolume),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_gameVolumeSlider = new MyGuiControlSlider(
                position: controlsOriginRight + 0 * controlsDelta,
                minValue: MyAudioConstants.GAME_MASTER_VOLUME_MIN,
                maxValue: MyAudioConstants.GAME_MASTER_VOLUME_MAX,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_gameVolumeSlider.ValueChanged = OnGameVolumeChange;
            Controls.Add(m_gameVolumeSlider);

            //  Music Volume
            Controls.Add(new MyGuiControlLabel(
                position: controlsOriginLeft + 1 * controlsDelta,
                text: MyTexts.GetString(MySpaceTexts.MusicVolume),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_musicVolumeSlider = new MyGuiControlSlider(
                position: controlsOriginRight + 1 * controlsDelta,
                minValue: MyAudioConstants.MUSIC_MASTER_VOLUME_MIN,
                maxValue: MyAudioConstants.MUSIC_MASTER_VOLUME_MAX,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_musicVolumeSlider.ValueChanged = OnMusicVolumeChange;
            Controls.Add(m_musicVolumeSlider);

            Controls.Add(new MyGuiControlLabel(
                position: controlsOriginLeft + 2 * controlsDelta,
                text: MyTexts.GetString(MySpaceTexts.HudWarnings),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_hudWarnings = new MyGuiControlCheckbox(
                position: controlsOriginRight + 2 * controlsDelta,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_hudWarnings.IsCheckedChanged = HudWarningsChecked;
            Controls.Add(m_hudWarnings);

            // Voice chat
            if (MyPerGameSettings.VoiceChatEnabled)
            {
                Controls.Add(new MyGuiControlLabel(
                    position: controlsOriginLeft + 3 * controlsDelta,
                    text: MyTexts.GetString(MySpaceTexts.EnableVoiceChat),
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            }
            m_enableVoiceChat = new MyGuiControlCheckbox(
                position: controlsOriginRight + 3 * controlsDelta,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_enableVoiceChat.IsCheckedChanged = VoiceChatChecked;
            if (MyPerGameSettings.VoiceChatEnabled)
                Controls.Add(m_enableVoiceChat);

            //  Buttons OK and CANCEL

            var m_okButton = new MyGuiControlButton(
                position: bottomCenter + new Vector2(-75f, -130f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MySpaceTexts.Ok),
                onButtonClick: OnOkClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            Controls.Add(m_okButton);

            var m_cancelButton = new MyGuiControlButton(
                position: bottomCenter + new Vector2(75f, -130f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MySpaceTexts.Cancel),
                onButtonClick: OnCancelClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            Controls.Add(m_cancelButton);


            //  Update controls with values from config file
            UpdateFromConfig(m_settingsOld);
            UpdateFromConfig(m_settingsNew);
            UpdateControls(m_settingsOld);

            CloseButtonEnabled = true;
            CloseButtonOffset = MakeXAndYEqual(new Vector2(-0.006f, 0.006f));

            m_gameAudioPausedWhenOpen = MyAudio.Static.GameSoundIsPaused;
            if (m_gameAudioPausedWhenOpen)
                MyAudio.Static.ResumeGameSounds();
        }

        private void VoiceChatChecked(MyGuiControlCheckbox checkbox)
        {
            m_settingsNew.EnableVoiceChat = checkbox.IsChecked;
        }

        private void HudWarningsChecked(MyGuiControlCheckbox obj)
        {
            m_settingsNew.HudWarnings = obj.IsChecked;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsAudio";
        }

        void UpdateFromConfig(MyGuiScreenOptionsAudioSettings settings)
        {
            settings.GameVolume = MySandboxGame.Config.GameVolume;
            settings.MusicVolume = MySandboxGame.Config.MusicVolume;
            settings.HudWarnings = MySandboxGame.Config.HudWarnings;
            settings.EnableVoiceChat = MySandboxGame.Config.EnableVoiceChat;
        }

        //void UpdateSettings(MyGuiScreenOptionsVideoSettings settings)
        //{
        //    settings.AspectRatio = (MyAspectRatioEnum)m_aspectRationCombobox.GetSelectedKey();
        //}

        void UpdateControls(MyGuiScreenOptionsAudioSettings settings)
        {
            m_gameVolumeSlider.Value = settings.GameVolume;
            m_musicVolumeSlider.Value = settings.MusicVolume;
            m_hudWarnings.IsChecked = settings.HudWarnings;
            m_enableVoiceChat.IsChecked = settings.EnableVoiceChat;
        }

        void Save()
        {
            MySandboxGame.Config.GameVolume = m_gameVolumeSlider.Value;
            MySandboxGame.Config.MusicVolume = m_musicVolumeSlider.Value;
            MySandboxGame.Config.HudWarnings = m_hudWarnings.IsChecked;
            MySandboxGame.Config.EnableVoiceChat = m_enableVoiceChat.IsChecked;
            MySandboxGame.Config.Save();
        }

        static void UpdateValues(MyGuiScreenOptionsAudioSettings settings)
        {
            MyAudio.Static.VolumeMusic = settings.MusicVolume;
            MyAudio.Static.VolumeGame = settings.GameVolume;
            MyAudio.Static.VolumeHud = settings.GameVolume;
            MyAudio.Static.EnableVoiceChat = settings.EnableVoiceChat;
            MyGuiAudio.HudWarnings = settings.HudWarnings;
        }

        public void OnOkClick(MyGuiControlButton sender)
        {
            //  Save values
            Save();

            CloseScreen();
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            //  Revert to OLD values
            UpdateValues(m_settingsOld);
            
            CloseScreen();
        }

        void OnGameVolumeChange(MyGuiControlSlider sender)
        {
            m_settingsNew.GameVolume = m_gameVolumeSlider.Value;
            UpdateValues(m_settingsNew);
        }

        void OnMusicVolumeChange(MyGuiControlSlider sender)
        {
            m_settingsNew.MusicVolume = m_musicVolumeSlider.Value;
            UpdateValues(m_settingsNew);
        }

        public override bool CloseScreen()
        {
            UpdateFromConfig(m_settingsOld);
            UpdateValues(m_settingsOld);
            if (m_gameAudioPausedWhenOpen)
                MyAudio.Static.PauseGameSounds();

            return base.CloseScreen();
        }
    }
}
