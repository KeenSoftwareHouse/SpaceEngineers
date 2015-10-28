using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{  
    public partial class MyClipmap
    {       
        class RequestCollector
        {
            private readonly HashSet<UInt64> m_sentRequests = new HashSet<UInt64>();
            //private readonly Dictionary<UInt64, CellRequest> m_unsentRequestsHigh = new Dictionary<UInt64, CellRequest>();
            private readonly Dictionary<UInt64, CellRequest>[] m_unsentRequestsLow;
            private readonly HashSet<UInt64> m_cancelRequests = new HashSet<UInt64>();
            private readonly Dictionary<UInt64, CellRequest> m_unsentRequests = new Dictionary<UInt64, CellRequest>();

            /// <summary>
            /// Sent requests + low priority requests are checked against this.
            /// High priority requests should be sent even when they are over limit.
            /// </summary>
            //private int m_maxRequests = 10000;//int.MaxValue;
            private uint m_clipmapId;

            public bool SentRequestsEmpty
            {
                get { return m_sentRequests.Count == 0; }
            }

            public RequestCollector(uint clipmapId)
            {
                m_clipmapId = clipmapId;
                m_unsentRequestsLow = new Dictionary<UInt64, CellRequest>[MyCellCoord.MAX_LOD_COUNT];
                for (int i = 0; i < m_unsentRequestsLow.Length; i++)
                {
                    m_unsentRequestsLow[i] = new Dictionary<UInt64, CellRequest>();
                }
            }

            struct CellRequest
            {
                public UInt64 CellId;
                public Func<int> PriorityFunc;
                public Action<Color> DebugDraw;
            }

            public void AddRequest(UInt64 cellId, bool isHighPriority, Func<int> priorityFunc, Action<Color> ddraw)
            {
                m_cancelRequests.Remove(cellId);
                if (!m_sentRequests.Contains(cellId))
                {
                    var cellRequest = new CellRequest() { CellId = cellId, PriorityFunc = priorityFunc, DebugDraw = ddraw };
                    //if(isHighPriority)
                    //    m_unsentRequests.Add(cellId, cellRequest);
                    //else
                    {
                        var lod = MyCellCoord.UnpackLod(cellId);
                        if (!m_unsentRequestsLow[lod].ContainsKey(cellId))
                            m_unsentRequestsLow[lod].Add(cellId, cellRequest);
                    }
                }
            }

            public void CancelRequest(UInt64 cellId)
            {
                var lod = MyCellCoord.UnpackLod(cellId);
                m_unsentRequestsLow[lod].Remove(cellId);
                m_unsentRequests.Remove(cellId);
                if (m_sentRequests.Contains(cellId))
                {
                    m_cancelRequests.Add(cellId);
                }
            }

            public void Submit()
            {
                ProfilerShort.Begin("RequestCollector.Submit");

                MyCellCoord cell = default(MyCellCoord);
                foreach (var cellId in m_cancelRequests)
                {
                    cell.SetUnpack(cellId);
                    MyRenderProxy.CancelClipmapCell(m_clipmapId, cell);
                    bool removed = m_sentRequests.Remove(cellId);
                    Debug.Assert(removed);
                }

                foreach (var request in m_unsentRequests)
                {
                    m_sentRequests.Add(request.Key);
                    cell.SetUnpack(request.Key);
                    MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, true, request.Value.PriorityFunc, request.Value.DebugDraw);
                }

                m_unsentRequests.Clear();

                //foreach (var highPriorityRequest in m_unsentRequestsHigh)
                //{
                //    cell.SetUnpack(highPriorityRequest);
                //    MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, highPriority: true);
                //}
                //m_unsentRequestsHigh.Clear();

                int addedCount = 0;
                for (int i = m_unsentRequestsLow.Length - 1; i >= 0; i--)
                {
                    var unsent = m_unsentRequestsLow[i];
                    while (0 < unsent.Count)// && m_sentRequests.Count < m_maxRequests*1000)
                    {
                        var pair = unsent.FirstPair();
                        var cellId = pair.Key;
                        var hs = new HashSet<object>();
                        cell.SetUnpack(cellId);
                        // Do Z-order style iteration of siblings that also need to
                        // be requested. This ensures faster processing of cells and
                        // shorter time when both lods are rendered.
                        var baseCoord = (cell.CoordInLod >> 1) << 1;
                        var offset = Vector3I.Zero;
                        for (var it = new Vector3I.RangeIterator(ref Vector3I.Zero, ref Vector3I.One);
                            it.IsValid(); it.GetNext(out offset))
                        {
                            cell.CoordInLod = baseCoord + offset;
                            cellId = cell.PackId64();
                            if (!unsent.Remove(cellId))
                            {
                                continue;
                            }

                            Debug.Assert(!m_cancelRequests.Contains(cellId));
                            MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, false, pair.Value.PriorityFunc, pair.Value.DebugDraw);
                            bool added = m_sentRequests.Add(cellId);
                            Debug.Assert(added);
                            addedCount++;
                        }
                    }

                    // When set reaches reasonably small size, stop freeing memory
                    //if (unsent.Count > 100) no trim for dictionary :(
                    //    unsent.TrimExcess();
                }

                m_cancelRequests.Clear();

                ProfilerShort.End();
            }

            internal void RequestFulfilled(UInt64 cellId)
            {
                m_sentRequests.Remove(cellId);
            }
        }


    }
}
