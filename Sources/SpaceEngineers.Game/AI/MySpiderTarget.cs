using Sandbox;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.AI
{
    [TargetType("Spider")]
    public class MySpiderTarget : MyAiTargetBase
    {
        public bool IsAttacking { get; private set; }

        private int m_attackStart;
        private int m_attackCtr;
        private bool m_attackPerformed;

        private BoundingSphereD m_attackBoundingSphere;

        private static readonly int ATTACK_LENGTH = 1000;
        private static readonly int ATTACK_ACTIVATION = 700;
        private static readonly int ATTACK_DAMAGE_TO_CHARACTER = 35; // spider attack (spider damage) to character
        private static readonly int ATTACK_DAMAGE_TO_GRID = 50; // spider attack (spider damage) to grid

        private static HashSet<MySlimBlock> m_tmpBlocks = new HashSet<MySlimBlock>();

        public MySpiderTarget(IMyEntityBot bot) : base(bot) { }

        public void Attack()
        {
            MyCharacter botEntity = m_bot.AgentEntity;
            if (botEntity == null) return;
            
            IsAttacking = true;
            m_attackPerformed = false;
            m_attackStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            string attackAnimation, attackSound;
            ChooseAttackAnimationAndSound(out attackAnimation, out attackSound);
            botEntity.PlayCharacterAnimation(attackAnimation, Sandbox.Game.Entities.MyBlendOption.Immediate, Sandbox.Game.Entities.MyFrameOption.PlayOnce, 0.0f, 1, sync: true);
            botEntity.DisableAnimationCommands();
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
                else if (attackTime > 500 && m_bot.AgentEntity.UseNewAnimationSystem && !m_attackPerformed)
                {
                    MySpiderLogic.TriggerAnimationEvent(m_bot.AgentEntity.EntityId, "attack");
                    if (Sync.IsServer)
                        MyMultiplayer.RaiseStaticEvent(x => MySpiderLogic.TriggerAnimationEvent, m_bot.AgentEntity.EntityId, "attack");
                }
                if (attackTime > 750 && !m_attackPerformed)
                {
                    MyCharacter botEntity = m_bot.AgentEntity;
                    if (botEntity != null)
                    {
                        Vector3D attackPosition = botEntity.WorldMatrix.Translation
                            + botEntity.PositionComp.WorldMatrix.Forward * 2.5
                            + botEntity.PositionComp.WorldMatrix.Up * 1.0;

                        m_attackBoundingSphere = new BoundingSphereD(attackPosition, 0.9);
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
                                touchDistSq = touchDistSq * touchDistSq;
                                if (Vector3D.DistanceSquared(m_attackBoundingSphere.Center, characterVolume.Center) > touchDistSq) continue;
                                
                                //Removed stealing inventory
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

        private void ChooseAttackAnimationAndSound(out string animation, out string sound)
        {
            m_attackCtr++;

            switch (TargetType)
            {
                case MyAiTargetEnum.CHARACTER:
                {
                    var character = TargetEntity as MyCharacter;
                    if (character != null && character.IsDead)
                    {
                        if (m_attackCtr % 3 == 0)
                        {
                            animation = "AttackFrontLegs";
                            sound = "ArcBotSpiderAttackClaw";
                            return;
                        }
                        else
                        {
                            animation = "AttackBite";
                            sound = "ArcBotSpiderAttackBite";
                            return;
                        }
                    }
                    else
                    {
                        if (m_attackCtr % 2 == 0)
                        {
                            animation = "AttackStinger";
                            sound = "ArcBotSpiderAttackSting";
                            return;
                        }
                        else
                        {
                            animation = "AttackBite";
                            sound = "ArcBotSpiderAttackBite";
                            return;
                        }
                    }
                }
                case MyAiTargetEnum.COMPOUND_BLOCK:
                case MyAiTargetEnum.CUBE:
                default:
                {
                    animation = "AttackFrontLegs";
                    sound = "ArcBotSpiderAttackClaw";
                    return;
                }
            }
        }
    }
}
