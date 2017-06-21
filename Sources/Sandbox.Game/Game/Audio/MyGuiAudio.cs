using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Audio;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.GUI
{
    public class MyGuiAudio : IMyGuiAudio
    {
        public static bool HudWarnings;
        public static IMyGuiAudio Static { get; set; }
        private static Dictionary<MyGuiSounds, MySoundPair> m_sounds = new Dictionary<MyGuiSounds, MySoundPair>(Enum.GetValues(typeof(MyGuiSounds)).Length);
        private static Dictionary<MyGuiSounds, int> m_lastTimePlaying = new Dictionary<MyGuiSounds, int>();

        static MyGuiAudio()
        {
            Static = new MyGuiAudio();

            foreach (MyGuiSounds sound in Enum.GetValues(typeof(MyGuiSounds)))
                m_sounds.Add(sound, new MySoundPair(sound.ToString(), false));
        }


        public void PlaySound(GuiSounds sound)
        {
            if (sound == GuiSounds.None)
                return;
            MyGuiSounds hudSound = GetSound(sound);
            PlaySound(hudSound);
        }

        public static IMySourceVoice PlaySound(MyGuiSounds sound)
        {
            if (MyFakes.ENABLE_NEW_SOUNDS && MySession.Static != null && MySession.Static.Settings.RealisticSound && MySession.Static.LocalCharacter != null
                && MySession.Static.LocalCharacter.OxygenComponent != null && MySession.Static.LocalCharacter.OxygenComponent.HelmetEnabled == false)
            {
                MySoundData soundData = MyAudio.Static.GetCue(m_sounds[sound].SoundId);
                if (soundData != null && soundData.CanBeSilencedByVoid)
                {
                    MyCockpit cockpit = MySession.Static.LocalCharacter.Parent as MyCockpit;
                    if ((cockpit == null || !cockpit.BlockDefinition.IsPressurized) && MySession.Static.LocalCharacter.EnvironmentOxygenLevel <= 0)
                        return null;//disables hud sound when in realistic mode in space without helmet
                }
            }

            if (CheckForSynchronizedSounds(sound))
                return MyAudio.Static.PlaySound(m_sounds[sound].SoundId);
            else
                return null;
        }

        private MyGuiSounds GetSound(GuiSounds sound)
        {
            switch(sound)
            {
                case(GuiSounds.MouseClick):
                    return MyGuiSounds.HudMouseClick;
                case(GuiSounds.MouseOver):
                    return MyGuiSounds.HudMouseOver;
                case (GuiSounds.Item):
                    return MyGuiSounds.HudItem;
                default:
                    return MyGuiSounds.HudClick;
            }
        }

        internal static MyCueId GetCue(MyGuiSounds sound)
        {
            return m_sounds[sound].SoundId;
        }

        private static bool CheckForSynchronizedSounds(MyGuiSounds sound)
        {
            MySoundData soundData = MyAudio.Static.GetCue(m_sounds[sound].SoundId);
            if (soundData != null && soundData.PreventSynchronization >= 0)
            {
                int lastTime;
                int now = MyFpsManager.GetSessionTotalFrames();
                if (m_lastTimePlaying.TryGetValue(sound, out lastTime))
                {
                    if (Math.Abs(now - lastTime) <= soundData.PreventSynchronization)
                    {
                        return false;
                    }
                    else
                        m_lastTimePlaying[sound] = now;
                }
                else
                {
                    m_lastTimePlaying.Add(sound, now);
                }
            }
            return true;
        }
    }
}
