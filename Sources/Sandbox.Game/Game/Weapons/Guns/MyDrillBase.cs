using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using VRage.ObjectBuilders;

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
            m_sphere = new BoundingSphereD(worldMatrix.Translation + worldMatrix.Forward * m_centerOffset, m_radius);
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

        // Last time of contact of drill with an object.
        private int m_lastContactTime;

        private MyParticleEffect m_dustParticles;
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
        private Sounds m_sounds;

        private bool m_initialHeatup = true;

        protected MyDrillCutOut m_cutOut;

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
            Sounds sounds,
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

            m_sounds = sounds;
            m_soundEmitter = new MyEntity3DSoundEmitter(m_drillEntity);
        }

        public bool Drill(bool collectOre = true, bool performCutout = true)
        {
            ProfilerShort.Begin("MyDrillBase::Drill()");

            bool drillingSuccess = false;

            MySoundPair sound = null;

            if ((m_drillEntity.Parent != null) && (m_drillEntity.Parent.Physics != null) && !m_drillEntity.Parent.Physics.Enabled)
                return false;

            if (performCutout)
            {
                var entitiesInRange = m_sensor.EntitiesInRange;
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
                            drillingSuccess = TryDrillBlocks(grid, entry.Value.DetectionPoint, !Sync.IsServer);
                        }
                        if (drillingSuccess)
                        {
                            m_initialHeatup = false;
                            sound = m_sounds.MetalLoop;
                            CreateParticles(entry.Value.DetectionPoint, false, true, false);
                        }
                    }
                    else if (entity is MyVoxelBase)
                    {
                        ProfilerShort.Begin("Drill voxel map");
                        var voxels = entity as MyVoxelBase;
                        drillingSuccess = TryDrillVoxels(voxels, entry.Value.DetectionPoint, collectOre, !Sync.IsServer);
                        ProfilerShort.BeginNextBlock("Create particles");
                        if (drillingSuccess)
                        {
                            sound = m_sounds.RockLoop;
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
                                    IMyInventoryOwner invOwn = m_drillEntity as IMyInventoryOwner;
                                    if (invOwn == null)
                                        invOwn = (m_drillEntity as MyHandDrill).Owner as IMyInventoryOwner;
                                    invOwn.GetInventory(0).TakeFloatingObject(flObj);
                                }
                                else
                                    (entity as MyFloatingObject).DoDamage(70, MyDamageType.Drill, true);
                            }
                            drillingSuccess = true;
                        }
                    }
                    else if (entity is MyCharacter)
                    {
                        var sphere = (BoundingSphereD)m_cutOut.Sphere;
                        sphere.Radius *= (4 / 5f);
                        var character = entity as MyCharacter;
                        if (entity.GetIntersectionWithSphere(ref sphere))
                        {
                            //MyRenderProxy.DebugDrawSphere(sphere.Center, sphere.Radius, Color.Green.ToVector3(), 1, true);
                            if (Sync.IsServer)
                                character.DoDamage(20, MyDamageType.Drill, true);
                            drillingSuccess = true;
                        }
                        else
                        {
                            BoundingSphereD headSphere = new BoundingSphereD(character.PositionComp.WorldMatrix.Translation + character.WorldMatrix.Up * 1.25f, 0.6f);
                            //MyRenderProxy.DebugDrawSphere(headSphere.Center, headSphere.Radius, Color.Red.ToVector3(), 1, false);
                            if (headSphere.Intersects(sphere))
                            {
                                //MyRenderProxy.DebugDrawSphere(sphere.Center, sphere.Radius, Color.Green.ToVector3(), 1, true);
                                if (Sync.IsServer)
                                    character.DoDamage(20, MyDamageType.Drill, true);
                                drillingSuccess = true;
                            }
                        }
                    }
                    if (drillingSuccess)
                    {
                        m_lastContactTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    }
                }
            }

            if (sound != null)
                StartLoopSound(sound);
            else
                StartIdleSound(m_sounds.IdleLoop);

            IsDrilling = true;

            m_animationLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            ProfilerShort.End();
            return drillingSuccess;
        }

        public virtual void Close()
        {
            IsDrilling = false;
            StopParticles();
            m_soundEmitter.StopSound(true);
        }

        public void StopDrill()
        {
            IsDrilling = false;
            m_initialHeatup = true;
            StopParticles();
            StopLoopSound();
        }

        public void UpdateAfterSimulation()
        {
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastContactTime) > MyDrillConstants.PARTICLE_EFFECT_DURATION)
                StopParticles();

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
            if (m_soundEmitter.IsPlaying && m_soundEmitter.SoundId != cuePair.SoundId && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastContactTime < 100)
                return;
            StartLoopSound(cuePair);
        }

        private void StartLoopSound(MySoundPair cueEnum)
        {
            if (m_soundEmitter.Loop)
                m_soundEmitter.PlaySingleSound(cueEnum, true, true);
            else
                m_soundEmitter.PlaySound(cueEnum);
        }

        public void StopLoopSound()
        {
            m_soundEmitter.StopSound(false);
        }

        protected void CreateParticles(Vector3D position, bool createDust, bool createSparks, bool createStones)
        {
            if (!m_particleEffectsEnabled)
                return;

            if (m_dustParticles != null && m_dustParticles.IsStopped)
                m_dustParticles = null;

            if (createDust)
            {
                if (m_dustParticles == null)
                {
                    ProfilerShort.Begin(string.Format("Create dust: stones = {0}", createStones));
                    //MyParticleEffectsIDEnum.Smoke_Construction
                    MyParticlesManager.TryCreateParticleEffect(createStones ? (int)m_dustEffectStonesId : (int)m_dustEffectId, out m_dustParticles);
                    ProfilerShort.End();
                }

                if (m_dustParticles != null)
                {
                    m_dustParticles.AutoDelete = false;
                    m_dustParticles.Near = m_drillEntity.Render.NearFlag;
                    m_dustParticles.WorldMatrix = MatrixD.CreateTranslation(position);
                }
            }

            if (createSparks)
            {
                ProfilerShort.Begin("Create sparks");
                MyParticleEffect sparks;
                if (MyParticlesManager.TryCreateParticleEffect((int)m_sparksEffectId, out sparks))
                {
                    sparks.WorldMatrix = Matrix.CreateTranslation(position);
                    sparks.Near = m_drillEntity.Render.NearFlag;
                }
                ProfilerShort.End();
            }
        }

        private void StopParticles()
        {
            if (m_dustParticles != null)
            {
                m_dustParticles.Stop();
                m_dustParticles = null;
            }
        }

        protected virtual bool TryDrillBlocks(MyCubeGrid grid, Vector3 worldPoint, bool onlyCheck)
        {
            var invWorld = grid.PositionComp.GetWorldMatrixNormalizedInv();
            var gridLocalPosCenter = Vector3.Transform(m_sensor.Center, invWorld);
            var gridLocalPos = Vector3.Transform(m_sensor.FrontPoint, invWorld);
            var gridLocalTarget = Vector3.Transform(worldPoint, invWorld);

            var gridSpacePos = Vector3I.Round(gridLocalPos / grid.GridSize);
            var block = grid.GetCubeBlock(gridSpacePos);

            bool createDebris = false;
            if (!onlyCheck)
            {
                if (block != null && block is IMyDestroyableObject && block.CubeGrid.BlocksDestructionEnabled)
                {
                    var destroyable = (block as IMyDestroyableObject);
                    destroyable.DoDamage(60, MyDamageType.Drill, Sync.IsServer);
                    createDebris = grid.Physics.ApplyDeformation(0.25f, 1.5f, 2f, gridLocalTarget, Vector3.Normalize(gridLocalPos - gridLocalPosCenter), MyDamageType.Drill);
                }
            }

            m_target = createDebris ? null : block;

            bool success = false;
            if (block != null)
            {
                if (createDebris)
                {
                    BoundingSphereD bsphere = m_cutOut.Sphere;
                    BoundingBoxD aabb = BoundingBoxD.CreateFromSphere(bsphere);
                    MyDebris.Static.CreateExplosionDebris(ref bsphere, block.CubeGrid, ref aabb, 0.3f);
                }

                success = true;
            }

            return success;
        }

        protected virtual bool TryDrillVoxels(MyVoxelBase voxels, Vector3D hitPosition, bool collectOre, bool onlyCheck)
        {
            const float DISCARDING_MULTIPLIER = 3.0f;

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
                out voxelsCountInPercent, out material, m_drilledMaterialBuffer, Sync.IsServer, onlyCheck);

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

        //Do one frame heatup only in singleplayer, lag will solve it in multiplayer
        protected bool InitialHeatup()
        {
            if (m_initialHeatup && !Sync.MultiplayerActive)
            {
                m_initialHeatup = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Converts voxel material to ore material and puts it into the inventory. If there is no
        /// corresponding ore for given voxel type, nothing happens.
        /// </summary>
        private bool TryHarvestOreMaterial(MyVoxelMaterialDefinition material, Vector3 hitPosition, int removedAmount, bool onlyCheck)
        {
            if (string.IsNullOrEmpty(material.MinedOre))
                return false;

            //Do one frame heatup only in singleplayer, lag will solve it in multiplayer
            if (InitialHeatup())
                return true;

            if (!onlyCheck)
            {
                ProfilerShort.Begin("TryHarvestOreMaterial");
                var oreObjBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                float amountCubicMeters = (float)(((float)removedAmount / (float)MyVoxelConstants.VOXEL_CONTENT_FULL) * MyVoxelConstants.VOXEL_VOLUME_IN_METERS * VoxelHarvestRatio);
                amountCubicMeters *= (float)material.MinedOreRatio;

                if (!MySession.Static.AmountMined.ContainsKey(material.MinedOre))
                    MySession.Static.AmountMined[material.MinedOre] = 0;
                MySession.Static.AmountMined[material.MinedOre] += (MyFixedPoint)amountCubicMeters;

                float maxDropCubicMeters = 0.150f;

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
            ProfilerShort.Begin("SpawnOrePieces");
            var forward = Vector3.Normalize(m_sensor.FrontPoint - m_sensor.Center);
            //var pos = m_sensor.CutOutSphere.Center + forward * m_floatingObjectSpawnOffset;
            var pos = hitPosition - forward * m_floatingObjectSpawnRadius;
            BoundingSphere bsphere = new BoundingSphere(pos, m_floatingObjectSpawnRadius);

            while (amountItems > 0)
            {
                MyFixedPoint dropAmount = MyFixedPoint.Min(amountItems, maxAmountPerDrop);
                amountItems -= dropAmount;
                var inventoryItem = new MyPhysicalInventoryItem(dropAmount, oreObjBuilder);
                var item = MyFloatingObjects.Spawn(inventoryItem, bsphere, null, voxelMaterial);
                item.Physics.LinearVelocity = MyUtils.GetRandomVector3HemisphereNormalized(forward) * MyUtils.GetRandomFloat(5, 8);
                item.Physics.AngularVelocity = MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(4, 8);
            }
            ProfilerShort.End();
        }

        public void DebugDraw()
        {
            m_sensor.DebugDraw();
            BoundingSphere bsphere = m_cutOut.Sphere;
            var color = new Vector3(0, 1, 1);
            MyRenderProxy.DebugDrawSphere(bsphere.Center, bsphere.Radius, color, 0.6f, true);
        }

        private Vector3 ComputeDebrisDirection()
        {
            var debrisDir = m_sensor.Center - m_sensor.FrontPoint;
            debrisDir.Normalize();
            return debrisDir;
        }

        public void UpdateAfterSimulation100()
        {
            m_soundEmitter.Update();
        }
    }
}
