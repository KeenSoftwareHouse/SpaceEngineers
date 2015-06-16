using Sandbox.Engine.Platform;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Utils;
using VRage.Data;
using VRageMath;
using VRageRender;
using VRage.Library.Utils;
using VRage.Data.Audio;

namespace Sandbox.Game.Entities
{
    public class MySoundPair
    {
        public static MySoundPair Empty = new MySoundPair();
        static StringBuilder m_cache = new StringBuilder();

        public MySoundPair()
        {
            Init(null);
        }

        public MySoundPair(string cueName)
        {
            Init(cueName);
        }

        public void Init(string cueName)
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
                m_cache.Clear();
                m_cache.Append("Arc").Append(cueName);
                m_arcade = MyAudio.Static.GetCueId(m_cache.ToString());
                m_cache.Clear();
                m_cache.Append("Real").Append(cueName);
                m_realistic = MyAudio.Static.GetCueId(m_cache.ToString());

                //Debug.Assert(m_arcade != MySpaceTexts.NullOrEmpty || m_realistic != MySpaceTexts.NullOrEmpty, string.Format("Could not find any sound for '{0}'", cueName));
                if (m_arcade.Hash == MyStringHash.NullOrEmpty && m_realistic.Hash == MyStringHash.NullOrEmpty)
                    MySandboxGame.Log.WriteLine(string.Format("Could not find any sound for '{0}'", cueName));
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

        //jn:TODO create properties on cues or something
        public MyCueId Arcade { get { return m_arcade; } }
        public MyCueId Realistic { get { return m_realistic; } }

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

        private MyCueId m_arcade;
        private MyCueId m_realistic;

