using System;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Utils;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.ObjectBuilders;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.Profiler;
using VRage.Voxels;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.World;

namespace Sandbox.Game.Weapons
{

    public class MyDrillCutOut
    {
        private float m_centerOffset;
        private float m_radius;
        protected BoundingSphereD m_sphere;

        public BoundingSphereD Sphere
        {
            get { return m_sphere; }
        }

        public MyDrillCutOut(float centerOffset, float radius)
        {
            m_centerOffset = centerOffset;
            m_radius = radius;
            m_sphere = new BoundingSphereD(Vector3D.Zero, m_radius);
        }

        public virtual void OnWorldPositionChanged(ref MatrixD worldMatrix)
        {
            m_sphere.Center = worldMatrix.Translation + worldMatrix.Forward * m_centerOffset;
        }
    }

    /// <summary>
    /// Common code for all drills (both cube blocks on ship and hand drill).
    /// </summary>
    /// 
    public class MyDrillBase
    {
        public struct Sounds
        {
            public MySoundPair IdleLoop;
            public MySoundPair MetalLoop;
            public MySoundPair RockLoop;
        }

        public HashSet<MyEntity> IgnoredEntities
        {
            get { return m_sensor.IgnoredEntities; }
        }
        public MyInventory OutputInventory;
        public float VoxelHarvestRatio = MyDrillConstants.VOXEL_HARVEST_RATIO;

        // Flag for lazy shape creation.
        protected MyEntity m_drillEntity;
        protected Dictionary<MyVoxelMaterialDefinition, int> m_drilledMaterialBuffer;
        private MyFixedPoint m_inventoryCollectionRatio;

        protected MyDrillSensorBase m_sensor;
        public MyStringHash m_drillMaterial = MyStringHash.GetOrCompute("HandDrill");
        public MySoundPair m_idleSoundLoop = new MySoundPair("ToolPlayDrillIdle");
        protected MyStringHash m_metalMaterial = MyStringHash.GetOrCompute("Metal");
        protected MyStringHash m_rockMaterial = MyStringHash.GetOrCompute("Rock");

        // Last time of contact of drill with an object.
        private int m_lastContactTime;
        //GK: Added in order to check for breakable environment items (e.g. trees) from hand tools (Driller,Grinder)
        private int m_lastItemId;

        public MyParticleEffect DustParticles;
        private MySlimBlock m_target;

        private MyParticleEffectsIDEnum m_dustEffectId;
        private MyParticleEffectsIDEnum m_dustEffectStonesId;
        private MyParticleEffectsIDEnum m_sparksEffectId;
        protected bool m_particleEffectsEnabled = true;

        private float m_animationMaxSpeedRatio;
        private float m_animationLastUpdateTime;
        private readonly float m_animationSlowdownTimeInSeconds;

        protected float m_floatingObjectSpawnOffset;
        protected float m_floatingObjectSpawnRadius;

        private MyEntity3DSoundEmitter m_soundEmitter;

        private bool m_initialHeatup = true;

        protected MyDrillCutOut m_cutOut;
        private readonly float m_drillCameraMeanShakeIntensity = 0.65f;
        private readonly float m_drillCameraMaxShakeIntensity = 2.25f;

        public MySoundPair CurrentLoopCueEnum { get; set; }

        public bool IsDrilling
        {
            get;
            private set;
        }

        public float AnimationMaxSpeedRatio
        {
            get { return m_animationMaxSpeedRatio; }
        }

        public MyDrillSensorBase Sensor
        {
            get { return m_sensor; }
        }

        public MyDrillCutOut CutOut
        {
            get { return m_cutOut; }
        }


