using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Network;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Entities.EnvironmentItems;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_Meteor))]
    public class MyMeteor : MyEntity, IMyDestroyableObject, IMyDecalProxy, IMyMeteor, IMyEventProxy
    {
        private static readonly int MAX_TRAJECTORY_LENGTH = 10000;
        private static readonly int SPEED = 90;

        MyMeteorGameLogic m_logic;
        public new MyMeteorGameLogic GameLogic { get { return m_logic; } set { base.GameLogic = value; } }

        public MyMeteor()
        {
            Components.ComponentAdded += Components_ComponentAdded;
            GameLogic = new MyMeteorGameLogic();
            Render = new MyRenderComponentDebrisVoxel();
        }

        void Components_ComponentAdded(Type arg1, MyComponentBase arg2)
        {
            if (arg1 == typeof(MyGameLogicComponent))
                m_logic = arg2 as MyMeteorGameLogic;
        }

        #region Spawn
        public static MyEntity SpawnRandom(Vector3 position, Vector3 direction)
        {
            string materialName = GetMaterialName();

            MyPhysicalInventoryItem i = new MyPhysicalInventoryItem(500 * (MyFixedPoint)MyUtils.GetRandomFloat(1f, 3f), MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(materialName));
            return Spawn(ref i, position, direction * SPEED);
        }

        private static string GetMaterialName()
        {
            string materialName = "Stone";
            bool materialFound = false;

            MyVoxelMaterialDefinition defintion = null;
            foreach (var mat in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (mat.MinedOre == materialName)
                {
                    materialFound = true;
                    break;
                }
                defintion = mat;
            }

            if (materialFound == false && defintion != null)
            {
                materialName = defintion.MinedOre;
            }
            return materialName;
        }

        public static MyEntity Spawn(ref MyPhysicalInventoryItem item, Vector3 position, Vector3 speed)
        {
            var builder = PrepareBuilder(ref item);
            var meteorEntity = MyEntities.CreateFromObjectBuilderNoinit(builder, false);
            MyEntities.CreateFromObjectBuilderParallel(builder, true, delegate() { SetSpawnSettings(meteorEntity, position, speed); }, meteorEntity);
            return meteorEntity;
        }

        private static void SetSpawnSettings(MyEntity meteorEntity, Vector3 position, Vector3 speed)
        {
            Vector3 forward = -MySector.DirectionToSunNormalized;
            Vector3 up = MyUtils.GetRandomVector3Normalized();
            while (forward == up)
                up = MyUtils.GetRandomVector3Normalized();

            Vector3 right = Vector3.Cross(forward, up);
            up = Vector3.Cross(right, forward);

            meteorEntity.WorldMatrix = Matrix.CreateWorld(position, forward, up);
            meteorEntity.Physics.RigidBody.MaxLinearVelocity = 500;
            meteorEntity.Physics.LinearVelocity = speed;
            meteorEntity.Physics.AngularVelocity = MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(1.5f, 3);
        }

        private static MyObjectBuilder_Meteor PrepareBuilder(ref MyPhysicalInventoryItem item)
        {
            var meteorBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Meteor>();
            meteorBuilder.Item = item.GetObjectBuilder();
            meteorBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            return meteorBuilder;
        }
        #endregion

        public override bool IsCCDForProjectiles
        {
            get { return true; }
        }

        public void OnDestroy()
        {
            GameLogic.OnDestroy();

        }

        public bool DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            GameLogic.DoDamage(damage, damageType, sync, hitInfo, attackerId);
            return true;
        }

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            // TODO
        }

        public float Integrity
        {
            get { return GameLogic.Integrity; }
        }

        private bool m_hasModifiableDamage;
        public bool UseDamageSystem
        {
            get { return m_hasModifiableDamage; }
        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return GameLogic.GetObjectBuilder(false);
        }

        // So much room for activities

        public class MyMeteorGameLogic : MyEntityGameLogic
        {
            internal MyMeteor Entity { get { return Container != null ? Container.Entity as MyMeteor : null; } }

            public MyPhysicalInventoryItem Item;
            public MyVoxelMaterialDefinition VoxelMaterial { get; set; }
            private bool InParticleVisibleRange { get { return Entity != null? (MySector.MainCamera.Position - Entity.WorldMatrix.Translation).LengthSquared() < (3000 * 3000) : false; } }
            private StringBuilder m_textCache;
            private float m_integrity = 100f;
            private string[] m_particleEffectNames = new string[2];
            private MyParticleEffect m_dustEffect;
            private int m_timeCreated;
            private Vector3 m_particleVectorForward = Vector3.Zero;
            private Vector3 m_particleVectorUp = Vector3.Zero;

            private enum MeteorStatus
            {
                InAtmosphere,
                InSpace
            }
            private MeteorStatus m_meteorStatus = MeteorStatus.InSpace;

            private MyEntity3DSoundEmitter m_soundEmitter;

            private bool m_closeAfterSimulation;
            private MySoundPair m_meteorFly = new MySoundPair("MeteorFly");
            private MySoundPair m_meteorExplosion = new MySoundPair("MeteorExplosion");

            public MyMeteorGameLogic()
            {
                m_soundEmitter = new MyEntity3DSoundEmitter(null);
            }

            public override void Init(MyObjectBuilder_EntityBase objectBuilder)
            {
                Entity.SyncFlag = true;
                base.Init(objectBuilder);
                var builder = (MyObjectBuilder_Meteor)objectBuilder;
                Item = new MyPhysicalInventoryItem(builder.Item);
                m_particleEffectNames[(int)MeteorStatus.InAtmosphere] = "Meteory_Fire_Atmosphere";
                m_particleEffectNames[(int)MeteorStatus.InSpace] = "Meteory_Fire_Space";
                InitInternal();

                Entity.Physics.LinearVelocity = builder.LinearVelocity;
                Entity.Physics.AngularVelocity = builder.AngularVelocity;

                m_integrity = builder.Integrity;
            }

            private void InitInternal()
            {
                // TODO: This will be fixed and made much more simple once ore models are done
                // https://app.asana.com/0/6594565324126/10473934569658
                var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(Item.Content);

                var ore = Item.Content as MyObjectBuilder_Ore;

                string model = physicalItem.Model;
                float scale = 1.0f;
                VoxelMaterial = null;
                if (ore != null)
                {
                    foreach (var mat in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
                    {
                        if (mat.MinedOre == ore.SubtypeName)
                        {
                            VoxelMaterial = mat;
                            model = MyDebris.GetRandomDebrisVoxel();
                            scale = (float)Math.Pow((float)Item.Amount * physicalItem.Volume  / MyDebris.VoxelDebrisModelVolume, 0.333f);
                            break;
                        }
                    }
                }

                if (scale < 0.15f)
                    scale = 0.15f;

                var voxelRender = (Entity.Render as MyRenderComponentDebrisVoxel);
                voxelRender.VoxelMaterialIndex = VoxelMaterial.Index;
                voxelRender.TexCoordOffset = 5;
                voxelRender.TexCoordScale = 8;
                Entity.Init(new StringBuilder("Meteor"), model, null, null, null);

                Entity.PositionComp.Scale = scale; // Must be set after init

                var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(Entity.PositionComp.LocalVolume.Radius, (float)(4 / 3f * Math.PI * Math.Pow(Entity.PositionComp.LocalVolume.Radius, 3)) * 3.7f);
                HkSphereShape transform = new HkSphereShape(Entity.PositionComp.LocalVolume.Radius);

                if (Entity.Physics != null)
                    Entity.Physics.Close();

                Entity.Physics = new MyPhysicsBody(Entity, RigidBodyFlag.RBF_BULLET);
                Entity.Physics.ReportAllContacts = true;
                Entity.GetPhysicsBody().CreateFromCollisionObject(transform, Vector3.Zero, MatrixD.Identity, massProperties, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                Entity.Physics.Enabled = true;
                Entity.Physics.RigidBody.ContactPointCallbackEnabled = true;
                Entity.GetPhysicsBody().ContactPointCallback += RigidBody_ContactPointCallback;
                transform.Base.RemoveReference();
                Entity.Physics.PlayCollisionCueEnabled = true;

                m_timeCreated = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

                StartLoopSound();
            }

            public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
            {
                var builder = (MyObjectBuilder_Meteor)base.GetObjectBuilder(copy);
                if (Entity == null || Entity.Physics == null)
                {
                    builder.LinearVelocity = Vector3.One * 10;
                    builder.AngularVelocity = Vector3.Zero;
                }
                else
                {
                    builder.LinearVelocity = Entity.Physics.LinearVelocity;
                    builder.AngularVelocity = Entity.Physics.AngularVelocity;
                }
                if (GameLogic != null)
                {
                    builder.Item = Item.GetObjectBuilder();
                    builder.Integrity = Integrity;
                }
                return builder;
            }

            public override void OnAddedToContainer()
            {
                base.OnAddedToContainer();
                m_soundEmitter.Entity = Container.Entity as MyEntity;
            }

            public override void MarkForClose()
            {
                DestroyMeteor();
                base.MarkForClose();
            }

            public override void UpdateBeforeSimulation()
            {
                base.UpdateBeforeSimulation();
                if (m_dustEffect != null)
                {
                    UpdateParticlePosition();
                }
            }

            public override void UpdateAfterSimulation()
            {
                if (m_closeAfterSimulation)
                {
                    CloseMeteorInternal();
                    m_closeAfterSimulation = false;
                }
                base.UpdateAfterSimulation();
            }

            private void UpdateParticlePosition()
            {
                if (m_particleVectorUp != Vector3.Zero)
                {
                    MatrixD m = MatrixD.CreateWorld(Entity.WorldMatrix.Translation, m_particleVectorForward, m_particleVectorUp);
                    m_dustEffect.Enabled = true;
                    m_dustEffect.WorldMatrix = m;
                }
                else
                {
                    m_dustEffect.Enabled = false;
                    m_dustEffect.WorldMatrix = MatrixD.CreateWorld(Vector3D.Zero, Vector3.Forward, Vector3.Up);
                }
            }

            public override void UpdateBeforeSimulation100()
            {
                base.UpdateBeforeSimulation100();
                if (m_particleVectorUp == Vector3.Zero)
                {
                    if (Entity.Physics.LinearVelocity != Vector3.Zero)
                        m_particleVectorUp = -Vector3.Normalize(Entity.Physics.LinearVelocity);
                    else
                        m_particleVectorUp = Vector3.Up;
                    m_particleVectorUp.CalculatePerpendicularVector(out m_particleVectorForward);
                }

                Vector3D pos = Entity.PositionComp.GetPosition();
                var planet = MyGamePruningStructure.GetClosestPlanet(pos);

                MeteorStatus orig = m_meteorStatus;
                if (planet != null && planet.HasAtmosphere && planet.GetAirDensity(pos) > 0.5f)
                    m_meteorStatus = MeteorStatus.InAtmosphere;
                else
                    m_meteorStatus = MeteorStatus.InSpace;

                if (orig != m_meteorStatus && m_dustEffect != null)
                {
                    m_dustEffect.Stop();
                    m_dustEffect = null;
                }

                if (m_dustEffect != null && !InParticleVisibleRange)
                {
                    m_dustEffect.Stop();
                    m_dustEffect = null;
                }

                if (m_dustEffect == null && InParticleVisibleRange)
                {
                    if (MyParticlesManager.TryCreateParticleEffect(m_particleEffectNames[(int)m_meteorStatus], out m_dustEffect))
                    {
                        UpdateParticlePosition();
                        m_dustEffect.UserScale = Entity.PositionComp.Scale.Value;
                    }
                }

                m_soundEmitter.Update();

                if (Sync.IsServer && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_timeCreated > Math.Min(MAX_TRAJECTORY_LENGTH / SPEED, MAX_TRAJECTORY_LENGTH / Entity.Physics.LinearVelocity.Length()) * 1000)
                {
                    CloseMeteorInternal();
                }
            }

            private void CloseMeteorInternal()
            {
                // If contact point callbacks destroy the meteor, we must not close it there, since there could be
                // multiple contact points per meteor. Instead, we wait until simulation is done and do it after.
                if (Entity.Physics != null)
                {
                    Entity.Physics.Enabled = false;
                    Entity.Physics.Deactivate();
                }
                MarkForClose();
            }


            public override void Close()
            {
                if (m_dustEffect != null)
                {
                    m_dustEffect.Stop();
                    m_dustEffect = null;
                }
                base.Close();
            }

            void RigidBody_ContactPointCallback(ref MyPhysics.MyContactPointEvent value)
            {
                if (this.MarkedForClose || !Entity.Physics.Enabled || m_closeAfterSimulation)
                    return;
                ProfilerShort.Begin("MyMeteor.CPCallback");
                var other = value.ContactPointEvent.GetOtherEntity(Entity);
                if (Sync.IsServer)
                {
                    if (other is MyCubeGrid)
                    {
                        var grid = other as MyCubeGrid;
                        if (grid.BlocksDestructionEnabled)
                        {
                            DestroyGrid(ref value, grid);
                        }
                    }
                    else if (other is MyCharacter)
                    {
                        (other as MyCharacter).DoDamage(50 * Entity.PositionComp.Scale.Value, MyDamageType.Environment, true, Entity.EntityId);
                    }
                    else if (other is MyFloatingObject)
                    {
                        (other as MyFloatingObject).DoDamage(100 * Entity.PositionComp.Scale.Value, MyDamageType.Deformation, true, Entity.EntityId);
                    }
                    else if (other is MyMeteor)
                    {
                        m_closeAfterSimulation = true;
                        (other.GameLogic as MyMeteorGameLogic).m_closeAfterSimulation = true;
                    }
                    m_closeAfterSimulation = true;
                }

                if (other is MyVoxelBase)
                {
                    CreateCrater(value, other as MyVoxelBase);
                }
                ProfilerShort.End();
            }

            private void DestroyMeteor()
            {
                MyParticleEffect impactParticle;
                if (InParticleVisibleRange && MyParticlesManager.TryCreateParticleEffect("Meteorit_Smoke1AfterHit", out impactParticle))
                {
                    impactParticle.WorldMatrix = Entity.WorldMatrix;
                    impactParticle.UserScale = 5 * MyUtils.GetRandomFloat(0.8f, 1.2f);
                }
                if (m_dustEffect != null)
                {
                    m_dustEffect.Stop();
                    if (MySession.Static.EnvironmentHostility != MyEnvironmentHostilityEnum.CATACLYSM_UNREAL)
                    {
                        m_dustEffect.Close(false);
                        if (InParticleVisibleRange && m_particleVectorUp != Vector3.Zero && MyParticlesManager.TryCreateParticleEffect("Meteorit_Smoke1AfterHit", out m_dustEffect))
                        {
                            MatrixD m = MatrixD.CreateWorld(Entity.WorldMatrix.Translation, m_particleVectorForward, m_particleVectorUp);
                            m_dustEffect.WorldMatrix = m;
                        }
                    }
                    m_dustEffect = null;
                }
                if (m_dustEffect != null)
                {
                    m_dustEffect.Stop();
                    m_dustEffect = null;
                }
                PlayExplosionSound();
            }

            private void CreateCrater(MyPhysics.MyContactPointEvent value, MyVoxelBase voxel)
            {
                if (Math.Abs(Vector3.Normalize(-Entity.WorldMatrix.Forward).Dot(value.ContactPointEvent.ContactPoint.Normal)) < 0.1)
                {
                    MyParticleEffect impactParticle1;
                    if (InParticleVisibleRange && MyParticlesManager.TryCreateParticleEffect("Meteorit_Smoke1AfterHit", out impactParticle1))
                    {
                        impactParticle1.WorldMatrix = Entity.WorldMatrix;
                        impactParticle1.UserScale = (float)Entity.PositionComp.WorldVolume.Radius * 2;
                    }
                    m_particleVectorUp = Vector3.Zero;
                    m_closeAfterSimulation = Sync.IsServer;
                    return;
                }
                if (Sync.IsServer)
                {
                    float craterRadius = Entity.PositionComp.Scale.Value * 5;
                    BoundingSphereD sphere = new BoundingSphere(value.Position, craterRadius);
                    Vector3 direction;
                    // if contact was send after reflection we need to get former direction
                    if (value.ContactPointEvent.SeparatingVelocity < 0)
                        direction = Vector3.Normalize(Entity.Physics.LinearVelocity);
                    else
                        direction = Vector3.Normalize(Vector3.Reflect(Entity.Physics.LinearVelocity, value.ContactPointEvent.ContactPoint.Normal));
                    var material = VoxelMaterial;
                    int tries = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count() * 2; // max amount of tries
                    while (!material.IsRare || !material.SpawnsFromMeteorites || material.MinVersion > MySession.Static.Settings.VoxelGeneratorVersion)
                    {
                        if (--tries < 0) // to prevent infinite loops in case all materials are disabled just use the meteorites' initial material
                        {
                            material = VoxelMaterial;
                            break;
                        }
                        material = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().ElementAt(MyUtils.GetRandomInt(MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count() - 1));
                    }
                    voxel.CreateVoxelMeteorCrater(sphere.Center, (float)sphere.Radius, -direction, material);
                    MyVoxelGenerator.MakeCrater(voxel, sphere, -direction, material);
                }
                m_soundEmitter.Entity = voxel as MyEntity;
                m_soundEmitter.SetPosition(Entity.PositionComp.GetPosition());
                m_closeAfterSimulation = Sync.IsServer;
            }

            private void DestroyGrid(ref MyPhysics.MyContactPointEvent value, MyCubeGrid grid)
            {
                MyGridContactInfo info = new MyGridContactInfo(ref value.ContactPointEvent, grid);
                info.EnableDeformation = false;
                info.EnableParticles = false;
                HkBreakOffPointInfo breakInfo = new HkBreakOffPointInfo()
                {
                    ContactPoint = value.ContactPointEvent.ContactPoint,
                    ContactPosition = info.ContactPosition,
                    ContactPointProperties = value.ContactPointEvent.ContactProperties,
                    IsContact = true,
                    BreakingImpulse = grid.Physics.Shape.BreakImpulse,
                    CollidingBody = value.ContactPointEvent.Base.BodyA == grid.Physics.RigidBody ? value.ContactPointEvent.Base.BodyB : value.ContactPointEvent.Base.BodyA,
                    ContactPointDirection = value.ContactPointEvent.Base.BodyB == grid.Physics.RigidBody ? -1 : 1,
                };
                m_soundEmitter.Entity = grid as MyEntity;
                m_soundEmitter.SetPosition(Entity.PositionComp.GetPosition());
                grid.Physics.PerformMeteoritDeformation(ref breakInfo, value.ContactPointEvent.SeparatingVelocity);
                m_closeAfterSimulation = Sync.IsServer;
            }

            private void StartLoopSound()
            {
                m_soundEmitter.PlaySingleSound(m_meteorFly);
            }

            private void StopLoopSound()
            {
                m_soundEmitter.StopSound(true);
            }

            private void PlayExplosionSound()
            {
                m_soundEmitter.SetVelocity(Vector3.Zero);
                m_soundEmitter.PlaySingleSound(m_meteorExplosion, true);
            }


            public void DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
            {
                if (sync)
                {
                    if (Sync.IsServer)
                        MySyncDamage.DoDamageSynced(Entity, damage, damageType, attackerId);
                }
                else
                {
                    MyDamageInformation info = new MyDamageInformation(false, damage, damageType, attackerId);

                    if (Entity.UseDamageSystem)
                        MyDamageSystem.Static.RaiseBeforeDamageApplied(Entity, ref info);

                    m_integrity -= info.Amount;

                    if (Entity.UseDamageSystem)
                        MyDamageSystem.Static.RaiseAfterDamageApplied(Entity, info);

                    if (m_integrity <= 0 && Sync.IsServer)
                    {
                        m_closeAfterSimulation = Sync.IsServer;

                        if (Entity.UseDamageSystem)
                            MyDamageSystem.Static.RaiseDestroyed(Entity, info);

                        return;
                    }
                }
                return;
            }

            public void OnDestroy()
            {
            }

            public float Integrity
            {
                get { return m_integrity; }
            }

            // Don't call remove reference on this, this shape is pooled
            protected virtual HkShape GetPhysicsShape(HkMassProperties massProperties, float mass, float scale)
            {
                const bool SimpleShape = false;

                Vector3 halfExtents = (Entity.Render.GetModel().BoundingBox.Max - Entity.Render.GetModel().BoundingBox.Min) / 2;
                HkShapeType shapeType;

                if (VoxelMaterial != null)
                {
                    shapeType = HkShapeType.Sphere;
                    massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(Entity.Render.GetModel().BoundingSphere.Radius * scale, mass);
                }
                else
                {
                    shapeType = HkShapeType.Box;
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(halfExtents, mass);
                }

                return MyDebris.Static.GetDebrisShape(Entity.Render.GetModel(), SimpleShape ? shapeType : HkShapeType.ConvexVertices);
            }
        }
    }
}
