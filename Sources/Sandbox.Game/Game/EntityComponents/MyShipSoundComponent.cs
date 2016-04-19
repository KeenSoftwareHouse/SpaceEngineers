using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
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

        private enum ShipTypeEnum
        {
            Debris = 0,//no thrusters or ship controller or too small -> no sound
            Tiny = 100,
            Small = 200,
            Medium = 300,
            Large = 400,
            Huge = 500
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
            LargeShipIdle = 10,
            ShipEngine = 11
        }

        private enum ShipSounds
        {
            MainLoopSlow = 0,
            MainLoopMedium = 1,
            MainLoopFast = 2,
            IonThrusters = 3,
            EnginesStart = 4,
            EnginesEnd = 5,
            HydrogenThrusters = 6,
            AtmoThrustersSlow = 7,
            AtmoThrustersMedium = 8,
            AtmoThrustersFast = 9,
            IonThrustersIdle = 10,
            HydrogenThrustersIdle = 11,
            AtmoThrustersIdle = 12,
            WheelsEngineRun = 13,
            LargeShipIdle = 14,
            ShipEngine = 15,
            EnginesSpeedUp = 16,
            EnginesSpeedDown = 17,
            WheelsSecondary = 18
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

        private void InitSoundPairs()
        {
            m_initializeAllSounds = false;

            //small size ships
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.MainLoopSlow, new MySoundPair("ShipSmallRunSlow"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.MainLoopMedium, new MySoundPair("ShipSmallRunMedium"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.MainLoopFast, new MySoundPair("ShipSmallRunFast"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.EnginesStart, new MySoundPair("ShipSmallStart"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.EnginesEnd, new MySoundPair("ShipSmallEnd"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.EnginesSpeedUp, new MySoundPair("ShipSmallSpeedUp"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.EnginesSpeedDown, new MySoundPair("ShipSmallSpeedDown"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.ShipEngine, new MySoundPair("ShipSmallEngine"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.IonThrusters, new MySoundPair("ShipSmallThrusterIon"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.IonThrustersIdle, new MySoundPair("ShipSmallThrusterIonIdle"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.HydrogenThrusters, new MySoundPair("ShipSmallThrusterHydrogen"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.HydrogenThrustersIdle, new MySoundPair("ShipSmallThrusterHydrogenIdle"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.AtmoThrustersSlow, new MySoundPair("ShipSmallThrusterAtmosphericSlow"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.AtmoThrustersMedium, new MySoundPair("ShipSmallThrusterAtmosphericMedium"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.AtmoThrustersFast, new MySoundPair("ShipSmallThrusterAtmosphericFast"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.AtmoThrustersIdle, new MySoundPair("ShipSmallThrusterAtmosphericIdle"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.WheelsEngineRun, new MySoundPair("ShipSmallWheelsRun"));
            m_shipSoundDatabase.Add((int)ShipTypeEnum.Small + (int)ShipSounds.WheelsSecondary, new MySoundPair("ShipSmallWheelsGround"));

            //large size ships
            if (MyFakes.ENABLE_NEW_LARGE_SHIP_SOUNDS)
            {
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.MainLoopSlow, new MySoundPair("ShipLargeRunLoop"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.MainLoopMedium, new MySoundPair("ShipLargeRunLoop"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.MainLoopFast, new MySoundPair("ShipLargeRunLoop"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.EnginesStart, new MySoundPair("ShipLargeStart"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.EnginesEnd, new MySoundPair("ShipLargeEnd"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.EnginesSpeedUp, new MySoundPair("ShipLargeSpeedUp"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.EnginesSpeedDown, new MySoundPair("ShipLargeSpeedDown"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.LargeShipIdle, new MySoundPair("ShipLargeIdle"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.ShipEngine, new MySoundPair("ShipLargeEngine"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.IonThrusters, new MySoundPair("ShipLargeThrusterIon"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.IonThrustersIdle, new MySoundPair("ShipLargeThrusterIonIdle"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.HydrogenThrusters, new MySoundPair("ShipLargeThrusterHydrogen"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.HydrogenThrustersIdle, new MySoundPair("ShipLargeThrusterHydrogenIdle"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.AtmoThrustersSlow, new MySoundPair("ShipLargeThrusterAtmosphericSlow"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.AtmoThrustersMedium, new MySoundPair("ShipLargeThrusterAtmosphericMedium"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.AtmoThrustersFast, new MySoundPair("ShipLargeThrusterAtmosphericFast"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.AtmoThrustersIdle, new MySoundPair("ShipLargeThrusterAtmosphericIdle"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.WheelsEngineRun, new MySoundPair("ShipLargeWheelsRun"));
                m_shipSoundDatabase.Add((int)ShipTypeEnum.Large + (int)ShipSounds.WheelsSecondary, new MySoundPair("ShipLargeWheelsGround"));
            }
        }

        #endregion

        #region MainConstants

        //main constants
        private const float MAX_UPDATE_RANGE = 2000f;
        private const float FULL_SPEED = 95f;// m/s
        private const float LARGE_SHIP_DETECTION_RADIUS = 15f;

        //main sound constants
        private const float ENGINES_RUN_THRESHOLD = 0.25f;// seconds
        private const float ENGINES_RUN_THRESHOLD2 = 1f;// seconds
        private const float ENGINES_MIN_VOLUME = 0.25f;// ratio
        private const float ENGINES_PITCH_RANGE = 4f;// semitones
        private const float ENGINES_TIME_TURN_ON = 3f;// seconds
        private const float ENGINES_TIME_TURN_OFF = 3f;// seconds
        private const float ENGINES_SPEED_THRESHOLD_1 = 33f;// m/s
        private const float ENGINES_SPEED_THRESHOLD_2 = 66f;// m/s

        //ship categories constants
        private const int SHIP_CATEGORY_MASS_TINY =    4000;// kg
        private const int SHIP_CATEGORY_MASS_SMALL =  15000;// kg
        private const int SHIP_CATEGORY_MASS_MEDIUM = 30000;// kg
        private const int SHIP_CATEGORY_MASS_LARGE =  75000;// kg
        private const int SHIP_CATEGORY_MASS_HUGE =  200000;// kg

        //wheel constants
        private const float WHEELS_LOWER_THRUSTERS_BY_RATIO = 0.33f;// %
        private const float WHEELS_SPEED_COMPENSATION = 3f;// %
        private const float WHEELS_GROUND_MIN_VOLUME = 0.5f;// ratio

        //thruster sound constants
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_1 = 25f;// m/s
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_2 = 50f;// m/s
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_3 = 75f;// m/s
        private const float THRUSTER_RATIO_VOLUME_1 = 0.4f;// ratio
        private const float THRUSTER_RATIO_VOLUME_2 = 0.65f;// ratio
        private const float THRUSTER_RATIO_VOLUME_3 = 0.85f;// ratio
        private const float THRUSTER_COMPOSITION_CHANGE_SPEED = 0.025f;
        private const float THRUSTER_COMPOSITION_MIN_VOLUME = 0.4f;

        #endregion

        #region CalculatedConstants

        private const float MAX_UPDATE_RANGE_SQ = MAX_UPDATE_RANGE * MAX_UPDATE_RANGE;
        private const float FULL_SPEED_SQ = FULL_SPEED * FULL_SPEED;
        private const float ENGINES_RUN_MAX = ENGINES_RUN_THRESHOLD2 + ENGINES_RUN_THRESHOLD;
        private const float ENGINES_PITCH_RANGE_HALF = ENGINES_PITCH_RANGE / 2f;
        private const float ENGINES_SPEED_THRESHOLD_1_SQ = ENGINES_SPEED_THRESHOLD_1 * ENGINES_SPEED_THRESHOLD_1;
        private const float ENGINES_SPEED_THRESHOLD_2_SQ = ENGINES_SPEED_THRESHOLD_2 * ENGINES_SPEED_THRESHOLD_2;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_1_SQ = THRUSTER_RATIO_SPEED_THRESHOLD_1 * THRUSTER_RATIO_SPEED_THRESHOLD_1;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_2_SQ = THRUSTER_RATIO_SPEED_THRESHOLD_2 * THRUSTER_RATIO_SPEED_THRESHOLD_2;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_3_SQ = THRUSTER_RATIO_SPEED_THRESHOLD_3 * THRUSTER_RATIO_SPEED_THRESHOLD_3;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_2_DIFF = THRUSTER_RATIO_SPEED_THRESHOLD_2_SQ - THRUSTER_RATIO_SPEED_THRESHOLD_1_SQ;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_3_DIFF = THRUSTER_RATIO_SPEED_THRESHOLD_3_SQ - THRUSTER_RATIO_SPEED_THRESHOLD_2_SQ;
        private const float THRUSTER_RATIO_SPEED_THRESHOLD_4_DIFF = FULL_SPEED_SQ - THRUSTER_RATIO_SPEED_THRESHOLD_3_SQ;
        private const float THRUSTER_RATIO_VOLUME_2_DIFF = THRUSTER_RATIO_VOLUME_2 - THRUSTER_RATIO_VOLUME_1;
        private const float THRUSTER_RATIO_VOLUME_3_DIFF = THRUSTER_RATIO_VOLUME_3 - THRUSTER_RATIO_VOLUME_2;
        private const float THRUSTER_RATIO_VOLUME_4_DIFF = 1f - THRUSTER_RATIO_VOLUME_3;
        private const float THRUSTER_COMPOSITION_MIN_VOLUME_CALC = THRUSTER_COMPOSITION_MIN_VOLUME / (1f - THRUSTER_COMPOSITION_MIN_VOLUME);

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
        private ShipTypeEnum m_shipCategory = ShipTypeEnum.Debris;
        private bool m_categoryChange = false;
        private bool m_forceSoundCheck = false;
        private bool m_driving = false;
        private float m_wheelVolumeModifier = 0;

        private ShipStateEnum m_shipState = ShipStateEnum.NoPower;
        private float m_shipEngineModifier = 0f;

        private MyEntity3DSoundEmitter[] m_emitters = new MyEntity3DSoundEmitter[(Enum.GetNames(typeof(ShipEmitters)).Length)];
        private float[] m_thrusterVolumes;
        private float[] m_thrusterVolumeTargets;
        private bool m_singleThrusterTypeShip = false;

        private static MyStringHash m_thrusterIon = MyStringHash.GetOrCompute("Ion");
        private static MyStringHash m_thrusterHydrogen = MyStringHash.GetOrCompute("Hydrogen");
        private static MyStringHash m_thrusterAtmospheric = MyStringHash.GetOrCompute("Atmospheric");
        private static MyStringHash m_crossfade = MyStringHash.GetOrCompute("CrossFade");
        private static bool m_initializeAllSounds = true;
        private static Dictionary<int, MySoundPair> m_shipSoundDatabase = new Dictionary<int, MySoundPair>();

        private float[] m_timers = new float[(Enum.GetNames(typeof(ShipTimers)).Length)];
        private float m_lastFrameShipSpeed = 0f;
        private int m_speedChange = 15;

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
            if (m_initializeAllSounds)
                InitSoundPairs();
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
                m_emitters[i] = new MyEntity3DSoundEmitter(m_shipGrid);
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
            if (m_initialized && m_shipGrid.Physics != null && m_shipGrid.IsStatic == false && (m_shipThrusters != null || m_shipWheels != null) && m_distanceToShip < MAX_UPDATE_RANGE_SQ)
            {
                //calculate current ship state
                m_driving = m_shipWheels.HasWorkingWheels(true);
                float shipSpeedSquered = m_driving == false ? m_shipGrid.Physics.LinearVelocity.LengthSquared() : (m_shipGrid.Physics.LinearVelocity * WHEELS_SPEED_COMPENSATION).LengthSquared();
                ShipStateEnum lastState = m_shipState;
                if (m_shipGrid.GridSystems.ResourceDistributor.ResourceState == MyResourceStateEnum.NoPower || m_shipCategory == ShipTypeEnum.Debris
                    || ((m_shipThrusters == null || m_shipThrusters.ThrustCount <= 0) && (m_shipWheels == null || m_shipWheels.WheelCount <= 0)))
                {
                    m_shipState = ShipStateEnum.NoPower;
                }
                else
                {
                    if (shipSpeedSquered < ENGINES_SPEED_THRESHOLD_1_SQ)
                        m_shipState = ShipStateEnum.Slow;
                    else if (shipSpeedSquered < ENGINES_SPEED_THRESHOLD_2_SQ)
                        m_shipState = ShipStateEnum.Medium;
                    else
                        m_shipState = ShipStateEnum.Fast;
                }

                //in first person change
                bool orig = m_shouldPlay2D;
                if (m_shipGrid.GridSizeEnum == MyCubeSize.Large)
                    m_shouldPlay2D = m_insideShip;
                else if (MySession.Static.ControlledEntity != null && MySession.Static.IsCameraUserControlledSpectator() == false && MySession.Static.ControlledEntity.Entity != null && MySession.Static.ControlledEntity.Entity.Parent == m_shipGrid)
                {
                    m_shouldPlay2D = ((MySession.Static.ControlledEntity.Entity is MyCockpit) && (MySession.Static.ControlledEntity.Entity as MyCockpit).IsInFirstPersonView)
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
                        m_thrusterVolumes[i] = Math.Min(m_thrusterVolumes[i] + THRUSTER_COMPOSITION_CHANGE_SPEED, m_thrusterVolumeTargets[i]);
                    }
                    else if (m_thrusterVolumes[i] > m_thrusterVolumeTargets[i])
                    {
                        m_thrusterVolumes[i] = Math.Max(m_thrusterVolumes[i] - THRUSTER_COMPOSITION_CHANGE_SPEED, m_thrusterVolumeTargets[i]);
                    }
                }

                if (m_driving)
                    m_wheelVolumeModifier = Math.Min(m_wheelVolumeModifier + 0.04f, 1f);
                else
                    m_wheelVolumeModifier = Math.Max(m_wheelVolumeModifier - 0.005f, 0f);

                //play sounds if there was change in state, ship type or thruster composition
                if (m_shipState != lastState || m_categoryChange || m_forceSoundCheck)
                {
                    if (m_shipState == ShipStateEnum.NoPower)
                    {
                        if (m_shipState != lastState)
                        {
                            for (int i = 0; i < m_emitters.Length; i++)
                                m_emitters[i].StopSound(false);
                            PlayShipSound(ShipEmitters.SingleSounds, ShipSounds.EnginesEnd);
                        }
                    }
                    else
                    {
                        if (m_shipState == ShipStateEnum.Slow)
                            PlayShipSound(ShipEmitters.MainSound, ShipSounds.MainLoopSlow);
                        else if (m_shipState == ShipStateEnum.Medium)
                            PlayShipSound(ShipEmitters.MainSound, ShipSounds.MainLoopMedium);
                        else if (m_shipState == ShipStateEnum.Fast)
                            PlayShipSound(ShipEmitters.MainSound, ShipSounds.MainLoopFast);
                        
                        PlayShipSound(ShipEmitters.ShipEngine, ShipSounds.ShipEngine);
                        if (m_shipGrid.GridSizeEnum == MyCubeSize.Large)
                            PlayShipSound(ShipEmitters.LargeShipIdle, ShipSounds.LargeShipIdle);

                        if (m_thrusterVolumes[(int)ShipThrusters.Ion] > 0f)
                        {
                            PlayShipSound(ShipEmitters.IonThrusters, ShipSounds.IonThrusters);
                            PlayShipSound(ShipEmitters.IonThrustersIdle, ShipSounds.IonThrustersIdle);
                        }

                        if (m_thrusterVolumes[(int)ShipThrusters.Hydrogen] > 0f)
                        {
                            PlayShipSound(ShipEmitters.HydrogenThrusters, ShipSounds.HydrogenThrusters);
                            PlayShipSound(ShipEmitters.HydrogenThrustersIdle, ShipSounds.HydrogenThrustersIdle);
                        }

                        if (m_thrusterVolumes[(int)ShipThrusters.Atmospheric] > 0f)
                        {
                            if (m_shipState == ShipStateEnum.Slow)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSounds.AtmoThrustersSlow, useFadeOut: true);
                            else if (m_shipState == ShipStateEnum.Medium)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSounds.AtmoThrustersMedium, useFadeOut: true);
                            else if (m_shipState == ShipStateEnum.Fast)
                                PlayShipSound(ShipEmitters.AtmosphericThrusters, ShipSounds.AtmoThrustersFast, useFadeOut: true);
                            PlayShipSound(ShipEmitters.AtmosphericThrustersIdle, ShipSounds.AtmoThrustersIdle);
                        }

                        if (m_shipWheels.WheelCount > 0)
                        {
                            PlayShipSound(ShipEmitters.WheelsMain, ShipSounds.WheelsEngineRun);
                            PlayShipSound(ShipEmitters.WheelsSecondary, ShipSounds.WheelsSecondary);
                        }

                        if(lastState == ShipStateEnum.NoPower)
                            PlayShipSound(ShipEmitters.SingleSounds, ShipSounds.EnginesStart);
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
                        m_shipEngineModifier = Math.Min(1f, m_shipEngineModifier + MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / ENGINES_TIME_TURN_ON);
                    float shipSpeedRatio = Math.Min(shipSpeedSquered / FULL_SPEED_SQ, 1f);
                    float shipSpeedVolume = (ENGINES_MIN_VOLUME + (1f - ENGINES_MIN_VOLUME) * shipSpeedRatio) * m_shipEngineModifier;
                    float shipThrusterRatio;
                    float shipThrusterIdleRatio = 1f;

                    //main sound - engines
                    if (m_emitters[(int)ShipEmitters.MainSound].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.MainSound].VolumeMultiplier = shipSpeedVolume;
                        float semitones = -ENGINES_PITCH_RANGE_HALF + ENGINES_PITCH_RANGE * shipSpeedRatio;
                        m_emitters[(int)ShipEmitters.MainSound].Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
                    }

                    //thruster base volume ratio
                    if (shipSpeedSquered <= THRUSTER_RATIO_SPEED_THRESHOLD_1_SQ)
                        shipThrusterRatio = THRUSTER_RATIO_VOLUME_1 * shipSpeedSquered / THRUSTER_RATIO_SPEED_THRESHOLD_1_SQ;
                    else if (shipSpeedSquered <= THRUSTER_RATIO_SPEED_THRESHOLD_2_SQ)
                        shipThrusterRatio = THRUSTER_RATIO_VOLUME_1 + THRUSTER_RATIO_VOLUME_2_DIFF * (shipSpeedSquered - THRUSTER_RATIO_SPEED_THRESHOLD_1_SQ) / THRUSTER_RATIO_SPEED_THRESHOLD_2_DIFF;
                    else if (shipSpeedSquered <= THRUSTER_RATIO_SPEED_THRESHOLD_3_SQ)
                        shipThrusterRatio = THRUSTER_RATIO_VOLUME_2 + THRUSTER_RATIO_VOLUME_3_DIFF * (shipSpeedSquered - THRUSTER_RATIO_SPEED_THRESHOLD_2_SQ) / THRUSTER_RATIO_SPEED_THRESHOLD_3_DIFF;
                    else
                        shipThrusterRatio = THRUSTER_RATIO_VOLUME_3 + THRUSTER_RATIO_VOLUME_4_DIFF * (shipSpeedSquered - THRUSTER_RATIO_SPEED_THRESHOLD_3_SQ) / THRUSTER_RATIO_SPEED_THRESHOLD_4_DIFF;
                    shipThrusterRatio = Math.Max(Math.Min(shipThrusterRatio, 1f) - m_wheelVolumeModifier * WHEELS_LOWER_THRUSTERS_BY_RATIO, 0f);
                    shipThrusterIdleRatio = MyMath.Clamp(1.2f - shipThrusterRatio * 3f, 0f, 1f);

                    //large ship special emitters
                    m_emitters[(int)ShipEmitters.ShipEngine].VolumeMultiplier = Math.Max(0f, shipSpeedVolume - shipThrusterIdleRatio);
                    if (m_shipGrid.GridSizeEnum == MyCubeSize.Large)
                        m_emitters[(int)ShipEmitters.LargeShipIdle].VolumeMultiplier = shipThrusterIdleRatio * m_shipEngineModifier;

                    //ion thruster run/idle sounds volumes + pitch
                    float thrusterPitch = MyAudio.Static.SemitonesToFrequencyRatio(-ENGINES_PITCH_RANGE_HALF + ENGINES_PITCH_RANGE * shipThrusterRatio);
                    if (m_emitters[(int)ShipEmitters.IonThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.IonThrusters].VolumeMultiplier = shipThrusterRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Ion];
                        m_emitters[(int)ShipEmitters.IonThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.IonThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.IonThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Ion];

                    //hydrogen thruster run/idle sounds volumes + pitch
                    if (m_emitters[(int)ShipEmitters.HydrogenThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.HydrogenThrusters].VolumeMultiplier = shipThrusterRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Hydrogen];
                        m_emitters[(int)ShipEmitters.HydrogenThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.HydrogenThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.HydrogenThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Hydrogen];

                    //atmospheric thruster run/idle sounds volumes + pitch
                    if (m_emitters[(int)ShipEmitters.AtmosphericThrusters].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.AtmosphericThrusters].VolumeMultiplier = shipThrusterRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Atmospheric];
                        m_emitters[(int)ShipEmitters.AtmosphericThrusters].Sound.FrequencyRatio = thrusterPitch;
                    }
                    if (m_emitters[(int)ShipEmitters.AtmosphericThrustersIdle].IsPlaying)
                        m_emitters[(int)ShipEmitters.AtmosphericThrustersIdle].VolumeMultiplier = shipThrusterIdleRatio * m_shipEngineModifier * m_thrusterVolumes[(int)ShipThrusters.Atmospheric];

                    //wheels volume + pitch
                    if (m_emitters[(int)ShipEmitters.WheelsMain].IsPlaying)
                    {
                        m_emitters[(int)ShipEmitters.WheelsMain].VolumeMultiplier = shipThrusterRatio * m_shipEngineModifier * m_wheelVolumeModifier;
                        m_emitters[(int)ShipEmitters.WheelsMain].Sound.FrequencyRatio = thrusterPitch;
                        m_emitters[(int)ShipEmitters.WheelsSecondary].VolumeMultiplier = (WHEELS_GROUND_MIN_VOLUME + (1f - WHEELS_GROUND_MIN_VOLUME) * shipThrusterRatio) * m_shipEngineModifier * m_wheelVolumeModifier;
                    }

                    //speed up/down sounds
                    if (m_speedChange >= 20 && m_timers[(int)ShipTimers.SpeedUp] <= 0f && m_wheelVolumeModifier <= 0f)
                    {
                        m_timers[(int)ShipTimers.SpeedUp] = (m_shipGrid.GridSizeEnum == MyCubeSize.Large ? 8f : 1f);
                        if (m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSounds.EnginesSpeedDown)))
                            m_emitters[(int)ShipEmitters.SingleSounds].StopSound(false);
                        PlayShipSound(ShipEmitters.SingleSounds, ShipSounds.EnginesSpeedUp, false, false);
                    }
                    else if (m_speedChange <= 15 && m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSounds.EnginesSpeedUp)))
                        m_emitters[(int)ShipEmitters.SingleSounds].StopSound(false);
                    if (m_speedChange <= 10 && m_timers[(int)ShipTimers.SpeedDown] <= 0f && m_wheelVolumeModifier <= 0f)
                    {
                        m_timers[(int)ShipTimers.SpeedDown] = (m_shipGrid.GridSizeEnum == MyCubeSize.Large ? 8f : 1f);
                        if (m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSounds.EnginesSpeedUp)))
                            m_emitters[(int)ShipEmitters.SingleSounds].StopSound(false);
                        PlayShipSound(ShipEmitters.SingleSounds, ShipSounds.EnginesSpeedDown, false, false);
                    }
                    else if (m_speedChange >= 15 && m_emitters[(int)ShipEmitters.SingleSounds].IsPlaying && m_emitters[(int)ShipEmitters.SingleSounds].SoundPair.Equals(GetShipSound(ShipSounds.EnginesSpeedDown)))
                        m_emitters[(int)ShipEmitters.SingleSounds].StopSound(false);
                }
                else
                {
                    if (m_shipEngineModifier > 0f)
                        m_shipEngineModifier = Math.Max(0f, m_shipEngineModifier - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / ENGINES_TIME_TURN_OFF);
                }

                if (m_shipThrusters != null && m_shipThrusters.ThrustCount <= 0)
                    m_shipThrusters = null;

                //speed up / speed down variable
                if (Math.Abs(shipSpeedSquered - m_lastFrameShipSpeed) > 0.5f && shipSpeedSquered >= 9f)
                    m_speedChange = (int)MyMath.Clamp(m_speedChange + (shipSpeedSquered > m_lastFrameShipSpeed ? 1 : -1), 0, 30);
                else if (m_speedChange != 15)
                    m_speedChange += m_speedChange > 15 ? -1 : 1;

                //speed up / speed down timers
                if (shipSpeedSquered >= m_lastFrameShipSpeed && m_timers[(int)ShipTimers.SpeedDown] > 0f)
                    m_timers[(int)ShipTimers.SpeedDown] -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (shipSpeedSquered <= m_lastFrameShipSpeed && m_timers[(int)ShipTimers.SpeedUp] > 0f)
                    m_timers[(int)ShipTimers.SpeedUp] -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                m_lastFrameShipSpeed = shipSpeedSquered;
            }
        }

        #endregion

        #region SlowUpdate

        public void Update100()
        {
            m_distanceToShip = m_initialized && m_shipGrid.Physics != null ? (m_shouldPlay2D ? 0 : (float)Vector3D.DistanceSquared(MySector.MainCamera.Position, m_shipGrid.PositionComp.GetPosition())) : MAX_UPDATE_RANGE_SQ + 1;
            if (m_initialized && m_shipGrid.Physics != null && m_shipGrid.IsStatic == false && m_distanceToShip < MAX_UPDATE_RANGE_SQ)
            {
                if (m_shipThrusters == null)
                    m_shipThrusters = m_shipGrid.Components.Get<MyEntityThrustComponent>();
                if (m_shipWheels == null)
                    m_shipWheels = m_shipGrid.GridSystems.WheelSystem;

                CalculateShipCategory();
                if (m_shipCategory != ShipTypeEnum.Debris && m_shipState != ShipStateEnum.NoPower && (m_singleThrusterTypeShip == false || ShipHasChanged
                    || m_shipThrusters == null || m_shipThrusters.FinalThrust == Vector3.Zero || m_shipWheels.HasWorkingWheels(false)))
                    CalculateThrusterComposition();

                if (m_shipSoundSource == null)
                    m_shipSoundSource = m_shipGrid;
                if (m_shipGrid.MainCockpit != null && m_shipGrid.GridSizeEnum == MyCubeSize.Small)
                    m_shipSoundSource = m_shipGrid.MainCockpit;

                if (m_shipGrid.GridSizeEnum == MyCubeSize.Large && MySession.Static.LocalCharacter != null)
                {
                    BoundingSphereD playerSphere = new BoundingSphereD(MySession.Static.LocalCharacter.PositionComp.GetPosition(), LARGE_SHIP_DETECTION_RADIUS);
                    HashSet<MySlimBlock> detectedBlocks = new HashSet<MySlimBlock>();
                    m_shipGrid.GetBlocksInsideSphere(ref playerSphere, detectedBlocks);
                    m_insideShip = detectedBlocks.Count > 0;
                }
            }
            for (int i = 0; i < m_emitters.Length; i++)
            {
                m_emitters[i].Entity = m_shipSoundSource;
                m_emitters[i].Update();
            }
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
                ion = ion > 0f ? (THRUSTER_COMPOSITION_MIN_VOLUME_CALC + (ion / sum)) / (1f + THRUSTER_COMPOSITION_MIN_VOLUME_CALC) : 0f;
                hydro = hydro > 0f ? (THRUSTER_COMPOSITION_MIN_VOLUME_CALC + (hydro / sum)) / (1f + THRUSTER_COMPOSITION_MIN_VOLUME_CALC) : 0f;
                atmo = atmo > 0f ? (THRUSTER_COMPOSITION_MIN_VOLUME_CALC + (atmo / sum)) / (1f + THRUSTER_COMPOSITION_MIN_VOLUME_CALC) : 0f;
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
            ShipTypeEnum originalType = m_shipCategory;
            if (m_shipThrusters == null && (m_shipWheels == null || m_shipWheels.WheelCount <= 0))
            {
                m_shipCategory = ShipTypeEnum.Debris;
            }
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
                    if (shipMass >= SHIP_CATEGORY_MASS_HUGE)
                        m_shipCategory = ShipTypeEnum.Huge;
                    else if (shipMass >= SHIP_CATEGORY_MASS_LARGE)
                        m_shipCategory = ShipTypeEnum.Large;
                    else if (shipMass >= SHIP_CATEGORY_MASS_MEDIUM)
                        m_shipCategory = ShipTypeEnum.Medium;
                    else if (shipMass >= SHIP_CATEGORY_MASS_SMALL)
                        m_shipCategory = ShipTypeEnum.Small;
                    else if (shipMass >= SHIP_CATEGORY_MASS_TINY)
                        m_shipCategory = ShipTypeEnum.Tiny;
                    else
                        m_shipCategory = ShipTypeEnum.Debris;
                }
                else
                    m_shipCategory = ShipTypeEnum.Debris;
            }

            if (originalType != m_shipCategory)
            {
                m_categoryChange = true;
                if (m_shipCategory == ShipTypeEnum.Debris)
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
            if(m_shipCategory == ShipTypeEnum.Debris)
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

        private void PlayShipSound(ShipEmitters emitter, ShipSounds sound, bool checkIfAlreadyPlaying = true, bool stopPrevious = true, bool useForce2D = true, bool useFadeOut = false)
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


        private MySoundPair GetShipSound(ShipSounds sound)
        {
            if (m_shipCategory == ShipTypeEnum.Debris)
                return MySoundPair.Empty;
            MySoundPair result;
            if (m_shipSoundDatabase.TryGetValue((int)m_shipCategory + (int)sound, out result))//sound for correct size
                return result;
            else
            {
                int category = (int)m_shipCategory;
                while (category > (int)ShipTypeEnum.Tiny)//correct sound not found, try sounds for smaller ships
                {
                    category -= 100;
                    if (m_shipSoundDatabase.TryGetValue(category + (int)sound, out result))//sound for correct size
                        return result;
                }
                category = (int)m_shipCategory;
                while (category < (int)ShipTypeEnum.Huge)//correct sound not found, try sounds for bigger ships
                {
                    category += 100;
                    if (m_shipSoundDatabase.TryGetValue(category + (int)sound, out result))//sound for correct size
                        return result;
                }
            }
            return MySoundPair.Empty;
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