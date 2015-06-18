using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Data;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Entities.Character
{
    class MyCharacterBreath
    {
        public enum State
        {
            Calm,
            Heated,
            Dead,
        }
        static MySoundPair BREATH_CALM = new MySoundPair("PlayVocBreath1L");
        static MySoundPair BREATH_HEAVY = new MySoundPair("PlayVocBreath2L");
        IMySourceVoice m_sound;

        MyCharacter m_character;
        private MyTimeSpan m_lastChange;
        private State m_state;
        private MyTimeSpan m_healthOverride;

        public MyCharacterBreath(MyCharacter character)
        {
            CurrentState = State.Calm;
            m_character = character;
        }

        public State CurrentState
        {
            private get
            {
                return m_state;
            }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    m_lastChange = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(2);
                    if (m_state == State.Dead)
                        m_healthOverride = MyTimeSpan.Zero;
                }
            }
        }

		public void ForceUpdate()
		{
			if (m_character == null)
				return;

			SetHealth(m_character.Health);
		}

        private void SetHealth(float health)
        {
            if (health<20)
                //play heavy breath indefinitely
                m_healthOverride = MyTimeSpan.MaxValue;
            else
            if (health<100)
                m_healthOverride = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(300 / (health - 19.99));
            Update(true);
        }

        public void Update(bool force=false)
        {
            if (!Sandbox.Game.World.MySession.Static.Settings.RealisticSound)
                return;
            if(CurrentState == State.Dead)
            {
                if (m_sound != null && m_sound.IsPlaying)
                    m_sound.Stop();
                return;
            }

            if (force || m_lastChange < MySandboxGame.Static.UpdateTime)
            {
                if (m_healthOverride > MySandboxGame.Static.UpdateTime || CurrentState == State.Heated)
                {//State.Heated
                    if (m_sound == null || m_sound.CueEnum != BREATH_HEAVY.SoundId)
                    {
                        PlaySound(BREATH_HEAVY.SoundId);
                    }
                }
                else
                {//State.Calm:
                    if (m_sound == null || m_sound.CueEnum != BREATH_CALM.SoundId)
                    {
                        PlaySound(BREATH_CALM.SoundId);
                    }
                }
            }
        }

        private void PlaySound(MyCueId soundId)
        {
            if (m_sound != null && m_sound.IsPlaying)
            {
                var effect = MyAudio.Static.ApplyEffect(m_sound, MyStringHash.GetOrCompute("CrossFade"), new MyCueId[] { soundId }, 2000);
                m_sound = effect.OutputSound;
            }
            else
                m_sound = MyAudio.Static.PlaySound(soundId, null, MySoundDimensions.D2);
        }

        public void Close()
        {
            if (m_sound != null)
                m_sound.Stop(true);
        }
    }
}
