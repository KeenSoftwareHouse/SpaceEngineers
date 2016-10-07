using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyComponentBuilder(typeof(MyObjectBuilder_ShipSoundComponent))]
    public class MyShipSoundComponent : MyEntityComponentBase
    {
        #region Enums

        private enum ShipStateEnum
        {
            NoPower = 0,
            Slow = 1,
            Medium = 2,
            Fast = 3
        }

        private enum ShipEmitters
        {
            MainSound = 0,
            SingleSounds = 1,
            IonThrusters = 2,
            HydrogenThrusters = 3,
            AtmosphericThrusters = 4,
            IonThrustersIdle = 5,
            HydrogenThrustersIdle = 6,
            AtmosphericThrustersIdle = 7,
            WheelsMain = 8,
            WheelsSecondary = 9,
            ShipIdle = 10,
            ShipEngine = 11
        }

        private enum ShipThrusters
        {
            Ion = 0,
            Hydrogen = 1,
            Atmospheric = 2
        }

        private enum ShipTimers
        {
            SpeedUp = 0,
            SpeedDown = 1
        }

        #endregion

        #region ShipSoundDatabase

        private static Dictionary<MyDefinitionId, MyShipSoundsDefinition> m_categories = new Dictionary<MyDefinitionId, MyShipSoundsDefinition>();
        private static MyShipSoundSystemDefinition m_definition = new MyShipSoundSystemDefinition();

        public static void ClearShipSounds()
        {
            m_categories.Clear();
        }

        public static void SetDefinition(MyShipSoundSystemDefinition def)
        {
            m_definition = def;
        }

        public static void AddShipSounds(MyShipSoundsDefinition shipSoundGroup)
        {
            if (m_categories.ContainsKey(shipSoundGroup.Id))
                m_categories.Remove(shipSoundGroup.Id);
            m_categories.Add(shipSoundGroup.Id, shipSoundGroup);
        }

        public static void ActualizeGroups(){
            foreach(var group in m_categories.Values){
                group.WheelsSpeedCompensation = m_definition.FullSpeed / group.WheelsFullSpeed;
            }
        }

        #endregion

        #region Fields

        private bool m_initialized = false;
        private bool m_shouldPlay2D = false;
        private bool m_shouldPlay2DChanged = false;
        private bool m_insideShip = false;
        private float m_distanceToShip = float.MaxValue;
        public bool ShipHasChanged = true;
        private MyEntity m_shipSoundSource = null;

        private MyCubeGrid m_shipGrid = null;
        private MyEntityThrustComponent m_shipThrusters = null;
        private MyGridWheelSystem m_shipWheels = null;
        private bool m_isDebris = true;
        private MyDefinitionId m_shipCategory = new MyDefinitionId();
        private MyShipSoundsDefinition m_groupData = null;
        private bool m_categoryChange = false;
        private bool m_forceSoundCheck = false;
        private float m_wheelVolumeModifierEngine = 0;
        private float m_wheelVolumeModifierWheels = 0;
        private HashSet<MySlimBlock> m_detectedBlocks = new HashSet<MySlimBlock>();

        private ShipStateEnum m_shipState = ShipStateEnum.NoPower;
        private float m_shipEngineModifier = 0f;
        private float m_singleSoundsModifier = 1f;
        private bool m_playingSpeedUpOrDown = false;

        private MyEntity3DSoundEmitter[] m_emitters = new MyEntity3DSoundEmitter[(Enum.GetNames(typeof(ShipEmitters)).Length)];
        private float[] m_thrusterVolumes;
        private float[] m_thrusterVolumeTargets;
        private bool m_singleThrusterTypeShip = false;

        private static MyStringHash m_thrusterIon = MyStringHash.GetOrCompute("Ion");
        private static MyStringHash m_thrusterHydrogen = MyStringHash.GetOrCompute("Hydrogen");
        private static MyStringHash m_thrusterAtmospheric = MyStringHash.GetOrCompute("Atmospheric");
        private static MyStringHash m_crossfade = MyStringHash.GetOrCompute("CrossFade");
        private static MyStringHash m_fadeOut = MyStringHash.GetOrCompute("FadeOut");

        private float[] m_timers = new float[(Enum.GetNames(typeof(ShipTimers)).Length)];
        private float m_lastFrameShipSpeed = 0f;
        private int m_speedChange = 15;

        private float m_shipCurrentPower = 0f;
        private float m_shipCurrentPowerTarget = 0f;
        private const float POWER_CHANGE_SPEED_UP = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.4f;
        private const float POWER_CHANGE_SPEED_DOWN = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.6f;

        private bool m_lastWheelUpdateStart = false;
        private bool m_lastWheelUpdateStop = false;
        private DateTime m_lastContactWithGround = DateTime.UtcNow;
        private bool m_shipWheelsAction = false;

        #endregion

        #region Initialization

        public MyShipSoundComponent()
        {
            for (int i = 0; i < m_emitters.Length; i++)
                m_emitters[i] = null;
            for (int i = 0; i < m_timers.Length; i++)
                m_timers[i] = 0f;
        }


        public bool InitComponent(MyCubeGrid shipGrid)
        {
            if (shipGrid.GridSizeEnum == MyCubeSize.Small && MyFakes.ENABLE_NEW_SMALL_SHIP_SOUNDS == false)
                return false;
            if (shipGrid.GridSizeEnum == MyCubeSize.Large && MyFakes.ENABLE_NEW_LARGE_SHIP_SOUNDS == false)
                return false;
            m_shipGrid = shipGrid;
            m_shipThrusters = m_shipGrid.Components.Get<MyEntityThrustComponent>();
            m_shipWheels = m_shipGrid.GridSystems.WheelSystem;

            m_thrusterVolumes = new float[(Enum.GetNames(typeof(ShipThrusters)).Length)];
            m_thrusterVolumeTargets = new float[(Enum.GetNames(typeof(ShipThrusters)).Length)];
            for (int i = 1; i < m_thrusterVolumes.Length; i++)
            {
                m_thrusterVolumes[i] = 0f;
                m_thrusterVolumeTargets[i] = 0f;
            }
            m_thrusterVolumes[0] = 1f;
            m_thrusterVolumeTargets[0] = 1f;

            for (int i = 0; i < m_emitters.Length; i++)
            {
                m_emitters[i] = new MyEntity3DSoundEmitter(m_shipGrid, true);
                m_emitters[i].Force2D = m_shouldPlay2D;
                m_emitters[i].Force3D = !m_shouldPlay2D;
            }

            m_initialized = true;

            return true;
        }

        #endregion

        #region FastUpdate

        public void Update()
        {
            if (m_initialized && m_shipGrid.Physics != null && m_shipGrid.IsStatic == false && (m_shipThrusters != null || m_shipWheels != null) && m_distanceToShip < m_definition.MaxUpdateRange_sq && m_groupData != null)
            {
                //calculate current ship state
                bool driving = ((DateTime.UtcNow - m_lastContactWithGround).TotalSeconds <= 0.2f);
                float shipSpeed = driving == false ? m_shipGrid.Physics.LinearVelocity.Length() : (m_shipGrid.Physics.LinearVelocity * m_groupData.WheelsSpeedCompensation).Length();
                float originalSpeed = Math.Min(shipSpeed / m_definition.FullSpeed, 1f);
                if (!MySandboxGame.Config.ShipSoundsAreBasedOnSpeed)
                    shipSpeed = m_shipCurrentPower * m_definition.FullSpeed;
                ShipStateEnum lastState = m_shipState;
                if (m_shipGrid.GridSystems.ResourceDistributor.ResourceState == MyResourceStateEnum.NoPower || m_isDebris
                    || ((m_shipThrusters == null || m_shipThrusters.ThrustCount <= 0) && (m_shipWheels == null || m_shipWheels.WheelCount <= 0)))
                {
                    m_shipState = ShipStateEnum.NoPower;
                }
                else
                {
                    if (shipSpeed < m_definition.SpeedThreshold1)
                        m_shipState = ShipStateEnum.Slow;
                    else if (shipSpeed < m_definition.SpeedThreshold2)
                        m_shipState = ShipStateEnum.Medium;
                    else
                        m_shipState = ShipStateEnum.Fast;
                }

                //alternate speed calculation (based on acceleration/decceleration)
                if (!MySandboxGame.Config.ShipSoundsAreBasedOnSpeed)
                {
                    m_shipCurrentPowerTarget = 0f;
                    if (driving)
                    {
                        if (m_shipWheels != null && m_shipWheels.WheelCount > 0)
                        {
                            if (m_shipWheels.AngularVelocity.LengthSquared() >= 1f)
                                m_shipCurrentPowerTarget = 1f;//accelerating/deccelerating
                            else if (m_shipGrid.Physics.LinearVelocity.LengthSquared() > 5f)
                                m_shipCurrentPowerTarget = 0.33f;//cruising
                        }
                    }
                    else
                    {
                        if(m_shipThrusters != null)
                        {
                            if (m_shipThrusters.FinalThrust.LengthSquared() >= 100f)
                                m_shipCurrentPowerTarget = 1f;//accelerating/deccelerating
                            else if (m_shipGrid.Physics.Gravity != Vector3.Zero && m_shipThrusters.DampenersEnabled && m_shipGrid.Physics.LinearVelocity.LengthSquared() < 4f)
                                m_shipCurrentPowerTarget = 0.33f;//hovering
                            else
                                m_shipCurrentPowerTarget = 0;//complete stop
                        }
                    }

                    if (m_shipCurrentPower < m_shipCurrentPowerTarget)
                        m_shipCurrentPower = Math.Min(m_shipCurrentPower + POWER_CHANGE_SPEED_UP, m_shipCurrentPowerTarget);
                    else if (m_shipCurrentPower > m_shipCurrentPowerTarget)
                        m_shipCurrentPower = Math.Max(m_shipCurrentPower - POWER_CHANGE_SPEED_DOWN, m_shipCurrentPowerTarget);
                }

                //in first person change
                bool orig = m_shouldPlay2D;
                if (m_shipGrid.GridSizeEnum == MyCubeSize.Large)
                    m_shouldPlay2D = m_insideShip;
                else if (MySession.Static.ControlledEntity != null && MySession.Static.IsCameraUserControlledSpectator() == false && MySession.Static.ControlledEntity.Entity != null && MySession.Static.ControlledEntity.Entity.Parent == m_shipGrid)
                {
                    m_shouldPlay2D = ((MySession.Static.ControlledEntity.Entity is MyCockpit) && (MySession.Static.ControlledEntity.Entity as MyCockpit).IsInFirstPersonView)

                        || ((MySession.Static.ControlledEntity.Entity is MyRemoteControl) && MySession.Static.LocalCharacter != null
                            && MySession.Static.LocalCharacter.IsUsing is MyCockpit && (MySession.Static.LocalCharacter.IsUsing as MyCockpit).Parent == m_shipGrid)

                        || (MySession.Static.CameraController is MyCameraBlock && (MySession.Static.CameraController as MyCameraBlock).Parent == m_shipGrid);
                }
                else
                    m_shouldPlay2D = false;

                m_shouldPlay2DChanged = (orig != m_shouldPlay2D);

                //thruster volume corrections
                for (int i = 0; i < m_thrusterVolumes.Length; i++)
                {
                    if (m_thrusterVolumes[i] < m_thrusterVolumeTargets[i])
                    {
                        m_thrusterVolumes[i] = Math.Min(m_thrusterVolumes[i] + m_groupData.ThrusterCompositionChangeSpeed, m_thrusterVolumeTargets[i]);
                    }
                    else if (m_thrusterVolumes[i] > m_thrusterVolumeTargets[i])
                    {
                        m_thrusterVolumes[i] = Math.Max(m_thrusterVolumes[i] - m_groupData.ThrusterCompositionChangeSpeed, m_thrusterVolumeTargets[i]);
                    }
                }

                if (driving)
                {
                    m_wheelVolumeModifierEngine = Math.Min(m_wheelVolumeModifierEngine + 0.01f, 1f);
                    m_wheelVolumeModifierWheels = Math.Min(m_wheelVolumeModifierWheels + 0.03f, 1f);
                }
                else
                {
                    m_wheelVolumeModifierEngine = Math.Max(m_wheelVolumeModifierEngine - 0.005f, 0f);
                    m_wheelVolumeModifierWheels = Math.Max(m_wheelVolumeModifierWheels - 0.03f, 0f);
                }

                //play sounds if there was change in state, ship type or thruster composition
                if (m_shipState != lastState || m_categoryChange || m_forceSoundCheck)
                {
                    if (m_shipState == ShipStateEnum.NoPower)
                    {
                        if (m_shipState != lastState)
                        {
                            for (int i = 0; i < m_emitters.Length; i++)
                                m_emitters[i].StopSound(false);
                            m_emitters[(int)ShipEmitters.SingleSounds].VolumeMultiplier = 1f;
                            PlayShipSound(ShipEmitters.SingleSounds, ShipSystemSoundsEnum.EnginesEnd);
                        }
                    }
                    else
                    {
                        if (m_shipState == ShipStateEnum.Slow)
                            PlayShipSound(ShipEmitters.MainSound, ShipSystemSoundsEnum.MainLoopSlow);
                        else if (m_shipState == ShipStateEnum.Medium)
                            PlayShipSound(ShipEmitters.MainSound, ShipSystemSoundsEnum.MainLoopMedium);
                        else if (m_shipState == ShipStateEnum.Fast)
                            PlayShipSound(ShipEmitters.MainSound, ShipSystemSoundsEnum.MainLoopFast);
                        
                        PlayShipSound(ShipEmitters.ShipEngine, ShipSystemSoundsEnum.ShipEngine);
                        PlayShipSound(ShipEmitters.ShipIdle, ShipSystemSoundsEnum.ShipIdle);

                        if (m_thrusterVolumes[(int)ShipThrusters.Ion] > 0f)
                        {
                            PlayShipSound(ShipEmitters.IonThrusters, ShipSystemSoundsEnum.IonThrusters);
                            PlayShipSound(ShipEmitters.IonThrustersIdle, ShipSystemSoundsEnum.IonThrustersIdle);
                        }

                        if (m_thrusterVolumes[(int)ShipThrusters.Hydrogen] > 0f)
                        {
                            PlayShipSound(ShipEmitters.HydrogenThrusters, ShipSystemSoundsEnum.HydrogenThrusters);
                            PlayShipSound(ShipEmitters.HydrogenThrustersIdle, ShipSystemSoundsEnum.HydrogenThrustersIdle);
                        }

                        if (m_thrusterVolumes[(int)ShipThrusters.Atmospheric] > 0f)
                        {
                            if (m_shipState == ShipStateEnum.Slow)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSystemSoundsEnum.AtmoThrustersSlow);
                            else if (m_shipState == ShipStateEnum.Medium)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSystemSoundsEnum.AtmoThrustersMedium);
                            else if (m_shipState == ShipStateEnum.Fast)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSystemSoundsEnum.AtmoThrustersFast);
                            PlayShipSound(ShipEmitters.AtmosphericThrustersIdle, ShipSystemSoundsEnum.AtmoThrustersIdle);
                        }

                        if (m_shipWheels.WheelCount > 0)
                        {
                            PlayShipSound(ShipEmitters.WheelsMain, ShipSystemSoundsEnum.WheelsEngineRun);
                            PlayShipSound(ShipEmitters.WheelsSecondary, ShipSystemSoundsEnum.WheelsSecondary);
                        }

                        if (lastState == ShipStateEnum.NoPower)
                        {
                            m_emitters[(int)ShipEmitters.SingleSounds].VolumeMultiplier = 1f;
                            PlayShipSound(ShipEmitters.SingleSounds, ShipSystemSoundsEnum.EnginesStart);
                        }
                    }
                    m_categoryChange = false;
                    m_forceSoundCheck = false;
                }

                //there was change in camera - sound should change from 2d to 3d or vice versa
                if (m_shouldPlay2DChanged)
                {
                    for (int i = 0; i < m_emitters.Length; i++)
                    {
                        m_emitters[i].Force2D = m_shouldPlay2D;
                        m_emitters[i].Force3D = !m_shouldPlay2D;
                        if (m_emitters[i].IsPlaying && m_emitters[i].Plays2D != m_shouldPlay2D && m_emitters[i].Loop)
                        {
                            m_emitters[i].StopSound(true);
                            m_emitters[i].PlaySound(m_emitters[i].SoundPair, true, true, m_shouldPlay2D);
                        }
                    }
                    m_shouldPlay2DChanged = false;
                }

                //update emitter volumes
                if (m_shipState != ShipStateEnum.NoPower)
                {
                    if (m_shipEngineModifier < 1f)
                        m_shipEngineModifier = Math.Min(1f, m_shipEngineModifier + MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / m_groupData.EngineTimeToTurnOn);
                    float shipSpeedRatio = Math.Min(shipSpeed / m_definition.FullSpeed, 1f);
                    float shipSpeedVolume = CalculateVolumeFromSpeed(shipSpeedRatio, ref m_groupData.EngineVolumes) * m_shipEngineModifier * m_singleSoundsModifier;
                    float shipThrusterRatio;
                    float shipThrusterIdleRatio = 1f;

                    //main sound - engines
                    if (m_emitters[(int)ShipEmitters.MainSound].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.MainSound].VolumeMultiplier = shipSpeedVolume;
                        float semitones = m_groupData.EnginePitchRangeInSemitones_h + m_groupData.EnginePitchRangeInSemitones * shipSpeedRatio;
                        m_emitters[(int)ShipEmitters.MainSound].Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
                    }

                    //thruster base volume ratio
                    shipThrusterRatio = CalculateVolumeFromSpeed(shipSpeedRatio, ref m_groupData.ThrusterVolumes);
                    shipThrusterRatio = Math.Max(Math.Min(shipThrusterRatio, 1f) - m_wheelVolumeModifierEngine * m_groupData.WheelsLowerThrusterVolumeBy, 0f);
                    shipThrusterIdleRatio = MyMath.Clamp(1.2f - shipThrusterRatio * 3f, 0f, 1f) * m_shipEngineModifier * m_singleSoundsModifier;
                    shipThrusterRatio *= m_shipEngineModifier * m_singleSoundsModifier;

                    //large ship special emitters
                    m_emitters[(int)ShipEmitters.ShipEngine].VolumeMultiplier = MySandboxGame.Config.ShipSoundsAreBasedOnSpeed ? Math.Max(0f, shipSpeedVolume - shipThrusterIdleRatio) : originalSpeed;
                    m_emitters[(int)ShipEmitters.ShipIdle].VolumeMultiplier = (MySandboxGame.Config.ShipSoundsAreBasedOnSpeed ? shipThrusterIdleRatio : MyMath.Clamp(1.2f - originalSpeed * 3f, 0f, 1f)) * m_shipEngineModifier * m_singleSoundsModifier;

                    //ion thruster run/idle sounds volumes + pitch
                    float thrusterPitch = MyAudio.Static.SemitonesToFrequencyRatio(m_groupData.ThrusterPitchRangeInSemitones_h + m_groupData.ThrusterPitchRangeInSemitones * shipThrusterRatio);
                    if (m_emitters[(int)ShipEmitters.IonThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.IonThrusters].VolumeMultiplier = shipThrusterRatio * m_thrusterVolumes[(int)ShipThrusters.Ion];
                        m_emitters[(int)ShipEmitters.IonThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.IonThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.IonThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_thrusterVolumes[(int)ShipThrusters.Ion];

                    //hydrogen thruster run/idle sounds volumes + pitch
                    if (m_emitters[(int)ShipEmitters.HydrogenThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.HydrogenThrusters].VolumeMultiplier = shipThrusterRatio * m_thrusterVolumes[(int)ShipThrusters.Hydrogen];
                        m_emitters[(int)ShipEmitters.HydrogenThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.HydrogenThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.HydrogenThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_thrusterVolumes[(int)ShipThrusters.Hydrogen];

                    //atmospheric thruster run/idle sounds volumes + pitch
                    if (m_emitters[(int)ShipEmitters.AtmosphericThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.AtmosphericThrusters].VolumeMultiplier = shipThrusterRatio * m_thrusterVolumes[(int)ShipThrusters.Atmospheric];
                        m_emitters[(int)ShipEmitters.AtmosphericThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.AtmosphericThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.AtmosphericThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_thrusterVolumes[(int)ShipThrusters.Atmospheric];

                    //wheels volume + pitch
                    if (m_emitters[(int)ShipEmitters.WheelsMain].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.MainSound].VolumeMultiplier = Math.Max(shipSpeedVolume - m_wheelVolumeModifierEngine * m_groupData.WheelsLowerThrusterVolumeBy, 0f);
                        m_emitters[(int)ShipEmitters.WheelsMain].VolumeMultiplier = shipThrusterRatio * m_wheelVolumeModifierEngine * m_singleSoundsModifier;
                        m_emitters[(int)ShipEmitters.WheelsMain].Sound.FrequencyRatio = thrusterPitch;
                        m_emitters[(int)ShipEmitters.WheelsSecondary].VolumeMultiplier = CalculateVolumeFromSpeed(shipSpeedRatio, ref m_groupData.WheelsVolumes) * m_shipEngineModifier * m_wheelVolumeModifierWheels * m_singleSoundsModifier;
                    }

                    //speed up/down sounds
                    float speedUpDownVolume = 0.5f + shipThrusterRatio / 2f;
                    m_playingSpeedUpOrDown = m_playingSpeedUpOrDown && m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying;

                    //speed up
                    if (m_speedChange >= 20 && m_timers[(int)ShipTimers.SpeedUp] <= 0f && m_wheelVolumeModifierEngine <= 0f)
                    {
                        m_timers[(int)ShipTimers.SpeedUp] = (m_shipGrid.GridSizeEnum == MyCubeSize.Large ? 8f : 1f);
                        if (m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedDown)))
                            FadeOutSound(duration: 1000);
                        m_emitters[(int)ShipEmitters.SingleSounds].VolumeMultiplier = speedUpDownVolume;
                        PlayShipSound(ShipEmitters.SingleSounds, ShipSystemSoundsEnum.EnginesSpeedUp, false, false);
                        m_playingSpeedUpOrDown = true;
                    }
                    else if (m_speedChange <= 15 && m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedUp)))
                        FadeOutSound(duration: 1000);

                    //speed down
                    if (m_speedChange <= 10 && m_timers[(int)ShipTimers.SpeedDown] <= 0f && m_wheelVolumeModifierEngine <= 0f)
                    {
                        m_timers[(int)ShipTimers.SpeedDown] = (m_shipGrid.GridSizeEnum == MyCubeSize.Large ? 8f : 2f);
                        if (m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedUp)))
                            FadeOutSound(duration: 1000);
                        m_emitters[(int)ShipEmitters.SingleSounds].VolumeMultiplier = speedUpDownVolume;
                        PlayShipSound(ShipEmitters.SingleSounds, ShipSystemSoundsEnum.EnginesSpeedDown, false, false);
                        m_playingSpeedUpOrDown = true;
                    }
                    else if (m_speedChange >= 15 && m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedDown)))
                        FadeOutSound(duration: 1000);

                    //volume change for all sound if speed up/down is playing
                    float singleSoundVolumeTarget = 1f;
                    if (m_playingSpeedUpOrDown && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedDown)))
                        singleSoundVolumeTarget = m_groupData.SpeedDownSoundChangeVolumeTo;
                    if (m_playingSpeedUpOrDown && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedUp)))
                        singleSoundVolumeTarget = m_groupData.SpeedUpSoundChangeVolumeTo;
                    if (m_singleSoundsModifier < singleSoundVolumeTarget)
                        m_singleSoundsModifier = Math.Min(m_singleSoundsModifier + m_groupData.SpeedUpDownChangeSpeed, singleSoundVolumeTarget);
                    else if (m_singleSoundsModifier > singleSoundVolumeTarget)
                        m_singleSoundsModifier = Math.Max(m_singleSoundsModifier - m_groupData.SpeedUpDownChangeSpeed, singleSoundVolumeTarget);

                    //speed down volume
                    if (m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && (m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedDown)) || m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSystemSoundsEnum.EnginesSpeedUp))))
                        m_emitters[(int)ShipEmitters.SingleSounds].VolumeMultiplier = speedUpDownVolume;
                }
                else
                {
                    if (m_shipEngineModifier > 0f)
                        m_shipEngineModifier = Math.Max(0f, m_shipEngineModifier - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / m_groupData.EngineTimeToTurnOff);
                }

                if (m_shipThrusters != null && m_shipThrusters.ThrustCount <= 0)
                    m_shipThrusters = null;

                //speed up / speed down variable
                if (Math.Abs(shipSpeed - m_lastFrameShipSpeed) > 0.01f && shipSpeed >= 3f)
                    m_speedChange = (int)MyMath.Clamp(m_speedChange + (shipSpeed > m_lastFrameShipSpeed ? 1 : -1), 0, 30);
                else if (m_speedChange != 15)
                    m_speedChange += m_speedChange > 15 ? -1 : 1;

                //speed up / speed down timers
                if (shipSpeed >= m_lastFrameShipSpeed && m_timers[(int)ShipTimers.SpeedDown] > 0f)
                    m_timers[(int)ShipTimers.SpeedDown] -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (shipSpeed <= m_lastFrameShipSpeed && m_timers[(int)ShipTimers.SpeedUp] > 0f)
                    m_timers[(int)ShipTimers.SpeedUp] -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                m_lastFrameShipSpeed = shipSpeed;
            }
        }

        #endregion

        #region SlowUpdate

        public void Update100()
        {
            m_distanceToShip = m_initialized && m_shipGrid != null && m_definition != null && m_shipGrid.Physics != null ? (m_shouldPlay2D ? 0 : (float)Vector3D.DistanceSquared(MySector.MainCamera.Position, m_shipGrid.PositionComp.GetPosition())) : float.MaxValue;
            UpdateCategory();
            UpdateSounds();
            UpdateWheels();
        }

        //thruster composition + ship category
        private void UpdateCategory()
        {
            if (m_initialized && m_shipGrid != null && m_shipGrid.Physics != null && m_shipGrid.IsStatic == false && m_definition != null)
            {
                if (m_distanceToShip < m_definition.MaxUpdateRange_sq)
                {
                    if (m_shipThrusters == null)
                        m_shipThrusters = m_shipGrid.Components.Get<MyEntityThrustComponent>();
                    if (m_shipWheels == null)
                        m_shipWheels = m_shipGrid.GridSystems.WheelSystem;

                    CalculateShipCategory();
                    if (m_isDebris == false && m_shipState != ShipStateEnum.NoPower && (m_singleThrusterTypeShip == false || ShipHasChanged
                        || m_shipThrusters == null || m_shipThrusters.FinalThrust == Vector3.Zero || (m_shipWheels != null && m_shipWheels.HasWorkingWheels(false))))
                        CalculateThrusterComposition();

                    if (m_shipSoundSource == null)
                        m_shipSoundSource = m_shipGrid;
                    if (m_shipGrid.MainCockpit != null && m_shipGrid.GridSizeEnum == MyCubeSize.Small)
                        m_shipSoundSource = m_shipGrid.MainCockpit;

                    if (m_shipGrid.GridSizeEnum == MyCubeSize.Large && MySession.Static != null && MySession.Static.LocalCharacter != null)
                    {
                        if (MySession.Static.Settings.RealisticSound == false 
                            || (MySession.Static.LocalCharacter.AtmosphereDetectorComp != null && (MySession.Static.LocalCharacter.AtmosphereDetectorComp.InAtmosphere || MySession.Static.LocalCharacter.AtmosphereDetectorComp.InShipOrStation)))
                        {
                            BoundingSphereD playerSphere = new BoundingSphereD(MySession.Static.LocalCharacter.PositionComp.GetPosition(), m_definition.LargeShipDetectionRadius);
                            m_shipGrid.GetBlocksInsideSphere(ref playerSphere, m_detectedBlocks);
                            m_insideShip = m_detectedBlocks.Count > 0;
                        }
                        else
                            m_insideShip = false;
                    }
                }
            }
        }

        //sound emitter update
        private void UpdateSounds()
        {
            for (int i = 0; i < m_emitters.Length; i++)
            {
                if (m_emitters[i] != null)
                {
                    m_emitters[i].Entity = m_shipSoundSource;
                    m_emitters[i].Update();
                }
            }
        }

        //wheel contact point callbacks
        private void UpdateWheels()
        {
            if (m_shipGrid != null && m_shipGrid.Physics != null && m_shipWheels != null && m_shipWheels.WheelCount > 0)
            {
                bool start = m_distanceToShip < m_definition.WheelsCallbackRangeCreate_sq && m_isDebris == false;
                bool stop = m_distanceToShip > m_definition.WheelsCallbackRangeRemove_sq || m_isDebris;
                if ((start || stop) && (m_lastWheelUpdateStart != start || m_lastWheelUpdateStop != stop))
                {
                    foreach (var motor in m_shipWheels.Wheels)
                    {
                        if (motor == null || motor.RotorGrid == null || motor.RotorGrid.Physics == null || motor.RotorGrid.Physics.RigidBody == null)
                            continue;
                        if (motor.RotorGrid.HasShipSoundEvents == false && start)
                        {
                            motor.RotorGrid.Physics.RigidBody.ContactPointCallback += RigidBody_ContactPointCallback;
                            motor.RotorGrid.Physics.RigidBody.CallbackLimit = 1;
                            motor.RotorGrid.OnClosing += RotorGrid_OnClosing;
                            motor.RotorGrid.HasShipSoundEvents = true;
                        }
                        else if (motor.RotorGrid.HasShipSoundEvents && stop)
                        {
                            motor.RotorGrid.HasShipSoundEvents = false;
                            motor.RotorGrid.Physics.RigidBody.ContactPointCallback -= RigidBody_ContactPointCallback;
                            motor.RotorGrid.OnClosing -= RotorGrid_OnClosing;
                        }
                    }
                    m_lastWheelUpdateStart = start;
                    m_lastWheelUpdateStop = stop;
                    if (start && m_shipWheelsAction == false)
                    {
                        m_shipWheels.OnMotorUnregister += m_shipWheels_OnMotorUnregister;
                        m_shipWheelsAction = true;
                    }
                    else if (stop && m_shipWheelsAction)
                    {
                        m_shipWheels.OnMotorUnregister -= m_shipWheels_OnMotorUnregister;
                        m_shipWheelsAction = false;
                    }
                }
            }
        }

        void m_shipWheels_OnMotorUnregister(MyCubeGrid obj)
        {
            if (obj.HasShipSoundEvents)
            {
                obj.HasShipSoundEvents = false;
                RotorGrid_OnClosing(obj);
            }
        }

        void RotorGrid_OnClosing(MyEntity obj)
        {
            obj.Physics.RigidBody.ContactPointCallback -= RigidBody_ContactPointCallback;
            obj.OnClose -= RotorGrid_OnClosing;
        }

        void RigidBody_ContactPointCallback(ref Havok.HkContactPointEvent A_0)
        {
            m_lastContactWithGround = DateTime.UtcNow;
        }


        //calculate ratio between each thruster type based on their current propulsion power
        private void CalculateThrusterComposition()
        {
            if (m_shipThrusters == null)
            {
                m_thrusterVolumeTargets[(int)ShipThrusters.Ion] = 0f;
                m_thrusterVolumeTargets[(int)ShipThrusters.Hydrogen] = 0f;
                m_thrusterVolumeTargets[(int)ShipThrusters.Atmospheric] = 0f;
                return;
            }
            float ion = 0f;
            float hydro = 0f;
            float atmo = 0f;

            bool hasIon = false;
            bool hasAtmo = false;
            bool hasHydro = false;

            //calculate total force for each thruster type
            foreach (MyThrust thruster in m_shipGrid.GetFatBlocks<MyThrust>())
            {
                if (thruster != null)
                {
                    if (thruster.BlockDefinition.ThrusterType == m_thrusterHydrogen)
                    {
                        hydro += thruster.CurrentStrength * (Math.Abs(thruster.ThrustForce.X) + Math.Abs(thruster.ThrustForce.Y) + Math.Abs(thruster.ThrustForce.Z));
                        hasHydro = hasHydro || (thruster.IsFunctional && thruster.Enabled);
                    }
                    else if (thruster.BlockDefinition.ThrusterType == m_thrusterAtmospheric)
                    {
                        atmo += thruster.CurrentStrength * (Math.Abs(thruster.ThrustForce.X) + Math.Abs(thruster.ThrustForce.Y) + Math.Abs(thruster.ThrustForce.Z));
                        hasAtmo = hasAtmo || (thruster.IsFunctional && thruster.Enabled);
                    }
                    else
                    {
                        ion += thruster.CurrentStrength * (Math.Abs(thruster.ThrustForce.X) + Math.Abs(thruster.ThrustForce.Y) + Math.Abs(thruster.ThrustForce.Z));
                        hasIon = hasIon || (thruster.IsFunctional && thruster.Enabled);
                    }
                }
            }

            ShipHasChanged = false;
            m_singleThrusterTypeShip = !((hasIon && hasAtmo) || (hasIon && hasHydro) || (hasHydro && hasAtmo));

            //calculate volume modifiers for each thruster types based on ratio between them
            if (m_singleThrusterTypeShip)
            {
                m_thrusterVolumeTargets[(int)ShipThrusters.Ion] = hasIon ? 1f : 0f;
                m_thrusterVolumeTargets[(int)ShipThrusters.Hydrogen] = hasHydro ? 1f : 0f;
                m_thrusterVolumeTargets[(int)ShipThrusters.Atmospheric] = hasAtmo ? 1f : 0f;
                if (!hasIon && !hasHydro && !hasAtmo)
                    ShipHasChanged = true;
            }
            else if (ion + hydro + atmo > 0f)//at least one thruster is thrusting
            {
                float sum = hydro + ion + atmo;
                ion = ion > 0f ? (m_groupData.ThrusterCompositionMinVolume_c + (ion / sum)) / (1f + m_groupData.ThrusterCompositionMinVolume_c) : 0f;
                hydro = hydro > 0f ? (m_groupData.ThrusterCompositionMinVolume_c + (hydro / sum)) / (1f + m_groupData.ThrusterCompositionMinVolume_c) : 0f;
                atmo = atmo > 0f ? (m_groupData.ThrusterCompositionMinVolume_c + (atmo / sum)) / (1f + m_groupData.ThrusterCompositionMinVolume_c) : 0f;
                m_thrusterVolumeTargets[(int)ShipThrusters.Ion] = ion;
                m_thrusterVolumeTargets[(int)ShipThrusters.Hydrogen] = hydro;
                m_thrusterVolumeTargets[(int)ShipThrusters.Atmospheric] = atmo;
            }

            //stop obsolete sounds
            if (m_thrusterVolumes[(int)ShipThrusters.Ion] <= 0f && m_emitters[(int)ShipEmitters.IonThrusters].IsPlaying)
            {
                m_emitters[(int)ShipEmitters.IonThrusters].StopSound(false);
                m_emitters[(int)ShipEmitters.IonThrustersIdle].StopSound(false);
            }
            if (m_thrusterVolumes[(int)ShipThrusters.Hydrogen] <= 0f && m_emitters[(int)ShipEmitters.HydrogenThrusters].IsPlaying)
            {
                m_emitters[(int)ShipEmitters.HydrogenThrusters].StopSound(false);
                m_emitters[(int)ShipEmitters.HydrogenThrustersIdle].StopSound(false);
            }
            if (m_thrusterVolumes[(int)ShipThrusters.Atmospheric] <= 0f && m_emitters[(int)ShipEmitters.AtmosphericThrusters].IsPlaying)
            {
                m_emitters[(int)ShipEmitters.AtmosphericThrusters].StopSound(false);
                m_emitters[(int)ShipEmitters.AtmosphericThrustersIdle].StopSound(false);
            }

            //check if we need to play new sound
            if ((m_thrusterVolumeTargets[(int)ShipThrusters.Ion] > 0f && !m_emitters[(int)ShipEmitters.IonThrusters].IsPlaying)
                || (m_thrusterVolumeTargets[(int)ShipThrusters.Hydrogen] > 0f && !m_emitters[(int)ShipEmitters.HydrogenThrusters].IsPlaying)
                || (m_thrusterVolumeTargets[(int)ShipThrusters.Atmospheric] > 0f && !m_emitters[(int)ShipEmitters.AtmosphericThrusters].IsPlaying))
                m_forceSoundCheck = true;
        }


        private void CalculateShipCategory()
        {
            bool originalDebris = m_isDebris;
            MyDefinitionId originalType = m_shipCategory;
            if (m_shipThrusters == null && (m_shipWheels == null || m_shipWheels.WheelCount <= 0))
                m_isDebris = true;
            else if (m_shipGrid.GridSystems.GyroSystem.GyroCount == 0)
                m_isDebris = true;
            else
            {
                bool hasController = false;
                foreach (var block in m_shipGrid.GetFatBlocks())
                {
                    if (block is MyShipController)
                    {
                        if (m_shipGrid.MainCockpit == null && m_shipGrid.GridSizeEnum == MyCubeSize.Small)
                            m_shipSoundSource = block as MyEntity;
                        hasController = true;
                        break;
                    }
                }
                if (hasController)
                {
                    int shipMass = m_shipGrid.GetCurrentMass();
                    float bestWeight = float.MinValue;
                    foreach (var group in m_categories.Values)
                    {
                        if (group.MinWeight < shipMass && ((group.AllowSmallGrid && m_shipGrid.GridSizeEnum == MyCubeSize.Small) || (group.AllowLargeGrid && m_shipGrid.GridSizeEnum == MyCubeSize.Large)))
                        {
                            if (bestWeight == float.MinValue || group.MinWeight > bestWeight)
                            {
                                bestWeight = group.MinWeight;
                                m_shipCategory = group.Id;
                                m_groupData = group;
                            }
                        }
                    }
                    if (bestWeight == float.MinValue)
                        m_isDebris = true;
                    else
                        m_isDebris = false;
                }
                else
                    m_isDebris = true;
            }
            if(m_groupData == null)
                m_isDebris = true;

            if (originalType != m_shipCategory || m_isDebris != originalDebris)
            {
                m_categoryChange = true;
                if (m_isDebris)
                {
                    for (int i = 0; i < m_emitters.Length; i++)
                    {
                        if(m_emitters[i].IsPlaying && m_emitters[i].Loop)
                        {
                            if(i == (int)ShipEmitters.WheelsMain || i == (int)ShipEmitters.WheelsSecondary)
                                m_emitters[i].StopSound(m_shipWheels == null);
                            else
                                m_emitters[i].StopSound(m_shipThrusters == null);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < m_emitters.Length; i++)
                    {
                        if (m_emitters[i].IsPlaying && m_emitters[i].Loop)
                            m_emitters[i].StopSound(true);
                    }
                }
            }
            if (m_isDebris)
                SetGridSounds(false);
            else
                SetGridSounds(true);
        }


        //turn on or off normal sounds of certain blocks (reactors, gyros, thrusters)
        private void SetGridSounds(bool silent)
        {
            bool originalChange;
            foreach (var block in m_shipGrid.GetFatBlocks())
            {
                if (block.BlockDefinition.SilenceableByShipSoundSystem && block.IsSilenced != silent)
                {
                    originalChange = block.SilenceInChange;
                    block.SilenceInChange = true;
                    block.IsSilenced = silent;
                    if (originalChange == false)
                    {
                        block.UsedUpdateEveryFrame = (block.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) != 0;
                        block.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                    }
                }
            }
        }

        #endregion

        #region Utility

        private float CalculateVolumeFromSpeed(float speedRatio, ref List<MyTuple<float, float>> pairs)
        {
            float result = 1f;
            if (pairs.Count > 0)
                result = pairs[pairs.Count - 1].Item2;

            for (int i = 1; i < pairs.Count; i++)
            {
                if (speedRatio < pairs[i].Item1)
                {
                    result = pairs[i - 1].Item2 + (pairs[i].Item2 - pairs[i - 1].Item2) * ((speedRatio - pairs[i - 1].Item1) / (pairs[i].Item1 - pairs[i - 1].Item1));
                    break;
                }
            }
            return result;
        }

        private void FadeOutSound(ShipEmitters emitter = ShipEmitters.SingleSounds, int duration = 2000)
        {
            if (m_emitters[(int)emitter].IsPlaying)
            {
                var effectSourceVoice = MyAudio.Static.ApplyEffect(m_emitters[(int)emitter].Sound, m_fadeOut, new MyCueId[] { }, duration);
                m_emitters[(int)emitter].Sound = effectSourceVoice.OutputSound;
            }
            if (emitter == ShipEmitters.SingleSounds)
                m_playingSpeedUpOrDown = false;
        }

        private void PlayShipSound(ShipEmitters emitter, ShipSystemSoundsEnum sound, bool checkIfAlreadyPlaying = true, bool stopPrevious = true, bool useForce2D = true, bool useFadeOut = false)
        {
            MySoundPair soundPair = GetShipSound(sound);
            if (soundPair == MySoundPair.Empty)
                return;
            if (m_emitters[(int)emitter] != null)
            {
                if (checkIfAlreadyPlaying && m_emitters[(int)emitter].IsPlaying && m_emitters[(int)emitter].SoundPair == soundPair)
                    return;
                if (m_emitters[(int)emitter].IsPlaying && useFadeOut)
                {
                    var effect = MyAudio.Static.ApplyEffect(m_emitters[(int)emitter].Sound, MyStringHash.GetOrCompute("CrossFade"), new MyCueId[] { soundPair.SoundId }, 1500);
                    m_emitters[(int)emitter].Sound = effect.OutputSound;
                }
                else
                    m_emitters[(int)emitter].PlaySound(soundPair, stopPrevious, force2D: useForce2D && m_shouldPlay2D);
            }
        }

        private MySoundPair GetShipSound(ShipSystemSoundsEnum sound)
        {
            if (m_isDebris)
                return MySoundPair.Empty;
            MyShipSoundsDefinition soundGroup;
            if (m_categories.TryGetValue(m_shipCategory, out soundGroup))
            {
                MySoundPair result;
                if(soundGroup.Sounds.TryGetValue(sound, out result))
                    return result;
                else
                    return MySoundPair.Empty;
            }
            else
            {
                return MySoundPair.Empty;
            }
        }


        public override string ComponentTypeDebugString
        {
            get { return "ShipSoundSystem"; }
        }


        public void DestroyComponent()
        {
            for (int i = 0; i < m_emitters.Length; i++)
            {
                if (m_emitters[i] != null)
                {
                    m_emitters[i].StopSound(true);
                    m_emitters[i] = null;
                }
            }
            m_shipGrid = null;
            m_shipThrusters = null;
            m_shipWheels = null;
        }

        #endregion
    }
}