        public override bool Equals(object obj)
        {
            if (obj is MySoundPair)
                return SoundId == (obj as MySoundPair).SoundId;
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
            m_cache.Clear();
            if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                m_cache.Append("Real").Append(cueName);
                return MyAudio.Static.GetCueId(m_cache.ToString());
            }
            else
            {
                m_cache.Append("Arc").Append(cueName);
                return MyAudio.Static.GetCueId(m_cache.ToString());
            }
        }
    }

    public class MyEntity3DSoundEmitter : IMy3DSoundEmitter
    {
        public enum MethodsEnum
        {
            CanHear,
            ShouldPlay2D,
            CueType,
            ImplicitEffect,
        }

        #region Fields

        private MyCueId m_cueEnum = new MyCueId(MyStringHash.NullOrEmpty);
        private IMySourceVoice m_sound;
        private MyEntity m_entity;
        private Vector3? m_position;
        private Vector3? m_velocity;
        private List<MyCueId> m_soundsQueue = new List<MyCueId>();
        private bool m_playing2D;

        #endregion

        #region Properties
        public Dictionary<MethodsEnum, List<Delegate>> EmitterMethods = new Dictionary<MethodsEnum, List<Delegate>>();

        public event Action<MyEntity3DSoundEmitter> StoppedPlaying;

        public bool Loop { get; private set; }

        public bool IsPlaying { get { return m_sound != null && m_sound.IsPlaying; } }

        public MyCueId SoundId
        {
            get { return m_cueEnum; }
            set
            {
                if (m_cueEnum != value)
                {
                    m_cueEnum = value;
                }
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

        public MyEntity3DSoundEmitter(MyEntity entity)
        {
            m_entity = entity;
            foreach (var value in Enum.GetValues(typeof(MethodsEnum)))
                EmitterMethods.Add((MethodsEnum)value, new List<Delegate>());
            EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsInTerminal);
            EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsControlledEntity);
            EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsBeingWelded);

            if (MySession.Static != null && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsInTerminal);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsCurrentWeapon);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsOnSameGrid);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsControlledEntity);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsBeingWelded);
                EmitterMethods[MethodsEnum.CanHear].Add((Func<bool>)IsInOxygen);

                EmitterMethods[MethodsEnum.ShouldPlay2D].Add((Func<bool>)IsCurrentWeapon);

                EmitterMethods[MethodsEnum.CueType].Add((Func<MySoundPair, MyCueId>)SelectCue);
                EmitterMethods[MethodsEnum.ImplicitEffect].Add((Func<MyStringHash>)SelectEffect);
            }
        }

        public void Update()
        {
            if (!CanHearSound())
            {
                if (m_sound != null && m_sound.IsPlaying)
                {
                    StopSound(true, false);
                    m_sound = null;
                }
                return;
            }
            else if ((m_sound == null || !m_sound.IsPlaying) && Loop)
                PlaySoundInternal(true);
            else if (m_sound != null && m_sound.IsPlaying && Loop && m_playing2D != ShouldPlay2D())
            {
                StopSound(true, false);
                PlaySoundInternal(true);
            }
        }

        private bool CanHearSound()
        {
            var canHear = EmitterMethods[MethodsEnum.CanHear].Count == 0;
            foreach (var func in EmitterMethods[MethodsEnum.CanHear])
                canHear |= ((Func<bool>)func)();
            return IsCloseEnough() && canHear;
        }

        private bool IsOnSameGrid()
        {
            if (Entity is MyCubeBlock)
            {
                if (MySession.ControlledEntity != null && MySession.ControlledEntity.Entity is MyCockpit)
                    return (MySession.ControlledEntity.Entity as MyCockpit).CubeGrid == (Entity as MyCubeBlock).CubeGrid;
                else
                    return false;
            }
            return false;
        }

        private bool IsCurrentWeapon()
        {
            if (Entity is IMyHandheldGunObject<MyDeviceBase>)
            {
                if (MySession.ControlledEntity != null && MySession.ControlledEntity.Entity is MyCharacter)
                    return (MySession.ControlledEntity.Entity as MyCharacter).CurrentWeapon == Entity;
                else
                    return false;
            }
            return false;
        }

        private bool IsCloseEnough()
        {
            return MyAudio.Static.SourceIsCloseEnoughToPlaySound(this, SoundId);
        }

        private bool IsInTerminal()
        {
            return MyGuiScreenTerminal.IsOpen && MyGuiScreenTerminal.InteractedEntity != null && MyGuiScreenTerminal.InteractedEntity == Entity;
        }

        private bool IsControlledEntity()
        {
            return MySession.ControlledEntity != null && m_entity == MySession.ControlledEntity.Entity;
        }

        private bool IsBeingWelded()
        {
            var controlledEntity = MySession.ControlledEntity;
            if (controlledEntity == null)
            {
                return false;
            }

            var character = MySession.ControlledEntity.Entity as MyCharacter;
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
            if (grid == null || cubeBlock == null || grid != cubeBlock.CubeGrid)
            {
                return false;
            }

            var targetCube = grid.GetCubeBlock(tool.TargetCube);
            if (targetCube == null)
            {
                return false;
            }


            return targetCube.FatBlock == cubeBlock;
        }

        private bool IsInOxygen()
        {
            return (MySession.LocalCharacter != null && MySession.LocalCharacter.EnvironmentOxygenLevel > 0.1f);
        }

        private MyCueId SelectCue(MySoundPair sound)
        {
            if (MySession.Static != null && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                if (IsInOxygen())
                {
                    return sound.Arcade;
                }
                else
                {
                    return sound.Realistic;
                }
            }
            else
                return sound.Arcade;
        }

        static MyStringHash m_helmetEffect = MyStringHash.GetOrCompute("LowPassHelmet");
        private MyStringHash SelectEffect()
        {
            if (MyFakes.ENABLE_NEW_SOUNDS && MySession.LocalCharacter != null && !MySession.LocalCharacter.Definition.NeedsOxygen && IsInOxygen())
            {
                return m_helmetEffect;
            }
            return MyStringHash.NullOrEmpty;
        }

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
                PlaySound(soundId, stopPrevious, skipIntro);
        }

        public void PlaySingleSound(MySoundPair soundId, /*bool loop = false,*/ bool stopPrevious = false, bool skipIntro = false)
        {
            var cueId = soundId.Arcade;
            if (EmitterMethods[MethodsEnum.CueType].Count > 0)
            {
                var select = (Func<MySoundPair, MyCueId>)EmitterMethods[MethodsEnum.CueType][0];
                cueId = select(soundId);
            }
            if (m_cueEnum == cueId)
                return;
            else
                PlaySound(cueId, stopPrevious, skipIntro);
        }

        public void PlaySound(MySoundPair soundId, bool stopPrevious = false, bool skipIntro = false)
        {
            var cueId = soundId.Arcade;
            if (EmitterMethods[MethodsEnum.CueType].Count > 0)
            {
                var select = (Func<MySoundPair, MyCueId>)EmitterMethods[MethodsEnum.CueType][0];
                cueId = select(soundId);
            }
            PlaySound(cueId, stopPrevious, skipIntro);
        }

        public void PlaySound(MyCueId soundId, bool stopPrevious = false, bool skipIntro = false)
        {
            if (m_sound != null)
                if (stopPrevious)
                    StopSound(true);
                else if (m_sound.IsLoopable)
                {
                    var sound = Sound;
                    StopSound(true);
                    m_soundsQueue.Add(sound.CueEnum);
                }
            SoundId = soundId;
            PlaySoundInternal(skipIntro);
        }

        public void StopSound(bool forced, bool cleanUp = true)
        {
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
                        SoundId = new MyCueId(MyStringHash.NullOrEmpty);
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
                    SoundId = new MyCueId(MyStringHash.NullOrEmpty);
                }
            }
        }

        private void PlaySoundInternal(bool skipIntro = false, bool skipToEnd = false)
        {
            m_playing2D = ShouldPlay2D() && !Force3D;
            Loop = MyAudio.Static.IsLoopable(SoundId) && !skipToEnd;
            if (m_playing2D)
                Sound = MyAudio.Static.PlaySound(SoundId, this, MySoundDimensions.D2, skipIntro, skipToEnd);
            else if (CanHearSound()) //Start 3D sound only if can be heard
                Sound = MyAudio.Static.PlaySound(SoundId, this, MySoundDimensions.D3, skipIntro, skipToEnd);
            if (Sound != null)
            {
                Sound.StoppedPlaying = OnStopPlaying;
                if (EmitterMethods[MethodsEnum.ImplicitEffect].Count > 0)
                {
                    var effectId = ((Func<MyStringHash>)EmitterMethods[MethodsEnum.ImplicitEffect][0])();
                    if (effectId != MyStringHash.NullOrEmpty)
                    {
                        var effect = MyAudio.Static.ApplyEffect(Sound, effectId);
                        if (effect != null)
                            Sound = effect.OutputSound;
                    }
                }
            }
            else
                OnStopPlaying();
        }

        private bool ShouldPlay2D()
        {
            var retVal = EmitterMethods[MethodsEnum.ShouldPlay2D].Count == 0;
            foreach (var func in EmitterMethods[MethodsEnum.ShouldPlay2D])
                retVal |= ((Func<bool>)func)();
            return retVal;
        }

        public void Cleanup()
        {
            if (Sound != null)
            {
                Sound.Cleanup();
                Sound = null;
            }
        }

        private void OnStopPlaying()
        {
            if (StoppedPlaying != null)
                StoppedPlaying(this);
        }

        public static void PreloadSound(MySoundPair soundId)
        {
            var sound = MyAudio.Static.GetSound(soundId.SoundId);
            if (sound != null)
            {
                sound.Start(false);
                sound.Stop(true);
            }
        }

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
                else if ((m_entity != null) && (m_entity.Physics != null))
                    return m_entity.Physics.LinearVelocity;
                else
                    return Vector3.Zero;
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
                    Sound.SetVolume(m_customVolume.Value);
                }
            }
        }

        public bool Force3D
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

        #endregion
    }
}
