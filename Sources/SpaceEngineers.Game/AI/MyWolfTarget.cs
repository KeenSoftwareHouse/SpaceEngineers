using Sandbox;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    [TargetType("Wolf")]
    [StaticEventOwner]
    public class MyWolfTarget : MyAiTargetBase
    {
        public bool IsAttacking { get; private set; }

        private int m_attackStart;
        private bool m_attackPerformed;

        private BoundingSphereD m_attackBoundingSphere;

        private static readonly int ATTACK_LENGTH = 1000;

        private static readonly int ATTACK_DAMAGE_TO_CHARACTER = 12;
            // Wolf attack (Wolf damage) to character

        private static readonly int ATTACK_DAMAGE_TO_GRID = 8; // Wolf attack (Wolf damage) to grid

        private static HashSet<MySlimBlock> m_tmpBlocks = new HashSet<MySlimBlock>();

        private static MyStringId m_stringIdAttackAction = MyStringId.GetOrCompute("attack");

        public MyWolfTarget(IMyEntityBot bot) : base(bot)
        {
        }

        public void Attack(bool playSound)
        {
            // SUCH DOGE, MUCH HEAD, VERY EYES // anton 2016
            MyCharacter botEntity = m_bot.AgentEntity;
            if (botEntity == null) return;

            IsAttacking = true;
            m_attackPerformed = false;
            m_attackStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            string attackAnimation = "WolfAttack";
            string attackSound = "ArcBotWolfAttack";
            if (botEntity.UseNewAnimationSystem)
            {
                // ---
            }
            else
            {
                botEntity.PlayCharacterAnimation(attackAnimation, MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.0f,
                    1, sync: true);
                botEntity.DisableAnimationCommands();
            }
            botEntity.SoundComp.StartSecondarySound(attackSound, true);
        }

        public override void Update()
        {
            base.Update();

            if (IsAttacking)
            {
                int attackTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_attackStart;
                if (attackTime > ATTACK_LENGTH)
                {
                    IsAttacking = false;
                    MyCharacter botEntity = m_bot.AgentEntity;
                    if (botEntity != null) botEntity.EnableAnimationCommands();
                }
                else if (attackTime > 500 && m_bot.AgentEntity.UseNewAnimationSystem)
                {
                    m_bot.AgentEntity.AnimationController.TriggerAction(m_stringIdAttackAction);
                    if (Sync.IsServer)
                        MyMultiplayer.RaiseStaticEvent(x => PlayAttackAnimation, m_bot.AgentEntity.EntityId);
                }
                if (attackTime > 500 && !m_attackPerformed)
                {
                    MyCharacter botEntity = m_bot.AgentEntity;
                    if (botEntity != null)
                    {
                        Vector3D attackPosition = botEntity.WorldMatrix.Translation
                                                  + botEntity.PositionComp.WorldMatrix.Forward*2.5
                                                  + botEntity.PositionComp.WorldMatrix.Up*1.0;

                        m_attackBoundingSphere = new BoundingSphereD(attackPosition, 1.1);
                        m_attackPerformed = true;
                        List<MyEntity> hitEntities = MyEntities.GetTopMostEntitiesInSphere(ref m_attackBoundingSphere);
                        foreach (var hitEntity in hitEntities)
                        {
                            //AttackEntity(hitEntity);
                            if (hitEntity is MyCharacter && hitEntity != botEntity)
                            {
                                var character = hitEntity as MyCharacter;
                                if (character.IsSitting) continue;

                                var characterVolume = character.PositionComp.WorldVolume;
                                double touchDistSq = m_attackBoundingSphere.Radius + characterVolume.Radius;
                                touchDistSq = touchDistSq*touchDistSq;
                                if (Vector3D.DistanceSquared(m_attackBoundingSphere.Center, characterVolume.Center) >
                                    touchDistSq) continue;

                                //Designers do not want Wolfs stealing inventory...
                                /*
                                if (character.IsDead)
                                {
                                    var inventory = character.GetInventory();
                                    if (inventory == null) continue;
                                    if (m_bot.AgentEntity == null) continue;
                                    var WolfInventory = m_bot.AgentEntity.GetInventory();
                                    if (WolfInventory == null) continue;

                                    MyInventory.TransferAll(inventory, WolfInventory);
                                }
                                else
                                {
                                    character.DoDamage(ATTACK_DAMAGE_TO_CHARACTER, MyDamageType.Bolt, updateSync: true, attackerId: botEntity.EntityId);
                                }
                                */

                                character.DoDamage(ATTACK_DAMAGE_TO_CHARACTER, MyDamageType.Bolt, updateSync: true, attackerId: botEntity.EntityId);
                            }
                            else if (hitEntity is MyCubeGrid && hitEntity.Physics != null)
                            {
                                var grid = hitEntity as MyCubeGrid;
                                m_tmpBlocks.Clear();
                                grid.GetBlocksInsideSphere(ref m_attackBoundingSphere, m_tmpBlocks);
                                foreach (var block in m_tmpBlocks)
                                {
                                    block.DoDamage(ATTACK_DAMAGE_TO_GRID, MyDamageType.Bolt);
                                }
                                m_tmpBlocks.Clear();
                            }
                        }
                        hitEntities.Clear();
                    }
                }
                if (attackTime > 500)
                {
                    //MyRenderProxy.DebugDrawSphere(m_attackBoundingSphere.Center, (float)m_attackBoundingSphere.Radius, Color.Red, 1.0f, false);   
                }
            }
        }

        [Event, Broadcast, Reliable]
        private static void PlayAttackAnimation(long entityId)
        {
            if (MyEntities.EntityExists(entityId))
            {
                MyCharacter character = MyEntities.GetEntityById(entityId) as MyCharacter;
                if (character != null)
                    character.AnimationController.TriggerAction(m_stringIdAttackAction);
            }
        }
    }
}
