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
            public float VoiceChatVolume;
            public bool HudWarnings;          
            public bool EnableVoiceChat;
            public bool EnableMuteWhenNotInFocus;
        }

        MyGuiControlSlider m_gameVolumeSlider;
        MyGuiControlSlider m_musicVolumeSlider;
        MyGuiControlSlider m_voiceChatVolumeSlider;
        MyGuiControlCheckbox m_hudWarnings;
        MyGuiControlCheckbox m_enableVoiceChat;
        MyGuiControlCheckbox m_enableMuteWhenNotInFocus;
        MyGuiScreenOptionsAudioSettings m_settingsOld = new MyGuiScreenOptionsAudioSettings();
        MyGuiScreenOptionsAudioSettings m_settingsNew = new MyGuiScreenOptionsAudioSettings();

        private bool m_gameAudioPausedWhenOpen;
        
        public MyGuiScreenOptionsAudio()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(1030f , 572f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            EnabledBackgroundFade = true;

            AddCaption(MyCommonTexts.ScreenCaptionAudioOptions);

            var topLeft = m_size.Value * -0.5f;
            var topCenter = m_size.Value * new Vector2(0f, -0.5f);
            var bottomCenter = m_size.Value * (MyPerGameSettings.VoiceChatEnabled ? new Vector2(0f, 0.7f) : new Vector2(0f, 0.6f));
            float startHeight = MyPerGameSettings.VoiceChatEnabled? 150f : 170f;

            Vector2 controlsOriginLeft = topLeft + new Vector2(110f, startHeight) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsOriginRight = topCenter + new Vector2(-25f, startHeight) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            Vector2 controlsDelta = new Vector2(0f, 60f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            //  Game Volume
            Controls.Add(new MyGuiControlLabel(
                position: controlsOriginLeft + 0 * controlsDelta,
                text: MyTexts.GetString(MyCommonTexts.GameVolume),
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
                text: MyTexts.GetString(MyCommonTexts.MusicVolume),
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
                text: MyTexts.GetString(MyCommonTexts.HudWarnings),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_hudWarnings = new MyGuiControlCheckbox(
                position: controlsOriginRight + 2 * controlsDelta,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_hudWarnings.IsCheckedChanged = HudWarningsChecked;
            Controls.Add(m_hudWarnings);

            Controls.Add(new MyGuiControlLabel(
                position: controlsOriginLeft + 3 * controlsDelta,
                text: MyTexts.GetString(MyCommonTexts.MuteWhenNotInFocus),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            m_enableMuteWhenNotInFocus = new MyGuiControlCheckbox(
                position: controlsOriginRight + 3 * controlsDelta,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_enableMuteWhenNotInFocus.IsCheckedChanged = EnableMuteWhenNotInFocusChecked;
            Controls.Add(m_enableMuteWhenNotInFocus);

            // Voice chat
            if (MyPerGameSettings.VoiceChatEnabled)
            {
                Controls.Add(new MyGuiControlLabel(
                    position: controlsOriginLeft + 4 * controlsDelta,
                    text: MyTexts.GetString(MyCommonTexts.EnableVoiceChat),
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            }
            m_enableVoiceChat = new MyGuiControlCheckbox(
                position: controlsOriginRight + 4 * controlsDelta,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_enableVoiceChat.IsCheckedChanged = VoiceChatChecked;

            m_voiceChatVolumeSlider = new MyGuiControlSlider(
                position: controlsOriginRight + 5 * controlsDelta,
                minValue: MyAudioConstants.VOICE_CHAT_VOLUME_MIN,
                maxValue: MyAudioConstants.VOICE_CHAT_VOLUME_MAX,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_voiceChatVolumeSlider.ValueChanged = OnVoiceChatVolumeChange;

            if (MyPerGameSettings.VoiceChatEnabled)
            {
                // voice char checkbox
                Controls.Add(m_enableVoiceChat);

                // label for voice chat
                Controls.Add(new MyGuiControlLabel(
                    position: controlsOriginLeft + 5 * controlsDelta,
                    text: MyTexts.GetString(MyCommonTexts.VoiceChatVolume),
                    originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));

                // adding of slider for volume of voice chat
                Controls.Add(m_voiceChatVolumeSlider);
            }

            //  Buttons OK and CANCEL

            var m_okButton = new MyGuiControlButton(
                position: bottomCenter + new Vector2(-75f, -130f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MyCommonTexts.Ok),
                onButtonClick: OnOkClick,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            Controls.Add(m_okButton);

            var m_cancelButton = new MyGuiControlButton(
                position: bottomCenter + new Vector2(75f, -130f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                size: MyGuiConstants.OK_BUTTON_SIZE,
                text: MyTexts.Get(MyCommonTexts.Cancel),
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

        private void EnableMuteWhenNotInFocusChecked(MyGuiControlCheckbox obj)
        {
            m_settingsNew.EnableMuteWhenNotInFocus = obj.IsChecked;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsAudio";
        }

        void UpdateFromConfig(MyGuiScreenOptionsAudioSettings settings)
        {
            if (MySandboxGame.Config.MusicVolume > 0f)
            {
                settings.MusicVolume = MathHelper.Clamp(MathHelper.InterpLogInv((float)MySandboxGame.Config.MusicVolume, 0.01f, 1f), 0.01f, 1f);
            }
            else
            {
                settings.MusicVolume = 0f;
            }
            if (MySandboxGame.Config.GameVolume > 0f)
            {
                settings.GameVolume = MathHelper.Clamp(MathHelper.InterpLogInv((float)MySandboxGame.Config.GameVolume, 0.01f, 1f), 0.01f, 1f);
            }
            else
            {
                settings.GameVolume = 0f;
            }
            settings.VoiceChatVolume = MySandboxGame.Config.VoiceChatVolume;
            settings.HudWarnings = MySandboxGame.Config.HudWarnings;
            settings.EnableVoiceChat = MySandboxGame.Config.EnableVoiceChat;
            settings.EnableMuteWhenNotInFocus = MySandboxGame.Config.EnableMuteWhenNotInFocus;
        }

        //void UpdateSettings(MyGuiScreenOptionsVideoSettings settings)
        //{
        //    settings.AspectRatio = (MyAspectRatioEnum)m_aspectRationCombobox.GetSelectedKey();
        //}

        void UpdateControls(MyGuiScreenOptionsAudioSettings settings)
        {
            m_gameVolumeSlider.Value = settings.GameVolume;
            m_musicVolumeSlider.Value = settings.MusicVolume;
            m_voiceChatVolumeSlider.Value = settings.VoiceChatVolume;
            m_hudWarnings.IsChecked = settings.HudWarnings;
            m_enableVoiceChat.IsChecked = settings.EnableVoiceChat;
            m_enableMuteWhenNotInFocus.IsChecked = settings.EnableMuteWhenNotInFocus;
        }

        void Save()
        {
            MySandboxGame.Config.GameVolume = MyAudio.Static.VolumeGame;
            MySandboxGame.Config.MusicVolume = MyAudio.Static.VolumeMusic;
            MySandboxGame.Config.VoiceChatVolume = m_voiceChatVolumeSlider.Value;
            MySandboxGame.Config.HudWarnings = m_hudWarnings.IsChecked;
            MySandboxGame.Config.EnableVoiceChat = m_enableVoiceChat.IsChecked;
            MySandboxGame.Config.EnableMuteWhenNotInFocus = m_enableMuteWhenNotInFocus.IsChecked;
            MySandboxGame.Config.Save();
        }

        static void UpdateValues(MyGuiScreenOptionsAudioSettings settings)
        {
            if (settings.MusicVolume > 0f)
            {
                MyAudio.Static.VolumeMusic = MathHelper.Clamp(MathHelper.InterpLog((float)settings.MusicVolume, 0.01f, 1f), 0.01f, 1f);
            }
            else
            {
                MyAudio.Static.VolumeMusic = 0f;
            }
            if (settings.GameVolume > 0f)
            {
                MyAudio.Static.VolumeGame = MathHelper.Clamp(MathHelper.InterpLog((float)settings.GameVolume, 0.01f, 1f), 0.01f, 1f);
            }
            else
            {
                MyAudio.Static.VolumeGame = 0f;
            }
            MyAudio.Static.VolumeVoiceChat = settings.VoiceChatVolume;
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

        void OnVoiceChatVolumeChange(MyGuiControlSlider sender)
        {
            m_settingsNew.VoiceChatVolume = m_voiceChatVolumeSlider.Value;
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
