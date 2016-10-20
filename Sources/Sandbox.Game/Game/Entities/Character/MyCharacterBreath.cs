using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Data;
using VRage.Data.Audio;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Entities.Character
{
    public class MyCharacterBreath
    {
        public enum State
        {
            Calm,
            Heated,
            VeryHeated,
            NoBreath,  // changed name from former Dead because it is used in other situations as well
            Choking
        }
        static MySoundPair BREATH_CALM = new MySoundPair("PlayVocBreath1L");
        static MySoundPair BREATH_HEAVY = new MySoundPair("PlayVocBreath2L");
        static MySoundPair OXYGEN_CHOKE_NORMAL = new MySoundPair("PlayChokeA");
        static MySoundPair OXYGEN_CHOKE_LOW = new MySoundPair("PlayChokeB");
        static MySoundPair OXYGEN_CHOKE_CRITICAL = new MySoundPair("PlayChokeC");

        private const float CHOKE_TRESHOLD_LOW = 55f;
        private const float CHOKE_TRESHOLD_CRITICAL = 25f;

        private const float STAMINA_DRAIN_TIME_RUN = 25;//in seconds
        private const float STAMINA_DRAIN_TIME_SPRINT = 8;//in seconds

        private const float STAMINA_RECOVERY_EXHAUSTED_TO_CALM = 5;//in seconds
        private const float STAMINA_RECOVERY_CALM_TO_ZERO = 15;//in seconds

        private const float STAMINA_AMOUNT_RUN = (STAMINA_RECOVERY_CALM_TO_ZERO / STAMINA_DRAIN_TIME_RUN) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        private const float STAMINA_AMOUNT_SPRINT = (STAMINA_RECOVERY_CALM_TO_ZERO / STAMINA_DRAIN_TIME_SPRINT) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        private const float STAMINA_AMOUNT_MAX = STAMINA_RECOVERY_EXHAUSTED_TO_CALM + STAMINA_RECOVERY_CALM_TO_ZERO;

        IMySourceVoice m_sound;

        MyCharacter m_character;
        private MyTimeSpan m_lastChange;
        private State m_state;
        private MyTimeSpan m_healthOverride;
        private float m_staminaDepletion = 0f;

        public MyCharacterBreath(MyCharacter character)
        {
            CurrentState = State.NoBreath;
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
                    if (m_state == State.NoBreath)
                        m_healthOverride = MyTimeSpan.Zero;
                }
            }
        }

		public void ForceUpdate()
		{
            if (m_character == null || m_character.StatComp == null || m_character.StatComp.Health == null || MySession.Static == null || MySession.Static.LocalCharacter != m_character)
				return;

			SetHealth(m_character.StatComp.Health.Value);
		}

        private void SetHealth(float health)
        {
            if (health <= 0)
                CurrentState = State.NoBreath;
            else if (health < 20)
                //play heavy breath indefinitely
                m_healthOverride = MyTimeSpan.MaxValue;
            else if (health < 100)
                m_healthOverride = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(300 / (health - 19.99));
            
            Update(true);
        }

        public void Update(bool force=false)
        {
            if (MySession.Static == null || MySession.Static.LocalCharacter != m_character || MySession.Static.CreativeMode)
                return;

            if (CurrentState == State.Heated)
                m_staminaDepletion = Math.Min(m_staminaDepletion + STAMINA_AMOUNT_RUN, STAMINA_AMOUNT_MAX);
            else if (CurrentState == State.VeryHeated)
                m_staminaDepletion = Math.Min(m_staminaDepletion + STAMINA_AMOUNT_SPRINT, STAMINA_AMOUNT_MAX);
            else
                m_staminaDepletion = Math.Max(m_staminaDepletion - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, 0f);

            if (CurrentState == State.NoBreath)
            {
                if (m_sound != null)
                {
                    m_sound.Stop();
                    m_sound = null;
                }
                return;
            }
            float health = m_character.StatComp.Health.Value;
            if (CurrentState == State.Choking)
            {
                if (health >= CHOKE_TRESHOLD_LOW && (m_sound == null || m_sound.IsPlaying == false || m_sound.CueEnum != OXYGEN_CHOKE_NORMAL.SoundId))
                    PlaySound(OXYGEN_CHOKE_NORMAL.SoundId, false);
                else if (health >= CHOKE_TRESHOLD_CRITICAL && health < CHOKE_TRESHOLD_LOW && (m_sound == null || m_sound.IsPlaying == false || m_sound.CueEnum != OXYGEN_CHOKE_LOW.SoundId))
                    PlaySound(OXYGEN_CHOKE_LOW.SoundId, false);
                else if (health > 0f && health < CHOKE_TRESHOLD_CRITICAL && (m_sound == null || m_sound.IsPlaying == false || m_sound.CueEnum != OXYGEN_CHOKE_CRITICAL.SoundId))
                    PlaySound(OXYGEN_CHOKE_CRITICAL.SoundId, false);
                return;
            }

            if (CurrentState == State.Calm || CurrentState == State.Heated || CurrentState == State.VeryHeated)
            {
                if (m_staminaDepletion < STAMINA_RECOVERY_CALM_TO_ZERO && health > 20f)
                {
                    if (!BREATH_CALM.SoundId.IsNull && (m_sound == null || m_sound.IsPlaying == false || m_sound.CueEnum != BREATH_CALM.SoundId))
                        PlaySound(BREATH_CALM.SoundId, true);
                    else if (m_sound != null && m_sound.IsPlaying && BREATH_CALM.SoundId.IsNull)
                        m_sound.Stop(true);
                }
                else
                {
                    if (!BREATH_HEAVY.SoundId.IsNull && (m_sound == null || m_sound.IsPlaying == false || m_sound.CueEnum != BREATH_HEAVY.SoundId))
                        PlaySound(BREATH_HEAVY.SoundId, true);
                    else if (m_sound != null && m_sound.IsPlaying && BREATH_HEAVY.SoundId.IsNull)
                        m_sound.Stop(true);
                }
            }
        }

        private void PlaySound(MyCueId soundId, bool useCrossfade)
        {
            if (m_sound != null && m_sound.IsPlaying && useCrossfade)
            {
                var effect = MyAudio.Static.ApplyEffect(m_sound, MyStringHash.GetOrCompute("CrossFade"), new MyCueId[] { soundId }, 2000);
                m_sound = effect.OutputSound;
            }
            else
            {
                if (m_sound != null)
                {
                    m_sound.Stop(true);
                }

                m_sound = MyAudio.Static.PlaySound(soundId, null, MySoundDimensions.D2);
            }
        }

        public void Close()
        {
            if (m_sound != null)
                m_sound.Stop(true);
        }
    }
}
