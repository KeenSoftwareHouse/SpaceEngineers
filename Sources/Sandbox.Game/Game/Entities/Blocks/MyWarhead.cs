#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Debugging;
using Sandbox.Game.GameSystems.Electricity;

using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Weapons;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Engine.Multiplayer;
using Sandbox.Common.ObjectBuilders.Definitions;
using SteamSDK;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Localization;
using Sandbox.Common.ModAPI;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Warhead))]
    class MyWarhead : MyTerminalBlock, IMyDestroyableObject, IMyWarhead
    {
        const float m_maxExplosionRadius = 30.0f;
        public static float ExplosionImpulse = 30000;
        bool m_isExploded = false;
        MyDamageType m_damageType = MyDamageType.Deformation;
        public int RemainingMS = 0;
        BoundingSphereD m_explosionShrinkenSphere;
        BoundingSphereD m_explosionFullSphere;
        BoundingSphereD m_explosionParticleSphere;

        bool m_marked = false;
        int m_warheadsInsideCount = 0;
        List<MyEntity> m_entitiesInShrinkenSphere = new List<MyEntity>();

        private bool m_countdownEmissivityColor;

        private int m_countdownMs;
        public bool IsCountingDown { get; private set; }

        private int BlinkDelay
        {
            get
            {
                if (m_countdownMs < 10000) return 100;
                if (m_countdownMs < 30000) return 250;
                if (m_countdownMs < 60000) return 500;
                return 1000;
            }
        }

        private bool m_isArmed;
        public bool IsArmed
        {
            get
            {
                return m_isArmed;
            }
            set
            {
                m_isArmed = value;
                RaisePropertiesChanged();
                UpdateEmissivity();
            }
        }

        private MyWarheadDefinition m_warheadDefinition;

        static MyWarhead()
        {
            var slider = new MyTerminalControlSlider<MyWarhead>("DetonationTime", MySpaceTexts.TerminalControlPanel_Warhead_DetonationTime, MySpaceTexts.TerminalControlPanel_Warhead_DetonationTime);
            slider.SetLogLimits(1, 60 * 60);
            slider.DefaultValue = 10;
            slider.Enabled = (x) => !x.IsCountingDown;
            slider.Getter = (x) => x.DetonationTime;
            slider.Setter = (x, v) => MySyncWarhead.SetTimer(x, v * 1000);
            slider.Writer = (x, sb) => MyValueFormatter.AppendTimeExact(Math.Max(x.m_countdownMs, 1000) / 1000, sb);
            slider.EnableActions();
            MyTerminalControlFactory.AddControl(slider);

            var startButton = new MyTerminalControlButton<MyWarhead>(
                "StartCountdown",
                MySpaceTexts.TerminalControlPanel_Warhead_StartCountdown,
                MySpaceTexts.TerminalControlPanel_Warhead_StartCountdown,
                (b) => MySyncWarhead.StartCountdown(b));
            startButton.EnableAction();
            MyTerminalControlFactory.AddControl(startButton);

            var stopButton = new MyTerminalControlButton<MyWarhead>(
                "StopCountdown",
                MySpaceTexts.TerminalControlPanel_Warhead_StopCountdown,
                MySpaceTexts.TerminalControlPanel_Warhead_StopCountdown,
                (b) => MySyncWarhead.StopCountdown(b));
            stopButton.EnableAction();
            MyTerminalControlFactory.AddControl(stopButton);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyWarhead>());

            var safetyCheckbox = new MyTerminalControlCheckbox<MyWarhead>(
                "Safety",
                MySpaceTexts.TerminalControlPanel_Warhead_Safety,
                MySpaceTexts.TerminalControlPanel_Warhead_SafetyTooltip,
                MySpaceTexts.TerminalControlPanel_Warhead_SwitchTextDisarmed,
                MySpaceTexts.TerminalControlPanel_Warhead_SwitchTextArmed);
            safetyCheckbox.Getter = (x) => !x.IsArmed;
            safetyCheckbox.Setter = (x, v) => MySyncWarhead.SetArm(x, !v);
            safetyCheckbox.EnableAction();
            MyTerminalControlFactory.AddControl(safetyCheckbox);

            var detonateButton = new MyTerminalControlButton<MyWarhead>(
                "Detonate",
                MySpaceTexts.TerminalControlPanel_Warhead_Detonate,
                MySpaceTexts.TerminalControlPanel_Warhead_Detonate,
                (b) => MySyncWarhead.Detonate(b));
            detonateButton.Enabled = (x) => x.IsArmed;
            detonateButton.EnableAction();
            MyTerminalControlFactory.AddControl(detonateButton);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            m_warheadDefinition = (MyWarheadDefinition)BlockDefinition;
            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_Warhead)objectBuilder;

            m_countdownMs = ob.CountdownMs;
            m_isArmed = ob.IsArmed;
            if (ob.IsCountingDown)
                StartCountdown();

            this.IsWorkingChanged += MyWarhead_IsWorkingChanged;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = base.GetObjectBuilderCubeBlock(copy);
            var warheadBuilder = builder as MyObjectBuilder_Warhead;

            warheadBuilder.CountdownMs = m_countdownMs;
            warheadBuilder.IsCountingDown = IsCountingDown;
            warheadBuilder.IsArmed = IsArmed;

            return warheadBuilder;
        }

        void MyWarhead_IsWorkingChanged(MyCubeBlock obj)
        {
            if (IsCountingDown && !IsWorking)
            {
                StopCountdown();
            }
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsWorking)
            {
                if (IsCountingDown)
                {
                    if (m_countdownEmissivityColor)
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Red, Color.White);
                    else
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Black, Color.White);
                }
                else if (IsArmed)
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Goldenrod, Color.White);
                else
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            }
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Gray, Color.White);
        }

        public override void ContactPointCallback(ref MyGridContactInfo value)
        {
            base.ContactPointCallback(ref value);

            if (value.CollidingEntity is MyDebrisBase)
                return;

            if (System.Math.Abs(value.Event.SeparatingVelocity) > 5 && IsFunctional)
            {
                if (CubeGrid.BlocksDestructionEnabled)
                    Explode();
            }
        }

        //public void DoDamage(float damage, MyDamageType damageType, bool sync)
        //{
        //    if (MarkedToExplode)
        //        return;
        //    //if (!IsFunctional)
        //    //    return false;

        //    if (sync)
        //    {
        //        if (Sync.IsServer)
        //            MySyncHelper.DoDamageSynced(this, damage, damageType);
        //    }
        //    else
        //    {
        //        m_damageType = damageType;
        //        if (damage > 0)
        //            OnDestroy();
        //    }
        //    return;
        //}

        public bool StartCountdown()
        {
            if (!IsFunctional || IsCountingDown) return false;

            IsCountingDown = true;
            MyWarheads.AddWarhead(this);
            RaisePropertiesChanged();
            UpdateEmissivity();
            return true;
        }

        public bool StopCountdown()
        {
            if (!IsFunctional || !IsCountingDown) return false;

            IsCountingDown = false;
            MyWarheads.RemoveWarhead(this);
            RaisePropertiesChanged();
            UpdateEmissivity();
            return true;
        }

        /// <summary>
        /// Returns true if the warhead should explode
        /// </summary>
        public bool Countdown(int frameMs)
        {
            if (!IsFunctional) return false;

            m_countdownMs -= frameMs;

            // Update emissivity
            if ((m_countdownMs % BlinkDelay) < frameMs)
            {
                m_countdownEmissivityColor = !m_countdownEmissivityColor;
                UpdateEmissivity();
            }

            // Update clients' countdown every five seconds (but only if there's no interpolation, which should take care of the timer sync)
            if (true && Sync.IsServer && (m_countdownMs % 5000) < frameMs)
                MySyncWarhead.SyncClientTimers(this);
            RaisePropertiesChanged();
            return m_countdownMs <= 0;
        }

        public void Detonate()
        {
            if (!IsFunctional) return;

            Explode();
        }

        public void Explode()
        {
            if (m_isExploded || !MySession.Static.WeaponsEnabled)
                return;

            m_isExploded = true;

            if (!m_marked)
                MarkForExplosion();

            MyExplosionTypeEnum particleID = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            if (m_explosionFullSphere.Radius <= 6)
            {
                particleID = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            }
            else
                if (m_explosionFullSphere.Radius <= 20)
                {
                    particleID = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15;
                }
                else
                    if (m_explosionFullSphere.Radius <= 40)
                    {
                        particleID = MyExplosionTypeEnum.WARHEAD_EXPLOSION_30;
                    }
                    else
                    {
                        particleID = MyExplosionTypeEnum.WARHEAD_EXPLOSION_50;
                    }


            //  Create explosion
            MyExplosionInfo info = new MyExplosionInfo()
            {
                PlayerDamage = 0,
                //Damage = m_ammoProperties.Damage,
                Damage = MyFakes.ENABLE_VOLUMETRIC_EXPLOSION ? m_warheadDefinition.WarheadExplosionDamage : 5000,
                ExplosionType = particleID,
                ExplosionSphere = m_explosionFullSphere,
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                CascadeLevel = 0,
                HitEntity = this,
                ParticleScale = 1,
                OwnerEntity = CubeGrid,
                Direction = (Vector3)WorldMatrix.Forward,
                VoxelExplosionCenter = m_explosionFullSphere.Center,// + 2 * WorldMatrix.Forward * 0.5f,
                ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 1.0f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                ObjectsRemoveDelayInMiliseconds = 40
            };
            MyExplosions.AddExplosion(ref info);
        }

        void MarkForExplosion()
        {
            m_marked = true;
            //Large grid = 12.5m radius of block
            //Small grid = 2.5m radius
            float radiusMultiplier = 4; //reduced by 20%
            float warheadBlockRadius = CubeGrid.GridSize * radiusMultiplier;
           
            float shrink = 0.85f;
            m_explosionShrinkenSphere = new BoundingSphereD(PositionComp.GetPosition(), (double)warheadBlockRadius * shrink);

            m_explosionParticleSphere = new BoundingSphereD(PositionComp.GetPosition(), double.MinValue);

            MyGamePruningStructure.GetAllEntitiesInSphere(ref m_explosionShrinkenSphere, m_entitiesInShrinkenSphere);
            m_warheadsInsideCount = 0;
            foreach (var entity in m_entitiesInShrinkenSphere)
            {
                if (Vector3D.DistanceSquared(PositionComp.GetPosition(), entity.PositionComp.GetPosition()) < warheadBlockRadius * shrink * warheadBlockRadius * shrink)
                {
                    MyWarhead warhead = entity as MyWarhead;
                    if (warhead != null)
                    {
                        m_warheadsInsideCount++;

                        if (!warhead.MarkedToExplode)
                            m_explosionParticleSphere = m_explosionParticleSphere.Include(new BoundingSphereD(warhead.PositionComp.GetPosition(), CubeGrid.GridSize * radiusMultiplier + warhead.CubeGrid.GridSize));
                    }
                    var block = entity as MyCubeBlock;
                    if (block != null)
                    {
                        block.MarkedToExplode = true;
                    }
                }
            }
            m_entitiesInShrinkenSphere.Clear();

            //m_radius += m_warheadsInsideCount * 0.1f;
            //Explosion radius is based on linear function where 1 warhead has explosion radius :
            //Large: 22.4415f
            // Small: 4.4883f
            //each warhead contribute 0.26 % of radius
            //explosion is clamped to maxExplosionRadius
            float fullExplosionRadius = Math.Min(m_maxExplosionRadius,(1 + 0.024f * m_warheadsInsideCount) * m_warheadDefinition.ExplosionRadius);
            //fullExplosionRadius = fullExplosionRadius;
            m_explosionFullSphere = new BoundingSphere(m_explosionParticleSphere.Center, (float)Math.Max(fullExplosionRadius, m_explosionParticleSphere.Radius));

            if (MyExplosion.DEBUG_EXPLOSIONS)
            {
                MyWarheads.DebugWarheadShrinks.Add(m_explosionShrinkenSphere);
                MyWarheads.DebugWarheadGroupSpheres.Add(m_explosionFullSphere);

                float particleRadius = (float)m_explosionParticleSphere.Radius;
            }
        }

        public override void OnDestroy()
        {
            if (Sandbox.Game.Multiplayer.Sync.IsServer)
            {
                if (!IsFunctional) return;
                if (m_damageType == MyDamageType.Bullet)
                {
                    Explode();
                }
                else
                {
                    MarkForExplosion();
                    ExplodeDelayed(500);
                }
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            if (IsCountingDown)
            {
                IsCountingDown = false;
                StartCountdown();
            }
            else
                UpdateEmissivity();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            if (IsCountingDown)
            {
                StopCountdown();
                IsCountingDown = true; // We need to remember the counting state for the case when we are moving the block to another grid
            }
        }

        void ExplodeDelayed(int maxMiliseconds)
        {
            RemainingMS = MyUtils.GetRandomInt(maxMiliseconds);
            m_countdownMs = 0;
            MyWarheads.AddWarhead(this);
        }

        //public float Integrity
        //{
        //    get { return 1; }
        //}

        [PreloadRequired]
        class MySyncWarhead
        {
            [MessageIdAttribute(7511, P2PMessageEnum.Reliable)]
            protected struct SetTimerMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int TimerMs;
            }

            [MessageIdAttribute(7512, P2PMessageEnum.Reliable)]
            protected struct CountdownMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit CountdownState;
            }

            [MessageIdAttribute(7513, P2PMessageEnum.Reliable)]
            protected struct ArmMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit IsArmed;
            }

            [MessageIdAttribute(7514, P2PMessageEnum.Reliable)]
            protected struct DetonateMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            static MySyncWarhead()
            {
                MySyncLayer.RegisterMessage<SetTimerMsg>(SetTimerRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<SetTimerMsg>(SetTimerSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<CountdownMsg>(CountdownRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<CountdownMsg>(CountdownSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<ArmMsg>(ArmSuccess, MyMessagePermissions.Any);
                MySyncLayer.RegisterMessage<DetonateMsg>(DetonateRequest, MyMessagePermissions.Any);
            }

            public static void SetTimer(MyWarhead warhead, float newTimerValue)
            {
                SetTimerMsg msg = new SetTimerMsg();
                msg.EntityId = warhead.EntityId;
                msg.TimerMs = (int)newTimerValue;
    
                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                else
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            public static void SyncClientTimers(MyWarhead warhead)
            {
                Debug.Assert(Sync.IsServer);
                if (!Sync.IsServer) return;

                SetTimerMsg msg = new SetTimerMsg();
                msg.EntityId = warhead.EntityId;
                msg.TimerMs = warhead.m_countdownMs;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }

            static void SetTimerRequest(ref SetTimerMsg msg, MyNetworkClient sender)
            {
                Debug.Assert(Sync.IsServer);
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

            static void SetTimerSuccess(ref SetTimerMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                {
                    warhead.m_countdownMs = msg.TimerMs;
                    warhead.RaisePropertiesChanged();
                }
            }

            public static void StartCountdown(MyWarhead warhead)
            {
                SetCountdown(warhead, true);
            }

            public static void StopCountdown(MyWarhead warhead)
            {
                SetCountdown(warhead, false);
            }

            private static void SetCountdown(MyWarhead warhead, bool countdownState)
            {
                CountdownMsg msg = new CountdownMsg();
                msg.EntityId = warhead.EntityId;
                msg.CountdownState = countdownState;

                if (Sync.IsServer)
                {
                    SetCountdownServer(warhead, ref msg);
                }
                else
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            private static void SetCountdownServer(MyWarhead warhead, ref CountdownMsg msg)
            {
                bool success = false;
                if (msg.CountdownState)
                    success = warhead.StartCountdown();
                else
                    success = warhead.StopCountdown();
                if (success)
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }

            static void CountdownRequest(ref CountdownMsg msg, MyNetworkClient sender)
            {
                Debug.Assert(Sync.IsServer);
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                    SetCountdownServer(warhead, ref msg);
            }

            static void CountdownSuccess(ref CountdownMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                {
                    if (msg.CountdownState)
                        warhead.StartCountdown();
                    else
                        warhead.StopCountdown();
                }
            }

            public static void SetArm(MyWarhead warhead, bool armed)
            {
                warhead.IsArmed = armed;

                ArmMsg msg = new ArmMsg();
                msg.EntityId = warhead.EntityId;
                msg.IsArmed = armed;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            static void ArmSuccess(ref ArmMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                    warhead.IsArmed = msg.IsArmed;
            }

            public static void Detonate(MyWarhead warhead)
            {
                // Armed state is checked only locally. Otherwise, it could happen that server does not detonate the warhead even though it was armed on the client
                if (!warhead.IsArmed) return;

                DetonateMsg msg = new DetonateMsg();
                msg.EntityId = warhead.EntityId;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            static void DetonateRequest(ref DetonateMsg msg, MyNetworkClient sender)
            {
                MyEntity entity;
                MyEntities.TryGetEntityById(msg.EntityId, out entity);
                var warhead = entity as MyWarhead;
                if (warhead != null)
                {
                    warhead.Detonate();
                }
            }
        }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        void IMyDestroyableObject.DoDamage(float damage, MyDamageType damageType, bool sync, MyHitInfo? hitInfo)
        {
            if (MarkedToExplode || (!MySession.Static.DestructibleBlocks))
                return;
            //if (!IsFunctional)
            //    return false;

            if (sync)
            {
                if (Sync.IsServer)
                    MySyncHelper.DoDamageSynced(this, damage, damageType);
            }
            else
            {
                m_damageType = damageType;
                if (damage > 0)
                    OnDestroy();
            }
            return;
        }

        float IMyDestroyableObject.Integrity
        {
            get { return 1; }
        }
      
        public float DetonationTime { get { return Math.Max(m_countdownMs, 1000) / 1000; } }
        bool IMyWarhead.IsCountingDown { get { return IsCountingDown; } }
        float IMyWarhead.DetonationTime { get { return DetonationTime; } }
    }
}
