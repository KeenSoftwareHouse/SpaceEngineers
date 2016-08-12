using Sandbox;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.AI;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    [StaticEventOwner]
    public class MySpiderLogic : MyAgentLogic
    {
        private bool m_burrowing;
        private bool m_deburrowing;
        private bool m_deburrowAnimationStarted;
        private bool m_deburrowSoundStarted;
        private int m_burrowStart;
        private int m_deburrowStart;
        private Vector3D? m_effectOnPosition;

        private static Dictionary<Vector3D, MyParticleEffect> m_burrowEffectTable;

        private static readonly int BURROWING_TIME = 750;
        private static readonly int BURROWING_FX_START = 300;
        private static readonly int DEBURROWING_TIME = 4800;
        private static readonly int DEBURROWING_ANIMATION_START = 2500;
        private static readonly int DEBURROWING_SOUND_START = 1500;

        public bool IsBurrowing { get { return m_burrowing; } }
        public bool IsDeburrowing { get { return m_deburrowing; } }

        static MySpiderLogic()
        {
            m_burrowEffectTable = new Dictionary<Vector3D, MyParticleEffect>();
        }

        public MySpiderLogic(MyAnimalBot bot)
            : base(bot)
        { 
        }

        public override void Update()
        {
            base.Update();

            if (m_burrowing || m_deburrowing)
            {
                UpdateBurrowing();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            DeleteBurrowingParticleFX();
        }

        public void StartBurrowing()
        {
            
            //GR: Added simple check so burrowing functions should happen only when on Voxel physics.
            //Not sure though if this check should be somewhere else (e.g. behaviour Tree)
            var result = Sandbox.Engine.Physics.MyPhysics.CastRay(AgentBot.BotEntity.WorldMatrix.Translation - 3 * AgentBot.BotEntity.WorldMatrix.Up, AgentBot.BotEntity.WorldMatrix.Translation + 3 * AgentBot.BotEntity.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoVoxelCollisionLayer);
            if (result != null && (result as VRage.Game.ModAPI.IHitInfo).HitEntity != AgentBot.BotEntity)
            {
                return;
            }

            if (AgentBot.AgentEntity.UseNewAnimationSystem)
            {
                MySpiderLogic.TriggerAnimationEvent(AgentBot.AgentEntity.EntityId, "burrow");
                if (Sync.IsServer)
                    MyMultiplayer.RaiseStaticEvent(x => MySpiderLogic.TriggerAnimationEvent, AgentBot.AgentEntity.EntityId, "burrow");
            }
            else
            {
                if (AgentBot.AgentEntity.HasAnimation("Burrow"))
                {
                    AgentBot.AgentEntity.PlayCharacterAnimation("Burrow", MyBlendOption.Immediate, MyFrameOption.Default,
                        0.0f, 1, sync: true);
                    AgentBot.AgentEntity.DisableAnimationCommands();
                }
            }
            AgentBot.AgentEntity.SoundComp.StartSecondarySound("ArcBotSpiderBurrowIn", true);

            m_burrowing = true;
            m_burrowStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void StartDeburrowing()
        {

            //GR: Added simple check so burrowing functions should happen only when on Voxel physics.
            //Not sure though if this check should be somewhere else (e.g. behaviour Tree)
            var result = Sandbox.Engine.Physics.MyPhysics.CastRay(AgentBot.BotEntity.WorldMatrix.Translation - 3 * AgentBot.BotEntity.WorldMatrix.Up, AgentBot.BotEntity.WorldMatrix.Translation + 3 * AgentBot.BotEntity.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoVoxelCollisionLayer);
            if (result != null && (result as VRage.Game.ModAPI.IHitInfo).HitEntity != AgentBot.BotEntity)
            {
                return;
            }

            if (AgentBot.AgentEntity.UseNewAnimationSystem)
            {
                MySpiderLogic.TriggerAnimationEvent(AgentBot.AgentEntity.EntityId, "deburrow");
                if (Sync.IsServer)
                    MyMultiplayer.RaiseStaticEvent(x => MySpiderLogic.TriggerAnimationEvent, AgentBot.AgentEntity.EntityId, "deburrow");
            }

            m_deburrowing = true;
            m_deburrowStart = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            CreateBurrowingParticleFX();
            m_deburrowAnimationStarted = false;
            m_deburrowSoundStarted = false;
        }

        private void UpdateBurrowing()
        {
            if (m_burrowing)
            {
                int burrowDiff = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_burrowStart;
                if (burrowDiff > BURROWING_FX_START && !m_effectOnPosition.HasValue)
                {
                    CreateBurrowingParticleFX();
                }
                if (burrowDiff >= BURROWING_TIME)
                {
                    m_burrowing = false;
                    DeleteBurrowingParticleFX();
                    AgentBot.AgentEntity.EnableAnimationCommands();
                }
            }

            if (m_deburrowing)
            {
                int deburrowDiff = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_deburrowStart;
                if (!m_deburrowSoundStarted && deburrowDiff >= DEBURROWING_SOUND_START)
                {
                    AgentBot.AgentEntity.SoundComp.StartSecondarySound("ArcBotSpiderBurrowOut", true);
                    m_deburrowSoundStarted = true;
                }
                if (!m_deburrowAnimationStarted && deburrowDiff >= DEBURROWING_ANIMATION_START)
                {
                    if (AgentBot.AgentEntity.HasAnimation("Deburrow"))
                    {
                        AgentBot.AgentEntity.EnableAnimationCommands();
                        AgentBot.AgentEntity.PlayCharacterAnimation("Deburrow", MyBlendOption.Immediate, MyFrameOption.Default, 0.0f, 1, sync: true);
                        AgentBot.AgentEntity.DisableAnimationCommands();
                    }
                    m_deburrowAnimationStarted = true;
                }
                if (deburrowDiff >= DEBURROWING_TIME)
                {
                    m_deburrowing = false;
                    DeleteBurrowingParticleFX();
                    AgentBot.AgentEntity.EnableAnimationCommands();
                }
            }
        }

        private void CreateBurrowingParticleFX()
        {
            Debug.Assert(m_effectOnPosition.HasValue == false, "Burrowing particle effect was not disposed properly!");

            Vector3D pos = AgentBot.BotEntity.PositionComp.WorldMatrix.Translation;
            pos += AgentBot.BotEntity.PositionComp.WorldMatrix.Forward * 0.2;
            m_effectOnPosition = pos;
            if (!MySandboxGame.IsDedicated)
                CreateBurrowingParticleFX_Client(pos);
            MyMultiplayer.RaiseStaticEvent(x => CreateBurrowingParticleFX_Client, pos);
        }

        private void DeleteBurrowingParticleFX()
        {
            if (m_effectOnPosition.HasValue)
            {
                if (!MySandboxGame.IsDedicated)
                    DeleteBurrowingParticleFX_Client(m_effectOnPosition.Value);

                MyMultiplayer.RaiseStaticEvent(x => DeleteBurrowingParticleFX_Client, m_effectOnPosition.Value);
            }

            m_effectOnPosition = null;
        }

        [Event, Reliable, Broadcast]
        private static void CreateBurrowingParticleFX_Client(Vector3D position)
        {
            Debug.Assert(m_burrowEffectTable.ContainsKey(position) == false, "Burrowing particle effect was not disposed properly (client)!");
            MyParticleEffect burrowEffect;
            if (MyParticlesManager.TryCreateParticleEffect("Burrowing", out burrowEffect))
            {
                burrowEffect.WorldMatrix = MatrixD.CreateTranslation(position);
                burrowEffect.UserColorMultiplier = new Vector4(1, 0.8f, 0.5f, 0.2f);
                burrowEffect.UserAxisScale = new Vector3D(2.0, 2.0, 2.0);
                m_burrowEffectTable.Add(position, burrowEffect);
            }
        }

        [Event, Reliable, Broadcast]
        private static void DeleteBurrowingParticleFX_Client(Vector3D position)
        {
            MyParticleEffect burrowEffect;
            if (m_burrowEffectTable.TryGetValue(position, out burrowEffect))
            {
                burrowEffect.Stop();
                m_burrowEffectTable.Remove(position);
            }
        }

        [Event, Broadcast, Reliable]
        public static void TriggerAnimationEvent(long entityId, string eventName)
        {
            MyCharacter character = MyEntities.GetEntityByIdOrDefault(entityId, null) as MyCharacter;
            if (character != null)
                character.AnimationController.TriggerAction(MyStringId.GetOrCompute(eventName));
        }
    }
}