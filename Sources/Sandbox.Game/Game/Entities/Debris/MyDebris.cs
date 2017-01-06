using System;
using System.Collections.Generic;
using Havok;
using Sandbox.Definitions;

using VRage;
using VRage.Utils;
using VRageMath;
using System.Linq;
using Sandbox.Engine.Physics;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Collections;

namespace Sandbox.Game.Entities.Debris
{

    /// <summary>
    /// Wrapper for different types of debris and their pools. Also used to create debris.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyDebris : MySessionComponentBase
    {
        private static MyDebris m_static;
        public static MyDebris Static
        {
            get { return m_static; }
            private set { m_static = value; }
        }

        struct MyModelShapeInfo
        {
            public MyModel Model;
            public HkShapeType ShapeType;
        }

        private List<Vector3D> m_positionBuffer;
        private List<Vector3> m_voxelDebrisOffsets;

        private static string[] m_debrisModels;

        //public static readonly string VoxelDebrisModel = "Models\\Debris\\RockDebris01.mwm";
        private static string[] m_debrisVoxels;
        public static readonly float VoxelDebrisModelVolume = 0.15f; // Model represents 150 liters of debris

        // From Voxel Debris
        private float m_debrisScaleLower = 0.0068f; // In size of explosion
        private float m_debrisScaleUpper = 0.0155f; // In size of explosion
        private float m_debrisScaleClamp = 0.5f; // Max size is half size of model

        MyConcurrentDictionary<MyModelShapeInfo, HkShape> m_shapes = new MyConcurrentDictionary<MyModelShapeInfo, HkShape>();

        private const int MaxDebrisCount = 100;
        private int m_debrisCount = 0;

        public MyDebris()
        {
            //m_debrisModels = new string[]
            //{
            //    "Models\\Debris\\Debris01.mwm",
            //    "Models\\Debris\\Debris04.mwm",
            //    "Models\\Debris\\Debris05.mwm",
            //    "Models\\Debris\\Debris06.mwm",
            //    "Models\\Debris\\Debris07.mwm",
            //    "Models\\Debris\\Debris08.mwm",
            //    "Models\\Debris\\Debris09.mwm",
            //    "Models\\Debris\\Debris10.mwm",
            //    "Models\\Debris\\Debris12.mwm",
            //    "Models\\Debris\\Debris13.mwm",
            //    "Models\\Debris\\Debris17.mwm",
            //    "Models\\Debris\\Debris21.mwm",
            //    "Models\\Debris\\Debris25.mwm",
            //    "Models\\Debris\\Debris28.mwm",
            //    "Models\\Debris\\Debris31.mwm",
            //};

            m_debrisModels = MyDefinitionManager.Static.GetDebrisDefinitions().Where(x => x.Type == MyDebrisType.Model).Select(x => x.Model).ToArray();
            m_debrisVoxels = MyDefinitionManager.Static.GetDebrisDefinitions().Where(x => x.Type == MyDebrisType.Voxel).Select(x => x.Model).ToArray();
        }

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyPhysics) };
            }
        }

        // Don't call remove reference on this, this shape is pooled
        public HkShape GetDebrisShape(MyModel model, HkShapeType shapeType)
        {
            MyModelShapeInfo info = new MyModelShapeInfo();
            info.Model = model;
            info.ShapeType = shapeType;

            HkShape shape;
            if (!m_shapes.TryGetValue(info, out shape))
            {
                shape = CreateShape(model, shapeType);
                m_shapes.TryAdd(info, shape);
            }
            return shape;
        }

        HkShape CreateShape(MyModel model, HkShapeType shapeType)
        {
            if (model.HavokCollisionShapes != null && model.HavokCollisionShapes.Length > 0)
            {
                HkShape sh;
                if (model.HavokCollisionShapes.Length == 1)
                {
                    sh = model.HavokCollisionShapes[0];
                    sh.AddReference();
                }
                else
                {
                    sh = new HkListShape(model.HavokCollisionShapes,HkReferencePolicy.None);
                }
                return sh;          
            }

            switch(shapeType)
            {
                case HkShapeType.Box:
                    Vector3 halfExtents = (model.BoundingBox.Max - model.BoundingBox.Min) / 2;
                    return new HkBoxShape(Vector3.Max(halfExtents-0.05f, new Vector3(0.025f)), 0.02f);
                    break;

                case HkShapeType.Sphere:
                    return new HkSphereShape(model.BoundingSphere.Radius);
                    break;

                case HkShapeType.ConvexVertices:
                    List<Vector3> verts = new List<Vector3>();
                    for (int i = 0; i < model.GetVerticesCount(); i++)
                    {
                        verts.Add(model.GetVertex(i));
                    }

                    return new HkConvexVerticesShape(verts.GetInternalArray(), verts.Count, true, 0.1f);
                    break;
            }
            throw new InvalidOperationException("This shape is not supported");
        }

        MyDebrisBaseDescription m_desc = new MyDebrisBaseDescription();
        int m_voxelDebrisModelIndex = 0;
        int m_debrisModelIndex = 0;

        public override void LoadData()
        {
            MyDebug.AssertDebug(Static == null);
            m_positionBuffer = new List<Vector3D>(MyDebrisConstants.APPROX_NUMBER_OF_DEBRIS_OBJECTS_PER_MODEL_EXPLOSION);
            m_voxelDebrisOffsets = new List<Vector3>(MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT_3);
            m_desc.LifespanMinInMiliseconds = MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_LIFESPAN_MIN_IN_MILISECONDS;
            m_desc.LifespanMaxInMiliseconds = MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_LIFESPAN_MAX_IN_MILISECONDS;
            m_desc.OnCloseAction = OnDebrisClosed;

            GenerateVoxelDebrisPositionOffsets(m_voxelDebrisOffsets);
            Static = this;
        }

        private void OnDebrisClosed(MyDebrisBase obj)
        {
            m_debrisCount--;
        }

        protected override void UnloadData()
        {
            if (Static == null)
                return;
            MyDebug.AssertDebug(Static != null);

            foreach (var shape in m_shapes)
            {
                shape.Value.RemoveReference();
            }
            m_shapes.Clear();

            m_positionBuffer = null;
            Static = null;
        }

        public void CreateDirectedDebris(Vector3 sourceWorldPosition,
                                         Vector3 offsetDirection,
                                         float minSourceDistance,
                                         float maxSourceDistance,
                                         float minDeviationAngle,
                                         float maxDeviationAngle,
                                         int debrisPieces,
                                         float scale,
                                         float initialSpeed)
        {
            MyDebug.AssertDebug(debrisPieces > 0);
            for (int i = 0; i < debrisPieces; ++i)
            {
                var newObj = CreateRandomDebris();
                if (newObj == null)
                {
                    break; // no point in continuing
                }

                float dist = MyUtils.GetRandomFloat(minSourceDistance, maxSourceDistance);
                float angleX = MyUtils.GetRandomFloat(minDeviationAngle, maxDeviationAngle);
                float angleY = MyUtils.GetRandomFloat(minDeviationAngle, maxDeviationAngle);
                var rotation = Matrix.CreateRotationX(angleX) * Matrix.CreateRotationY(angleY);
                var deviatedDir = Vector3.Transform(offsetDirection, rotation);
                var startPos = sourceWorldPosition + deviatedDir * dist;
                var initialVelocity = deviatedDir * initialSpeed;
                newObj.Debris.Start(startPos, initialVelocity, scale);
            }
        }

        public void CreateDirectedDebris(Vector3 sourceWorldPosition,
                                         Vector3 offsetDirection,
                                         float minSourceDistance,
                                         float maxSourceDistance,
                                         float minDeviationAngle,
                                         float maxDeviationAngle,
                                         int debrisPieces,
                                         float initialSpeed,
                                         float scale,
                                         MyVoxelMaterialDefinition material)
        {
            ProfilerShort.Begin("Create directed debris");
            MyDebug.AssertDebug(debrisPieces > 0);
            for (int i = 0; i < debrisPieces; ++i)
            {
                var newObj = CreateVoxelDebris();
                if (newObj == null)
                {
                    break; // no point in continuing
                }

                float dist = MyUtils.GetRandomFloat(minSourceDistance, maxSourceDistance);
                float angleX = MyUtils.GetRandomFloat(minDeviationAngle, maxDeviationAngle);
                float angleY = MyUtils.GetRandomFloat(minDeviationAngle, maxDeviationAngle);
                var rotation = Matrix.CreateRotationX(angleX) * Matrix.CreateRotationY(angleY);
                var deviatedDir = Vector3.Transform(offsetDirection, rotation);
                var startPos = sourceWorldPosition + deviatedDir * dist;
                var initialVelocity = deviatedDir * initialSpeed;
                (newObj.Debris as MyDebrisVoxel.MyDebrisVoxelLogic).Start(startPos, initialVelocity, scale, material);
            }
            ProfilerShort.End();
        }

        public void CreateExplosionDebris(ref BoundingSphereD explosionSphere, MyEntity entity)
        {
            BoundingBoxD bbox = entity.PositionComp.WorldAABB;
            CreateExplosionDebris(ref explosionSphere, entity, ref bbox);
        }

        public void CreateExplosionDebris(ref BoundingSphereD explosionSphere, MyEntity entity, ref BoundingBoxD bb, float scaleMultiplier = 1.0f, bool applyVelocity = true)
        {
            var offsetDir = MyUtils.GetRandomVector3Normalized();
            float offsetDist = MyUtils.GetRandomFloat(0.0f, (float)explosionSphere.Radius);
            GeneratePositions(bb, m_positionBuffer);
            //float scale = Math.Max(explosionSphere.Radius / m_positionBuffer.Count, 0.35f);
            float scale = Math.Max((float)explosionSphere.Radius, 0.35f) * scaleMultiplier;
            foreach (Vector3D positionInWorldSpace in m_positionBuffer)
            {
                var newObj = CreateRandomDebris();
                if (newObj == null)
                {
                    break; // no point in continuing
                }

                var velocity = applyVelocity ? MyUtils.GetRandomVector3Normalized() *
                    MyUtils.GetRandomFloat(MyDebrisConstants.EXPLOSION_DEBRIS_INITIAL_SPEED_MIN,
                    MyDebrisConstants.EXPLOSION_DEBRIS_INITIAL_SPEED_MAX) :
                    Vector3.Zero;
                newObj.Debris.Start(positionInWorldSpace, velocity, scale);
            }
        }

        public void CreateExplosionDebris(ref BoundingSphereD explosionSphere, float voxelsCountInPercent, MyVoxelMaterialDefinition voxelMaterial, MyVoxelBase voxelMap)
        {
            MyDebug.AssertDebug((voxelsCountInPercent >= 0.0f) && (voxelsCountInPercent <= 1.0f));
            MyDebug.AssertDebug(explosionSphere.Radius > 0);

            ProfilerShort.Begin("CreateExplosionDebris");

            ProfilerShort.Begin("Matrices");
            //  This matrix will rotate all newly created debrises, so they won't apper as alligned with coordinate system
            MatrixD randomRotationMatrix = MatrixD.CreateRotationX(MyUtils.GetRandomRadian()) *
                                          MatrixD.CreateRotationY(MyUtils.GetRandomRadian());

            float highScale = MathHelper.Clamp((float)explosionSphere.Radius * m_debrisScaleUpper, 0, m_debrisScaleClamp);
            float lowScale = highScale * (m_debrisScaleLower / m_debrisScaleUpper);

            int objectsToGenerate = (int)(m_voxelDebrisOffsets.Count * voxelsCountInPercent);
            ProfilerShort.End();

            ProfilerShort.Begin("m_positionOffsets");
            const float SPHERE_FIT_CUBE_SCALE = 1 / 1.73f; // Resize sphere to fit inside cube
            int debrisCount = m_voxelDebrisOffsets.Count;
            //float debrisScale = Math.Max(explosionSphere.Radius / debrisCount, 0.2f);
            float debrisScale = Math.Max((float)explosionSphere.Radius, 0.2f);
            for (int i = 0; i < debrisCount; i++)
            {
                MyDebrisVoxel newObj = CreateVoxelDebris();
                if (newObj == null)
                {
                    break; // no point in continuing
                }

                Vector3D position = m_voxelDebrisOffsets[i] * (float)explosionSphere.Radius * SPHERE_FIT_CUBE_SCALE;
                Vector3D.Transform(ref position, ref randomRotationMatrix, out position);
                position += explosionSphere.Center;

                var initialVelocity = MyUtils.GetRandomVector3Normalized();
                if (initialVelocity == Vector3.Zero)
                    continue;
                initialVelocity *= MyUtils.GetRandomFloat(MyDebrisConstants.EXPLOSION_DEBRIS_INITIAL_SPEED_MIN,
                                                               MyDebrisConstants.EXPLOSION_DEBRIS_INITIAL_SPEED_MAX);
                (newObj.Debris as MyDebrisVoxel.MyDebrisVoxelLogic).Start(position, initialVelocity, debrisScale, voxelMaterial);

            }
            ProfilerShort.End();

            ProfilerShort.End();
        }

        private Vector3 GetDirection(Vector3 position, Vector3 sphereCenter)
        {
            Vector3 dist = position - sphereCenter;
            if (dist.IsValid() && MyUtils.HasValidLength(dist))
                return Vector3.Normalize(dist);
            else
                return MyUtils.GetRandomVector3Normalized();
        }

        private void GeneratePositions(BoundingBoxD boundingBox, List<Vector3D> positionBuffer)
        {
            positionBuffer.Clear();

            Vector3D minMax = boundingBox.Max - boundingBox.Min;
            var product = minMax.X * minMax.Y * minMax.Z;

            var a3 = MyDebrisConstants.APPROX_NUMBER_OF_DEBRIS_OBJECTS_PER_MODEL_EXPLOSION / product;

            var a = Math.Pow(a3, 1f / 3.0f);

            Vector3D minMaxScaled = minMax * a;

            int maxX = (int)Math.Ceiling(minMaxScaled.X);
            int maxY = (int)Math.Ceiling(minMaxScaled.Y);
            int maxZ = (int)Math.Ceiling(minMaxScaled.Z);

            Vector3D offset = new Vector3D(minMax.X / maxX, minMax.Y / maxY, minMax.Z / maxZ);

            Vector3D origin = boundingBox.Min + 0.5 * offset;

            for (int x = 0; x < maxX; x++)
            {
                for (int y = 0; y < maxY; y++)
                {
                    for (int z = 0; z < maxZ; z++)
                    {
                        Vector3D pos = origin + new Vector3D(x * offset.X, y * offset.Y, z * offset.Z);
                        positionBuffer.Add(pos);
                    }
                }
            }
        }

        //  Prepare offset positions for explosion debris voxels
        private void GenerateVoxelDebrisPositionOffsets(List<Vector3> offsetBuffer)
        {
            offsetBuffer.Clear();

            // Normalized size should be 1, but we need to take into account size of debris
            // size of debris is 30% of explosion radius at max
            const float normalizedSize = 0.7f;
            Vector3 origin = new Vector3(-normalizedSize);

            // calculate spacing between debris (from -1 to 1)
            const float spacing = normalizedSize * 2.0f / (MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT - 1);

            for (int x = 0; x < MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT; x++)
            {
                for (int y = 0; y < MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT; y++)
                {
                    for (int z = 0; z < MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_OFFSET_COUNT; z++)
                    {
                        Vector3 pos = origin + new Vector3(x * spacing, y * spacing, z * spacing);
                        offsetBuffer.Add(pos);
                    }
                }
            }
        }

        public static string GetRandomDebrisModel()
        {
            return MyUtils.GetRandomItem(m_debrisModels);
        }

        public static string GetRandomDebrisVoxel()
        {
            return MyUtils.GetRandomItem(m_debrisVoxels);
        }

        private MyDebrisVoxel CreateVoxelDebris()
        {
            if (m_debrisCount > MaxDebrisCount)
                return null;
            m_desc.ScaleMin = MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_INITIAL_SCALE_MIN;
            m_desc.ScaleMax = MyDebrisConstants.EXPLOSION_VOXEL_DEBRIS_INITIAL_SCALE_MAX;
            var newObj = new MyDebrisVoxel();
            m_desc.Model = m_debrisVoxels[m_voxelDebrisModelIndex];
            m_voxelDebrisModelIndex++;
            m_voxelDebrisModelIndex %= m_debrisVoxels.Length;

            newObj.Debris.Init(m_desc);
            m_debrisCount++;
            return newObj;
        }

        private MyDebrisBase CreateRandomDebris()
        {
            if (m_debrisCount > MaxDebrisCount)
                return null;
            var debris = (MyDebrisBase)CreateDebris(m_debrisModels[m_debrisModelIndex]);
            m_debrisModelIndex++;
            m_debrisModelIndex %= m_debrisModels.Length;
            return debris;
        }


        public MyEntity CreateDebris(string model)
        {
            m_desc.ScaleMin = MyDebrisConstants.EXPLOSION_MODEL_DEBRIS_INITIAL_SCALE_MIN;
            m_desc.ScaleMax = MyDebrisConstants.EXPLOSION_MODEL_DEBRIS_INITIAL_SCALE_MAX;

            var newObj = new MyDebrisBase();
            m_desc.Model = model;
            newObj.Debris.Init(m_desc);
            m_debrisCount++;
            m_desc.LifespanMinInMiliseconds = MyDebrisConstants.EXPLOSION_MODEL_DEBRIS_LIFESPAN_MIN_IN_MILISECONDS;
            m_desc.LifespanMaxInMiliseconds = MyDebrisConstants.EXPLOSION_MODEL_DEBRIS_LIFESPAN_MAX_IN_MILISECONDS;
            return newObj;
        }
    }
}
