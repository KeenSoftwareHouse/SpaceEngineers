using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Components;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.Components
{
	public enum CharacterSoundsEnum : int
	{
		NONE_SOUND,
		JUMP_SOUND,

		JETPACK_IDLE_SOUND,
		JETPACK_RUN_SOUND,

		CROUCH_DOWN_SOUND,
		CROUCH_UP_SOUND,
		CROUCH_RUN_ROCK_SOUND,
		CROUCH_RUN_METAL_SOUND,

		PAIN_SOUND,
		DEATH,

		IRONSIGHT_ACT_SOUND,
		IRONSIGHT_DEACT_SOUND,
	}

	[MyComponentBuilder(typeof(MyObjectBuilder_CharacterSoundComponent))]
	public class MyCharacterSoundComponent : MyEntityComponentBase
	{
		#region Internal types
		private static readonly Dictionary<int, MySoundPair> CharacterSounds = new Dictionary<int, MySoundPair>()
        {
            { (int)CharacterSoundsEnum.NONE_SOUND, new MySoundPair() },
            { (int)CharacterSoundsEnum.JUMP_SOUND, new MySoundPair("PlayJump") },

            { (int)CharacterSoundsEnum.JETPACK_IDLE_SOUND, new MySoundPair("PlayJet") },
            { (int)CharacterSoundsEnum.JETPACK_RUN_SOUND, new MySoundPair("PlayJetRun") },

            { (int)CharacterSoundsEnum.CROUCH_DOWN_SOUND, new MySoundPair("PlayCrouchDwn") },
            { (int)CharacterSoundsEnum.CROUCH_UP_SOUND, new MySoundPair("PlayCrouchUp") },
            { (int)CharacterSoundsEnum.CROUCH_RUN_ROCK_SOUND, new MySoundPair("PlayCrouchRock") },
            { (int)CharacterSoundsEnum.CROUCH_RUN_METAL_SOUND, new MySoundPair("PlayCrouchMetal") },

            { (int)CharacterSoundsEnum.PAIN_SOUND, new MySoundPair("PlayVocPain") },
			{ (int)CharacterSoundsEnum.DEATH, new MySoundPair("") },

            { (int)CharacterSoundsEnum.IRONSIGHT_ACT_SOUND, new MySoundPair("PlayIronSightActivate") },
            { (int)CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND, new MySoundPair("PlayIronSightDeactivate") },
        };

		enum MySoundEmitterEnum
		{
			PrimaryState = 0,
			SecondaryState = 1,
			WalkState = 2,
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

		private int m_lastStepTime = 0;
		const int m_stepDelayWalk = 600;
		const int m_stepDelayRun = 350;
		const int m_stepDelaySprint = 300;
		MyCharacterMovementEnum m_lastUpdateMovementState;

		private MyCharacter m_character = null;

		#endregion

		public MyCharacterSoundComponent()
		{
			m_soundEmitters = new List<MyEntity3DSoundEmitter>(Enum.GetNames(typeof(MySoundEmitterEnum)).Length);
			foreach(var name in Enum.GetNames(typeof(MySoundEmitterEnum)))
			{
				m_soundEmitters.Add(new MyEntity3DSoundEmitter(Entity as MyEntity));
			}
		}

		private void InitSounds()
		{
			CharacterSounds[(int)CharacterSoundsEnum.DEATH] = new MySoundPair(m_character.Definition.DeathSoundName);
		}

		public static void Preload()
		{
			foreach (var sound in CharacterSounds.Values)
			{
				MyEntity3DSoundEmitter.PreloadSound(sound);
			}
		}

		public void UpdateBeforeSimulation100()
		{
			m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState].Update();
		}

		public void FindAndPlayStateSound()
		{
			m_character.Breath.Update();
			var cueEnum = SelectSound();

			var primaryEmitter = m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState];
			var walkEmitter = m_soundEmitters[(int)MySoundEmitterEnum.WalkState];
			bool sameSoundAlreadyPlaying = (cueEnum.Arcade == primaryEmitter.SoundId
											|| (MyFakes.ENABLE_NEW_SOUNDS && cueEnum.Realistic == primaryEmitter.SoundId));

			if (primaryEmitter.Sound != null)
				primaryEmitter.Sound.SetVolume(MathHelper.Clamp(m_character.Physics.LinearAcceleration.Length() / 3.0f, 0.6f, 1));

			if (!sameSoundAlreadyPlaying)
			{
			    bool skipIntro = cueEnum == CharacterSounds[(int) CharacterSoundsEnum.JETPACK_RUN_SOUND] || cueEnum == CharacterSounds[(int) CharacterSoundsEnum.JETPACK_IDLE_SOUND];

				if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND])
				{
					if (primaryEmitter.Loop)
						primaryEmitter.StopSound(false);
                    primaryEmitter.PlaySound(cueEnum, false, skipIntro);
				}
				else if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND] && m_soundEmitters[(int)MySoundEmitterEnum.PrimaryState].SoundId == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND].SoundId)
				{
					primaryEmitter.StopSound(false);
				}
				else if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND])
				{
					foreach(var soundEmitter in m_soundEmitters)
					{
						if (soundEmitter.Loop)
							soundEmitter.StopSound(false);
					}
				}
				else
				{
					if (primaryEmitter.Loop)
						primaryEmitter.StopSound(false);
                    primaryEmitter.PlaySound(cueEnum, true, skipIntro);
				}
			}
			else if (MyPerGameSettings.NonloopingCharacterFootsteps)
			{
				var movementState = m_character.GetCurrentMovementState();

				if (movementState.GetMode() == MyCharacterMovement.Flying)
					return;

				if (movementState.GetSpeed() != m_lastUpdateMovementState.GetSpeed())
					walkEmitter.StopSound(true);

				bool playSound = false;
				int usedDelay = int.MaxValue;
			    if (movementState.GetDirection() != MyCharacterMovement.NoDirection)
			    {
			        switch (movementState.GetSpeed())
			        {
			            case MyCharacterMovement.NormalSpeed:
			                usedDelay = m_stepDelayWalk;
			                break;
			            case MyCharacterMovement.Fast:
			                usedDelay = m_stepDelayRun;
			                break;
			            case MyCharacterMovement.VeryFast:
			                usedDelay = m_stepDelaySprint;
			                break;
			        }
			    }

			    playSound = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastStepTime) >= usedDelay;
				if (playSound)
				{
					walkEmitter.PlaySound(cueEnum);
					m_lastStepTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
				}

				m_lastUpdateMovementState = movementState;
			}
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

			if (!m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].IsPlaying)
			{
				m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(cueId);
			}

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
			MySoundPair soundPair = CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND];

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
						m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
						soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Walk, MyMaterialType.CHARACTER, RayCastGround());
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
						m_character.Breath.CurrentState = MyCharacterBreath.State.Heated;
						soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Run, MyMaterialType.CHARACTER, RayCastGround());
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
						m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
						soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.CrouchWalk, MyMaterialType.CHARACTER, RayCastGround());
					}
					break;
                case MyCharacterMovementEnum.Crouching:
                case MyCharacterMovementEnum.Standing:
                    {
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
						m_character.Breath.CurrentState = MyCharacterBreath.State.Heated;
						soundPair = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Sprint, MyMaterialType.CHARACTER, RayCastGround());
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
					}
					break;
				case MyCharacterMovementEnum.Flying:
					{
						m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
                        if (m_character.Physics.LinearAcceleration.Length()<1)
							soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND];
						else
							soundPair = CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND];
					}
					break;
				case MyCharacterMovementEnum.Falling:
					{
						m_character.Breath.CurrentState = MyCharacterBreath.State.Calm;
					}
					break;
				default:
					{
					}
					break;
			}

			return soundPair;
		}

		public void PlayFallSound()
		{
			var walkSurfaceMaterial = RayCastGround();
			if (walkSurfaceMaterial != MyStringHash.NullOrEmpty)
			{
				var emitter = MyAudioComponent.TryGetSoundEmitter(); //we need to use other emmiter otherwise the sound would be cut by silence next frame
				if (emitter != null)
				{
					emitter.Entity = m_character;
					if (MyMaterialPropertiesHelper.Static != null)
					{
						var cue = MyMaterialPropertiesHelper.Static.GetCollisionCue(MovementSoundType.Fall, MyMaterialType.CHARACTER, walkSurfaceMaterial);
						emitter.PlaySingleSound(cue);
					}
				}
			}
		}

		private MyStringHash RayCastGround()
		{
			MyStringHash walkSurfaceMaterial = new MyStringHash();

			var from = m_character.PositionComp.GetPosition() + m_character.PositionComp.WorldMatrix.Up * 0.1; //(needs some small distance from the bottom or the following call to HavokWorld.CastRay will find no hits)
			var to = from + m_character.PositionComp.WorldMatrix.Down * MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE;

			MyPhysics.CastRay(from, to, m_hits);

			// Skips invalid hits (null body, self character)
			int index = 0;
			while ((index < m_hits.Count) && ((m_hits[index].HkHitInfo.Body == null) || (m_hits[index].HkHitInfo.GetHitEntity() == Entity.Components)))
			{
				index++;
			}

			//m_walkingSurfaceType = MyWalkingSurfaceType.None;
			if (index < m_hits.Count)
			{
				// We must take only closest hit (others are hidden behind)
				var h = m_hits[index];
				var entity = h.HkHitInfo.GetHitEntity();

				var sqDist = Vector3D.DistanceSquared((Vector3D)h.Position, from);
				if (sqDist < MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE * MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE)
				{
					var cubeGrid = entity as MyCubeGrid;
					var voxelBase = entity as MyVoxelBase;
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
					m_soundEmitters[(int)MySoundEmitterEnum.SecondaryState].PlaySound(CharacterSounds[(int)CharacterSoundsEnum.PAIN_SOUND]);
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