        /// <param name="drillEntity">Entity to which this drill is attached.</param>
        /// <param name="inventoryCollectionRatio">Ratio (0 to 1) of mined material that will be stored in inventory (if one is assigned), rest will be thrown out in space.</param>
        public MyDrillBase(
            MyEntity drillEntity,
            MyParticleEffectsIDEnum dustEffectId,
            MyParticleEffectsIDEnum dustEffectStonesId,
            MyParticleEffectsIDEnum sparksEffectId,
            MyDrillSensorBase sensor,
            MyDrillCutOut cutOut,
            float animationSlowdownTimeInSeconds,
            float floatingObjectSpawnOffset,
            float floatingObjectSpawnRadius,
            float inventoryCollectionRatio = 0f)
        {
            m_drillEntity = drillEntity;
            m_sensor = sensor;
            m_cutOut = cutOut;
            m_dustEffectId = dustEffectId;
            m_dustEffectStonesId = dustEffectStonesId;
            m_sparksEffectId = sparksEffectId;
            m_animationSlowdownTimeInSeconds = animationSlowdownTimeInSeconds;
            m_floatingObjectSpawnOffset = floatingObjectSpawnOffset;
            m_floatingObjectSpawnRadius = floatingObjectSpawnRadius;

            Debug.Assert(inventoryCollectionRatio >= 0f && inventoryCollectionRatio <= 1f,
                "Inventory collection ratio must be in range <0;1>");
            m_inventoryCollectionRatio = (MyFixedPoint)inventoryCollectionRatio;

            m_drilledMaterialBuffer = new Dictionary<MyVoxelMaterialDefinition, int>();

            m_soundEmitter = new MyEntity3DSoundEmitter(m_drillEntity, true);
        }

