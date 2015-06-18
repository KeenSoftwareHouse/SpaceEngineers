using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Utils;
using VRageMath;
using VRageRender;
using FrameT = System.Int16;

namespace VRage.Voxels
{
    partial class MyClipmap
    {
        private static readonly float[][] m_lodRangeGroups;

        private static MyBinaryHeap<FrameT, UpdateQueueItem> m_updateQueue = new MyBinaryHeap<FrameT, UpdateQueueItem>(comparer: new UpdateFrameComparer());
        private static HashSet<MyClipmap> m_toRemove = new HashSet<MyClipmap>();
        private static HashSet<MyClipmap> m_notReady = new HashSet<MyClipmap>();
        private static FrameT m_currentFrameIdx;

        static MyClipmap()
        {
            const int valueCount = MyCellCoord.MAX_LOD_COUNT + 1;
            m_lodRangeGroups = new float[MyEnum<MyClipmapScaleEnum>.Values.Length][];
            for (int i = 0; i < m_lodRangeGroups.Length; i++)
            {
                m_lodRangeGroups[i] = new float[valueCount];
            }
            UpdateLodRanges(MyRenderConstants.m_renderQualityProfiles[0].LodClipmapRanges);
        }

        public static bool UpdateLodRanges(float[][] lodDistances)
        {
            for (int group = 0; group < m_lodRangeGroups.Length; group++)
            {
                var lodRanges = m_lodRangeGroups[group];
                lodRanges[0] = 0f;
                int i = 1;
                int copyStop = Math.Min(lodRanges.Length, lodDistances[group].Length + 1);
                for (; i < copyStop; ++i)
                {
                    lodRanges[i] = lodDistances[group][i - 1];
                }

                for (; i < lodRanges.Length; ++i)
                {
                    lodRanges[i] = lodRanges[i - 1] * 2f;
                }
            }

            return true;
        }

        public static void ComputeLodViewBounds(MyClipmapScaleEnum scale, int lod, out float min, out float max)
        {
            min = m_lodRangeGroups[(int)scale][lod];
            max = m_lodRangeGroups[(int)scale][lod + 1];
        }

        public static void AddToUpdate(Vector3D cameraPos, MyClipmap clipmap)
        {
            if (m_toRemove.Contains(clipmap))
                m_toRemove.Remove(clipmap);
            else
                m_updateQueue.Insert(clipmap.m_updateQueueItem, ComputeNextUpdateFrame(ref cameraPos, clipmap));

            m_notReady.Add(clipmap);
        }

        public static void RemoveFromUpdate(MyClipmap clipmap)
        {
            m_toRemove.Add(clipmap);
            m_notReady.Remove(clipmap);
        }

        private static FrameT ComputeNextUpdateFrame(ref Vector3D cameraPos, MyClipmap clipmap)
        {
            var cameraDistance = clipmap.m_worldAABB.Distance(cameraPos);
            FrameT framesTillUpdate;
            var lodRanges = m_lodRangeGroups[(int)clipmap.m_scaleGroup];
            if (cameraDistance < lodRanges[1])
                framesTillUpdate = 3;
            if (cameraDistance < lodRanges[2])
                framesTillUpdate = 10;
            else if (cameraDistance < lodRanges[5])
                framesTillUpdate = 25;
            else
                framesTillUpdate = 100;

            return (FrameT)(m_currentFrameIdx + framesTillUpdate);
        }

        public static void UpdateQueued(Vector3D cameraPos, float farPlaneDistance, float largeDistanceFarPlane)
        {
            ++m_currentFrameIdx;

            UpdateLodRanges(MyRenderConstants.RenderQualityProfile.LodClipmapRanges);

            var oldNotReadyCount = m_notReady.Count;

            int updatedCount = 0;
            int maxUpdates = m_updateQueue.Count / 53; // 53 is just some prime number so it spreads better
            while (m_updateQueue.Count > 0)
            {
                var item = m_updateQueue.Min();
                if (m_toRemove.Contains(item.Clipmap))
                {
                    m_toRemove.Remove(item.Clipmap);
                    m_updateQueue.RemoveMin();
                    continue;
                }

                FrameT untilUpdate = (FrameT)(item.HeapKey - m_currentFrameIdx);
                if (untilUpdate > 0)
                    break;

                ++updatedCount;
                BoundingBoxD tmp;
                item.Clipmap.UpdateWorldAABB(out tmp);
                float clipmapFarPlane = (item.Clipmap.m_scaleGroup == MyClipmapScaleEnum.Massive) ? largeDistanceFarPlane : farPlaneDistance;
                item.Clipmap.Update(ref cameraPos, clipmapFarPlane);
                m_updateQueue.ModifyDown(item, ComputeNextUpdateFrame(ref cameraPos, item.Clipmap));
                if (updatedCount > maxUpdates)
                    break;
            }

            if (oldNotReadyCount != m_notReady.Count && m_notReady.Count == 0)
            {
                MyRenderProxy.SendClipmapsReady();
            }
        }

        class UpdateQueueItem : HeapItem<FrameT>
        {
            public readonly MyClipmap Clipmap;

            public UpdateQueueItem(MyClipmap clipmap)
            {
                Clipmap = clipmap;
            }
        }

        class UpdateFrameComparer : IComparer<FrameT>
        {
            public int Compare(FrameT x, FrameT y)
            {
                // This ensures that even if frame counter overflows, comparisons will still work.
                // Comparison will fail when difference itself overflows, but that shouldn't happen.
                x -= m_currentFrameIdx;
                y -= m_currentFrameIdx;
                return x - y;
            }
        }

    }
}
