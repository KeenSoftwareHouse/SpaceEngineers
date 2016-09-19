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
            private readonly Queue<CellRequest> m_unsentRequests = new Queue<CellRequest>();
            private uint m_clipmapId;
            public static int MaxRequestPerBatch = 100;

            public bool SentRequestsEmpty
            {
                get { return m_sentRequests.Count == 0 && m_unsentRequests.Count == 0; }
            }

            public RequestCollector(uint clipmapId)
            {
                m_clipmapId = clipmapId;
            }

            struct CellRequest
            {
                public UInt64 CellId;
                public MyClipmap_CellData Data;
            }

            public void AddRequest(UInt64 cellId, MyClipmap_CellData data, bool highPriority)
            {
                var cellRequest = new CellRequest() { CellId = cellId, Data = data };
                m_unsentRequests.Enqueue(cellRequest);
                
                data.State = CellState.Invalid;
                data.HighPriority = highPriority;
            }

            public void Submit()
            {
                ProfilerShort.Begin("RequestCollector.Submit");

                MyCellCoord cell = default(MyCellCoord);

                int count = MaxRequestPerBatch;
                while (m_unsentRequests.Count > 0 && count > 0)
                {
                    var request = m_unsentRequests.Dequeue();
                
                    m_sentRequests.Add(request.CellId);
                    cell.SetUnpack(request.CellId);
                    MyRenderProxy.RequireClipmapCell(m_clipmapId, cell, request.Data.GetPriority);

                    request.Data.State = CellState.Pending;

                    count--;
                }

                m_unsentRequests.Clear();

                ProfilerShort.End();
            }

            internal void RequestFulfilled(UInt64 cellId)
            {
                m_sentRequests.Remove(cellId);
            }
        }


    }
}
