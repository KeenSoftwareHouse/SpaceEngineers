using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.VoxelMaps;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using VRageMath;
using VRageRender;

//  This class is used for changing voxel maps (creating voxel sphere or boxes, or on the other hand cutting out spheres and boxes)
namespace Sandbox.Game.Voxels
{
    public abstract class MyShape
    {
        /// <returns>Minimal coordinate of aabb of the shape</returns>
        public abstract Vector3 GetMin();

        /// <returns>Maximal coordinate of aabb of the shape</returns>
        public abstract Vector3 GetMax();

        /// <returns>Center coordinate of aabb of the shape</returns>
        public abstract Vector3 GetCenter();

        /// <summary>
        /// Gets volume of intersection of shape and voxel
        /// </summary>
        /// <param name="voxelPosition">Left bottom point of voxel</param>
        /// <returns>Normalized volume of intersection</returns>
        public abstract float GetVolume(ref Vector3 voxelPosition);

        public abstract void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex);
        public abstract void SendCutOutRequest(MySyncVoxel voxelSync, float removeRatio);
        public abstract void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex, float fillRatio);
    }

    public class MyShapeBox : MyShape
    {
        public BoundingBox Boundaries;

        public override Vector3 GetMin()    { return Boundaries.Min; }
        public override Vector3 GetMax()    { return Boundaries.Max; }
        public override Vector3 GetCenter() { return Boundaries.Center; }

        public override float GetVolume(ref Vector3 voxelPosition)
        {
            var t = Vector3.Abs(voxelPosition - Boundaries.Center);
            var halfSize = Boundaries.HalfExtents;
            var d = halfSize - t;
            var dist = d.Min();
            const float TRANSITION_SIZE = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            return MathHelper.Clamp(dist, -TRANSITION_SIZE, TRANSITION_SIZE) / (2f * TRANSITION_SIZE) + 0.5f;
        }

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            throw new NotImplementedException();
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync, float removeRatio)
        {
            //throw new NotImplementedException();
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex, float fillRatio)
        {
            throw new NotImplementedException();
        }
    }

    public class MyShapeSphere : MyShape
    {
        public Vector3 Center;
        public float   Radius;

        public override Vector3 GetMin()    { return Center - Radius; }
        public override Vector3 GetMax()    { return Center + Radius; }
        public override Vector3 GetCenter() { return Center; }

        public override float GetVolume(ref Vector3 voxelPosition)
        {
            const float TRANSITION_SIZE = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            float dist = (voxelPosition - Center).Length();
            float diff = dist - Radius;
            return MathHelper.Clamp(-diff, -TRANSITION_SIZE, TRANSITION_SIZE) / (2f * TRANSITION_SIZE) + 0.5f;
        }

        public override void SendCutOutRequest(MySyncVoxel voxelSync, float removeRatio)
        {
            voxelSync.RequestVoxelCutoutExplosion(Center, Radius, false, removeRatio);
        }

        public override void SendPaintRequest(MySyncVoxel voxelSync, byte newMaterialIndex)
        {
            throw new NotImplementedException();
        }

        public override void SendFillRequest(MySyncVoxel voxelSync, byte newMaterialIndex, float fillRatio)
        {
            throw new NotImplementedException();
        }
    }

    public static class MyVoxelGenerator
    {
        private static MyStorageDataCache m_cache = new MyStorageDataCache();

        public static void MakeCrater(MyVoxelMap voxelMap, BoundingSphere sphere, Vector3 normal, MyVoxelMaterialDefinition material, ref bool changed, out Vector3I minChanged, out Vector3I maxChanged)
        {
            Profiler.Begin("MakeCrater");
            Vector3I minCorner = voxelMap.GetVoxelCoordinateFromMeters(sphere.Center - (sphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES));
            Vector3I maxCorner = voxelMap.GetVoxelCoordinateFromMeters(sphere.Center + (sphere.Radius + MyVoxelConstants.VOXEL_SIZE_IN_METRES));

            voxelMap.FixVoxelCoord(ref minCorner);
            voxelMap.FixVoxelCoord(ref maxCorner);

            //  We are tracking which voxels were changed, so we can invalidate only needed cells in the cache
            minChanged = maxCorner;
            maxChanged = minCorner;
            Profiler.Begin("Reading cache");
            m_cache.Resize(ref minCorner, ref maxCorner);
            voxelMap.Storage.ReadRange(m_cache, true, true, MyVoxelGeometry.GetLodIndex(MyLodTypeEnum.LOD0), ref minCorner, ref maxCorner);
            Profiler.End();

            Profiler.Begin("Changing cache");
            int removedVoxelContent = 0;
            Vector3I tempVoxelCoord;
            Vector3I cachePos;
            for (tempVoxelCoord.Z = minCorner.Z, cachePos.Z = 0; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++, ++cachePos.Z)
            {
                for (tempVoxelCoord.Y = minCorner.Y, cachePos.Y = 0; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++, ++cachePos.Y)
                {
                    for (tempVoxelCoord.X = minCorner.X, cachePos.X = 0; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++, ++cachePos.X)
                    {
                        bool cellChanged = false;
                        Vector3 voxelPosition = voxelMap.GetVoxelPositionAbsolute(ref tempVoxelCoord);

                        float addDist = (voxelPosition - sphere.Center).Length();
                        float addDiff = addDist - sphere.Radius;

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

                            cellChanged = true;
                            m_cache.Content(ref cachePos, newContent);
                        }

                        float delDist = (voxelPosition - (sphere.Center + sphere.Radius * 0.7f * normal)).Length();
                        float delDiff = delDist - sphere.Radius;

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
                            cellChanged = true;

                            int newVal = originalContent - contentToRemove;
                            if (newVal < MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                                newVal = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                            m_cache.Content(ref cachePos, (byte)newVal);

                            removedVoxelContent += originalContent - newVal;
                        }

                        float setDist = (voxelPosition - (sphere.Center - sphere.Radius * 0.5f * normal)).Length();
                        float setDiff = setDist - sphere.Radius / 4f;

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
                            cellChanged = true;
                        }

                        float dist = (voxelPosition - sphere.Center).Length();
                        float diff = dist - sphere.Radius;

                        if (diff <= 0f)
                        {
                            originalContent = m_cache.Content(ref cachePos);
                            if (originalContent > MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                            {
                                bool result = m_cache.WrinkleVoxelContent(ref cachePos, MyVoxelConstants.DEFAULT_WRINKLE_WEIGHT_ADD, MyVoxelConstants.DEFAULT_WRINKLE_WEIGHT_REMOVE);
                                if (cellChanged == false) cellChanged = result;
                            }
                        }

                        if (cellChanged)
                        {
                            if (tempVoxelCoord.X < minChanged.X) minChanged.X = tempVoxelCoord.X;
                            if (tempVoxelCoord.Y < minChanged.Y) minChanged.Y = tempVoxelCoord.Y;
                            if (tempVoxelCoord.Z < minChanged.Z) minChanged.Z = tempVoxelCoord.Z;
                            if (tempVoxelCoord.X > maxChanged.X) maxChanged.X = tempVoxelCoord.X;
                            if (tempVoxelCoord.Y > maxChanged.Y) maxChanged.Y = tempVoxelCoord.Y;
                            if (tempVoxelCoord.Z > maxChanged.Z) maxChanged.Z = tempVoxelCoord.Z;
                            changed = true;
                        }
                    }
                }
            }
            Profiler.End();

            if (changed)
            {
                Profiler.Begin("RemoveSmallVoxelsUsingChachedVoxels");
                RemoveSmallVoxelsUsingChachedVoxels();
                Profiler.BeginNextBlock("Writing cache");
                voxelMap.Storage.WriteRange(m_cache, true, true, ref minCorner, ref maxCorner);
                Profiler.End();
            }

            Profiler.End();
        }

        private static void RemoveSmallVoxelsUsingChachedVoxels()
        {
            Profiler.Begin("MyVoxelGenerator::RemoveSmallVoxelsUsingChachedVoxels()");
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
            Profiler.End();
        }

        public static void FillInShape(
            MyVoxelMap voxelMap,
            MyShape shape,
            out float voxelsCountInPercent,
            byte materialIdx,
            float fillRatio = 1,
            bool updateSync = false,
            bool onlyCheck = false)
        {
            Profiler.Begin("MyVoxelGenerator::FillInShape()");

            if (voxelMap == null)
            {
                voxelsCountInPercent = 0f;
                return;
            }

            if (updateSync && Sync.IsServer)
            {
                shape.SendFillRequest(voxelMap.SyncObject, materialIdx, fillRatio);
            }

            int originalSum = 0;
            int filledSum   = 0;

            Vector3I minCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMin() - MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * 1.01f);
            Vector3I maxCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMax() + MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF * 1.01f);
            voxelMap.FixVoxelCoord(ref minCorner);
            voxelMap.FixVoxelCoord(ref maxCorner);

            m_cache.Resize(ref minCorner, ref maxCorner);
            voxelMap.Storage.ReadRange(m_cache, true, true, MyVoxelGeometry.GetLodIndex(MyLodTypeEnum.LOD0), ref minCorner, ref maxCorner);

            Vector3I tempVoxelCoord;
            for (tempVoxelCoord.X = minCorner.X; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++)
            for (tempVoxelCoord.Y = minCorner.Y; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++)
            for (tempVoxelCoord.Z = minCorner.Z; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++)
            {
                var relPos   = tempVoxelCoord - minCorner; // get original amount
                var original = m_cache.Content(ref relPos);

                if (original == MyVoxelConstants.VOXEL_CONTENT_FULL) // if there is nothing to add
                    continue;

                var vpos   = voxelMap.GetVoxelPositionAbsolute(ref tempVoxelCoord);
                var volume = shape.GetVolume(ref vpos);

                if (volume <= 0f) // there is nothing to fill
                    continue;

                m_cache.Material(ref relPos, materialIdx); // set material

                var maxFill = (int)(volume * MyVoxelConstants.VOXEL_CONTENT_FULL);
                var toFill  = (int)(maxFill * fillRatio);

                if (original > maxFill)
                    maxFill = original;

                var newVal = MathHelper.Clamp(original + toFill, 0, maxFill);

                if (!onlyCheck)
                    m_cache.Content(ref relPos, (byte)newVal);

                originalSum += original;
                filledSum += original + newVal;
            }

            if (filledSum > 0 && !onlyCheck)
            {
                voxelMap.Storage.WriteRange(m_cache, true, true, ref minCorner, ref maxCorner);
                voxelMap.InvalidateCache(minCorner - 1, maxCorner + 1);
            }
            voxelsCountInPercent = (originalSum > 0f) ? (float)filledSum / (float)originalSum : 0f;
            Profiler.End();
        }

        public static void PaintInShape(
            MyVoxelMap voxelMap,
            MyShape shape,
            byte materialIdx,
            bool updateSync = false)
        {
            Profiler.Begin("MyVoxelGenerator::PaintInShape()");

            if (voxelMap == null)
            {
                return;
            }

            if (updateSync && Sync.IsServer)
            {
                shape.SendPaintRequest(voxelMap.SyncObject, materialIdx);
            }

            Vector3I minCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMin());
            Vector3I maxCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMax());
            voxelMap.FixVoxelCoord(ref minCorner);
            voxelMap.FixVoxelCoord(ref maxCorner);

            m_cache.Resize(ref minCorner, ref maxCorner);
            voxelMap.Storage.ReadRange(m_cache, false, true, MyVoxelGeometry.GetLodIndex(MyLodTypeEnum.LOD0), ref minCorner, ref maxCorner);

            Vector3I tempVoxelCoord;
            for (tempVoxelCoord.X = minCorner.X; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++)
            for (tempVoxelCoord.Y = minCorner.Y; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++)
            for (tempVoxelCoord.Z = minCorner.Z; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++)
            {
                var relPos = tempVoxelCoord - minCorner;
                m_cache.Material(ref relPos, materialIdx); // set material
            }

            Profiler.End();
        }

        public static void CutOutShape(
            MyVoxelMap voxelMap,
            MyShape shape,
            out float voxelsCountInPercent,
            out MyVoxelMaterialDefinition voxelMaterial,
            float removeRatio = 1,
            Dictionary<MyVoxelMaterialDefinition, int> exactCutOutMaterials = null,
            bool updateSync = true,
            bool onlyCheck = false)
        {
            Profiler.Begin("MyVoxelGenerator::CutOutShape()");

            if (updateSync && Sync.IsServer)
            {
                shape.SendCutOutRequest(voxelMap.SyncObject, removeRatio);
            }

            int originalSum = 0;
            int removedSum  = 0;

            Vector3I minCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMin());
            Vector3I maxCorner = voxelMap.GetVoxelCoordinateFromMeters(shape.GetMax());
            voxelMap.FixVoxelCoord(ref minCorner);
            voxelMap.FixVoxelCoord(ref maxCorner);

            var cacheMin = minCorner - 1;
            var cacheMax = maxCorner + 1;
            voxelMap.FixVoxelCoord(ref cacheMin);
            voxelMap.FixVoxelCoord(ref cacheMax);
            m_cache.Resize(ref cacheMin, ref cacheMax);
            voxelMap.Storage.ReadRange(m_cache, true, true, MyVoxelGeometry.GetLodIndex(MyLodTypeEnum.LOD0), ref cacheMin, ref cacheMax);

            {
                Vector3I exactCenter = voxelMap.GetVoxelCoordinateFromMeters(shape.GetCenter());
                exactCenter -= cacheMin;
                exactCenter = Vector3I.Clamp(exactCenter, Vector3I.Zero, m_cache.Size3D-1);
                voxelMaterial = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref exactCenter));
            }

            Vector3I tempVoxelCoord;
            for (tempVoxelCoord.X = minCorner.X; tempVoxelCoord.X <= maxCorner.X; tempVoxelCoord.X++)
            for (tempVoxelCoord.Y = minCorner.Y; tempVoxelCoord.Y <= maxCorner.Y; tempVoxelCoord.Y++)
            for (tempVoxelCoord.Z = minCorner.Z; tempVoxelCoord.Z <= maxCorner.Z; tempVoxelCoord.Z++)
            {
                var relPos   = tempVoxelCoord - cacheMin; // get original amount
                var original = m_cache.Content(ref relPos);

                if (original == MyVoxelConstants.VOXEL_CONTENT_EMPTY) // if there is nothing to remove
                    continue;

                var vpos   = voxelMap.GetVoxelPositionAbsolute(ref tempVoxelCoord);
                var volume = shape.GetVolume(ref vpos);

                if (volume == 0f) // if there is no intersection
                    continue;

                var maxRemove = (int)(volume * MyVoxelConstants.VOXEL_CONTENT_FULL);
                var voxelMat  = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_cache.Material(ref relPos));
                var toRemove  = (int)(maxRemove * removeRatio * voxelMat.DamageRatio);
                if (voxelMap.Storage is MyCellStorage)
                {
                    if (toRemove < MyCellStorage.Quantizer.GetMinimumQuantizableValue())
                        toRemove = MyCellStorage.Quantizer.GetMinimumQuantizableValue();
                }
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

                voxelMap.Storage.WriteRange(m_cache, true, false, ref cacheMin, ref cacheMax);
                voxelMap.InvalidateCache(cacheMin, cacheMax);
            }
            voxelsCountInPercent = (originalSum > 0f) ? (float)removedSum / (float)originalSum : 0f;
            Profiler.End();
        }
    }
}