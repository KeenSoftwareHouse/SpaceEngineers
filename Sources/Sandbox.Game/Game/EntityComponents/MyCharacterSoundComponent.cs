using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities.Character.Components;
using VRage.Data.Audio;
using Sandbox.Game.GUI;
using VRage.Game.Entity;
using VRageRender;
using System.Diagnostics;
using Sandbox.Definitions;
using VRageRender.Animations;
using VRage.Game;
using Sandbox.Definitions;
using Sandbox.Game.SessionComponents;

namespace Sandbox.Game.Components
{
	public enum CharacterSoundsEnum : int
	{
		JUMP_SOUND,

		JETPACK_IDLE_SOUND,
		JETPACK_RUN_SOUND,

		CROUCH_DOWN_SOUND,
		CROUCH_UP_SOUND,

		PAIN_SOUND,
        SUFFOCATE_SOUND,
        DEATH_SOUND,
        DEATH_SOUND_SUFFOCATE,

		IRONSIGHT_ACT_SOUND,
		IRONSIGHT_DEACT_SOUND,

        FAST_FLY_SOUND,

        HELMET_NORMAL,
        HELMET_LOW,
        HELMET_CRITICAL,
        HELMET_NONE,
        MOVEMENT_SOUND,
        MAGNETIC_SOUND
	}

	[MyComponentBuilder(typeof(MyObjectBuilder_CharacterSoundComponent))]
	public class MyCharacterSoundComponent : MyEntityComponentBase
	{
		#region Internal types
        private readonly Dictionary<int, MySoundPair> CharacterSounds = new Dictionary<int, MySoundPair>();
        private static readonly MySoundPair EmptySoundPair = new MySoundPair();

		enum MySoundEmitterEnum
		{
			PrimaryState = 0,
			SecondaryState = 1,
			WalkState = 2,
            Action = 3
		}

		private struct MovementSoundType
		{
			public static readonly MyStringId Walk = MyStringId.GetOrCompute("Walk");
			public static readonly MyStringId CrouchWalk = MyStringId.GetOrCompute("CrouchWalk");
			public static readonly MyStringId Run = MyStringId.GetOrCompute("Run");
			public static readonly MyStringId Sprint = MyStringId.GetOrCompute("Sprint");
			public static readonly MyStringId Fall = MyStringId.GetOrCompute("Fall");
		}
        private static MyStringHash LowPressure = MyStringHash.GetOrCompute("LowPressure");

		#endregion

		#region Fields

		private List<MyEntity3DSoundEmitter> m_soundEmitters;
		
		List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

		private int m_lastScreamTime;
        private float m_jetpackSustainTimer = 0f;
        private float m_jetpackMinIdleTime = 0f;
        private const float JETPACK_TIME_BETWEEN_SOUNDS = 0.25f;
        private bool m_jumpReady = false;
		const int SCREAM_DELAY_MS = 800;

        // default distance of the ankle from the ground
        // (last resort if not found in sbc)
        const float DEFAULT_ANKLE_HEIGHT = 0.2f;// 0.13f;
        // last time step sound was played
		private int m_lastStepTime = 0;
        // minimum time difference between two step sounds
        // TODO: probably compute time from animations? :)
		const int m_stepMinimumDelayWalk = 500;
		const int m_stepMinimumDelayRun = 300;
        const int m_stepMinimumDelaySprint = 250;

		MyCharacterMovementEnum m_lastUpdateMovementState;

		private MyCharacter m_character = null;
        private MyCubeGrid m_standingOnGrid = null;
        private MyVoxelBase m_standingOnVoxel = null;
        private MyStringHash m_characterPhysicalMaterial = MyMaterialType.CHARACTER;
        private bool m_isWalking = false;

        private const float WIND_SPEED_LOW = 40f;
        private const float WIND_SPEED_HIGH = 80f;
        private const float WIND_SPEED_DIFF = WIND_SPEED_HIGH - WIND_SPEED_LOW;
        private const float WIND_CHANGE_SPEED = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 0.5f;
        private float m_windVolume = 0;
        private float m_windTargetVolume = 0;
        private bool m_inAtmosphere = true;
        private MyEntity3DSoundEmitter m_windEmitter;
        private bool m_windSystem = false;

        private MyEntity3DSoundEmitter m_oxygenEmitter;
        private MyEntity3DSoundEmitter m_movementEmitter;

        private MySoundPair m_lastActionSound = null;
        private MySoundPair m_lastPrimarySound = null;

