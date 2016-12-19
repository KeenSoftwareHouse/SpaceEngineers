using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using VRage.ModAPI;
using VRage.Game.Entity;
using Sandbox.Game.AI;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.WorldEnvironment;
using VRage.Profiler;
using VRage.Voxels;
using VRage.Utils;

namespace Sandbox.Engine.Voxels
{
    #region Shapes
    public abstract partial class MyShape
    {
        protected MatrixD m_transformation = MatrixD.Identity;
        protected MatrixD m_inverse        = MatrixD.Identity;
        protected bool    m_inverseIsDirty = false;

        public MatrixD Transformation
        {
            get { return m_transformation; }
            set
            {
                m_transformation = value;
                m_inverseIsDirty = true;
            }
        }

        public MatrixD InverseTransformation
        {
            get
            {
                if (m_inverseIsDirty) MatrixD.Invert(ref m_transformation, out m_inverse);
                return m_inverse;
            }
        }

        public abstract BoundingBoxD GetWorldBoundaries();
        public abstract BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition);

        /// <summary>
        /// Gets volume of intersection of shape and voxel
        /// </summary>
        /// <param name="voxelPosition">Left bottom point of voxel</param>
        /// <returns>Normalized volume of intersection</returns>
        public abstract float GetVolume(ref Vector3D voxelPosition);

        /// <returns>Recomputed density value from signed distance</returns>
        protected float SignedDistanceToDensity(float signedDistance)
        {
            const float TRANSITION_SIZE = MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            const float NORMALIZATION_CONSTANT = 1 / (2 * MyVoxelConstants.VOXEL_SIZE_IN_METRES);
            return MathHelper.Clamp(-signedDistance, -TRANSITION_SIZE, TRANSITION_SIZE) * NORMALIZATION_CONSTANT + 0.5f;
        }

