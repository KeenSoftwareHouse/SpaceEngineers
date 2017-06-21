using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Audio;
using VRage.Data.Audio;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    #region MySoundPair

    public class MySoundPair
    {
        public static MySoundPair Empty = new MySoundPair();
        [ThreadStatic]
        static StringBuilder m_cache;

        static StringBuilder Cache
        {
            get 
            {
                if (m_cache == null)
                    m_cache = new StringBuilder();
                return m_cache; 
            }
        }

		//jn:TODO create properties on cues or something
		private MyCueId m_arcade;
		public MyCueId Arcade { get { return m_arcade; } }

		private MyCueId m_realistic;
		public MyCueId Realistic { get { return m_realistic; } }

        public MySoundPair()
        {
            Init(null);
        }

        public MySoundPair(string cueName, bool useLog = true)
        {
            Init(cueName, useLog);
        }

        public void Init(string cueName, bool useLog = true)
        {
            if (string.IsNullOrEmpty(cueName) || MySandboxGame.IsDedicated || MyAudio.Static == null)
            {
                m_arcade = new MyCueId(MyStringHash.NullOrEmpty);
                m_realistic = new MyCueId(MyStringHash.NullOrEmpty);
            }
            else
            {
                m_arcade = MyAudio.Static.GetCueId(cueName);
                if (m_arcade.Hash != MyStringHash.NullOrEmpty)
                {
                    m_realistic = m_arcade;
                    return;
                }
                Cache.Clear();
                Cache.Append("Arc").Append(cueName);
                m_arcade = MyAudio.Static.GetCueId(Cache.ToString());
                Cache.Clear();
                Cache.Append("Real").Append(cueName);
                m_realistic = MyAudio.Static.GetCueId(Cache.ToString());

                //Debug.Assert(m_arcade.Hash != MyStringHash.NullOrEmpty || m_realistic.Hash != MyStringHash.NullOrEmpty, string.Format("Could not find any sound for '{0}'", cueName));
                if (useLog)
                {
                if (m_arcade.Hash == MyStringHash.NullOrEmpty && m_realistic.Hash == MyStringHash.NullOrEmpty)
                    MySandboxGame.Log.WriteLine(string.Format("Could not find any sound for '{0}'", cueName));
                else
                {
                    if (m_arcade.IsNull)
                        string.Format("Could not find arcade sound for '{0}'", cueName);
                    if (m_realistic.IsNull)
                        string.Format("Could not find realistic sound for '{0}'", cueName);
                }
            }
        }
        }

        public void Init(MyCueId cueId)
        {
            if (!MySandboxGame.IsDedicated)
            {
                if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
                {
                    m_realistic = cueId;
                    m_arcade = new MyCueId(MyStringHash.NullOrEmpty);
                }
                else
                {
                    m_arcade = cueId;
                    m_realistic = new MyCueId(MyStringHash.NullOrEmpty);
                }
            }
            else
            {
                m_arcade = new MyCueId(MyStringHash.NullOrEmpty);
                m_realistic = new MyCueId(MyStringHash.NullOrEmpty);
            }
        }

        public MyCueId SoundId
        {
            get
            {
                if (MySession.Static != null)
                    return MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS ? m_realistic : m_arcade;
                else
                    return m_arcade;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is MySoundPair)
                return (Arcade == (obj as MySoundPair).Arcade) && (Realistic == (obj as MySoundPair).Realistic);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return SoundId.ToString();
        }

        public static MyCueId GetCueId(string cueName)
        {
            if (string.IsNullOrEmpty(cueName))
            {
                return new MyCueId(MyStringHash.NullOrEmpty);
            }

            var cueId = MyAudio.Static.GetCueId(cueName);
            if (cueId.Hash != MyStringHash.NullOrEmpty)
                return cueId;
            Cache.Clear();
            if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                Cache.Append("Real").Append(cueName);
                return MyAudio.Static.GetCueId(Cache.ToString());
            }
            else
            {
                Cache.Append("Arc").Append(cueName);
                return MyAudio.Static.GetCueId(Cache.ToString());
            }
        }
    }

    #endregion

    public class MyEntity3DSoundEmitter : IMy3DSoundEmitter
    {
        #region Fields

        public enum MethodsEnum
        {
            CanHear,
            ShouldPlay2D,
            CueType,
            ImplicitEffect,
        }

        private MyCueId m_cueEnum = new MyCueId(MyStringHash.NullOrEmpty);
        private readonly MyCueId myEmptyCueId = new MyCueId(MyStringHash.NullOrEmpty);
        private MySoundPair m_soundPair = MySoundPair.Empty;
        private IMySourceVoice m_sound;
        private IMySourceVoice m_secondarySound = null;
        private MyCueId m_secondaryCueEnum = new MyCueId(MyStringHash.NullOrEmpty);
        private float m_secondaryVolumeRatio = 0f;
        private bool m_secondaryEnabled = false;
        private float m_secondaryBaseVolume = 1f;
        private float m_baseVolume = 1f;
        private MyEntity m_entity;
        private Vector3? m_position;
        private Vector3? m_velocity;
        private List<MyCueId> m_soundsQueue = new List<MyCueId>();
        private bool m_playing2D;
        private bool m_usesDistanceSounds = false;
        private bool m_useRealisticByDefault = false;
        private bool m_alwaysHearOnRealistic = false;
        private MyCueId m_closeSoundCueId = new MyCueId(MyStringHash.NullOrEmpty);
        private MySoundPair m_closeSoundSoundPair = MySoundPair.Empty;
        private bool m_realistic = false;
        bool IMy3DSoundEmitter.Realistic { get { return m_realistic; } }
        private float m_volumeMultiplier = 1f;
        private bool m_volumeChanging = false;
        private MySoundData m_lastSoundData = null;
        private MyStringHash m_activeEffect = MyStringHash.NullOrEmpty;
        private static Dictionary<MyCueId, int> m_lastTimePlaying = new Dictionary<MyCueId,int>();
        private static List<MyEntity3DSoundEmitter> m_entityEmitters = new List<MyEntity3DSoundEmitter>();

        #endregion

        #region Properties
        public Dictionary<MethodsEnum, List<Delegate>> EmitterMethods = new Dictionary<MethodsEnum, List<Delegate>>();

        public event Action<MyEntity3DSoundEmitter> StoppedPlaying;

        public bool Loop { get; private set; }

        public bool CanPlayLoopSounds = true;

        public bool IsPlaying { get { return m_sound != null && m_sound.IsPlaying; } }

        public MyCueId SoundId
        {
            get { return m_cueEnum; }
            set
            {
                if (m_cueEnum != value)
                {
                    m_cueEnum = value;
                    if (m_cueEnum.Hash == MyStringHash.GetOrCompute("None")) Debugger.Break();
                }
            }
        }

        public MySoundData LastSoundData
        {
            get { return m_lastSoundData; }
        }

        private float RealisticVolumeChange
        {
            get
            {
                return (m_realistic && m_lastSoundData != null) ? m_lastSoundData.RealisticVolumeChange : 1f;
            }
        }

        public float VolumeMultiplier
        {
            get { return m_volumeMultiplier; }
            set
            {
                m_volumeMultiplier = value;
                if (Sound != null)
                    Sound.VolumeMultiplier = m_volumeMultiplier;
            }
        }

        public MySoundPair SoundPair
        {
            get
            {
                return m_closeSoundSoundPair;
            }
        }

        public IMySourceVoice Sound
        {
            get { return m_sound; }
            set { m_sound = value; }
        }

        public void SetPosition(Vector3? position)
        {
            m_position = position;
        }

        public void SetVelocity(Vector3? velocity)
        {
            m_velocity = velocity;
        }
        #endregion

        #region Initialization

        public MyEntity3DSoundEmitter(MyEntity entity, bool useStaticList = false)
        {
            m_entity = entity;
            foreach (var value in Enum.GetValues(typeof(MethodsEnum)))
                EmitterMethods.Add((MethodsEnum)value, new List<Delegate>());
            //EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsInTerminal);
            EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsControlledEntity);
            //EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsBeingWelded);

            if (MySession.Static != null && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsInAtmosphere);
                //EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsInTerminal);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsCurrentWeapon);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsOnSameGrid);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsControlledEntity);
                //EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsBeingWelded);

                EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsCurrentWeapon);

                EmitterMethods[MethodsEnum.CueType].Add((Func<MySoundPair, MyCueId>)SelectCue);
                EmitterMethods[MethodsEnum.ImplicitEffect].Add((Func<MyStringHash>)SelectEffect);
            }

            m_useRealisticByDefault = (MySession.Static != null && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS);
            if (MySession.Static != null && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS && useStaticList && entity != null && MyFakes.ENABLE_NEW_SOUNDS_QUICK_UPDATE)
            {
                lock (m_entityEmitters)
                {
                    m_entityEmitters.Add(this);
                }
            }
                
        }

        ~MyEntity3DSoundEmitter()
        {
            foreach (List<Delegate> list in EmitterMethods.Values)
            {
                list.Clear();
            }
            EmitterMethods.Clear();
            m_soundsQueue.Clear();
        }

        #endregion

        #region Update

        public void Update()
        {
            bool validSoundIsPlaying = m_sound != null && m_sound.IsPlaying;
            if (!CanHearSound())
            {
                if (validSoundIsPlaying)
                {
                    StopSound(true, false);
                    m_sound = null;
                }
                return;
            }
            else if (!validSoundIsPlaying && Loop)
            {
                PlaySound(m_closeSoundSoundPair,true,true);
            }
            else if (validSoundIsPlaying && Loop && m_playing2D != ShouldPlay2D() && ((Force2D == true && m_playing2D == false) || (Force3D == true && m_playing2D == true)))
            {
                StopSound(true, false);
                PlaySound(m_closeSoundSoundPair, true, true);
            }
            else if (validSoundIsPlaying && Loop && m_playing2D == false && m_usesDistanceSounds)
            {
                MyCueId oldSecondary = (m_secondaryEnabled ? m_secondaryCueEnum : myEmptyCueId);
                MyCueId newSound = CheckDistanceSounds(m_closeSoundCueId);
                if (newSound != m_cueEnum || oldSecondary != m_secondaryCueEnum)
                {
                    PlaySoundWithDistance(newSound, true, true, useDistanceCheck: false);
                }
                else if (m_secondaryEnabled)
                {
                    if(Sound != null)
                        Sound.SetVolume(RealisticVolumeChange * m_baseVolume * (1f - m_secondaryVolumeRatio));
                    if(m_secondarySound != null)
                        m_secondarySound.SetVolume(RealisticVolumeChange * m_secondaryBaseVolume * m_secondaryVolumeRatio);
                }
            }
            if (validSoundIsPlaying && Loop)
            {
                //arcade/real sound change
                MyCueId newCueId = SelectCue(m_soundPair);
                if (newCueId.Equals(m_cueEnum) == false)
                    PlaySoundWithDistance(newCueId, true, true);

                //active filter changed
                MyStringHash newEffect = SelectEffect();
                if(m_activeEffect != newEffect)
                    PlaySoundWithDistance(newCueId, true, true);
            }
        }

        public bool FastUpdate(bool silenced)
        {
            if (silenced)
            {
                VolumeMultiplier = Math.Max(0f, m_volumeMultiplier - 0.01f);
                if (m_volumeMultiplier == 0f)
                    return false;
            }
            else
            {
                VolumeMultiplier = Math.Min(1f, m_volumeMultiplier + 0.01f);
                if (m_volumeMultiplier == 1f)
                    return false;
            }
            return true;
        }

        #endregion

        #region CheckFunctions

        private bool ShouldPlay2D()
        {
            var retVal = EmitterMethods[MethodsEnum.ShouldPlay2D].Count == 0;
            foreach (var func in EmitterMethods[MethodsEnum.ShouldPlay2D])
            {
                if (func != null)
                    retVal |= ((Func<bool>)func)();
            }
            return retVal;
        }

        private bool CanHearSound()
        {
            var canHear = EmitterMethods[MethodsEnum.CanHear].Count == 0;
            if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS && m_alwaysHearOnRealistic)
                canHear = true;
            foreach (var func in EmitterMethods[MethodsEnum.CanHear])
            {
                var checkCanHear = (Func<bool>)func;
                if(checkCanHear == null)
                {
                    Debug.Fail("CanHear in EmitterMethods contains a null delegate!");
                    continue;
                }
                canHear |= checkCanHear();
                if (canHear)
                    break;
            }
            return IsCloseEnough() && canHear;
        }

        private bool IsOnSameGrid()
        {
            if (Entity is MyCubeBlock || Entity is MyCubeGrid)
            {
                MyCubeGrid firstCubeGrid = null;
                if(MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity is MyCockpit)
                    firstCubeGrid = (MySession.Static.ControlledEntity.Entity as MyCockpit).CubeGrid;
                else if (MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.SoundComp != null)
                    firstCubeGrid = MySession.Static.LocalCharacter.SoundComp.StandingOnGrid;
                if (firstCubeGrid == null)//character is not touching grid but he may be in pressurized room
                {
                    if (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.AtmosphereDetectorComp == null)
                        return false;
                    if (MySession.Static.LocalCharacter.AtmosphereDetectorComp.InShipOrStation)
                        firstCubeGrid = MySession.Static.LocalCharacter.OxygenComponent.OxygenSourceGrid;
                }
                MyCubeGrid secondCubeGrid = Entity is MyCubeBlock ? (Entity as MyCubeBlock).CubeGrid : (Entity as MyCubeGrid);
                if (firstCubeGrid == null && MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.SoundComp != null
                    && MySession.Static.LocalCharacter.SoundComp.StandingOnVoxel != null)
                {
                    if (secondCubeGrid.IsStatic)
                        return true;//character is standing on voxel near this station
                    else
                    {
                        List<IMyEntity> entities = secondCubeGrid.GridSystems.LandingSystem.GetAttachedEntities();
                        foreach (IMyEntity entity in entities)
                        {
                            if ((entity is MyVoxelBase) && (entity as MyVoxelBase == MySession.Static.LocalCharacter.SoundComp.StandingOnVoxel as MyVoxelBase))
                                return true;//character is standing on voxel that is connected to this ship via landing gears
                        }
                    }
                }
                    if (firstCubeGrid == null)
                        return false;
                if (firstCubeGrid == secondCubeGrid)
                    return true;//character is standing on this grid
                if (MyCubeGridGroups.Static.Physical.HasSameGroup(firstCubeGrid, secondCubeGrid))
                    return true;//character is on neighbouring grid
            }
            else if (Entity is MyVoxelBase)
            {
                if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity is MyCockpit)
                    return false;
                else
                {
                    if (MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.SoundComp != null)
                    {
                        if (MySession.Static.LocalCharacter.SoundComp.StandingOnVoxel as MyVoxelBase == Entity as MyVoxelBase)
                            return true;//character is standing on this voxel
                        if (MySession.Static.LocalCharacter.SoundComp.StandingOnGrid != null)
                        {
                            if (MySession.Static.LocalCharacter.SoundComp.StandingOnGrid.IsStatic)
                                return true;//character is standing on station grid near this voxel
                            else
                            {
                                List<IMyEntity> entities = MySession.Static.LocalCharacter.SoundComp.StandingOnGrid.GridSystems.LandingSystem.GetAttachedEntities();
                                foreach (IMyEntity entity in entities)
                                {
                                    if ((entity is MyVoxelBase) && (entity as MyVoxelBase == Entity as MyVoxelBase))
                                        return true;//character is standing on ship that is connected to this voxel via landing gears
                }
            }
                        }
                    }
                }
            }
            return false;
        }

        private bool IsCurrentWeapon()
        {
            if (Entity is IMyHandheldGunObject<MyDeviceBase>)
            {
                if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity is MyCharacter)
                    return (MySession.Static.ControlledEntity.Entity as MyCharacter).CurrentWeapon == Entity;
                else
                    return false;
            }
            return false;
        }

        private bool IsCloseEnough()
        {
            return m_playing2D || MyAudio.Static.SourceIsCloseEnoughToPlaySound(this.SourcePosition, SoundId, this.CustomMaxDistance);
        }

        private bool IsInTerminal()
        {
            return MyGuiScreenTerminal.IsOpen && MyGuiScreenTerminal.InteractedEntity != null && MyGuiScreenTerminal.InteractedEntity == Entity;
        }

        private bool IsControlledEntity()
        {
            return MySession.Static.ControlledEntity != null && m_entity == MySession.Static.ControlledEntity.Entity;
        }

        private bool IsBeingWelded()
        {
            if (MySession.Static == null)
            {
                return false;
            }
            var controlledEntity = MySession.Static.ControlledEntity;
            if (controlledEntity == null)
            {
                return false;
            }

            var character = MySession.Static.ControlledEntity.Entity as MyCharacter;
            if (character == null)
            {
                return false;
            }

            var tool = character.CurrentWeapon as MyEngineerToolBase;
            if (tool == null)
            {
                return false;
            }

            var grid = tool.GetTargetGrid();
            var cubeBlock = (Entity as MyCubeBlock);
            if (grid == null || cubeBlock == null || grid != cubeBlock.CubeGrid || tool.HasHitBlock == false)
            {
                return false;
            }

            var targetCube = grid.GetCubeBlock(tool.TargetCube);
            if (targetCube == null)
            {
                return false;
            }


            return (targetCube.FatBlock == cubeBlock && tool.IsShooting);
        }

        private bool IsThereAir()
        {
            // player is in pressurized ship or in planet with atmosphere
            if (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.AtmosphereDetectorComp == null)
                return false;
            return !MySession.Static.LocalCharacter.AtmosphereDetectorComp.InVoid;
        }

        private bool IsInAtmosphere()
        {
            // player is in planet with atmosphere
            if (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.AtmosphereDetectorComp == null)
                return false;
            return MySession.Static.LocalCharacter.AtmosphereDetectorComp.InAtmosphere;
        }

        private MyCueId SelectCue(MySoundPair sound)
        {
            if (m_useRealisticByDefault)
            {
                if (m_lastSoundData == null)
                    m_lastSoundData = MyAudio.Static.GetCue(sound.Realistic);

                if (m_lastSoundData != null && m_lastSoundData.AlwaysUseOneMode)
                {
                    m_realistic = true;
                    return sound.Realistic;
                }

                MyCockpit cockpit = MySession.Static.LocalCharacter != null ? MySession.Static.LocalCharacter.Parent as MyCockpit : null;
                bool isLargePressurizedCockpit = (cockpit != null && cockpit.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large && cockpit.BlockDefinition.IsPressurized);
                if (IsThereAir() || isLargePressurizedCockpit)
                {
                    m_realistic = false;
                    return sound.Arcade;
                }
                else
                {
                    m_realistic = true;
                    return sound.Realistic;
                }
            }
            else
            {
                m_realistic = false;
                return sound.Arcade;
            }
        }

        static MyStringHash m_effectHasHelmetInOxygen = MyStringHash.GetOrCompute("LowPassHelmet");
        static MyStringHash m_effectNoHelmetNoOxygen = MyStringHash.GetOrCompute("LowPassNoHelmetNoOxy");
        static MyStringHash m_effectEnclosedCockpitInSpace = MyStringHash.GetOrCompute("LowPassCockpitNoOxy");
        static MyStringHash m_effectEnclosedCockpitInAir = MyStringHash.GetOrCompute("LowPassCockpit");
        private MyStringHash SelectEffect()
        {
            if (m_lastSoundData != null && m_lastSoundData.ModifiableByHelmetFilters == false)
                return MyStringHash.NullOrEmpty;
            if (MySession.Static == null || MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.OxygenComponent == null || MyFakes.ENABLE_NEW_SOUNDS == false || MySession.Static.Settings.RealisticSound == false)
                return MyStringHash.NullOrEmpty;
            bool air = IsThereAir();
            MyCockpit cockpit = MySession.Static.LocalCharacter.Parent as MyCockpit;
            bool isPressurizedCockpit = (cockpit != null && cockpit.BlockDefinition != null && cockpit.BlockDefinition.IsPressurized);
            if (air && isPressurizedCockpit)
                return m_effectEnclosedCockpitInAir;//in enclosed cockpit in oxygen
            if (air == false && isPressurizedCockpit && cockpit.CubeGrid != null && cockpit.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large)
                return m_effectEnclosedCockpitInSpace;//in enclosed large cockpit in space
            if (MySession.Static.LocalCharacter.OxygenComponent.HelmetEnabled && air)
                return m_effectHasHelmetInOxygen;//helmet in oxygen
            if (m_lastSoundData != null && MySession.Static.LocalCharacter.OxygenComponent.HelmetEnabled && air == false)
                return m_lastSoundData.RealisticFilter;//helmet in space
            if (MySession.Static.LocalCharacter.OxygenComponent.HelmetEnabled == false && air == false && (cockpit == null || cockpit.BlockDefinition == null || cockpit.BlockDefinition.IsPressurized == false))
                return m_effectNoHelmetNoOxygen;//no helmet in space
            if (m_lastSoundData != null && cockpit != null && cockpit.BlockDefinition != null && cockpit.BlockDefinition.IsPressurized && cockpit.CubeGrid != null && cockpit.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small)
                return m_lastSoundData.RealisticFilter;//no helmet in small ship in space
                return MyStringHash.NullOrEmpty;//no helmet in oxygen
        }

        private bool CheckForSynchronizedSounds()
        {
            if (m_lastSoundData != null && m_lastSoundData.PreventSynchronization >= 0)
            {
                int lastTime;
                int now = MyFpsManager.GetSessionTotalFrames();
                if (m_lastTimePlaying.TryGetValue(SoundId, out lastTime))
                {
                    if (Math.Abs(now - lastTime) <= m_lastSoundData.PreventSynchronization)
                    {
                        return false;
                    }
                    else
                        m_lastTimePlaying[SoundId] = now;
                }
                else
                {
                    m_lastTimePlaying.Add(SoundId, now);
                }
            }
            return true;
        }

        #endregion

        #region PlaySoundBase

        public void PlaySound(byte[] buffer, int size, int sampleRate, float volume = 1, float maxDistance = 0, MySoundDimensions dimension = MySoundDimensions.D3)
        {
            CustomMaxDistance = maxDistance;
            CustomVolume = volume;
            if (Sound == null)
                Sound = MyAudio.Static.GetSound(this, sampleRate, 1, dimension);
            if (Sound != null)
            {
                Sound.SubmitBuffer(buffer, size);
                if (!Sound.IsPlaying)
                    Sound.StartBuffered();
            }
        }

        public void PlaySingleSound(MyCueId soundId, /*bool loop = false,*/ bool stopPrevious = false, bool skipIntro = false)
        {
            if (m_cueEnum == soundId)
                return;
            else
                PlaySoundWithDistance(soundId, stopPrevious, skipIntro);
        }

        public void PlaySingleSound(MySoundPair soundId, /*bool loop = false,*/ bool stopPrevious = false, bool skipIntro = false, bool skipToEnd = false)
        {
            m_closeSoundSoundPair = soundId;
            m_soundPair = soundId;
            var cueId = m_useRealisticByDefault ? soundId.Realistic : soundId.Arcade;
            if (EmitterMethods[MethodsEnum.CueType].Count > 0)
            {
                var select = (Func<MySoundPair, MyCueId>)EmitterMethods[MethodsEnum.CueType][0];
                cueId = select(soundId);
            }
            if (m_cueEnum.Equals(cueId))
                return;
            else
                PlaySoundWithDistance(cueId, stopPrevious, skipIntro, skipToEnd: skipToEnd);
        }

        public void PlaySound(MySoundPair soundId, bool stopPrevious = false, bool skipIntro = false, bool force2D = false, bool alwaysHearOnRealistic = false, bool skipToEnd = false)
        {
            m_closeSoundSoundPair = soundId;
            m_soundPair = soundId;
            var cueId = m_useRealisticByDefault ? soundId.Realistic : soundId.Arcade;
            if (EmitterMethods[MethodsEnum.CueType].Count > 0)
            {
                var select = (Func<MySoundPair, MyCueId>)EmitterMethods[MethodsEnum.CueType][0];
                cueId = select(soundId);
            }
            PlaySoundWithDistance(cueId, stopPrevious, skipIntro, force2D : force2D, alwaysHearOnRealistic: alwaysHearOnRealistic, skipToEnd: skipToEnd);
        }

        #endregion

        #region PlaySoundWithDistance

        public void PlaySoundWithDistance(MyCueId soundId, bool stopPrevious = false, bool skipIntro = false, bool force2D = false, bool useDistanceCheck = true, bool alwaysHearOnRealistic = false, bool skipToEnd = false)
        {
            m_lastSoundData = MyAudio.Static.GetCue(soundId);

            if (useDistanceCheck)
                m_closeSoundCueId = soundId;

            if (useDistanceCheck && ShouldPlay2D() == false && force2D == false)
                soundId = CheckDistanceSounds(soundId);

            bool usesDistanceSoundsCache = m_usesDistanceSounds;
            if (m_sound != null)
            {
                if (stopPrevious)
                    StopSound(true);
                else if (m_sound.IsLoopable)
                {
                    var sound = Sound;
                    StopSound(true);
                    m_soundsQueue.Add(sound.CueEnum);
                }
            }
            if (m_secondarySound != null)
            {
                m_secondarySound.Stop(true);
            }
            SoundId = soundId;
            PlaySoundInternal((skipIntro || skipToEnd), force2D: force2D, alwaysHearOnRealistic: alwaysHearOnRealistic, skipToEnd: skipToEnd);
            m_usesDistanceSounds = usesDistanceSoundsCache;
        }

        private MyCueId CheckDistanceSounds(MyCueId soundId)
        {
            if (soundId.IsNull == false)
            {
                if (m_lastSoundData != null && m_lastSoundData.DistantSounds != null && m_lastSoundData.DistantSounds.Count > 0)
                {
                    float distanceToSoundSquered = Vector3.DistanceSquared(MySector.MainCamera.Position, this.SourcePosition);
                    int bestSoundIndex = -1;
                    m_usesDistanceSounds = true;
                    m_secondaryEnabled = false;
                    float dist, crossfadeDist;
                    for (int i = 0; i < m_lastSoundData.DistantSounds.Count; i++)
                    {
                        dist = m_lastSoundData.DistantSounds[i].distance * m_lastSoundData.DistantSounds[i].distance;
                        if (distanceToSoundSquered > dist)
                            bestSoundIndex = i;
                        else
                        {
                            crossfadeDist = m_lastSoundData.DistantSounds[i].distanceCrossfade >= 0f ? m_lastSoundData.DistantSounds[i].distanceCrossfade * m_lastSoundData.DistantSounds[i].distanceCrossfade : float.MaxValue;
                            if (distanceToSoundSquered > crossfadeDist)
                            {
                                m_secondaryVolumeRatio = (distanceToSoundSquered - crossfadeDist) / (dist - crossfadeDist);
                                m_secondaryEnabled = true;
                                MySoundPair secondarySoundPair = new MySoundPair(m_lastSoundData.DistantSounds[i].sound);
                                if (secondarySoundPair != MySoundPair.Empty)
                                {
                                    m_secondaryCueEnum = SelectCue(secondarySoundPair);
                                }
                                else
                                    m_secondaryCueEnum = new MyCueId(MyStringHash.GetOrCompute(m_lastSoundData.DistantSounds[bestSoundIndex].sound));
                            }
                            else
                                break;
                        }
                    }
                    if (bestSoundIndex >= 0)
                    {
                        MySoundPair soundPair = new MySoundPair(m_lastSoundData.DistantSounds[bestSoundIndex].sound);
                        if (soundPair != MySoundPair.Empty)
                        {
                            m_soundPair = soundPair;
                            soundId = SelectCue(m_soundPair);
                        } else
                            soundId = new MyCueId(MyStringHash.GetOrCompute(m_lastSoundData.DistantSounds[bestSoundIndex].sound));
                    }
                    else
                    {
                        m_soundPair = m_closeSoundSoundPair;
                    }
                }
                else
                {
                    m_usesDistanceSounds = false;
                }
            }
            if (m_secondaryEnabled == false)
                m_secondaryCueEnum = myEmptyCueId;
            return soundId;
        }

        #endregion

        #region PlaySoundInternal

        private void PlaySoundInternal(bool skipIntro = false, bool skipToEnd = false, bool force2D = false, bool alwaysHearOnRealistic = false)
        {
            Force2D = force2D;
            m_alwaysHearOnRealistic = alwaysHearOnRealistic;
            m_playing2D = (ShouldPlay2D() && !Force3D) || force2D || Force2D;
            Loop = MyAudio.Static.IsLoopable(SoundId) && !skipToEnd && CanPlayLoopSounds;
            if (!SoundId.IsNull)
            {
                if (Loop && MySession.Static.ElapsedPlayTime.TotalSeconds < 6)
                    skipIntro = true;
                if (m_playing2D && CheckForSynchronizedSounds())
                    Sound = MyAudio.Static.PlaySound(SoundId, this, MySoundDimensions.D2, skipIntro, skipToEnd);
                else if (CanHearSound() && CheckForSynchronizedSounds()) //Start 3D sound only if can be heard
                    Sound = MyAudio.Static.PlaySound(SoundId, this, MySoundDimensions.D3, skipIntro, skipToEnd);
            }
            if (Sound != null && Sound.IsPlaying)
            {
                if (MyMusicController.Static != null && m_lastSoundData != null && m_lastSoundData.DynamicMusicCategory != MyStringId.NullOrEmpty && m_lastSoundData.DynamicMusicAmount > 0)
                    MyMusicController.Static.IncreaseCategory(m_lastSoundData.DynamicMusicCategory, m_lastSoundData.DynamicMusicAmount);
                m_baseVolume = Sound.Volume;
                Sound.SetVolume(Sound.Volume * RealisticVolumeChange);
                if (m_secondaryEnabled && m_secondaryCueEnum != null)
                {
                    m_secondarySound = MyAudio.Static.PlaySound(m_secondaryCueEnum, this, MySoundDimensions.D3, skipIntro, skipToEnd);
                    if (Sound == null)
                        return;
                    if (m_secondarySound != null)
                    {
                        m_secondaryBaseVolume = m_secondarySound.Volume;
                        Sound.SetVolume(RealisticVolumeChange * m_baseVolume * (1f - m_secondaryVolumeRatio));
                        m_secondarySound.SetVolume(RealisticVolumeChange * m_secondaryBaseVolume * m_secondaryVolumeRatio);
                        m_secondarySound.VolumeMultiplier = m_volumeMultiplier;
                    }
                }
                Sound.VolumeMultiplier = m_volumeMultiplier;
                Sound.StoppedPlaying = OnStopPlaying;
                if (EmitterMethods[MethodsEnum.ImplicitEffect].Count > 0)
                {
                    m_activeEffect = MyStringHash.NullOrEmpty;
                    var effectId = ((Func<MyStringHash>)EmitterMethods[MethodsEnum.ImplicitEffect][0])();
                    if (effectId != MyStringHash.NullOrEmpty)
                    {
                        var effect = MyAudio.Static.ApplyEffect(Sound, effectId);
                        if (effect != null)
                        {
                            Sound = effect.OutputSound;
                            m_activeEffect = effectId;
                        }
                    }
                }
            }
            else
                OnStopPlaying();
        }

        #endregion

        #region OtherFunctions

        public void StopSound(bool forced, bool cleanUp = true)
        {
            m_usesDistanceSounds = false;
            if (m_sound != null)
            {
                m_sound.Stop(forced);
                if (Loop && !forced)
                    PlaySoundInternal(true, true);
                if (m_soundsQueue.Count == 0)
                {
                    m_sound = null;
                    if (cleanUp)
                    {
                        Loop = false;
                        SoundId = myEmptyCueId;
                    }
                }
                else
                {
                    if (cleanUp)
                    {
                        SoundId = m_soundsQueue[0];
                        PlaySoundInternal(true);
                        m_soundsQueue.RemoveAt(0);
                    }
                }
            }
            else
            {
                if (cleanUp)
                {
                    Loop = false;
                    SoundId = myEmptyCueId;
                }
            }
            if (m_secondarySound != null)
            {
                m_secondarySound.Stop(true);
            }
        }

        public void Cleanup()
        {
            if (Sound != null)
            {
                Sound.Cleanup();
                Sound = null;
            }
            if (m_secondarySound != null)
            {
                m_secondarySound.Cleanup();
                m_secondarySound = null;
            }
        }

        private void OnStopPlaying()
        {
            if (StoppedPlaying != null)
                StoppedPlaying(this);
        }

        #endregion

        #region Static

        public static void PreloadSound(MySoundPair soundId)
        {
            var sound = MyAudio.Static.GetSound(soundId.SoundId);
            if (sound != null)
            {
                sound.Start(false);
                sound.Stop(true);
            }
        }

        private static int m_lastUpdate = int.MinValue;
        public static void UpdateEntityEmitters(bool removeUnused, bool updatePlaying, bool updateNotPlaying)
        {
            int now = MyFpsManager.GetSessionTotalFrames();
            if (now == 0 || Math.Abs(m_lastUpdate - now) < 5)
                return;
            m_lastUpdate = now;
            lock (m_entityEmitters)
            {
                for (int i = 0; i < m_entityEmitters.Count; i++)
                {
                    if (m_entityEmitters[i] != null && m_entityEmitters[i].Entity != null && m_entityEmitters[i].Entity.Closed == false)
                    {
                        if ((m_entityEmitters[i].IsPlaying && updatePlaying) || (!m_entityEmitters[i].IsPlaying && updateNotPlaying))
                        {
                            m_entityEmitters[i].Update();
                        }
                    }
                    else if (removeUnused)
                    {
                        m_entityEmitters.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public static void ClearEntityEmitters()
        {
            lock (m_entityEmitters)
                m_entityEmitters.Clear();
        }

        #endregion

        #region IMy3DSoundEmitter
        public Vector3 SourcePosition
        {
            get
            {
                if (m_position.HasValue)
                    return m_position.Value;
                else if (m_entity != null)
                    return m_entity.WorldMatrix.Translation;
                else
                    return Vector3.Zero;
            }
        }
        public Vector3 Velocity
        {
            get
            {
                if (m_velocity.HasValue)
                    return m_velocity.Value;
                return (m_entity != null && m_entity.Physics != null) ? m_entity.Physics.LinearVelocity : Vector3.Zero;
            }
        }
        public MyEntity Entity
        {
            get
            {
                return m_entity;
            }
            set
            {
                m_entity = value;
            }
        }
        public float? CustomMaxDistance
        {
            get;
            set;
        }

        private float? m_customVolume;
        public float? CustomVolume
        {
            get { return m_customVolume; }
            set
            {
                m_customVolume = value;
                if (m_customVolume.HasValue && Sound != null)
                {
                    Sound.SetVolume(RealisticVolumeChange * m_customVolume.Value);
                }
            }
        }

        public bool Force3D
        {
            get;
            set;
        }

        public bool Force2D
        {
            get;
            set;
        }

        public bool Plays2D
        {
            get { return m_playing2D; }
        }

        public int SourceChannels
        {
            get;
            set;
        }

		private int m_lastPlayedWaveNumber = -1;
		int IMy3DSoundEmitter.LastPlayedWaveNumber
		{
			get { return m_lastPlayedWaveNumber; }
			set { m_lastPlayedWaveNumber = value; }
		}

        #endregion
    }
}
