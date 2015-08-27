using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Audio;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;

namespace Sandbox.Game.GUI
{
    public enum MyGuiSounds
    {
        HudClick,
        HudUse,
        HudRotateBlock,
        HudPlaceBlock,
        HudDeleteBlock,
        HudColorBlock,
        HudMouseClick,
        HudMouseOver,
        HudUnable,
        PlayDropItem,
        HudVocInventoryFull,
        HudVocMeteorInbound,
        HudVocHealthLow,
        HudVocHealthCritical,
        None,
        HudVocEnergyLow,
        HudVocStationFuelLow,
        HudVocShipFuelLow,
        HudVocEnergyCrit,
        HudVocStationFuelCrit,
        HudVocShipFuelCrit,
        HudVocEnergyNo,
        HudVocStationFuelNo,
        HudVocShipFuelNo,

    }
    public class MyGuiAudio : IMyGuiAudio
    {
        public static bool HudWarnings;
        public static IMyGuiAudio Static { get; set; }
        private static Dictionary<MyGuiSounds, MySoundPair> m_sounds = new Dictionary<MyGuiSounds, MySoundPair>(Enum.GetValues(typeof(MyGuiSounds)).Length);

        static MyGuiAudio()
        {
            Static = new MyGuiAudio();

            foreach (MyGuiSounds sound in Enum.GetValues(typeof(MyGuiSounds)))
                m_sounds.Add(sound, new MySoundPair(sound.ToString()));
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
            return MyAudio.Static.PlaySound(m_sounds[sound].SoundId);
        }

        private MyGuiSounds GetSound(GuiSounds sound)
        {
            switch(sound)
            {
                case(GuiSounds.MouseClick):
                    return MyGuiSounds.HudMouseClick;
                case(GuiSounds.MouseOver):
                    return MyGuiSounds.HudMouseOver;
                default:
                    return MyGuiSounds.HudClick;
            }
        }

        internal static MyCueId GetCue(MyGuiSounds sound)
        {
            return m_sounds[sound].SoundId;
        }
    }
}