        public bool Drill(bool collectOre = true, bool performCutout = true, bool assignDamagedMaterial = false, float speedMultiplier = 1f)
        {
            ProfilerShort.Begin("MyDrillBase::Drill()");

            bool drillingSuccess = false;

            MySoundPair sound = null;

            if ((m_drillEntity.Parent != null) && (m_drillEntity.Parent.Physics != null) && !m_drillEntity.Parent.Physics.Enabled)
                return false;

            if (performCutout)
            {
                StopSparkParticles();
                StopDustParticles();
                var entitiesInRange = m_sensor.EntitiesInRange;
                MyStringHash targetMaterial = MyStringHash.NullOrEmpty;
                MyStringHash bestMaterial = MyStringHash.NullOrEmpty;
                float distanceBest = float.MaxValue;
                bool targetIsBlock = false;
                foreach (var entry in entitiesInRange)
                {
                    drillingSuccess = false;
                    var entity = entry.Value.Entity;
                    if (entity.MarkedForClose)
                        continue;
                    if (entity is MyCubeGrid)
                    {
                        var grid = entity as MyCubeGrid;
                        if (grid.Physics != null && grid.Physics.Enabled)
                        {
                            drillingSuccess = TryDrillBlocks(grid, entry.Value.DetectionPoint, !Sync.IsServer, out targetMaterial);
                            targetIsBlock = true;
                        }
                        if (drillingSuccess)
                        {
                            m_initialHeatup = false;
                            CreateParticles(entry.Value.DetectionPoint, false, true, false);
                        }
                    }
                    else if (entity is MyVoxelBase)
                    {
                        ProfilerShort.Begin("Drill voxel map");
                        var voxels = entity as MyVoxelBase;
                        drillingSuccess = TryDrillVoxels(voxels, entry.Value.DetectionPoint, collectOre, !Sync.IsServer, assignDamagedMaterial);
                        ProfilerShort.BeginNextBlock("Create particles");
                        if (drillingSuccess)
                        {
                            Vector3D drillHitPoint = entry.Value.DetectionPoint;
                            if(targetMaterial == MyStringHash.NullOrEmpty)
                            {
                                var voxelMaterial = voxels.GetMaterialAt(ref drillHitPoint);
                                if (voxelMaterial != null)
                                    targetMaterial = MyStringHash.GetOrCompute(voxelMaterial.MaterialTypeName);
                            }
                            CreateParticles(entry.Value.DetectionPoint, true, false, true);
                        }
                        ProfilerShort.End();
                    }
                    else if (entity is MyFloatingObject)
                    {
                        var sphere = (BoundingSphereD)m_cutOut.Sphere;
                        sphere.Radius *= 1.33f;
                        if (entity.GetIntersectionWithSphere(ref sphere))
                        {
                            MyFloatingObject flObj = entity as MyFloatingObject;
                            if (Sync.IsServer)
                            {
                                if (flObj.Item.Content.TypeId == typeof(MyObjectBuilder_Ore))
                                {
                                    var invOwn = (m_drillEntity != null && m_drillEntity.HasInventory) ? m_drillEntity : null;
                                    if (invOwn == null)
                                        invOwn = (m_drillEntity as MyHandDrill).Owner;

                                    System.Diagnostics.Debug.Assert((invOwn.GetInventory(0) as MyInventory) != null, "Null or unexpected inventory type!");
                                    (invOwn.GetInventory(0) as MyInventory).TakeFloatingObject(flObj);
                                }
                                else
                                    (entity as MyFloatingObject).DoDamage(70, MyDamageType.Drill, true, attackerId: m_drillEntity != null ? m_drillEntity.EntityId : 0);
                            }
                            drillingSuccess = true;
                        }
                    }
                    else if (entity is MyCharacter)
                    {
                        var sphere = (BoundingSphereD)m_cutOut.Sphere;
                        sphere.Radius *= 0.8f;
                        var character = entity as MyCharacter;
                        if (targetMaterial == MyStringHash.NullOrEmpty)
                            targetMaterial = MyStringHash.GetOrCompute((entity as MyCharacter).Definition.PhysicalMaterial);
                        if (entity.GetIntersectionWithSphere(ref sphere))
                        {
                            //MyRenderProxy.DebugDrawSphere(sphere.Center, sphere.Radius, Color.Green.ToVector3(), 1, true);

                            //damage tracking
                            if ((m_drillEntity is MyHandDrill) && (m_drillEntity as MyHandDrill).Owner == MySession.Static.LocalCharacter && character != MySession.Static.LocalCharacter && character.IsDead == false)
                                MySession.Static.TotalDamageDealt += 20;

                            if (Sync.IsServer)
                                character.DoDamage(20, MyDamageType.Drill, true, attackerId: m_drillEntity != null ? m_drillEntity.EntityId : 0);
                            drillingSuccess = true;
                        }
                        else
                        {
                            BoundingSphereD headSphere = new BoundingSphereD(character.PositionComp.WorldMatrix.Translation + character.WorldMatrix.Up * 1.25f, 0.6f);
                            //MyRenderProxy.DebugDrawSphere(headSphere.Center, headSphere.Radius, Color.Red.ToVector3(), 1, false);
                            if (headSphere.Intersects(sphere))
                            {
                                //MyRenderProxy.DebugDrawSphere(sphere.Center, sphere.Radius, Color.Green.ToVector3(), 1, true);

                                //damage tracking
                                if ((m_drillEntity is MyHandDrill) && (m_drillEntity as MyHandDrill).Owner == MySession.Static.LocalCharacter && character != MySession.Static.LocalCharacter && character.IsDead == false)
                                    MySession.Static.TotalDamageDealt += 20;

                                if (Sync.IsServer)
                                    character.DoDamage(20, MyDamageType.Drill, true, attackerId: m_drillEntity != null ? m_drillEntity.EntityId : 0);
                                drillingSuccess = true;
                            }
                        }
                    }
                    else if (entity is MyEnvironmentSector)
                    {
                        if (m_lastItemId != entry.Value.ItemId)
                        {
                            m_lastItemId = entry.Value.ItemId;
                            m_lastContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        }
                        if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastContactTime > MyDebrisConstants.CUT_TREE_IN_MILISECONDS * speedMultiplier)
                        {
                            var sectorProxy = (entity as MyEnvironmentSector).GetModule<MyBreakableEnvironmentProxy>();
                            sectorProxy.BreakAt(entry.Value.ItemId, entry.Value.DetectionPoint, Vector3D.Zero, 0);
                            drillingSuccess = true;
                            m_lastItemId = 0;
                        }
                    }
                    if (drillingSuccess)
                    {
                        m_lastContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                        float dist = Vector3.DistanceSquared(entry.Value.DetectionPoint, Sensor.Center);
                        if (targetMaterial != null && targetMaterial != MyStringHash.NullOrEmpty && dist < distanceBest)
                        {
                            bestMaterial = targetMaterial;
                            distanceBest = dist;
                        }
                    }
                }

                if (bestMaterial != null && bestMaterial != MyStringHash.NullOrEmpty)
                {
                    sound = MyMaterialPropertiesHelper.Static.GetCollisionCue(MyMaterialPropertiesHelper.CollisionType.Start, m_drillMaterial, bestMaterial);
                    if (sound == null || sound == MySoundPair.Empty)//target material was not set in definition - using metal/rock sound
                    {
                        if (targetIsBlock)
                        {
                            bestMaterial = m_metalMaterial;
                        }
                        else
                        {
                            bestMaterial = m_rockMaterial;
                        }
                    }
                    sound = MyMaterialPropertiesHelper.Static.GetCollisionCue(MyMaterialPropertiesHelper.CollisionType.Start, m_drillMaterial, bestMaterial);
                }
            }

            if (sound != null && sound != MySoundPair.Empty)
                StartLoopSound(sound);
            else
                StartIdleSound(m_idleSoundLoop);