        public MyCubeGrid StandingOnGrid
        {
            get { return m_standingOnGrid; }
        }
        public MyVoxelBase StandingOnVoxel
        {
            get { return m_standingOnVoxel; }
        }

        private bool ShouldUpdateSoundEmitters
        {
            get
            {
                return (m_character == MySession.Static.LocalCharacter && m_character.AtmosphereDetectorComp != null && m_character.AtmosphereDetectorComp.InAtmosphere == false 
                    && MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS_QUICK_UPDATE);
            }
        }

		#endregion

		public MyCharacterSoundComponent()
		{
			m_soundEmitters = new List<MyEntity3DSoundEmitter>(Enum.GetNames(typeof(MySoundEmitterEnum)).Length);
			foreach(var name in Enum.GetNames(typeof(MySoundEmitterEnum)))
			{
				m_soundEmitters.Add(new MyEntity3DSoundEmitter(Entity as MyEntity));
			}

            for (int i = 0; i < Enum.GetNames(typeof(CharacterSoundsEnum)).Length; i++)
            {
                CharacterSounds.Add(i, EmptySoundPair);
            }

            if (MySession.Static != null && (MySession.Static.Settings.EnableOxygen || MySession.Static.CreativeMode))
                m_oxygenEmitter = new MyEntity3DSoundEmitter(Entity as MyEntity);
		}

		private void InitSounds()
		{
            if (m_character.Definition.JumpSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JUMP_SOUND] = new MySoundPair(m_character.Definition.JumpSoundName);

            if (m_character.Definition.JetpackIdleSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND] = new MySoundPair(m_character.Definition.JetpackIdleSoundName);
            if (m_character.Definition.JetpackRunSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND] = new MySoundPair(m_character.Definition.JetpackRunSoundName);

            if (m_character.Definition.CrouchDownSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.CROUCH_DOWN_SOUND] = new MySoundPair(m_character.Definition.CrouchDownSoundName);
            if (m_character.Definition.CrouchUpSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.CROUCH_UP_SOUND] = new MySoundPair(m_character.Definition.CrouchUpSoundName);
         
            if (m_character.Definition.PainSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.PAIN_SOUND] = new MySoundPair(m_character.Definition.PainSoundName);
            if (m_character.Definition.SuffocateSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.SUFFOCATE_SOUND] = new MySoundPair(m_character.Definition.SuffocateSoundName);
            if (m_character.Definition.DeathSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND] = new MySoundPair(m_character.Definition.DeathSoundName);
            if (m_character.Definition.DeathBySuffocationSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND_SUFFOCATE] = new MySoundPair(m_character.Definition.DeathBySuffocationSoundName);

            if (m_character.Definition.IronsightActSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_ACT_SOUND] = new MySoundPair(m_character.Definition.IronsightActSoundName);
            if (m_character.Definition.IronsightDeactSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND] = new MySoundPair(m_character.Definition.IronsightDeactSoundName);

            if (m_character.Definition.FastFlySoundName != null)
            {
                m_windEmitter = new MyEntity3DSoundEmitter(Entity as MyEntity);
                m_windEmitter.Force3D = false;
                m_windSystem = true;
                CharacterSounds[(int)CharacterSoundsEnum.FAST_FLY_SOUND] = new MySoundPair(m_character.Definition.FastFlySoundName);
            }

            //helmet sounds
            if (m_character.Definition.HelmetOxygenNormalSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.HELMET_NORMAL] = new MySoundPair(m_character.Definition.HelmetOxygenNormalSoundName);
            if (m_character.Definition.HelmetOxygenLowSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.HELMET_LOW] = new MySoundPair(m_character.Definition.HelmetOxygenLowSoundName);
            if (m_character.Definition.HelmetOxygenCriticalSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.HELMET_CRITICAL] = new MySoundPair(m_character.Definition.HelmetOxygenCriticalSoundName);
            if (m_character.Definition.HelmetOxygenNoneSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.HELMET_NONE] = new MySoundPair(m_character.Definition.HelmetOxygenNoneSoundName);

            if (m_character.Definition.MovementSoundName != null)
            {
                CharacterSounds[(int)CharacterSoundsEnum.MOVEMENT_SOUND] = new MySoundPair(m_character.Definition.MovementSoundName);
                m_movementEmitter = new MyEntity3DSoundEmitter(Entity as MyEntity);
            }

            CharacterSounds[(int)CharacterSoundsEnum.MAGNETIC_SOUND] = new MySoundPair("PlayFallGlass"); 
            
            //Preload();
            //if (m_character.Definition.DeathSoundName != null && m_character.Definition.DeathSoundName.Length > 0) CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND] = new MySoundPair(m_character.Definition.DeathSoundName);
		}

