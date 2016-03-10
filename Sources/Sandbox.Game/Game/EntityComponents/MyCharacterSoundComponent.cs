using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using Sandbox.Game.Entities.Character.Components;
using VRage.Data.Audio;
using Sandbox.Game.GUI;
using VRage.Game.Entity;
using VRageRender;
using System.Diagnostics;
using VRage.Animations;
using VRage.Game;

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
		DEATH_SOUND,

		IRONSIGHT_ACT_SOUND,
		IRONSIGHT_DEACT_SOUND,

        FAST_FLY_SOUND
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

		#endregion

		#region Fields

		private List<MyEntity3DSoundEmitter> m_soundEmitters;
		
		List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

		private int m_lastScreamTime;
		const int SCREAM_DELAY_MS = 800;

        // default distance of the ankle from the ground
        // (last resort if not found in sbc)
        const float DEFAULT_ANKLE_HEIGHT = 0.13f;
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

        private const float WIND_SPEED_LOW = 40f;
        private const float WIND_SPEED_HIGH = 80f;
        private const float WIND_SPEED_DIFF = WIND_SPEED_HIGH - WIND_SPEED_LOW;
        private float windVolume = 0;
        private bool inAtmosphere = true;
        private MyEntity3DSoundEmitter windEmitter;
        private bool windSystem = false;

        private MySoundPair lastActionSound = null;

        public MyCubeGrid StandingOnGrid
        {
            get { return m_standingOnGrid; }
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
		}

		private void InitSounds()
		{
            if (m_character.Definition.JumpSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JUMP_SOUND] = new MySoundPair(m_character.Definition.JumpSoundName);

            if (m_character.Definition.JetpackIdleSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND] = new MySoundPair(m_character.Definition.JetpackIdleSoundName);
            if (m_character.Definition.JetpackRunSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND] = new MySoundPair(m_character.Definition.JetpackRunSoundName);

            if (m_character.Definition.CrouchDownSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.CROUCH_DOWN_SOUND] = new MySoundPair(m_character.Definition.CrouchDownSoundName);
            if (m_character.Definition.CrouchUpSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.CROUCH_UP_SOUND] = new MySoundPair(m_character.Definition.CrouchUpSoundName);
         
            if (m_character.Definition.PainSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.PAIN_SOUND] = new MySoundPair(m_character.Definition.PainSoundName);
            if (m_character.Definition.DeathSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.DEATH_SOUND] = new MySoundPair(m_character.Definition.DeathSoundName);

            if (m_character.Definition.IronsightActSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_ACT_SOUND] = new MySoundPair(m_character.Definition.IronsightActSoundName);
            if (m_character.Definition.IronsightDeactSoundName != null) CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND] = new MySoundPair(m_character.Definition.IronsightDeactSoundName);

            if (m_character.Definition.FastFlySoundName != null)
            {
                windEmitter = new MyEntity3DSoundEmitter(Entity as MyEntity);
                windEmitter.Force3D = false;
                windSystem = true;
                CharacterSounds[(int)CharacterSoundsEnum.FAST_FLY_SOUND] = new MySoundPair(m_character.Definition.FastFlySoundName);
            }

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
            if (windEmitter.IsPlaying)
                windEmitter.StopSound(true);
        }

        public void UpdateWindSounds()
        {
            if (windSystem && m_character.IsDead == false)
            {
                if (inAtmosphere)
                {
                    float speed = m_character.Physics.LinearVelocity.Length();
                    if (speed < WIND_SPEED_LOW)
                    {
                        windVolume = 0f;
                    }
                    else if (speed < WIND_SPEED_HIGH)
                    {
                        windVolume = (speed - WIND_SPEED_LOW) / WIND_SPEED_DIFF;
                    }
                    else
                    {
                        windVolume = 1f;
                    }
                }
                else
                {
                    windVolume = 0f;
                }
                if (windEmitter.IsPlaying)
                {
                    if (windVolume <= 0f)
                    {
                        windEmitter.StopSound(true);
                    }
                    else
                    {
                        windEmitter.CustomVolume = windVolume;
                    }
                }
                else
                {
                    if (windVolume > 0f)
                    {
                        windEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.FAST_FLY_SOUND],true,force2D : true);
                        windEmitter.CustomVolume = windVolume;
                    }
                }
            }
        }

		public void UpdateBeforeSimulation100()
		{
			m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState].Update();
            if (windSystem)
            {
                inAtmosphere = m_character.InAtmosphere;
                windEmitter.Update();
            }
		}

        public void PlayActionSound(MySoundPair actionSound)
        {
            lastActionSound = actionSound;
            m_soundEmitters[(int)MySoundEmitterEnum.Action].PlaySound(lastActionSound);
        }

		public void FindAndPlayStateSound()
		{
            if (m_character.Breath != null)
			    m_character.Breath.Update();

			var cueEnum = SelectSound();

			var primaryEmitter = m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState];
			var walkEmitter = m_soundEmitters[(int)MySoundEmitterEnum.WalkState];
			bool sameSoundAlreadyPlaying = (cueEnum.Arcade == primaryEmitter.SoundId
											|| (MyFakes.ENABLE_NEW_SOUNDS && cueEnum.Realistic == primaryEmitter.SoundId));

            if (primaryEmitter.Sound != null)
            {
                MySoundData cue = MyAudio.Static.GetCue(cueEnum.SoundId);
                if (cue != null)
                {
                    float definedVolume = cue.Volume;
                    float scaledVolume = definedVolume * MathHelper.Clamp(m_character.Physics.LinearVelocity.Length() / 7.5f, 0.6f, 1);
                    primaryEmitter.Sound.SetVolume(scaledVolume);
                }
            }

			if (!sameSoundAlreadyPlaying)
			{
				if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND])
				{
					if (primaryEmitter.Loop)
						primaryEmitter.StopSound(true);
                    primaryEmitter.PlaySound(cueEnum, false, false);
				}
                else if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND] && primaryEmitter.SoundId == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND].SoundId)
				{
					primaryEmitter.StopSound(false);
                    primaryEmitter.PlaySound(cueEnum, false, true);
				}
                else if (cueEnum == EmptySoundPair)
                {
                    foreach (var soundEmitter in m_soundEmitters)
                    {
                        if (soundEmitter.Loop)
                            soundEmitter.StopSound(false);
                    }
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
                if (posLeftFoot.Y - ankleHeight < m_character.PositionComp.LocalAABB.Min.Y
                    || posRightFoot.Y - ankleHeight < m_character.PositionComp.LocalAABB.Min.Y)
                {
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

		public void StartSecondarySound(string cueName, bool sync = false)
		{
			StartSecondarySound(MySoundPair.GetCueId(cueName), sync);
		}

		public void StartSecondarySound(MyCueId cueId, bool sync = false)
		{
			if (cueId.IsNull) return;

			m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(cueId);            

			if (sync)
			{
				m_character.SyncObject.PlaySecondarySound(cueId);
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
            Definitions.MyCharacterDefinition myCharDefinition = (Definitions.MyCharacterDefinition)m_character.Definition;
            MyStringHash physMaterial = MyStringHash.GetOrCompute(myCharDefinition.PhysicalMaterial);

			switch (m_character.GetCurrentMovementState())
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
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Heated;
                        soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Sprint, physMaterial, RayCastGround());
					}
					break;
				case MyCharacterMovementEnum.Jump:
					{
						if (m_character.GetPreviousMovementState() == MyCharacterMovementEnum.Jump)
							break;
						m_character.SetPreviousMovementState(m_character.GetCurrentMovementState());
						var emitter = MyAudioComponent.TryGetSoundEmitter(); // We need to use another emitter otherwise the sound would be cut by silence next frame
						if (emitter != null)
						{
							emitter.Entity = m_character;
							emitter.PlaySingleSound(CharacterSounds[(int)CharacterSoundsEnum.JUMP_SOUND]);
						}
                        m_standingOnGrid = null;
					}
					break;
				case MyCharacterMovementEnum.Flying:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        if (m_character.Physics.LinearAcceleration.Length()<1)
							soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND];
						else
							soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND];

                        m_standingOnGrid = null;
					}
					break;
				case MyCharacterMovementEnum.Falling:
                    {
                        if (m_character.Breath != null)
						    m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        m_standingOnGrid = null;
					}
					break;
				default:
					{
					}
					break;
			}

            // turn off breathing when helmet is off and there is no oxygen https://app.asana.com/0/64822442925263/33431071589757
            if (m_character.OxygenComponent != null && !m_character.OxygenComponent.EnabledHelmet &&
                m_character.OxygenComponent.EnvironmentOxygenLevel < MyCharacterOxygenComponent.LOW_OXYGEN_RATIO)
            {
                if (m_character.Breath != null)
                    m_character.Breath.CurrentState = MyCharacterBreath.State.NoBreath;
            }

			return soundPair;
		}

		public void PlayFallSound()
		{
			var walkSurfaceMaterial = RayCastGround();
            if (walkSurfaceMaterial != MyStringHash.NullOrEmpty && MyMaterialPropertiesHelper.Static != null)
			{
                var cue = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Fall, MyMaterialType.CHARACTER, walkSurfaceMaterial);
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
                m_standingOnGrid = null;

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

                    m_standingOnGrid = cubeGrid;

					if (cubeGrid != null)
						walkSurfaceMaterial = cubeGrid.Physics.GetMaterialAt(h.Position);
					else if (voxelBase != null && voxelBase.Storage != null && voxelBase.Storage.DataProvider != null)
					{
                        var materialDefinition = voxelBase.GetMaterialAt(ref h.Position);
						if(materialDefinition != null)
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
			m_lastUpdateMovementState = m_character.GetCurrentMovementState();
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
