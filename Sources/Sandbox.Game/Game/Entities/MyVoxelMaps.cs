using Sandbox.Engine.Voxels;
using Sandbox.Game.Components;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyVoxelMaps
    {
        private readonly Dictionary<uint, MyRenderComponentVoxelMap> m_renderComponentsByClipmapId = new Dictionary<uint, MyRenderComponentVoxelMap>();
        private readonly Dictionary<long, MyVoxelBase> m_voxelMapsByEntityId = new Dictionary<long, MyVoxelBase>();

        public MyVoxelMaps()
        {
        }

        public void Clear()
        {
            foreach (var entry in m_voxelMapsByEntityId)
                entry.Value.Close();

            MyStorageBase.ResetCache();

            m_voxelMapsByEntityId.Clear();
            m_renderComponentsByClipmapId.Clear();
        }

        public DictionaryValuesReader<long, MyVoxelBase> Instances
        {
            get { return m_voxelMapsByEntityId; }
        }

        public bool Exist(MyVoxelBase voxelMap)
        {
            return m_voxelMapsByEntityId.ContainsKey(voxelMap.EntityId);
        }

        public void RemoveVoxelMap(MyVoxelBase voxelMap)
        {
            if (m_voxelMapsByEntityId.Remove(voxelMap.EntityId))
            {
                var render = voxelMap.Render;
                if (render is MyRenderComponentVoxelMap)
                {
                    var clipMapId = (render as MyRenderComponentVoxelMap).ClipmapId;
                    m_renderComponentsByClipmapId.Remove(clipMapId);
                }
            }
        }

        // This is not thread safe...
        private List<MyVoxelBase> m_tmpVoxelMapsList = new List<MyVoxelBase>();

        public MyVoxelBase GetOverlappingWithSphere(ref BoundingSphereD sphere)
        {
            MyVoxelBase ret = null;
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, m_tmpVoxelMapsList);
            foreach (var voxelMap in m_tmpVoxelMapsList)
            {
                if (voxelMap.DoOverlapSphereTest((float)sphere.Radius, sphere.Center))
                {
                    ret = voxelMap;
                    break;
                }
            }
            m_tmpVoxelMapsList.Clear();
            return ret;
        }

        public void GetAllOverlappingWithSphere(ref BoundingSphereD sphere, List<MyVoxelBase> voxels)
        {
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, voxels);
        }

        public void Add(MyVoxelBase voxelMap)
        {
            if (!Exist(voxelMap))
            {
                m_voxelMapsByEntityId.Add(voxelMap.EntityId, voxelMap);

                // On dedicated servers, ClipmapIDs are all 0, but it's fine since there is no rendering anyway.
                var render = voxelMap.Render;
                if (render is MyRenderComponentVoxelMap)
                {
                   var clipMapId= (render as MyRenderComponentVoxelMap).ClipmapId;
                   m_renderComponentsByClipmapId[clipMapId] = render as MyRenderComponentVoxelMap;
                }
            }
        }

        internal bool TryGetRenderComponent(uint clipmapId, out MyRenderComponentVoxelMap render)
        {
            return m_renderComponentsByClipmapId.TryGetValue(clipmapId, out render);
        }

        /// <summary>
        /// Return reference to voxel map that intersects the box. If not voxel map found, null is returned.
        /// </summary>
        public MyVoxelBase GetVoxelMapWhoseBoundingBoxIntersectsBox(ref BoundingBoxD boundingBox, MyVoxelBase ignoreVoxelMap)
        {
            foreach (var voxelMap in m_voxelMapsByEntityId.Values)
            {
                if (voxelMap == ignoreVoxelMap)
                    continue;
                if (voxelMap is MyVoxelBase)
                {
                    if (voxelMap.IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref boundingBox))
                        return voxelMap;
                }
            }

            //  If we get here, no intersection was found
            return null;
        }

        public MyVoxelBase TryGetVoxelMapByNameStart(string name)
        {
            foreach (var voxelMap in m_voxelMapsByEntityId.Values)
            {
                if (voxelMap.StorageName != null && voxelMap.StorageName.StartsWith(name))
                    return voxelMap;
            }

            return null;
        }

        public MyVoxelBase TryGetVoxelMapByName(string name)
        {
            foreach (var voxelMap in m_voxelMapsByEntityId.Values)
            {
                if (voxelMap.StorageName == name)
                    return voxelMap;
            }

            return null;
        }

        public Dictionary<string, byte[]> GetVoxelMapsArray(bool includeChanged)
        {
            ProfilerShort.Begin("GetVoxelMapsArray");

            Dictionary<string, byte[]> voxelMaps = new Dictionary<string, byte[]>();

            byte[] compressedData;
            foreach (var voxelMap in m_voxelMapsByEntityId.Values)
            {
                if (includeChanged == false && (voxelMap.ContentChanged || voxelMap.BeforeContentChanged))
                {
                    continue;
                }

                if (voxelMap.Save == false)
                    continue;

                if (voxelMaps.ContainsKey(voxelMap.StorageName))
                    continue;

                voxelMap.Storage.Save(out compressedData);
                voxelMaps.Add(voxelMap.StorageName, compressedData);
            }

            ProfilerShort.End();
            return voxelMaps;
        }

    }
}