		public void Preload()
		{
			foreach (var sound in CharacterSounds.Values)
			{
				MyEntity3DSoundEmitter.PreloadSound(sound);
			}
		}

        public void CharacterDied()
        {
            if (m_windEmitter.IsPlaying)
                m_windEmitter.StopSound(true);
        }

        public void UpdateWindSounds()
        {
            if (m_windSystem && m_character.IsDead == false)
            {
                if (m_inAtmosphere)
                {
                    float speed = m_character.Physics.LinearVelocity.Length();
                    if (speed < WIND_SPEED_LOW)
                    {
                        m_windTargetVolume = 0f;
                    }
                    else if (speed < WIND_SPEED_HIGH)
                    {
                        m_windTargetVolume = (speed - WIND_SPEED_LOW) / WIND_SPEED_DIFF;
                    }
                    else
                    {
                        m_windTargetVolume = 1f;
                    }
                }
                else
                {
                    m_windTargetVolume = 0f;
                }
                bool volumeChanged = m_windVolume != m_windTargetVolume;
                if (m_windVolume < m_windTargetVolume)
                    m_windVolume = Math.Min(m_windVolume + WIND_CHANGE_SPEED, m_windTargetVolume);
                else if (m_windVolume > m_windTargetVolume)
                    m_windVolume = Math.Max(m_windVolume - WIND_CHANGE_SPEED, m_windTargetVolume);

                if (m_windEmitter.IsPlaying)
                {
                    if (m_windVolume <= 0f)
                    {
                        m_windEmitter.StopSound(true);
                    }
                    else
                    {
                        m_windEmitter.CustomVolume = m_windVolume;
                    }
                }
                else
                {
                    if (m_windVolume > 0f)
                    {
                        m_windEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.FAST_FLY_SOUND],true,force2D : true);
                        m_windEmitter.CustomVolume = m_windVolume;
                    }
                }
                if(volumeChanged)
                {
                    var ambient = MySession.Static.GetComponent<MySessionComponentPlanetAmbientSounds>();
                    if (ambient != null)
                        ambient.VolumeModifierGlobal = 1f - m_windVolume;
                }
            }
        }

		public void UpdateBeforeSimulation100()
		{
            UpdateOxygenSounds();
			m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState].Update();
            if (m_windSystem)
            {
                m_inAtmosphere = m_character.AtmosphereDetectorComp != null && m_character.AtmosphereDetectorComp.InAtmosphere;
                m_windEmitter.Update();
            }
            if (m_oxygenEmitter != null)
                m_oxygenEmitter.Update();
		}

        public void PlayActionSound(MySoundPair actionSound)
        {
            m_lastActionSound = actionSound;
            m_soundEmitters[(int)MySoundEmitterEnum.Action].PlaySound(m_lastActionSound);
        }

		public void FindAndPlayStateSound()
		{
            if (m_character.Breath != null)
			    m_character.Breath.Update();

			var cueEnum = SelectSound();
            UpdateBreath();

            if (m_isWalking && m_character.IsMagneticBootsEnabled && (CharacterSounds[(int)CharacterSoundsEnum.MAGNETIC_SOUND] != MySoundPair.Empty))
                cueEnum = CharacterSounds[(int)CharacterSoundsEnum.MAGNETIC_SOUND];

            if (m_movementEmitter != null)
            {
                //if (m_character.IsMagneticBootsEnabled)
                //{
                //    if (CharacterSounds[(int)CharacterSoundsEnum.MAGNETIC_SOUND] != MySoundPair.Empty)
                //    {
                //        if (m_isWalking && !m_movementEmitter.IsPlaying)
                //            m_movementEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.MAGNETIC_SOUND]);
                //        if (!m_isWalking && m_movementEmitter.IsPlaying)
                //            m_movementEmitter.StopSound(false);
                //    }
                //}
                //else
                {
                    if (CharacterSounds[(int)CharacterSoundsEnum.MOVEMENT_SOUND] != MySoundPair.Empty)
                    {
                        if (m_isWalking && !m_movementEmitter.IsPlaying)
                            m_movementEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.MOVEMENT_SOUND]);
                        if (!m_isWalking && m_movementEmitter.IsPlaying)
                            m_movementEmitter.StopSound(false);
                    }
                }            
            }

			var primaryEmitter = m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState];
			var walkEmitter = m_soundEmitters[(int)MySoundEmitterEnum.WalkState];
            bool sameSoundAlreadyPlaying = (cueEnum.Equals(primaryEmitter.SoundPair) && primaryEmitter.IsPlaying);

            if (primaryEmitter.Sound != null)
            {
                if (primaryEmitter.LastSoundData != null)
                {
                    float scaledVolume = primaryEmitter.LastSoundData.Volume * MathHelper.Clamp(m_character.Physics.LinearVelocity.Length() / 7.5f, 0.6f, 1);
                    primaryEmitter.Sound.SetVolume(scaledVolume);
                }
            }

            if (!sameSoundAlreadyPlaying && (m_isWalking == false || m_character.Definition.LoopingFootsteps))
			{
                if (cueEnum != EmptySoundPair && cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND])
				{
                    if (m_jetpackSustainTimer >= JETPACK_TIME_BETWEEN_SOUNDS)
                    {
                        if (primaryEmitter.Loop)
                            primaryEmitter.StopSound(true);
                        primaryEmitter.PlaySound(cueEnum, false, false);
                    }
				}
                else if (primaryEmitter.SoundId.IsNull == false && primaryEmitter.SoundId == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND].SoundId)
				{
                    if (m_jetpackSustainTimer <= 0f || cueEnum != CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND])
                    {
                        primaryEmitter.StopSound(false);
                        primaryEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND], false, true);
                    }
				}
                else if (cueEnum == EmptySoundPair)
                {
                    foreach (var soundEmitter in m_soundEmitters)
                    {
                        if (soundEmitter.Loop)
                            soundEmitter.StopSound(false);
                    }
                }
                else if (cueEnum == m_lastPrimarySound && (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.CROUCH_DOWN_SOUND] || cueEnum == CharacterSounds[(int)CharacterSoundsEnum.CROUCH_UP_SOUND]))
                {
                    //do nothing
                }
				else
				{
					if (primaryEmitter.Loop)
						primaryEmitter.StopSound(false);
                    primaryEmitter.PlaySound(cueEnum, true, false);
				}
			}
            else if (!m_character.Definition.LoopingFootsteps&& walkEmitter != null && cueEnum != null)
			{
                IKFeetStepSounds(walkEmitter, cueEnum);
			}
            m_lastPrimarySound = cueEnum;
		}

        private void IKFeetStepSounds(MyEntity3DSoundEmitter walkEmitter, MySoundPair cueEnum)
        {
            var movementState = m_character.GetCurrentMovementState();

			if (movementState.GetMode() == MyCharacterMovement.Flying)
				return;

            if (movementState.GetSpeed() != m_lastUpdateMovementState.GetSpeed())
            {
                walkEmitter.StopSound(true);
                m_lastStepTime = 0;
            }

			int usedMinimumDelay = int.MaxValue;
			if (movementState.GetDirection() != MyCharacterMovement.NoDirection)
			{
			    switch (movementState.GetSpeed())
			    {
			        case MyCharacterMovement.NormalSpeed:
			            usedMinimumDelay = m_stepMinimumDelayWalk;
			            break;
			        case MyCharacterMovement.Fast:
			            usedMinimumDelay = m_stepMinimumDelayRun;
			            break;
			        case MyCharacterMovement.VeryFast:
			            usedMinimumDelay = m_stepMinimumDelaySprint;
			            break;
			    }
			}

            bool minimumDelayExceeded = false;
			minimumDelayExceeded = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastStepTime) >= usedMinimumDelay;
            //MyRenderProxy.DebugDrawAABB(m_character.PositionComp.WorldAABB, Color.White);

			if (minimumDelayExceeded)
            {
                int leftAnkleBoneIndex, rightAnkleBoneIndex;
                MyCharacterBone leftAnkleBone = m_character.AnimationController != null 
                    ? m_character.AnimationController.FindBone(m_character.Definition.LeftAnkleBoneName, out leftAnkleBoneIndex) 
                    : null;
                MyCharacterBone rightAnkleBone = m_character.AnimationController != null 
                    ? m_character.AnimationController.FindBone(m_character.Definition.RightAnkleBoneName, out rightAnkleBoneIndex)
                    : null;
                Vector3 posLeftFoot = leftAnkleBone != null
                    ? leftAnkleBone.AbsoluteTransform.Translation
                    : m_character.PositionComp.LocalAABB.Center;
                Vector3 posRightFoot = rightAnkleBone != null 
                    ? rightAnkleBone.AbsoluteTransform.Translation 
                    : m_character.PositionComp.LocalAABB.Center;
                float ankleHeight;
                MyFeetIKSettings settingsIK;
                if (m_character.Definition.FeetIKSettings != null
                    && m_character.Definition.FeetIKSettings.TryGetValue(MyCharacterMovementEnum.Standing, out settingsIK))
                {
                    ankleHeight = settingsIK.FootSize.Y;
                }
                else
                {
                    ankleHeight = DEFAULT_ANKLE_HEIGHT;
                }
                float charSpeed = 0f;
                if (m_character.AnimationController != null)
                    m_character.AnimationController.Variables.GetValue(MyAnimationVariableStorageHints.StrIdSpeed, out charSpeed);
                if (posLeftFoot.Y - ankleHeight < m_character.PositionComp.LocalAABB.Min.Y
                    || posRightFoot.Y - ankleHeight < m_character.PositionComp.LocalAABB.Min.Y)
                {
                    if(charSpeed > 0.05f)
                        walkEmitter.PlaySound(cueEnum);
                    m_lastStepTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                }
            }

			m_lastUpdateMovementState = movementState;
        }

		public bool StopStateSound(bool forceStop = true)
		{
			m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState].StopSound(forceStop);
			return true;	// Should return false when failed to stop sound
		}

		public void PlaySecondarySound(CharacterSoundsEnum soundEnum, bool stopPrevious = false)
		{
			m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(CharacterSounds[(int)soundEnum], stopPrevious);
		}

        public void PlayDeathSound(MyStringHash damageType, bool stopPrevious = false)
        {
            if(damageType == LowPressure)
                m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND_SUFFOCATE], stopPrevious);
            else
                m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND], stopPrevious);
        }

		public void StartSecondarySound(string cueName, bool sync = false)
		{
			StartSecondarySound(MySoundPair.GetCueId(cueName), sync);
		}

		public void StartSecondarySound(MyCueId cueId, bool sync = false)
		{
			if (cueId.IsNull) return;

			m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySoundWithDistance(cueId);            

			if (sync)
			{
				m_character.PlaySecondarySound(cueId);
			}
		}

		public bool StopSecondarySound(bool forceStop = true)
		{
			m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].StopSound(forceStop);
			return true;	// Should return false when failed to stop sound
		}

		private MySoundPair SelectSound()
		{
            MySoundPair soundPair = EmptySoundPair;
            MyCharacterDefinition myCharDefinition = m_character.Definition;
            MyStringHash physMaterial = MyStringHash.GetOrCompute(myCharDefinition.PhysicalMaterial);
            m_isWalking = false;

            bool updateEmitterSounds = false;
            MyCharacterMovementEnum movementState = m_character.GetCurrentMovementState();
            switch (movementState)
			{
				case MyCharacterMovementEnum.Walking:
				case MyCharacterMovementEnum.BackWalking:
				case MyCharacterMovementEnum.WalkingLeftFront:
				case MyCharacterMovementEnum.WalkingRightFront:
				case MyCharacterMovementEnum.WalkingLeftBack:
				case MyCharacterMovementEnum.WalkingRightBack:
				case MyCharacterMovementEnum.WalkStrafingLeft:
				case MyCharacterMovementEnum.WalkStrafingRight:
					{
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Walk, physMaterial, RayCastGround());
                        m_isWalking = true;
					}
					break;
				case MyCharacterMovementEnum.Running:
				case MyCharacterMovementEnum.Backrunning:
				case MyCharacterMovementEnum.RunStrafingLeft:
				case MyCharacterMovementEnum.RunStrafingRight:
				case MyCharacterMovementEnum.RunningRightFront:
				case MyCharacterMovementEnum.RunningRightBack:
				case MyCharacterMovementEnum.RunningLeftFront:
				case MyCharacterMovementEnum.RunningLeftBack:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Heated;
                        soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Run, physMaterial, RayCastGround());
                        m_isWalking = true;
					}
					break;
				case MyCharacterMovementEnum.CrouchWalking:
				case MyCharacterMovementEnum.CrouchBackWalking:
				case MyCharacterMovementEnum.CrouchWalkingLeftFront:
				case MyCharacterMovementEnum.CrouchWalkingRightFront:
				case MyCharacterMovementEnum.CrouchWalkingLeftBack:
				case MyCharacterMovementEnum.CrouchWalkingRightBack:
				case MyCharacterMovementEnum.CrouchStrafingLeft:
				case MyCharacterMovementEnum.CrouchStrafingRight:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.CrouchWalk, physMaterial, RayCastGround());
                        m_isWalking = true;
					}
					break;
                case MyCharacterMovementEnum.Crouching:
                case MyCharacterMovementEnum.Standing:
                    {
                        if (m_character.Breath != null)
                            m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        var previousMovementState = m_character.GetPreviousMovementState();
                        var currentMovementState = m_character.GetCurrentMovementState();
                        if (previousMovementState != currentMovementState
                            && (previousMovementState == MyCharacterMovementEnum.Standing || previousMovementState == MyCharacterMovementEnum.Crouching))
                            soundPair = (currentMovementState == MyCharacterMovementEnum.Standing) ? CharacterSounds[(int)CharacterSoundsEnum.CROUCH_UP_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.CROUCH_DOWN_SOUND];
                    }
                    break;
				case MyCharacterMovementEnum.Sprinting:
                    {
                        if (m_character.Breath != null)
                            m_character.Breath.CurrentState = MyCharacterBreath.State.VeryHeated;
                        soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Sprint, physMaterial, RayCastGround());
                        m_isWalking = true;
					}
					break;
				case MyCharacterMovementEnum.Jump:
					{
                        if (!m_jumpReady)
							break;
                        m_jumpReady = false;
						m_character.SetPreviousMovementState(m_character.GetCurrentMovementState());
						var emitter = MyAudioComponent.TryGetSoundEmitter(); // We need to use another emitter otherwise the sound would be cut by silence next frame
						if (emitter != null)
						{
							emitter.Entity = m_character;
							emitter.PlaySingleSound(CharacterSounds[(int)CharacterSoundsEnum.JUMP_SOUND]);
						}

                        if ((m_standingOnGrid != null || m_standingOnVoxel != null) && ShouldUpdateSoundEmitters)
                            updateEmitterSounds = true;
                        m_standingOnGrid = null;
                        m_standingOnVoxel = null;
					}
					break;
				case MyCharacterMovementEnum.Flying:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        if (m_character.JetpackComp != null && m_jetpackMinIdleTime <= 0f && m_character.JetpackComp.FinalThrust.LengthSquared() >= 50000f)
                        {
                            soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND];
                            m_jetpackSustainTimer = Math.Min(JETPACK_TIME_BETWEEN_SOUNDS, m_jetpackSustainTimer + MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                        }
                        else
                        {
                            soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND];
                            m_jetpackSustainTimer = Math.Max(0f, m_jetpackSustainTimer - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                        }
                        m_jetpackMinIdleTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        if ((m_standingOnGrid != null || m_standingOnVoxel != null) && ShouldUpdateSoundEmitters)
                            updateEmitterSounds = true;
                        m_standingOnGrid = null;
                        m_standingOnVoxel = null;
					}
					break;
				case MyCharacterMovementEnum.Falling:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;

                        if ((m_standingOnGrid != null || m_standingOnVoxel != null) && ShouldUpdateSoundEmitters)
                            updateEmitterSounds = true;
                        m_standingOnGrid = null;
                        m_standingOnVoxel = null;
					}
					break;
				default:
					{
					}
					break;
			}

            //MyRenderProxy.DebugDrawText2D(Vector2.Zero, movementState.ToString(), Color.Red, 1.0f);

            if (movementState != MyCharacterMovementEnum.Flying)
            {
                m_jetpackSustainTimer = 0f;
                m_jetpackMinIdleTime = 0.5f;
            }
            if(updateEmitterSounds)
                MyEntity3DSoundEmitter.UpdateEntityEmitters(true, true, false);
			return soundPair;
		}


        private void UpdateOxygenSounds()
        {
            if(m_oxygenEmitter == null)
                return;
            if (m_character.IsDead == false && MySession.Static != null && MySession.Static.Settings.EnableOxygen && !MySession.Static.CreativeMode && m_character.OxygenComponent != null && m_character.OxygenComponent.HelmetEnabled)
            {
                MySoundPair oxygenSoundToPlay;
                if (MySession.Static.CreativeMode)
                {
                    oxygenSoundToPlay = CharacterSounds[(int)CharacterSoundsEnum.HELMET_NORMAL];
                }
                else
                {
                    if (m_character.OxygenComponent.SuitOxygenLevel > MyCharacterOxygenComponent.LOW_OXYGEN_RATIO)
                        oxygenSoundToPlay = CharacterSounds[(int)CharacterSoundsEnum.HELMET_NORMAL];
                    else if (m_character.OxygenComponent.SuitOxygenLevel > MyCharacterOxygenComponent.LOW_OXYGEN_RATIO / 3f)
                        oxygenSoundToPlay = CharacterSounds[(int)CharacterSoundsEnum.HELMET_LOW];
                    else if (m_character.OxygenComponent.SuitOxygenLevel > 0f)
                        oxygenSoundToPlay = CharacterSounds[(int)CharacterSoundsEnum.HELMET_CRITICAL];
                    else
                        oxygenSoundToPlay = CharacterSounds[(int)CharacterSoundsEnum.HELMET_NONE];
                }

                if (m_oxygenEmitter.IsPlaying == false || m_oxygenEmitter.SoundPair != oxygenSoundToPlay)
                    m_oxygenEmitter.PlaySound(oxygenSoundToPlay, true);
            }
            else if (m_oxygenEmitter.IsPlaying)
                m_oxygenEmitter.StopSound(true);
        }

        private void UpdateBreath()
        {
            // turn off breathing when helmet is off and there is no oxygen https://app.asana.com/0/64822442925263/33431071589757
            if (m_character.OxygenComponent != null && m_character.Breath != null)
            {
                if (MySession.Static.Settings.EnableOxygen && MySession.Static.CreativeMode == false)
                {
                    if (m_character.Parent is MyCockpit && (m_character.Parent as MyCockpit).BlockDefinition.IsPressurized)
                    {
                        if (m_character.OxygenComponent.HelmetEnabled)
                        {
                            if (m_character.OxygenComponent.SuitOxygenAmount > 0f)
                                m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;//oxygen in suit - breathing
                            else
                                m_character.Breath.CurrentState = MyCharacterBreath.State.Choking;//no oxygen in suit
                        }
                        else
                        {
                            if (m_character.OxygenComponent.EnvironmentOxygenLevel >= MyCharacterOxygenComponent.LOW_OXYGEN_RATIO)
                                m_character.Breath.CurrentState = MyCharacterBreath.State.NoBreath;//oxygen in cockpit - no sound
                            else
                                m_character.Breath.CurrentState = MyCharacterBreath.State.Choking;//no oxygen in cockpit
                        }
                    }
                    else
                    {
                        if (m_character.OxygenComponent.HelmetEnabled)
                        {
                            if (m_character.OxygenComponent.SuitOxygenAmount <= 0f)
                                m_character.Breath.CurrentState = MyCharacterBreath.State.Choking;//no oxygen in suit
                        }
                        else
                        {
                            if (m_character.OxygenComponent.EnvironmentOxygenLevel >= MyCharacterOxygenComponent.LOW_OXYGEN_RATIO)
                                m_character.Breath.CurrentState = MyCharacterBreath.State.NoBreath;//oxygen in environment - possibly add silent breathing sound
                            else if (m_character.OxygenComponent.EnvironmentOxygenLevel > 0)
                                m_character.Breath.CurrentState = MyCharacterBreath.State.VeryHeated;//low oxygen in environment
                            else
                                m_character.Breath.CurrentState = MyCharacterBreath.State.Choking;//no oxygen in environment
                        }
                    }
                }
                else
                {
                    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                }
            }
        }

		public void PlayFallSound()
		{
			var walkSurfaceMaterial = RayCastGround();
            if (walkSurfaceMaterial != MyStringHash.NullOrEmpty && MyMaterialPropertiesHelper.Static != null)
			{
                var cue = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Fall, m_characterPhysicalMaterial, walkSurfaceMaterial);
                if (cue.SoundId.IsNull == false)
                {
                    var emitter = MyAudioComponent.TryGetSoundEmitter(); //we need to use other emmiter otherwise the sound would be cut by silence next frame
                    if (emitter != null)
                    {
                        emitter.Entity = m_character;
                        emitter.PlaySingleSound(cue);
                    }
                }
			}
		}

        private MyStringHash RayCastGround()
		{
            MyStringHash walkSurfaceMaterial = new MyStringHash();

            float maxDistValue = MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE;
            var from = m_character.PositionComp.GetPosition() + m_character.PositionComp.WorldMatrix.Up * 0.5; //(needs some small distance from the bottom or the following call to HavokWorld.CastRay will find no hits)
            var to = from + m_character.PositionComp.WorldMatrix.Down * maxDistValue;

			MyPhysics.CastRay(from, to, m_hits, MyPhysics.CollisionLayers.CharacterCollisionLayer);

			// Skips invalid hits (null body, self character)
			int index = 0;
			while ((index < m_hits.Count) && ((m_hits[index].HkHitInfo.Body == null) || (m_hits[index].HkHitInfo.GetHitEntity() == Entity.Components)))
			{
				index++;
			}

            if (m_hits.Count == 0)
            {
                if ((m_standingOnGrid != null || m_standingOnVoxel != null) && ShouldUpdateSoundEmitters)
                {
                m_standingOnGrid = null;
                m_standingOnVoxel = null;
                    MyEntity3DSoundEmitter.UpdateEntityEmitters(true, true, false);
            }
                else
                {
                    m_standingOnGrid = null;
                    m_standingOnVoxel = null;
                }
            }

			if (index < m_hits.Count)
			{
				// We must take only closest hit (others are hidden behind)
				var h = m_hits[index];
				var entity = h.HkHitInfo.GetHitEntity();

				var sqDist = Vector3D.DistanceSquared((Vector3D)h.Position, from);
                if (sqDist < maxDistValue * maxDistValue)
				{
					var cubeGrid = entity as MyCubeGrid;
					var voxelBase = entity as MyVoxelBase;

                    if (((cubeGrid != null && m_standingOnGrid != cubeGrid) || (voxelBase != null && m_standingOnVoxel != voxelBase)) && ShouldUpdateSoundEmitters)
                    {
                    m_standingOnGrid = cubeGrid;
                    m_standingOnVoxel = voxelBase;
                        MyEntity3DSoundEmitter.UpdateEntityEmitters(true, true, true);
                    }
                    else
                    {
                        m_standingOnGrid = cubeGrid;
                        m_standingOnVoxel = voxelBase;
                    }
                    if(cubeGrid != null || voxelBase != null)
                        m_jumpReady = true;

                    if (cubeGrid != null)
                        walkSurfaceMaterial = cubeGrid.Physics.GetMaterialAt(h.Position + m_character.PositionComp.WorldMatrix.Down * 0.1f);
                    else if (voxelBase != null && voxelBase.Storage != null && voxelBase.Storage.DataProvider != null)
                    {
                        var materialDefinition = voxelBase.GetMaterialAt(ref h.Position);
                        if (materialDefinition != null)
                            walkSurfaceMaterial = MyStringHash.GetOrCompute(materialDefinition.MaterialTypeName);
                    }
					if (walkSurfaceMaterial.ToString().Length == 0)
						walkSurfaceMaterial = MyMaterialType.ROCK;
				}
			}

			m_hits.Clear();

			return walkSurfaceMaterial;
		}

		public void PlayDamageSound(float oldHealth)
		{
			if (MyFakes.ENABLE_NEW_SOUNDS)
			{
				if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastScreamTime > SCREAM_DELAY_MS)
				{
					m_lastScreamTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    if (m_character.StatComp != null && m_character.StatComp.LastDamage.Type == LowPressure)
                        PlaySecondarySound(CharacterSoundsEnum.SUFFOCATE_SOUND);
                    else
                    PlaySecondarySound(CharacterSoundsEnum.PAIN_SOUND);
				}
			}
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();
			m_character = Entity as MyCharacter;
			foreach(var soundEmitter in m_soundEmitters)
			{
				soundEmitter.Entity = Entity as MyEntity;
			}
            if(m_windEmitter != null)
                m_windEmitter.Entity = Entity as MyEntity;
            if (m_oxygenEmitter != null)
                m_oxygenEmitter.Entity = Entity as MyEntity;
			m_lastUpdateMovementState = m_character.GetCurrentMovementState();
            m_characterPhysicalMaterial = MyStringHash.GetOrCompute(m_character.Definition.PhysicalMaterial);
			InitSounds();
		}

		public override void OnBeforeRemovedFromContainer()
		{
			StopStateSound();

			m_character = null;
			base.OnBeforeRemovedFromContainer();
		}

		public override string ComponentTypeDebugString
		{
			get { return "CharacterSound"; }
		}
	}
}
