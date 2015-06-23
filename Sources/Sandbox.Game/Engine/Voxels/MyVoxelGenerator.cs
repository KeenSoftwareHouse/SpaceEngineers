using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Engine.Voxels
{
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
            return MathHelper.Clamp(-signedDistance, -TRANSITION_SIZE, TRANSITION_SIZE) / (2f * TRANSITION_SIZE) + 0.5f;
        }

        public abstract void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex);
        public abstract void SendCutOutRequest(MySyncVoxel voxelSync);
        public virtual void SendDrillCutOutRequest(MySyncVoxel voxelSync){ }
        public abstract void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex);
    }

    public partial  class MyShapeBox : MyShape
    {
        public BoundingBoxD Boundaries;

        public override BoundingBoxD GetWorldBoundaries()
        {
            return Boundaries.Transform(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return Boundaries.Transform(newTransformation);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            if (m_inverseIsDirty)
            {
                m_inverse = MatrixD.Invert(m_transformation);
                m_inverseIsDirty = false;
            }

            voxelPosition = Vector3D.Transform(voxelPosition, m_inverse);

            var boxD = Vector3.Abs(voxelPosition) - Boundaries.HalfExtents;
            return SignedDistanceToDensity((float)boxD.Max());
        }

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintBox(Boundaries, Transformation, newMaterialIndex, MySyncVoxel.PaintType.Paint);
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync)
        {
            voxelSync.RequestVoxelPaintBox(Boundaries, Transformation, 0, MySyncVoxel.PaintType.Cut);
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintBox(Boundaries,Transformation,newMaterialIndex, MySyncVoxel.PaintType.Fill);
        }
    }

    public partial class MyShapeSphere : MyShape
    {
        public Vector3D Center; // in World space
        public float    Radius;

        public override BoundingBoxD GetWorldBoundaries()
        {
            return new BoundingBoxD(Center - Radius, Center + Radius);
            //var bbox = new BoundingBoxD(Center - Radius, Center + Radius);
            //return bbox.Transform(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            return new BoundingBoxD(targetPosition - Radius, targetPosition + Radius);
        }

        public override float GetVolume(ref Vector3D voxelPosition)
        {
            float dist = (float)(voxelPosition - Center).Length();
            float diff = dist - Radius;
            return SignedDistanceToDensity(diff);
        }

        public override void SendDrillCutOutRequest(MySyncVoxel voxelSync)
        {
            voxelSync.RequestVoxelCutoutSphere(Center, Radius, false);
        }
        public override void SendCutOutRequest(MySyncVoxel voxelSync)
        {       
            voxelSync.RequestVoxelPaintSphere(Center, Radius, 0, MySyncVoxel.PaintType.Cut);
        }

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintSphere(Center, Radius,newMaterialIndex, MySyncVoxel.PaintType.Paint);
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintSphere(Center, Radius, newMaterialIndex, MySyncVoxel.PaintType.Fill);
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
            return m_boundaries.Transform(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return m_boundaries.Transform(newTransformation);
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

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintEllipsoid(Radius, Transformation, newMaterialIndex, MySyncVoxel.PaintType.Paint);
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync)
        {
            voxelSync.RequestVoxelPaintEllipsoid(Radius, Transformation, 0, MySyncVoxel.PaintType.Cut);
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintEllipsoid(Radius, Transformation, newMaterialIndex, MySyncVoxel.PaintType.Fill);
        }
    }

    public partial class MyShapeRamp : MyShape
    {
        public BoundingBoxD Boundaries;
        public Vector3D     RampNormal; // normal of the sloped plane
        public double       RampNormalW;

        public override BoundingBoxD GetWorldBoundaries()
        {
            return Boundaries.Transform(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            return Boundaries.Transform(newTransformation);
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

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintRamp(Boundaries, RampNormal,RampNormalW,Transformation, newMaterialIndex, MySyncVoxel.PaintType.Paint);
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync)
        {
            voxelSync.RequestVoxelPaintRamp(Boundaries, RampNormal, RampNormalW, Transformation, 0, MySyncVoxel.PaintType.Cut);
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintRamp(Boundaries, RampNormal, RampNormalW, Transformation, newMaterialIndex, MySyncVoxel.PaintType.Fill);
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
            return bbox.Transform(Transformation);
        }

        public override BoundingBoxD PeekWorldBoundaries(ref Vector3D targetPosition)
        {
            MatrixD newTransformation = Transformation;
            newTransformation.Translation = targetPosition;
            var bbox = new BoundingBoxD(A - Radius, B + Radius);
            return bbox.Transform(newTransformation);
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

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintCapsule(A,B, Radius,Transformation, newMaterialIndex, MySyncVoxel.PaintType.Paint);
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync)
        {
            voxelSync.RequestVoxelPaintCapsule(A, B, Radius, Transformation, 0, MySyncVoxel.PaintType.Cut);
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            voxelSync.RequestVoxelPaintCapsule(A, B, Radius, Transformation, newMaterialIndex, MySyncVoxel.PaintType.Fill);
        }
    }

    public static class MyVoxelGenerator
    {
        const int CELL_SIZE = 16;
        const int VOXEL_CLAMP_BORDER_DISTANCE = 2;

        private static MyStorageDataCache m_cache = new MyStorageDataCache();

        public static void MakeCrater(MyVoxelBase voxelMap, BoundingSphereD sphere, Vector3 normal, MyVoxelMaterialDefinition material)
        {
            ProfilerShort.Begin("MakeCrater");

            Vector3I minCorner, maxCorner;
            {
                Vector3D sphereMin = sphere.Center - (sphere.Radius - MyVoxelConstants.VOXEL_SIZE_IN_METRES);
                Vector3D sphereMax = sphere.Center + (sphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES);
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref sphereMin, out minCorner);
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref sphereMax, out maxCorner);
            }


            voxelMap.Storage.ClampVoxelCoord(ref minCorner);
            voxelMap.Storage.ClampVoxelCoord(ref maxCorner);

            //  We are tracking which voxels were changed, so we can invalidate only needed cells in the cache
            bool changed = false;
            ProfilerShort.Begin("Reading cache");
            m_cache.Resize(minCorner, maxCorner);
            voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, ref minCorner, ref maxCorner);
            ProfilerShort.End();

            ProfilerShort.Begin("Changing cache");
            int removedVoxelContent = 0;
            Vector3I tempVoxelCoord;
            Vector3I cachePos;
            for (tempVoxelCoord.Z = minCorner.Z, cachePos.Z = 0; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++, ++cachePos.Z)
            {
                for (tempVoxelCoord.Y = minCorner.Y, cachePos.Y = 0; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++, ++cachePos.Y)
                {
                    for (tempVoxelCoord.X = minCorner.X, cachePos.X = 0; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++, ++cachePos.X)
                    {
                        Vector3D voxelPosition;
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner, ref tempVoxelCoord, out voxelPosition);

                        float addDist = (float)(voxelPosition - sphere.Center).Length();
                        float addDiff = (float)(addDist - sphere.Radius);

                        byte newContent;
                        if (addDiff > MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)
                        {
                            newContent = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                        }
                        else if (addDiff < -MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)
                        {
                            newContent = MyVoxelConstants.VOXEL_CONTENT_FULL;
                        }
                        else
                        {
                            //  This formula will work even if diff is positive or negative
                            newContent = (byte)(MyVoxelConstants.VOXEL_ISO_LEVEL - addDiff / MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * MyVoxelConstants.VOXEL_ISO_LEVEL);
                        }

                        byte originalContent = m_cache.Content(ref cachePos);

                        if (newContent > originalContent && originalContent > 0)
                        {
                            if (material != null)
                            {
                                m_cache.Material(ref cachePos, material.Index);
                            }

                            changed = true;
                            m_cache.Content(ref cachePos, newContent);
                        }

                        float delDist = (float)(voxelPosition - (sphere.Center + (float)sphere.Radius * 0.7f * normal)).Length();
                        float delDiff = (float)(delDist - sphere.Radius);

                        byte contentToRemove;
                        if (delDiff > MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)
                        {
                            contentToRemove = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                        }
                        else if (delDiff < -MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)
                        {
                            contentToRemove = MyVoxelConstants.VOXEL_CONTENT_FULL;
                        }
                        else
                        {
                            //  This formula will work even if diff is positive or negative
                            contentToRemove = (byte)(MyVoxelConstants.VOXEL_ISO_LEVEL - delDiff / MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * MyVoxelConstants.VOXEL_ISO_LEVEL);
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

                        float setDist = (float)(voxelPosition - (sphere.Center - (float)sphere.Radius * 0.5f * normal)).Length();
                        float setDiff = (float)(setDist - sphere.Radius / 4f);

                        if (setDiff <= MyVoxelConstants.VOXEL_SIZE_IN_METRES * 1.5f)  // could be VOXEL_SIZE_IN_METRES_HALF, but we want to set material in empty cells correctly
                        {
                            byte indestructibleContentToSet = MyVoxelConstants.VOXEL_CONTENT_FULL;
                            if (setDiff >= MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)  // outside
                            {
                                indestructibleContentToSet = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                            }
                            else if (setDiff >= -MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF)  // boundary
                            {
                                indestructibleContentToSet = (byte)(MyVoxelConstants.VOXEL_ISO_LEVEL - setDiff / MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * MyVoxelConstants.VOXEL_ISO_LEVEL);
                            }

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
                                if (setDiff >= MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF && content != MyVoxelConstants.VOXEL_CONTENT_EMPTY)  // set material behind boundary only for empty voxels
                                    newMaterial = originalMaterial;
                            }

                            if (originalMaterial == newMaterial)
                            {
                                continue;
                            }

                            m_cache.Material(ref cachePos, newMaterial.Index);
                            changed = true;
                        }

                        float dist = (float)(voxelPosition - sphere.Center).Length();
                        float diff = (float)(dist - sphere.Radius);

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
                voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, ref minCorner, ref maxCorner);
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
                shape.SendPaintRequest(map.GetSyncObject, materialIdx);
            }
        }

        public static void RequestFillInShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape, byte materialIdx)
        {
            var map = voxelMap as MyVoxelBase;
            var shape = voxelShape as MyShape;
            if (map != null && shape != null)
            {
                shape.SendFillRequest(map.GetSyncObject, materialIdx);
            }
        }

        public static void RequestCutOutShape(IMyVoxelBase voxelMap, IMyVoxelShape voxelShape)
        {
            var map = voxelMap as MyVoxelBase;
            var shape = voxelShape as MyShape;
            if (map != null && shape != null)
            {
                shape.SendCutOutRequest(map.GetSyncObject);
            }
        }

        public static void CutOutShapeWithProperties(
            MyVoxelBase voxelMap,
            MyShape shape,
            out float voxelsCountInPercent,
            out MyVoxelMaterialDefinition voxelMaterial,
            Dictionary<MyVoxelMaterialDefinition, int> exactCutOutMaterials = null,
            bool updateSync = false,
            bool onlyCheck = false)
        {
            ProfilerShort.Begin("MyVoxelGenerator::CutOutShapeWithProperties()");

            int originalSum = 0;
            int removedSum = 0;

            var bbox = shape.GetWorldBoundaries();
            Vector3I minCorner, maxCorner;
            ComputeShapeBounds(voxelMap,ref bbox, voxelMap.PositionLeftBottomCorner, voxelMap.Storage.Size, out minCorner, out maxCorner);

            var cacheMin = minCorner - 1;
            var cacheMax = maxCorner + 1;
            voxelMap.Storage.ClampVoxelCoord(ref cacheMin);
            voxelMap.Storage.ClampVoxelCoord(ref cacheMax);
            m_cache.Resize(cacheMin, cacheMax);
            voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, ref cacheMin, ref cacheMax);

            {
                var shapeCenter = bbox.Center;
                Vector3I exactCenter;
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref shapeCenter, out exactCenter);
                exactCenter -= cacheMin;
                exactCenter = Vector3I.Clamp(exactCenter, Vector3I.Zero, m_cache.Size3D - 1);
                voxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref exactCenter));
            }

            for (var it = new Vector3I.RangeIterator(ref minCorner, ref maxCorner); it.IsValid(); it.MoveNext())
            {
                var relPos   = it.Current - cacheMin; // get original amount
                var original = m_cache.Content(ref relPos);

                if (original == MyVoxelConstants.VOXEL_CONTENT_EMPTY) // if there is nothing to remove
                    continue;

                Vector3D vpos;
                MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxelMap.PositionLeftBottomCorner - (Vector3)voxelMap.StorageMin, ref it.Current, out vpos);
                var volume = shape.GetVolume(ref vpos);

                if (volume == 0f) // if there is no intersection
                    continue;

                var maxRemove = (int)(volume * MyVoxelConstants.VOXEL_CONTENT_FULL);
                var voxelMat  = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref relPos));
                var toRemove  = (int)(maxRemove * voxelMat.DamageRatio);
                var newVal    = MathHelper.Clamp(original - toRemove, 0, maxRemove);
                var removed   = Math.Abs(original - newVal);

                if (!onlyCheck)
                    m_cache.Content(ref relPos, (byte)newVal);

                originalSum += original;
                removedSum  += removed;

                if (exactCutOutMaterials != null)
                {
                    int value = 0;
                    exactCutOutMaterials.TryGetValue(voxelMat, out value);
                    value += (MyFakes.ENABLE_REMOVED_VOXEL_CONTENT_HACK ? (int)(removed * 3.9f) : removed);
                    exactCutOutMaterials[voxelMat] = value;
                }
            }

            if (removedSum > 0 && !onlyCheck)
            {
                //  Clear all small voxel that may have been created during explosion. They can be created even outside the range of
                //  explosion sphere, e.g. if you have three voxels in a row A, B, C, where A is 255, B is 60, and C is 255. During the
                //  explosion you change C to 0, so now we have 255, 60, 0. Than another explosion that will change A to 0, so we
                //  will have 0, 60, 0. But B was always outside the range of the explosion. So this is why we need to do -1/+1 and remove
                //  B voxels too.
                //!! TODO AR & MK : check if this is needed !!
                RemoveSmallVoxelsUsingChachedVoxels();

                voxelMap.Storage.WriteRange(m_cache, MyStorageDataTypeFlags.Content, ref cacheMin, ref cacheMax);
            }

            if (removedSum > 0 && updateSync && Sync.IsServer)
            {
                shape.SendDrillCutOutRequest(voxelMap.GetSyncObject);
            }

            voxelsCountInPercent = (originalSum > 0f) ? (float)removedSum / (float)originalSum : 0f;
            ProfilerShort.End();
        }

        public static ulong FillInShape(MyVoxelBase voxelMap, MyShape shape, byte materialIdx)
        {
            Vector3I minCorner, maxCorner, numCells;
            ulong retValue = 0;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);

            for (var itCells = new Vector3I.RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
            {
                Vector3I cellMinCorner, cellMaxCorner;
                GetCellCorners(ref minCorner, ref maxCorner, ref itCells, out cellMinCorner, out cellMaxCorner);

                Vector3I originalMinCorner = cellMinCorner;
                Vector3I originalMaxCorner = cellMaxCorner;

                voxelMap.Storage.ClampVoxelCoord(ref cellMinCorner, VOXEL_CLAMP_BORDER_DISTANCE);
                voxelMap.Storage.ClampVoxelCoord(ref cellMaxCorner, VOXEL_CLAMP_BORDER_DISTANCE);

                ClampingInfo minCornerClamping = CheckForClamping(originalMinCorner, cellMinCorner);
                ClampingInfo maxCornerClamping = CheckForClamping(originalMaxCorner, cellMaxCorner);

                m_cache.Resize(cellMinCorner, cellMaxCorner);
                voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, ref cellMinCorner, ref cellMaxCorner);

                ulong filledSum = 0;

                for (var it = new Vector3I.RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
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

            return retValue;
        }

        public static void PaintInShape(MyVoxelBase voxelMap, MyShape shape, byte materialIdx)
        {
            Vector3I minCorner, maxCorner, numCells;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);

            for (var itCells = new Vector3I.RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
            {
                Vector3I cellMinCorner, cellMaxCorner;
                GetCellCorners(ref minCorner, ref maxCorner, ref itCells, out cellMinCorner, out cellMaxCorner);

                m_cache.Resize(cellMinCorner, cellMaxCorner);
                voxelMap.Storage.ReadRange(m_cache, MyStorageDataTypeFlags.Material, 0, ref cellMinCorner, ref cellMaxCorner);

                for (var it = new Vector3I.RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
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
            Vector3I minCorner, maxCorner, numCells;
            GetVoxelShapeDimensions(voxelMap, shape, out minCorner, out maxCorner, out numCells);
            ulong changedVolumeAmount = 0;

            for (var itCells = new Vector3I.RangeIterator(ref Vector3I.Zero, ref numCells); itCells.IsValid(); itCells.MoveNext())
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

                for (var it = new Vector3I.RangeIterator(ref cellMinCorner, ref cellMaxCorner); it.IsValid(); it.MoveNext())
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
            voxelMax += 1;
            storageSize -= 1;
            Vector3I.Clamp(ref voxelMin, ref Vector3I.Zero, ref storageSize, out voxelMin);
            Vector3I.Clamp(ref voxelMax, ref Vector3I.Zero, ref storageSize, out voxelMax);
        }

        private static void GetVoxelShapeDimensions(MyVoxelBase voxelMap, MyShape shape, out Vector3I minCorner, out Vector3I maxCorner, out Vector3I numCells, float extent = 0.0f)
        {
            {
                var bbox = shape.GetWorldBoundaries();
                ComputeShapeBounds(voxelMap,ref bbox, voxelMap.PositionLeftBottomCorner, voxelMap.Storage.Size, out minCorner, out maxCorner);
            }
            numCells = new Vector3I((maxCorner.X - minCorner.X) / CELL_SIZE, (maxCorner.Y - minCorner.Y) / CELL_SIZE, (maxCorner.Z - minCorner.Z) / CELL_SIZE);
        }

        private static void GetCellCorners(ref Vector3I minCorner, ref Vector3I maxCorner, ref Vector3I.RangeIterator it, out Vector3I cellMinCorner, out Vector3I cellMaxCorner)
        {
            cellMinCorner = new Vector3I(minCorner.X + it.Current.X * CELL_SIZE, minCorner.Y + it.Current.Y * CELL_SIZE, minCorner.Z + it.Current.Z * CELL_SIZE);
            cellMaxCorner = new Vector3I(Math.Min(maxCorner.X, cellMinCorner.X + CELL_SIZE), Math.Min(maxCorner.Y, cellMinCorner.Y + CELL_SIZE), Math.Min(maxCorner.Z, cellMinCorner.Z + CELL_SIZE));
        }

    }
}