        public abstract void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex);
        public abstract void SendCutOutRequest(MyVoxelBase voxelbool);
        public virtual void SendDrillCutOutRequest(MyVoxelBase voxel, bool damage = false) { }
        public abstract void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex);
    }

    public partial  class MyShapeBox : MyShape
    {
        public BoundingBoxD Boundaries;

        public override BoundingBoxD GetWorldBoundaries()
        {
            return Boundaries.TransformFast(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return Boundaries.TransformFast(newTransformation);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty)
            {
                m_inverse = MatrixD.Invert(m_transformation);
                m_inverseIsDirty = false;
            }

            voxelPosition = Vector3D.Transform(voxelPosition, m_inverse);

            var center = Boundaries.Center;

            var boxD = Vector3.Abs(voxelPosition - center) - (center - Boundaries.Min);
            return SignedDistanceToDensity((float)boxD.Max());
        }

        public override void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationBox(Boundaries, Transformation, newMaterialIndex, OperationType.Paint);
        }

        public override void SendCutOutRequest(MyVoxelBase voxel)
        {
            voxel.RequestVoxelOperationBox(Boundaries, Transformation, 0, OperationType.Cut);
        }

        public override void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationBox(Boundaries, Transformation, newMaterialIndex, OperationType.Fill);
        }
    }

    public partial class MyShapeSphere : MyShape
    {
        public Vector3D Center; // in World space
        public float    Radius;

        public override BoundingBoxD GetWorldBoundaries()
        {
            //return new BoundingBoxD(Center - Radius, Center + Radius);
            var bbox = new BoundingBoxD(Center - Radius, Center + Radius);
            return bbox.TransformFast(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            return new BoundingBoxD(targetPosition - Radius, targetPosition + Radius);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty) { MatrixD.Invert(ref m_transformation, out m_inverse); m_inverseIsDirty = false; }
            Vector3D.Transform(ref voxelPosition, ref m_inverse, out voxelPosition);
            float dist = (float)(voxelPosition - Center).Length();
            float diff = dist - Radius;
            return SignedDistanceToDensity(diff);
        }

        public override void SendDrillCutOutRequest(MyVoxelBase voxel, bool damage = false)
        {
            voxel.RequestVoxelCutoutSphere(Center, Radius, false, damage);
        }
        public override void SendCutOutRequest(MyVoxelBase voxel)
        {
            voxel.RequestVoxelOperationSphere(Center, Radius, 0, OperationType.Cut);
        }

        public override void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationSphere(Center, Radius, newMaterialIndex, OperationType.Paint);
        }

        public override void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationSphere(Center, Radius, newMaterialIndex, OperationType.Fill);
        }
    }

    public class MyShapeEllipsoid : MyShape
    {
        private BoundingBoxD m_boundaries;
        private Matrix m_scaleMatrix = Matrix.Identity;
        private Matrix m_scaleMatrixInverse = Matrix.Identity;

        private Vector3 m_radius;
        public Vector3 Radius
        {
            get { return m_radius; }
            set
            {
                m_radius = value;

                m_scaleMatrix = Matrix.CreateScale(m_radius);
                m_scaleMatrixInverse = Matrix.Invert(m_scaleMatrix);

                m_boundaries = new BoundingBoxD(-Radius, Radius);
            }
        }

        public BoundingBoxD Boundaries
        {
            get { return m_boundaries; }
        }


        public override BoundingBoxD GetWorldBoundaries()
        {
            return m_boundaries.TransformFast(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return m_boundaries.TransformFast(newTransformation);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty)
            {
                m_inverse = MatrixD.Invert(m_transformation);
                m_inverseIsDirty = false;
            }

            // Local voxel position
            voxelPosition = Vector3D.Transform(voxelPosition, m_inverse);
            // Local voxel position in unit sphere space
            Vector3 voxelInUnitSphere = Vector3.Transform(voxelPosition, m_scaleMatrixInverse);
            // Normalize the position so we have point on unit sphere.
            voxelInUnitSphere.Normalize();
            // Transform back to local system using scale matrix
            Vector3 localPointOnEllipsoid = Vector3.Transform(voxelInUnitSphere, m_scaleMatrix);

            float diff = (float)(voxelPosition.Length() - localPointOnEllipsoid.Length());
            return SignedDistanceToDensity(diff);
        }

        public override void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationElipsoid(Radius, Transformation, newMaterialIndex, OperationType.Paint);
        }

        public override void SendCutOutRequest(MyVoxelBase voxel)
        {
            voxel.RequestVoxelOperationElipsoid(Radius, Transformation, 0, OperationType.Cut);
        }

        public override void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationElipsoid(Radius, Transformation, newMaterialIndex, OperationType.Fill);
        }
    }

    public partial class MyShapeRamp : MyShape
    {
        public BoundingBoxD Boundaries;
        public Vector3D     RampNormal; // normal of the sloped plane
        public double       RampNormalW;

        public override BoundingBoxD GetWorldBoundaries()
        {
            return Boundaries.TransformFast(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return Boundaries.TransformFast(newTransformation);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty)
            {
                m_inverse = MatrixD.Invert(m_transformation);
                m_inverseIsDirty = false;
            }

            voxelPosition = Vector3D.Transform(voxelPosition, m_inverse);

            var boxD   = Vector3.Abs(voxelPosition) - Boundaries.HalfExtents;
            var planeD = Vector3D.Dot(voxelPosition, RampNormal) + RampNormalW;

            return SignedDistanceToDensity((float)Math.Max(boxD.Max(), -planeD));
        }

        public override void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationRamp(Boundaries, RampNormal, RampNormalW, Transformation, newMaterialIndex, OperationType.Paint);
        }

        public override void SendCutOutRequest(MyVoxelBase voxel)
        {
            voxel.RequestVoxelOperationRamp(Boundaries, RampNormal, RampNormalW, Transformation, 0, OperationType.Cut);
        }

        public override void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationRamp(Boundaries, RampNormal, RampNormalW, Transformation, newMaterialIndex, OperationType.Fill);
        }
    }

    public partial class MyShapeCapsule : MyShape
    {
        public Vector3D A;
        public Vector3D B;
        public float    Radius;

        public override BoundingBoxD GetWorldBoundaries()
        {
            var bbox = new BoundingBoxD(A - Radius, B + Radius);
            return bbox.TransformFast(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            var bbox = new BoundingBoxD(A - Radius, B + Radius);
            return bbox.TransformFast(newTransformation);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty)
            {
                m_inverse = MatrixD.Invert(m_transformation);
                m_inverseIsDirty = false;
            }

            voxelPosition = Vector3D.Transform(voxelPosition, m_inverse);

            var pa = voxelPosition - A;
            var ba = B - A;
            var h  = MathHelper.Clamp(pa.Dot(ref ba) / ba.LengthSquared(), 0.0, 1.0);
            var sd = (float)((pa - ba * h).Length() - Radius);
            return SignedDistanceToDensity(sd);
        }

        public override void SendPaintRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationCapsule(A, B, Radius, Transformation, newMaterialIndex, OperationType.Paint);
        }

        public override void SendCutOutRequest(MyVoxelBase voxel)
        {
            voxel.RequestVoxelOperationCapsule(A, B, Radius, Transformation, 0, OperationType.Cut);
        }

        public override void SendFillRequest(MyVoxelBase voxel, byte newMaterialIndex)
        {
            voxel.RequestVoxelOperationCapsule(A, B, Radius, Transformation, newMaterialIndex, OperationType.Fill);
        }
    }
    #endregion

    public static class MyVoxelGenerator
    {
        const int CELL_SIZE = 16;
        const int VOXEL_CLAMP_BORDER_DISTANCE = 2;

        private static MyStorageData m_cache = new MyStorageData();
        static List<MyEntity> m_overlapList = new List<MyEntity>();

        public static void MakeCrater(MyVoxelBase voxelMap, BoundingSphereD sphere, Vector3 direction, MyVoxelMaterialDefinition material)
        {
            if (voxelMap == null)
            {
                return;
            }

            if (voxelMap.Storage == null)
            {
                MyLog.Default.WriteLine("Storage shouldn't be null for Voxel:" + voxelMap);
            }

            ProfilerShort.Begin("MakeCrater");

            Vector3 normal = voxelMap.RootVoxel != null ? Vector3.Normalize(sphere.Center - voxelMap.RootVoxel.WorldMatrix.Translation) : Vector3.Normalize(sphere.Center - voxelMap.WorldMatrix.Translation);

            Vector3I minCorner, maxCorner;
            {
                Vector3D sphereMin = sphere.Center - (sphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES) * 1.3f;
                Vector3D sphereMax = sphere.Center + (sphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES) * 1.3f;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref sphereMin, out minCorner);
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref sphereMax, out maxCorner);
            }

            voxelMap.Storage.ClampVoxelCoord(ref minCorner);
            voxelMap.Storage.ClampVoxelCoord(ref maxCorner);

            Vector3I worldMinCorner = minCorner + voxelMap.StorageMin;
            Vector3I worldMaxCorner = maxCorner + voxelMap.StorageMin;

            //  We are tracking which voxels were changed, so we can invalidate only needed cells in the cache
            bool changed = false;
            ProfilerShort.Begin("Reading cache");
            m_cache.Resize(minCorner, maxCorner);

            voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, ref worldMinCorner, ref worldMaxCorner);

            ProfilerShort.End();

            ProfilerShort.Begin("Changing cache");
            int removedVoxelContent = 0;
            Vector3I tempVoxelCoord;
            Vector3I cachePos = (maxCorner - minCorner) / 2;

            byte oldMaterial = m_cache.Material(ref cachePos);

            float digRatio = 1 - Vector3.Dot(normal, direction);

            Vector3 newCenter = sphere.Center - normal * (float)sphere.Radius * 1.1f;//0.9f;
            float sphRadA = (float)(sphere.Radius * 1.5f);
            float sphRadSqA = (float)(sphRadA * sphRadA);
            float voxelSizeHalfTransformedPosA = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (2 * sphRadA + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);
            float voxelSizeHalfTransformedNegA = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (-2 * sphRadA + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);

            Vector3 newDelCenter = newCenter + normal * (float)sphere.Radius * (0.7f + digRatio) + direction * (float)sphere.Radius * 0.65f;
            float sphRadD = (float)(sphere.Radius);
            float sphRadSqD = (float)(sphRadD * sphRadD);
            float voxelSizeHalfTransformedPosD = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (2 * sphRadD + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);
            float voxelSizeHalfTransformedNegD = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (-2 * sphRadD + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);

            Vector3 newSetCenter = newCenter + normal * (float)sphere.Radius * (digRatio) + direction * (float)sphere.Radius * 0.3f;
            float sphRadS = (float)(sphere.Radius * 0.1f);
            float sphRadSqS = (float)(sphRadS * sphRadS);
            float voxelSizeHalfTransformedPosS = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * (2 * sphRadS + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF);

            for (tempVoxelCoord.Z = minCorner.Z, cachePos.Z = 0; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++, ++cachePos.Z)
            {
                for (tempVoxelCoord.Y = minCorner.Y, cachePos.Y = 0; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++, ++cachePos.Y)
                {
                    for (tempVoxelCoord.X = minCorner.X, cachePos.X = 0; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++, ++cachePos.X)
                    {
                        Vector3D voxelPosition;
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner, ref tempVoxelCoord, out voxelPosition);

                        byte originalContent = m_cache.Content(ref cachePos);

                        //Add sphere
                        if (originalContent != MyVoxelConstants.VOXEL_CONTENT_FULL)
                        {

                            float addDist = (float)(voxelPosition - newCenter).LengthSquared();
                            float addDiff = (float)(addDist - sphRadSqA);

                        byte newContent;
                            if (addDiff > voxelSizeHalfTransformedPosA)
                        {
                            newContent = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                        }
                            else if (addDiff < voxelSizeHalfTransformedNegA)
                        {
                            newContent = MyVoxelConstants.VOXEL_CONTENT_FULL;
                        }
                        else
                        {
                                float value = (float)Math.Sqrt(addDist + sphRadSqA - 2 * sphRadA * Math.Sqrt(addDist));
                                if (addDiff < 0) { value = -value; }
                            //  This formula will work even if diff is positive or negative
                                newContent = (byte)(MyVoxelConstants.VOXEL_ISO_LEVEL - value / MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * MyVoxelConstants.VOXEL_ISO_LEVEL);
                        }

                            if (newContent > originalContent)
                        {
                            if (material != null)
                            {
                                    m_cache.Material(ref cachePos, oldMaterial);
                            }

                            changed = true;
                            m_cache.Content(ref cachePos, newContent);
                        }
                        }

                        //Delete sphere
                        float delDist = (float)(voxelPosition - newDelCenter).LengthSquared();
                        float delDiff = (float)(delDist - sphRadSqD);

                        byte contentToRemove;
                        if (delDiff > voxelSizeHalfTransformedPosD)
                        {
                            contentToRemove = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                        }
                        else if (delDiff < voxelSizeHalfTransformedNegD)
                        {
                            contentToRemove = MyVoxelConstants.VOXEL_CONTENT_FULL;
                        }
                        else
                        {
                            float value = (float)Math.Sqrt(delDist + sphRadSqD - 2 * sphRadD * Math.Sqrt(delDist));
                            if (delDiff < 0) { value = -value; }
                            //  This formula will work even if diff is positive or negative
                            contentToRemove = (byte)(MyVoxelConstants.VOXEL_ISO_LEVEL - value / MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * MyVoxelConstants.VOXEL_ISO_LEVEL);
                        }

                        originalContent = m_cache.Content(ref cachePos);

                        if (originalContent > MyVoxelConstants.VOXEL_CONTENT_EMPTY && contentToRemove > MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                        {
                            changed = true;

                            int newVal = originalContent - contentToRemove;
                            if (newVal < MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                                newVal = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                            m_cache.Content(ref cachePos, (byte)newVal);

                            removedVoxelContent += originalContent - newVal;
                        }

                        //Set material

                        float setDist = (float)(voxelPosition - newSetCenter).LengthSquared();
                        float setDiff = (float)(setDist - sphRadSqS);

                        if (setDiff <= MyVoxelConstants.VOXEL_SIZE_IN_METRES * 1.5f)  // could be VOXEL_SIZE_IN_METRES_HALF, but we want to set material in empty cells correctly
                        {

                            MyVoxelMaterialDefinition originalMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref cachePos));

                            // Change the material:
                            // - always on boundaries between material and nothing
                            // - smoothly on inner boundaries
                            MyVoxelMaterialDefinition newMaterial = material;
                            if (setDiff > 0)
                            {
                                byte content = m_cache.Content(ref cachePos);
                                if (content == MyVoxelConstants.VOXEL_CONTENT_FULL)
                                    newMaterial = originalMaterial;
                                if (setDiff >= voxelSizeHalfTransformedPosS && content != MyVoxelConstants.VOXEL_CONTENT_EMPTY)  // set material behind boundary only for empty voxels
                                    newMaterial = originalMaterial;
                            }

                            if (originalMaterial == newMaterial)
                            {
                                continue;
                            }
                            if (newMaterial != null)
                            {
                                m_cache.Material(ref cachePos, newMaterial.Index);
                            }
                            changed = true;
                        }

                        float dist = (float)(voxelPosition - newCenter).LengthSquared();
                        float diff = (float)(dist - sphRadSqA);

                        if (diff <= 0f)
                        {
                            originalContent = m_cache.Content(ref cachePos);
                            if (originalContent > MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                            {
                                bool wrinkled = m_cache.WrinkleVoxelContent(ref cachePos, MyVoxelConstants.DEFAULT_WRINKLE_WEIGHT_ADD, MyVoxelConstants.DEFAULT_WRINKLE_WEIGHT_REMOVE);
                                if (wrinkled)
                                    changed = true;
                            }
                        }
                    }
                }
            }
            ProfilerShort.End();

            if (changed)
            {
                ProfilerShort.Begin("RemoveSmallVoxelsUsingChachedVoxels");
                RemoveSmallVoxelsUsingChachedVoxels();
                ProfilerShort.BeginNextBlock("Writing cache");
                minCorner += voxelMap.StorageMin;
                maxCorner += voxelMap.StorageMin;
                voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, ref minCorner, ref maxCorner);
                MyShapeSphere sphereShape = new MyShapeSphere();
                sphereShape.Center = sphere.Center;
                sphereShape.Radius = (float)(sphere.Radius*1.5);
                OnVoxelChanged(OperationType.Cut, voxelMap, sphereShape);
                ProfilerShort.End();
            }

            ProfilerShort.End();
        }

        public static void RequestPaintInShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape, byte materialIdx)
        {
            var map = voxelMap as MyVoxelBase;
            var shape = voxelShape as MyShape;
            if (map != null && shape != null)
            {
                shape.SendPaintRequest(map, materialIdx);
            }
        }

        public static void RequestFillInShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape, byte materialIdx)
        {
            var map = voxelMap as MyVoxelBase;
            var shape = voxelShape as MyShape;
            if (map != null && shape != null)
            {
                shape.SendFillRequest(map, materialIdx);
            }
        }

        public static void RequestCutOutShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape)
        {
            var map = voxelMap as MyVoxelBase;
            var shape = voxelShape as MyShape;
            if (map != null && shape != null)
            {
                shape.SendCutOutRequest(map);
            }
        }

        public static void CutOutShapeWithProperties(
            MyVoxelBase voxelMap,
            MyShape shape,
            out float voxelsCountInPercent,
            out MyVoxelMaterialDefinition voxelMaterial,
            Dictionary<MyVoxelMaterialDefinition, int> exactCutOutMaterials = null,
            bool updateSync = false,
            bool onlyCheck = false,
            bool applyDamageMaterial = false,
            bool onlyApplyMaterial = false)
        {
            if (MySession.Static.EnableVoxelDestruction == false)
            {
                voxelsCountInPercent = 0;
                voxelMaterial = null;
                return;
            }

            ProfilerShort.Begin("MyVoxelGenerator::CutOutShapeWithProperties()");

            int originalSum = 0;
            int removedSum = 0;
            bool materials = exactCutOutMaterials != null;

            // Bring the shape into voxel space.
            var oldTranmsform = shape.Transformation;
            var newTransf = oldTranmsform * voxelMap.PositionComp.WorldMatrixInvScaled;
            newTransf.Translation += voxelMap.SizeInMetresHalf;
            shape.Transformation = newTransf;

            // This boundary should now be in our local space
            var bbox = shape.GetWorldBoundaries();

            Vector3I minCorner, maxCorner;
            ComputeShapeBounds(voxelMap, ref bbox, Vector3.Zero, voxelMap.Storage.Size, out minCorner, out maxCorner);

            bool readMaterial = exactCutOutMaterials != null || applyDamageMaterial;

            var cacheMin = minCorner - 1;
            var cacheMax = maxCorner + 1;

            //try on making the read/write cell alligned see MyOctreeStorage.WriteRange - Micro octree leaf
            /*const int SHIFT = 4;
            const int REM = (1 << SHIFT) - 1;
            const int MASK = ~REM;
            cacheMin &= MASK;
            cacheMax = (cacheMax + REM) & MASK;*/

            voxelMap.Storage.ClampVoxelCoord(ref cacheMin);
            voxelMap.Storage.ClampVoxelCoord(ref cacheMax);
            m_cache.Resize(cacheMin, cacheMax);
            m_cache.ClearMaterials(0);

            // Advise that the read content shall be cached
            MyVoxelRequestFlags flags = MyVoxelRequestFlags.AdviseCache;
            voxelMap.Storage.ReadRange(m_cache, readMaterial ? MyStorageDataTypeFlags.ContentAndMaterial : MyStorageDataTypeFlags.Content, 0, ref cacheMin, ref cacheMax, ref flags);

            Vector3I center;
            if (materials)
            {
                center = m_cache.Size3D / 2;
                voxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref center));
            }
            else
            {
                center = (cacheMin + cacheMax) / 2;
                voxelMaterial = voxelMap.Storage.GetMaterialAt(ref center);
            }

            MyVoxelMaterialDefinition voxelMat = null;

            ProfilerShort.Begin("Main loop");
            Vector3I pos;
            for (pos.X = minCorner.X; pos.X <= maxCorner.X; ++pos.X)
                for (pos.Y = minCorner.Y; pos.Y <= maxCorner.Y; ++pos.Y)
                    for (pos.Z = minCorner.Z; pos.Z <= maxCorner.Z; ++pos.Z)
                    {
                        // get original amount
                        var relPos = pos - cacheMin;
                        var lin = m_cache.ComputeLinear(ref relPos);
                        var original = m_cache.Content(lin);

                        if (original == MyVoxelConstants.VOXEL_CONTENT_EMPTY) // if there is nothing to remove
                            continue;

                        Vector3D spos = (Vector3D)(pos - voxelMap.StorageMin) * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
                        var volume = shape.GetVolume(ref spos);

                        if (volume == 0f) // if there is no intersection
                            continue;

                        var maxRemove = (int)(volume * MyVoxelConstants.VOXEL_CONTENT_FULL);
                        var toRemove = maxRemove;// (int)(maxRemove * voxelMat.DamageRatio);
                        var newVal = Math.Max(original - toRemove, 0);//MathHelper.Clamp(original - toRemove, 0, original-maxRemove);
                        var removed = original - newVal;

                        if (!onlyCheck && !onlyApplyMaterial)
                            m_cache.Content(lin, (byte)newVal);

                        originalSum += original;
                        removedSum += removed;

                        var material = m_cache.Material(lin);
                        if (material != MyVoxelConstants.NULL_MATERIAL)
                        {
                            if (readMaterial)
                                voxelMat = MyDefinitionManager.Static.GetVoxelMaterialDefinition(material);

                            if (exactCutOutMaterials != null)
                            {
                                int value = 0;
                                exactCutOutMaterials.TryGetValue(voxelMat, out value);
                                value += (MyFakes.ENABLE_REMOVED_VOXEL_CONTENT_HACK ? (int)(removed * 3.9f) : removed);
                                exactCutOutMaterials[voxelMat] = value;
                            }

                            if (applyDamageMaterial && voxelMat.HasDamageMaterial && !onlyCheck)
                                m_cache.Material(lin, voxelMat.DamagedMaterialId);
                        }
                    }

            if (removedSum > 0 && updateSync && Sync.IsServer)
            {
                shape.SendDrillCutOutRequest(voxelMap, applyDamageMaterial);
            }

            ProfilerShort.BeginNextBlock("Write");

            if (removedSum > 0 && !onlyCheck)
            {
                //  Clear all small voxel that may have been created during explosion. They can be created even outside the range of
                //  explosion sphere, e.g. if you have three voxels in a row A, B, C, where A is 255, B is 60, and C is 255. During the
                //  explosion you change C to 0, so now we have 255, 60, 0. Than another explosion that will change A to 0, so we
                //  will have 0, 60, 0. But B was always outside the range of the explosion. So this is why we need to do -1/+1 and remove
                //  B voxels too.
                //!! TODO AR & MK : check if this is needed !!
                //RemoveSmallVoxelsUsingChachedVoxels();

                var dataTypeFlags = applyDamageMaterial ? MyStorageDataTypeFlags.ContentAndMaterial : MyStorageDataTypeFlags.Content;
                if (MyFakes.LOG_NAVMESH_GENERATION && MyAIComponent.Static.Pathfinding != null) MyAIComponent.Static.Pathfinding.GetPathfindingLog().LogStorageWrite(voxelMap, m_cache, dataTypeFlags, cacheMin, cacheMax);
                voxelMap.Storage.WriteRange(m_cache, dataTypeFlags, ref cacheMin, ref cacheMax);
            }
            ProfilerShort.End();


            voxelsCountInPercent = (originalSum > 0f) ? (float)removedSum / (float)originalSum : 0f;

            shape.Transformation = oldTranmsform;

            if (removedSum > 0)
                OnVoxelChanged(OperationType.Cut, voxelMap, shape);

            ProfilerShort.End();
        }

        public static ulong FillInShape(MyVoxelBase voxelMap, MyShape shape, byte materialIdx)
        {
            Vector3I minCorner, maxCorner, numCells;
            ulong retValue = 0;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);

            //voxel must be at least 1 m from side to be closed (e.g. without holes in it)
            minCorner = Vector3I.Max(Vector3I.One, minCorner);
            maxCorner = Vector3I.Max(minCorner, maxCorner);

            for (var itCells = new Vector3I_RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
            {
                Vector3I cellMinCorner, cellMaxCorner;
                GetCellCorners(ref minCorner, ref maxCorner, ref itCells, out cellMinCorner, out cellMaxCorner);

                Vector3I originalMinCorner = cellMinCorner;
                Vector3I originalMaxCorner = cellMaxCorner;

                voxelMap.Storage.ClampVoxelCoord(ref cellMinCorner, 0);
                voxelMap.Storage.ClampVoxelCoord(ref cellMaxCorner, 0);

                ClampingInfo minCornerClamping = CheckForClamping(originalMinCorner, cellMinCorner);
                ClampingInfo maxCornerClamping = CheckForClamping(originalMaxCorner, cellMaxCorner);

                m_cache.Resize(cellMinCorner, cellMaxCorner);
                voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, ref cellMinCorner, ref cellMaxCorner);

                ulong filledSum = 0;

                for (var it = new Vector3I_RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
                {
                    var relPos = it.Current - cellMinCorner; // get original amount
                    var original = m_cache.Content(ref relPos);

                    if (original == MyVoxelConstants.VOXEL_CONTENT_FULL) // if there is nothing to add
                        continue;

                    //if there was some claping, fill the clamp region with material
                    if ((it.Current.X == cellMinCorner.X && minCornerClamping.X) || (it.Current.X == cellMaxCorner.X && maxCornerClamping.X) ||
                        (it.Current.Y == cellMinCorner.Y && minCornerClamping.Y) || (it.Current.Y == cellMaxCorner.Y && maxCornerClamping.Y) ||
                        (it.Current.Z == cellMinCorner.Z && minCornerClamping.Z) || (it.Current.Z == cellMaxCorner.Z && maxCornerClamping.Z))
                    {
                        m_cache.Material(ref relPos, materialIdx);
                        continue;
                    }

                    Vector3D vpos;
                    MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner, ref it.Current, out vpos);
                    var volume = shape.GetVolume(ref vpos);


                    if (volume <= 0f) // there is nothing to fill
                        continue;

                    m_cache.Material(ref relPos, materialIdx); // set material

                    var toFill = (int)(volume * MyVoxelConstants.VOXEL_CONTENT_FULL);
                    long newVal = MathHelper.Clamp(original + toFill, 0, Math.Max(original, toFill));

                    m_cache.Content(ref relPos, (byte)newVal);
                    filledSum += (ulong)(newVal - original);
                }
                if (filledSum > 0)
                {
                    RemoveSmallVoxelsUsingChachedVoxels();
                    voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, ref cellMinCorner, ref cellMaxCorner);
                }

                retValue += filledSum;
            }

            if (retValue > 0)
                OnVoxelChanged(OperationType.Fill, voxelMap, shape);

            return retValue;
        }

        public static void PaintInShape(MyVoxelBase voxelMap, MyShape shape, byte materialIdx)
        {
            Vector3I minCorner, maxCorner, numCells;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);

            for (var itCells = new Vector3I_RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
            {
                Vector3I cellMinCorner, cellMaxCorner;
                GetCellCorners(ref minCorner, ref maxCorner, ref itCells, out cellMinCorner, out cellMaxCorner);

                m_cache.Resize(cellMinCorner, cellMaxCorner);
                voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, 0, ref cellMinCorner, ref cellMaxCorner);

                for (var it = new Vector3I_RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
                {
                    var relPos = it.Current - cellMinCorner;

                    Vector3D vpos;
                    MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner, ref it.Current, out vpos);
                    float volume = shape.GetVolume(ref vpos);
                    if (volume > 0.5f)
                        m_cache.Material(ref relPos, materialIdx); // set material
                }

                voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.Material, ref cellMinCorner, ref cellMaxCorner);
            }
        }

        public static ulong CutOutShape(MyVoxelBase voxelMap, MyShape shape)
        {
            if(MySession.Static.EnableVoxelDestruction == false)
            {
                return 0;
            }

            Vector3I minCorner, maxCorner, numCells;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);
            ulong changedVolumeAmount = 0;

            for (var itCells = new Vector3I_RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
            {
                Vector3I cellMinCorner, cellMaxCorner;
                GetCellCorners(ref minCorner, ref maxCorner, ref itCells, out cellMinCorner, out cellMaxCorner);

                var cacheMin = cellMinCorner - 1;
                var cacheMax = cellMaxCorner + 1;
                voxelMap.Storage.ClampVoxelCoord(ref cacheMin);
                voxelMap.Storage.ClampVoxelCoord(ref cacheMax);

                ulong removedSum = 0;
                m_cache.Resize(cacheMin, cacheMax);
                voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.Content, 0, ref cacheMin, ref cacheMax);

                for (var it = new Vector3I_RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
                {
                    var relPos = it.Current - cacheMin; // get original amount
                    var original = m_cache.Content(ref relPos);

                    if (original == MyVoxelConstants.VOXEL_CONTENT_EMPTY) // if there is nothing to remove
                        continue;

                    Vector3D vpos;
                    MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner, ref it.Current, out vpos);
                    var volume = shape.GetVolume(ref vpos);

                    if (volume == 0f) // if there is no intersection
                        continue;

                    var toRemove = (int)(MyVoxelConstants.VOXEL_CONTENT_FULL - (volume * MyVoxelConstants.VOXEL_CONTENT_FULL));
                    var newVal = Math.Min(toRemove, original);
                    ulong removed = (ulong)Math.Abs(original - newVal);

                    m_cache.Content(ref relPos, (byte)newVal);
                    removedSum += removed;
                }

                if (removedSum > 0)
                {
                    RemoveSmallVoxelsUsingChachedVoxels(); // must stay because of the around when filling voxels
                    voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.Content, ref cacheMin, ref cacheMax);
                }

                changedVolumeAmount += removedSum;
            }

            if (changedVolumeAmount > 0)
                OnVoxelChanged(OperationType.Cut, voxelMap, shape);

            return changedVolumeAmount;
        }

        struct ClampingInfo
        {
            public bool X;
            public bool Y;
            public bool Z;

            public ClampingInfo(bool X, bool Y, bool Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }

        private static ClampingInfo CheckForClamping(Vector3I originalValue, Vector3I clampedValue)
        {
            ClampingInfo ret = new ClampingInfo(false, false, false);
            if (originalValue.X != clampedValue.X)
            {
                ret.X = true;
            }

            if (originalValue.Y != clampedValue.Y)
            {
                ret.Y = true;
            }
            if (originalValue.Z != clampedValue.Z)
            {
                ret.Z = true;
            }
            return ret;
        }

        private static void RemoveSmallVoxelsUsingChachedVoxels()
        {
            ProfilerShort.Begin("MyVoxelGenerator::RemoveSmallVoxelsUsingChachedVoxels()");
            Vector3I voxel;
            var cacheSize = m_cache.Size3D;
            var sizeMinusOne = cacheSize - 1;
            for (voxel.X = 0; voxel.X < cacheSize.X; voxel.X++)
            {
                for (voxel.Y = 0; voxel.Y < cacheSize.Y; voxel.Y++)
                {
                    for (voxel.Z = 0; voxel.Z < cacheSize.Z; voxel.Z++)
                    {
                        //  IMPORTANT: When doing transformations on 'content' value, cast it to int always!!!
                        //  It's because you can easily forget that result will be negative and if you put negative into byte, it will
                        //  be overflown and you will be surprised by results!!
                        int content = m_cache.Content(ref voxel);

                        //  Check if this is small/invisible voxel (less than 127), but still not empty (more than 0)
                        if ((content > 0) && (content < MyVoxelConstants.VOXEL_ISO_LEVEL))
                        {
                            Vector3I neighborVoxel;
                            Vector3I neighborVoxelMin = voxel - 1;
                            Vector3I neighborVoxelMax = voxel + 1;
                            Vector3I.Clamp(ref neighborVoxelMin, ref Vector3I.Zero, ref sizeMinusOne, out neighborVoxelMin);
                            Vector3I.Clamp(ref neighborVoxelMax, ref Vector3I.Zero, ref sizeMinusOne, out neighborVoxelMax);

                            bool foundNonEmptyVoxel = false;
                            for (neighborVoxel.X = neighborVoxelMin.X; neighborVoxel.X <= neighborVoxelMax.X; neighborVoxel.X++)
                            {
                                for (neighborVoxel.Y = neighborVoxelMin.Y; neighborVoxel.Y <= neighborVoxelMax.Y; neighborVoxel.Y++)
                                {
                                    for (neighborVoxel.Z = neighborVoxelMin.Z; neighborVoxel.Z <= neighborVoxelMax.Z; neighborVoxel.Z++)
                                    {
                                        int neighborContent = m_cache.Content(ref neighborVoxel);

                                        //  Check if this is small/invisible voxel
                                        if (neighborContent >= MyVoxelConstants.VOXEL_ISO_LEVEL)
                                        {
                                            foundNonEmptyVoxel = true;
                                            goto END_NEIGHBOR_LOOP;
                                        }
                                    }
                                }
                            }

                        END_NEIGHBOR_LOOP:

                            if (foundNonEmptyVoxel == false)
                            {
                                m_cache.Content(ref voxel, MyVoxelConstants.VOXEL_CONTENT_EMPTY);
                            }
                        }
                    }
                }
            }
            ProfilerShort.End();
        }

        private static void ComputeShapeBounds(MyVoxelBase voxelMap,
            ref BoundingBoxD shapeAabb, Vector3D voxelMapMinCorner, Vector3I storageSize,
            out Vector3I voxelMin, out Vector3I voxelMax)
        {
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMapMinCorner, ref shapeAabb.Min, out voxelMin);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMapMinCorner, ref shapeAabb.Max, out voxelMax);
            voxelMin += voxelMap.StorageMin;
            voxelMax += voxelMap.StorageMin;

            voxelMax += 1; //what? why? Another hack of MK?

            storageSize -= 1;
            Vector3I.Clamp(ref voxelMin, ref Vector3I.Zero, ref storageSize, out voxelMin);
            Vector3I.Clamp(ref voxelMax, ref Vector3I.Zero, ref storageSize, out voxelMax);
        }

        private static void GetVoxelShapeDimensions(MyVoxelBase voxelMap, MyShape shape, out Vector3I minCorner, out Vector3I maxCorner, out Vector3I numCells)
        {
            {
                var bbox = shape.GetWorldBoundaries();
                ComputeShapeBounds(voxelMap,ref bbox, voxelMap.PositionLeftBottomCorner, voxelMap.Storage.Size, out minCorner, out maxCorner);
            }
            numCells = new Vector3I((maxCorner.X - minCorner.X) / CELL_SIZE, (maxCorner.Y - minCorner.Y) / CELL_SIZE, (maxCorner.Z - minCorner.Z) / CELL_SIZE);
        }

        private static void GetCellCorners(ref Vector3I minCorner, ref Vector3I maxCorner, ref Vector3I_RangeIterator it, out Vector3I cellMinCorner, out Vector3I cellMaxCorner)
        {
            cellMinCorner = new Vector3I(minCorner.X + it.Current.X * CELL_SIZE, minCorner.Y + it.Current.Y * CELL_SIZE, minCorner.Z + it.Current.Z * CELL_SIZE);
            cellMaxCorner = new Vector3I(Math.Min(maxCorner.X, cellMinCorner.X + CELL_SIZE), Math.Min(maxCorner.Y, cellMinCorner.Y + CELL_SIZE), Math.Min(maxCorner.Z, cellMinCorner.Z + CELL_SIZE));
        }

        private static void OnVoxelChanged(OperationType type, MyVoxelBase voxelMap, MyShape shape)
        {
            if (Sync.IsServer)
            {
            BoundingBoxD cutOutBox = shape.GetWorldBoundaries();
            cutOutBox.Inflate(0.25);

            MyEntities.GetElementsInBox(ref cutOutBox, m_overlapList);
            // Check static grids around (possible change to dynamic)
            if (/*MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL &&*/ MyFakes.ENABLE_BLOCKS_IN_VOXELS_TEST && MyStructuralIntegrity.Enabled)
            {
                foreach (var entity in m_overlapList)
                {
                    var grid = entity as MyCubeGrid;
                    if (grid != null && grid.IsStatic)
                    {
                        if (grid.Physics != null && grid.Physics.Shape != null)
                        {
                            grid.Physics.Shape.RecalculateConnectionsToWorld(grid.GetBlocks());
                        }

                        if (type == OperationType.Cut)
                            grid.TestDynamic = MyCubeGrid.MyTestDynamicReason.GridSplit;
                    }
                }

            }
            }
            var voxelPhysics = voxelMap as MyVoxelPhysics;
            MyPlanet planet = voxelPhysics != null ? voxelPhysics.Parent : voxelMap as MyPlanet;
            if (planet != null)
            {
                var planetEnvironment = planet.Components.Get<MyPlanetEnvironmentComponent>();
                if (planetEnvironment != null)
                {
                    var sectors = planetEnvironment.GetSectorsInRange(shape);
                    if (sectors != null)
                        foreach (var sector in sectors)
                            sector.DisableItemsInShape(shape);
                }
            }
            m_overlapList.Clear();
        }


    }
}
