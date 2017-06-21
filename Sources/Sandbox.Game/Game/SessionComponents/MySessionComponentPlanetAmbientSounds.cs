using System;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Audio;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using VRage.Game;
using VRage.Data.Audio;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class MySessionComponentPlanetAmbientSounds : MySessionComponentBase
    {
        private IMySourceVoice m_sound;
        private IMyAudioEffect m_effect;
        private readonly MyStringHash m_crossFade = MyStringHash.GetOrCompute("CrossFade");
        private readonly MyStringHash m_fadeIn = MyStringHash.GetOrCompute("FadeIn");
        private readonly MyStringHash m_fadeOut = MyStringHash.GetOrCompute("FadeOut");

        private MyPlanet m_nearestPlanet;
        private MyPlanet Planet { get { return m_nearestPlanet; } set { SetNearestPlanet(value); } }

        private long m_nextPlanetRecalculation = -1;

        private int m_planetRecalculationIntervalInSpace = 300;
        private int m_planetRecalculationIntervalOnPlanet = 300;
        private float m_volumeModifier = 1f;
        private static float m_volumeModifierTarget = 1f;
        private float m_volumeOriginal = 1f;
        private const float VOLUME_CHANGE_SPEED = 0.25f;
        public float VolumeModifierGlobal = 1f;

        private MyPlanetEnvironmentalSoundRule[] m_nearestSoundRules;

        private readonly MyPlanetEnvironmentalSoundRule[] m_emptySoundRules = new MyPlanetEnvironmentalSoundRule[0];

        public override void LoadData()
        {
            base.LoadData();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            if (m_sound != null)
                m_sound.Stop();

            m_nearestPlanet = null;
            m_nearestSoundRules = null;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MySandboxGame.IsDedicated)
                return;

            if (m_volumeModifier != m_volumeModifierTarget)
            {
                if (m_volumeModifier < m_volumeModifierTarget)
                    m_volumeModifier = MyMath.Clamp(m_volumeModifier + MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * VOLUME_CHANGE_SPEED, 0f, m_volumeModifierTarget);
                else
                    m_volumeModifier = MyMath.Clamp(m_volumeModifier - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * VOLUME_CHANGE_SPEED, m_volumeModifierTarget, 1f);
                if (m_sound != null && m_sound.IsPlaying)
                    m_sound.SetVolume(m_volumeOriginal * m_volumeModifier * VolumeModifierGlobal);
            }

            long currentFrame = MySession.Static.GameplayFrameCounter;
            if (currentFrame >= m_nextPlanetRecalculation)
            {
                Planet = FindNearestPlanet(MySector.MainCamera.Position);
                if(Planet == null)
                    m_nextPlanetRecalculation = currentFrame + m_planetRecalculationIntervalInSpace;
                else
                    m_nextPlanetRecalculation = currentFrame + m_planetRecalculationIntervalOnPlanet;
            }

            if (Planet == null || Planet.Provider == null || (MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound && Planet.HasAtmosphere == false))
            {
                if (m_sound != null)
                    m_sound.Stop(true);
                return;
            }
            Vector3D localPosition = MySector.MainCamera.Position - Planet.PositionComp.GetPosition();
            double distanceToCenter = localPosition.Length();

            float height = Planet.Provider.Shape.DistanceToRatio((float)distanceToCenter);

            if (height < 0)
                return;

            Vector3D gravity = -localPosition / distanceToCenter;

            float angleFromEquator = (float)-gravity.Y;
            float sunAngleFromZenith = (float)MySector.DirectionToSunNormalized.Dot(-gravity);

            int ruleIndex;
            if (!FindSoundRuleIndex(angleFromEquator, height, sunAngleFromZenith, m_nearestSoundRules, out ruleIndex))
            {
                PlaySound(new MyCueId());
                return;
            }

            PlaySound(new MyCueId(m_nearestSoundRules[ruleIndex].EnvironmentSound));
        }

        private static MyPlanet FindNearestPlanet(Vector3D worldPosition)
        {
            BoundingBoxD bb = new BoundingBoxD(worldPosition, worldPosition);
            MyPlanet foundPlanet = MyGamePruningStructure.GetClosestPlanet(ref bb);
            if (foundPlanet != null && foundPlanet.AtmosphereAltitude > Vector3D.Distance(worldPosition, foundPlanet.PositionComp.GetPosition()))
                return null;

            return foundPlanet;
        }

        private void SetNearestPlanet(MyPlanet planet)
        {
            m_nearestPlanet = planet;

            if (m_nearestPlanet != null && m_nearestPlanet.Generator != null)
            {
                m_nearestSoundRules = m_nearestPlanet.Generator.SoundRules ?? m_emptySoundRules;
            }
        }

        private static bool FindSoundRuleIndex(float angleFromEquator, float height, float sunAngleFromZenith, MyPlanetEnvironmentalSoundRule[] soundRules, out int outRuleIndex)
        {
            outRuleIndex = -1;
            if (soundRules == null)
                return false;

            for (int ruleIndex = 0; ruleIndex < soundRules.Length; ++ruleIndex)
            {
                if (!soundRules[ruleIndex].Check(angleFromEquator, height, sunAngleFromZenith))
                    continue;

                outRuleIndex = ruleIndex;
                return true;
            }
            return false;
        }

        private void PlaySound(MyCueId sound)
        {
            if (m_sound == null || !m_sound.IsPlaying)
            {
                m_sound = MyAudio.Static.PlaySound(sound);
                if (!sound.IsNull)
                    m_effect = MyAudio.Static.ApplyEffect(m_sound, m_fadeIn, null);
                if (m_effect != null)
                    m_sound = m_effect.OutputSound;
            }
            else if (m_effect != null && m_effect.Finished && sound.IsNull)
                m_sound.Stop(true);
            else if (m_sound.CueEnum != sound)
            {
                if (m_effect != null && !m_effect.Finished)
                {
                    //m_effect.SetPositionRelative(1f);
                    m_effect.AutoUpdate = true;
                }
                if (sound.IsNull)
                    m_effect = MyAudio.Static.ApplyEffect(m_sound, m_fadeOut, null, 5000f);
                else
                    m_effect = MyAudio.Static.ApplyEffect(m_sound, m_crossFade, new MyCueId[] { sound }, 5000f);

                if (m_effect != null && !m_effect.Finished)
                {
                    m_effect.AutoUpdate = true;
                    m_sound = m_effect.OutputSound;
                }
            }
            if (m_sound != null)
            {
                MySoundData data = MyAudio.Static.GetCue(sound);
                m_volumeOriginal = data != null ? data.Volume :1f;
                m_sound.SetVolume(m_volumeOriginal * m_volumeModifier * VolumeModifierGlobal);
            }
        }

        public static void SetAmbientOn()
        {
            m_volumeModifierTarget = 1f;
        }

        public static void SetAmbientOff()
        {
            m_volumeModifierTarget = 0f;
        }

        public override bool IsRequiredByGame { get { return base.IsRequiredByGame && MyFakes.ENABLE_PLANETS; }}
    }
}
