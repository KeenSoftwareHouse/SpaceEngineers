using Sandbox;
using Sandbox.Game;
using Sandbox.Game.AI;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System.Diagnostics;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    public class MyWolfLogic : MyAgentLogic
    {
        private readonly static int SELF_DESTRUCT_TIME_MS = 4000;
        private readonly static float EXPLOSION_RADIUS = 4.0f;
        private readonly static int EXPLOSION_DAMAGE = 7500;
        private readonly static int EXPLOSION_PLAYER_DAMAGE = 0;

        private bool m_selfDestruct = false;
        private int m_selfDestructStartedInTime;
        private bool m_lastWasAttacking = false;

        public bool SelfDestructionActivated { get { return m_selfDestruct; } }

        public MyWolfLogic(MyAnimalBot bot)
            : base(bot)
        { }

        public override void Update()
        {
            base.Update();

            if (m_selfDestruct && MySandboxGame.TotalGamePlayTimeInMilliseconds >= m_selfDestructStartedInTime + SELF_DESTRUCT_TIME_MS)
            {
                MyAIComponent.Static.RemoveBot(AgentBot.Player.Id.SerialId, removeCharacter: true);

                var explosionSphere = new BoundingSphere(AgentBot.Player.GetPosition(), EXPLOSION_RADIUS);
                MyExplosionInfo info = new MyExplosionInfo()
                {
                    PlayerDamage = EXPLOSION_PLAYER_DAMAGE,
                    Damage = EXPLOSION_DAMAGE,
                    ExplosionType = MyExplosionTypeEnum.BOMB_EXPLOSION,
                    ExplosionSphere = explosionSphere,
                    LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                    CascadeLevel = 0,
                    HitEntity = AgentBot.Player.Character,
                    ParticleScale = 0.5f,
                    OwnerEntity = AgentBot.Player.Character,
                    Direction = Vector3.Zero,
                    VoxelExplosionCenter = AgentBot.Player.Character.PositionComp.GetPosition(),// + 2 * WorldMatrix.Forward * 0.5f,
                    ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                    VoxelCutoutScale = 0.6f,
                    PlaySound = true,
                    ApplyForceAndDamage = true,
                    ObjectsRemoveDelayInMiliseconds = 40
                };
                MyExplosions.AddExplosion(ref info);
            }

            var target = (AiTarget as MyWolfTarget);
            Debug.Assert(target != null);
            if (AgentBot.Player.Character != null && AgentBot.Player.Character.UseNewAnimationSystem == false) // obsolete in new animation system
            {
                if ((!target.IsAttacking && !m_lastWasAttacking) && target.HasTarget()
                    && !target.PositionIsNearTarget(AgentBot.Player.Character.PositionComp.GetPosition(), 1.5f))
                {
                    if (AgentBot.Navigation.Stuck)
                    {
                        // correct aiming (aim in front of dog)
                        Vector3D houndPosition = AgentBot.Player.Character.PositionComp.GetPosition();
                        Vector3D gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(houndPosition);
                        Vector3D aimingDirection = AgentBot.Player.Character.AimedPoint - houndPosition;
                        Vector3D newAimVector = aimingDirection -
                                                gravity*Vector3D.Dot(aimingDirection, gravity)/gravity.LengthSquared();
                        newAimVector.Normalize();
                        AgentBot.Navigation.AimAt(null, houndPosition + 100.0f*newAimVector);
                        // play idle animation
                        AgentBot.Player.Character.PlayCharacterAnimation("WolfIdle1", MyBlendOption.Immediate,
                            MyFrameOption.Loop, 0);
                        AgentBot.Player.Character.DisableAnimationCommands();
                    }
                    else
                    {
                        AgentBot.Player.Character.EnableAnimationCommands();
                    }
                }
            }
            m_lastWasAttacking = target.IsAttacking;
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        public void ActivateSelfDestruct()
        {
            if (!m_selfDestruct)
            {
                m_selfDestructStartedInTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_selfDestruct = true;
                string attackSound = "ArcBotCyberSelfActDestr";
                AgentBot.AgentEntity.SoundComp.StartSecondarySound(attackSound, true);
            }
        }

        public void Remove()
        {
            MyAIComponent.Static.RemoveBot(AgentBot.Player.Id.SerialId, removeCharacter: true); // reached destination, delete
        }
    }
}