            if (!IsDrilling)
            {
                IsDrilling = true;
                m_animationLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }

            ProfilerShort.End();
            return drillingSuccess;
        }

        public virtual void Close()
        {
            IsDrilling = false;
            StopDustParticles();
            StopSparkParticles();
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
        }

        public void StopDrill()
        {
            IsDrilling = false;
            m_initialHeatup = true;
            StopDustParticles();
            StopSparkParticles();
            StopLoopSound();
        }

        public void UpdateAfterSimulation()
        {
            /*if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastContactTime) > MyDrillConstants.PARTICLE_EFFECT_DURATION)
                StopDustParticles();*/

            if (!IsDrilling && m_animationMaxSpeedRatio > float.Epsilon)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_animationLastUpdateTime) / 1000f;
                m_animationMaxSpeedRatio -= timeDelta / m_animationSlowdownTimeInSeconds;
                if (m_animationMaxSpeedRatio < float.Epsilon)
                    m_animationMaxSpeedRatio = 0f;
            }

            if (IsDrilling)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_animationLastUpdateTime) / 1000f;
                m_animationMaxSpeedRatio += 2f * timeDelta / m_animationSlowdownTimeInSeconds;
                if (m_animationMaxSpeedRatio > 1f)
                    m_animationMaxSpeedRatio = 1f;
            }

            m_animationLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void OnWorldPositionChanged(MatrixD worldMatrix)
        {
            m_sensor.OnWorldPositionChanged(ref worldMatrix);
            m_cutOut.OnWorldPositionChanged(ref worldMatrix);
        }

        private void StartIdleSound(MySoundPair cuePair)
        {
            if (m_soundEmitter == null)
                return;
            if (!m_soundEmitter.IsPlaying)
            {
                //no sound is playing, start idle sound normally
                m_soundEmitter.PlaySound(cuePair);
            }
            else if (!m_soundEmitter.SoundPair.Equals(cuePair))
            {
                //different sound is playing, play end sound for currently playing and start idle sound without intro
                m_soundEmitter.StopSound(false);
                m_soundEmitter.PlaySound(cuePair, false, true);
            }
        }

        private void StartLoopSound(MySoundPair cueEnum)
        {
            if (m_soundEmitter == null)
                return;
            if (!m_soundEmitter.IsPlaying)
            {
                //no sound is playing, start sound normally
                m_soundEmitter.PlaySound(cueEnum);
            }
            else if (!m_soundEmitter.SoundPair.Equals(cueEnum))
            {
                if (m_soundEmitter.SoundPair.Equals(m_idleSoundLoop))
                {
                    //idle sound is playing, stop idle sound and start new sound normally
                    m_soundEmitter.StopSound(true);
                    m_soundEmitter.PlaySound(cueEnum);
                }
                else
                {
                    //different sound is playing, play end sound for currently playing and start new sound without intro
                    m_soundEmitter.StopSound(false);
                    m_soundEmitter.PlaySound(cueEnum, false, true);
                }
            }
        }

        public void StopLoopSound()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(false);
        }

        public MyParticleEffect SparkEffect = null;
        protected void CreateParticles(Vector3D position, bool createDust, bool createSparks, bool createStones)
        {
            if (!m_particleEffectsEnabled)
                return;

            if (createDust)
            {
                if (DustParticles == null)
                {
                    ProfilerShort.Begin(string.Format("Create dust: stones = {0}", createStones));
                    //MyParticleEffectsIDEnum.Smoke_Construction
                    MyParticlesManager.TryCreateParticleEffect(createStones ? (int)m_dustEffectStonesId : (int)m_dustEffectId, out DustParticles);
                    ProfilerShort.End();
                }

                if (DustParticles != null)
                {
                    DustParticles.WorldMatrix = MatrixD.CreateTranslation(position);
                }
            }

            if (createSparks)
            {
                ProfilerShort.Begin("Create sparks");
                if (MyParticlesManager.TryCreateParticleEffect((int)m_sparksEffectId, out SparkEffect))
                {
                    SparkEffect.WorldMatrix = Matrix.CreateTranslation(position);
                }
                ProfilerShort.End();
            }
        }

        private void StopDustParticles()
        {
            if (DustParticles != null)
            {
                DustParticles.Stop();
                DustParticles = null;
            }
        }

        public void StopSparkParticles()
        {
            if (SparkEffect != null)
            {
                SparkEffect.Stop();
                SparkEffect = null;
            }
        }

        protected virtual bool TryDrillBlocks(MyCubeGrid grid, Vector3 worldPoint, bool onlyCheck, out MyStringHash blockMaterial)
        {
            var invWorld = grid.PositionComp.WorldMatrixNormalizedInv;
            var gridLocalPosCenter = Vector3.Transform(m_sensor.Center, invWorld);
            var gridLocalPos = Vector3.Transform(m_sensor.FrontPoint, invWorld);
            var gridLocalTarget = Vector3.Transform(worldPoint, invWorld);

            var gridSpacePos = Vector3I.Round(gridLocalPos / grid.GridSize);
            var block = grid.GetCubeBlock(gridSpacePos);

            if (block != null)
            {
                if (block.BlockDefinition.PhysicalMaterial.Id.SubtypeId == MyStringHash.NullOrEmpty)
                    blockMaterial = m_metalMaterial;
                else
                    blockMaterial = block.BlockDefinition.PhysicalMaterial.Id.SubtypeId;
            }
            else
                blockMaterial = MyStringHash.NullOrEmpty;

            int createDebris = 0;
            if (!onlyCheck)
            {
                if (block != null && block is IMyDestroyableObject && block.CubeGrid.BlocksDestructionEnabled)
                {
                    var destroyable = (block as IMyDestroyableObject);
                    destroyable.DoDamage(60, MyDamageType.Drill, Sync.IsServer, attackerId: m_drillEntity != null ? m_drillEntity.EntityId : 0);
                    createDebris = grid.Physics.ApplyDeformation(0.25f, 1.5f, 2f, gridLocalTarget, Vector3.Normalize(gridLocalPos - gridLocalPosCenter), MyDamageType.Drill, attackerId: m_drillEntity != null ? m_drillEntity.EntityId : 0);
                }
            }

            m_target = createDebris != 0 ? null : block;

            bool success = false;
            if (block != null)
            {
                if (createDebris != 0)
                {
                    BoundingSphereD bsphere = m_cutOut.Sphere;
                    BoundingBoxD aabb = BoundingBoxD.CreateFromSphere(bsphere);
                    MyDebris.Static.CreateExplosionDebris(ref bsphere, block.CubeGrid, ref aabb, 0.3f);
                }

                success = true;
            }

            return success;
        }

        protected virtual bool TryDrillVoxels(MyVoxelBase voxels, Vector3D hitPosition, bool collectOre, bool onlyCheck, bool applyDamagedMaterial)
        {
            const float DISCARDING_MULTIPLIER = 3.0f;

            if (voxels.GetOrePriority() == MyVoxelConstants.PRIORITY_IGNORE_EXTRACTION) return false;

            bool somethingDrilled = false;
            var  bsphere = new MyShapeSphere()
            {
                Center = m_cutOut.Sphere.Center,
                Radius = (float)m_cutOut.Sphere.Radius
            };
            if (!collectOre) bsphere.Radius *= DISCARDING_MULTIPLIER;

            float voxelsCountInPercent;
            MyVoxelMaterialDefinition material;
            MyVoxelGenerator.CutOutShapeWithProperties(voxels, bsphere,
                out voxelsCountInPercent, out material, m_drilledMaterialBuffer, Sync.IsServer, onlyCheck, applyDamagedMaterial);

            foreach (var entry in m_drilledMaterialBuffer)
            {
                somethingDrilled = (!collectOre || TryHarvestOreMaterial(entry.Key, hitPosition, entry.Value, onlyCheck)) || somethingDrilled;

                if (somethingDrilled && !onlyCheck)
                {
                    MyDebris.Static.CreateDirectedDebris(hitPosition,
                            MyUtils.GetRandomVector3Normalized(), 0.1f, 1, 0, MathHelper.Pi, 5, 1, 0.15f, entry.Key);
                }
            }

            m_drilledMaterialBuffer.Clear();
            return somethingDrilled;
        }

        public void PerformCameraShake()
        {
            if (MySector.MainCamera == null)
                return;

            float intensity = (float)(-Math.Log(MyRandom.Instance.NextDouble()) * m_drillCameraMeanShakeIntensity);
            intensity = MathHelper.Clamp(intensity, 0, m_drillCameraMaxShakeIntensity);
            MySector.MainCamera.CameraShake.AddShake(intensity);
        }

        /// <summary>
        /// Converts voxel material to ore material and puts it into the inventory. If there is no
        /// corresponding ore for given voxel type, nothing happens.
        /// </summary>
        private bool TryHarvestOreMaterial(MyVoxelMaterialDefinition material, Vector3 hitPosition, int removedAmount, bool onlyCheck)
        {
            if (string.IsNullOrEmpty(material.MinedOre))
                return false;

            if (!onlyCheck)
            {
                ProfilerShort.Begin("TryHarvestOreMaterial");
                var oreObjBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                oreObjBuilder.MaterialTypeName = material.Id.SubtypeId;
                float amountCubicMeters = (float)(((float)removedAmount / (float)MyVoxelConstants.VOXEL_CONTENT_FULL) * MyVoxelConstants.VOXEL_VOLUME_IN_METERS * VoxelHarvestRatio);
                amountCubicMeters *= (float)material.MinedOreRatio;

                if (!MySession.Static.AmountMined.ContainsKey(material.MinedOre))
                    MySession.Static.AmountMined[material.MinedOre] = 0;
                MySession.Static.AmountMined[material.MinedOre] += (MyFixedPoint)amountCubicMeters;

                float maxDropCubicMeters = MyDrillConstants.MAX_DROP_CUBIC_METERS;

                var physItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oreObjBuilder);
                MyFixedPoint amountInItemCount = (MyFixedPoint)(amountCubicMeters / physItem.Volume);
                MyFixedPoint maxAmountPerDrop = (MyFixedPoint)(maxDropCubicMeters / physItem.Volume);

                if (OutputInventory != null)
                {
                    MyFixedPoint amountDropped = amountInItemCount * (1 - m_inventoryCollectionRatio);
                    amountDropped = MyFixedPoint.Min(maxAmountPerDrop * 10 - (MyFixedPoint)0.001, amountDropped);
                    MyFixedPoint inventoryAmount = (amountInItemCount * m_inventoryCollectionRatio) - amountDropped;
                    OutputInventory.AddItems(inventoryAmount, oreObjBuilder);
                    SpawnOrePieces(amountDropped, maxAmountPerDrop, hitPosition, oreObjBuilder, material);
                }
                else
                {
                    SpawnOrePieces(amountInItemCount, maxAmountPerDrop, hitPosition, oreObjBuilder, material);
                }
                ProfilerShort.End();
            }

            return true;
        }

        private void SpawnOrePieces(MyFixedPoint amountItems, MyFixedPoint maxAmountPerDrop, Vector3 hitPosition, MyObjectBuilder_PhysicalObject oreObjBuilder, MyVoxelMaterialDefinition voxelMaterial)
        {
            if(Sync.IsServer == false)
            {
                return;
            }

            ProfilerShort.Begin("SpawnOrePieces");
            var forward = Vector3.Normalize(m_sensor.FrontPoint - m_sensor.Center);
            //var pos = m_sensor.CutOutSphere.Center + forward * m_floatingObjectSpawnOffset;
            var pos = hitPosition - forward * m_floatingObjectSpawnRadius;
            BoundingSphere bsphere = new BoundingSphere(pos, m_floatingObjectSpawnRadius);

            while (amountItems > 0)
            {
                //new: MyFixedPoint dropAmount = amountItems;
                //original: MyFixedPoint dropAmount = MyFixedPoint.Min(amountItems, maxAmountPerDrop);
                MyFixedPoint dropAmount = MyFixedPoint.Min(amountItems, maxAmountPerDrop);
                amountItems -= dropAmount;
                var inventoryItem = new MyPhysicalInventoryItem(dropAmount, oreObjBuilder);
                var item = MyFloatingObjects.Spawn(inventoryItem, bsphere, null, voxelMaterial);
                item.Physics.LinearVelocity = MyUtils.GetRandomVector3HemisphereNormalized(forward) * MyUtils.GetRandomFloat(1.5f, 4);//original speed 5-8
                item.Physics.AngularVelocity = MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(4, 8);
            }
            ProfilerShort.End();
        }

        public void DebugDraw()
        {
            m_sensor.DebugDraw();
            MyRenderProxy.DebugDrawSphere((Vector3)m_cutOut.Sphere.Center, (float)m_cutOut.Sphere.Radius, Color.Red, 0.6f, true);
        }

        private Vector3 ComputeDebrisDirection()
        {
            var debrisDir = m_sensor.Center - m_sensor.FrontPoint;
            debrisDir.Normalize();
            return debrisDir;
        }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }
    }
}
