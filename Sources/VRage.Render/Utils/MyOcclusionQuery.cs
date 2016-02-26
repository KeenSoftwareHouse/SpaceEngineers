#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using SharpDX;
using SharpDX.Direct3D9;

using VRageRender;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Rectangle = VRageMath.Rectangle;
using Matrix = VRageMath.Matrix;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;
using MathHelper = VRageMath.MathHelper;
using VRage.Utils;
using System.Diagnostics;

#endregion

namespace VRageRender
{
   
    class MyOcclusionQueries : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.OcclusionQueries;
        }


        static readonly HashSet<MyOcclusionQuery> m_existingQueries = new HashSet<MyOcclusionQuery>();
        static readonly Stack<MyOcclusionQuery> m_queriesStack = new Stack<MyOcclusionQuery>(256);
        static Device m_device;

        public static MyOcclusionQuery Get()
        {
            MyOcclusionQuery query = null;
            if (m_queriesStack.Count > 0)
            {
                query = m_queriesStack.Pop();
            }
            else
            {
                query = MyOcclusionQuery.CreateQuery();
                if(query == null)
                {
                    Debug.Fail("OcclusionQuery cannot be null!");
                    return null;
                }
                
                query.LoadContent(m_device);
                m_existingQueries.Add(query);
            }

            System.Diagnostics.Debug.Assert(!m_queriesStack.Contains(query));

            return query;
        }

        public static void Return(MyOcclusionQuery query)
        {
            System.Diagnostics.Debug.Assert(!m_queriesStack.Contains(query));

            m_queriesStack.Push(query);
        }

        public override void LoadContent(Device device)
        {
            MyRender.Log.WriteLine("MyOcclusionQueries.LoadContent - START");

            m_device = device;
            m_queriesStack.Clear();

            foreach (var q in m_existingQueries)
            {
                q.LoadContent(m_device);
                m_queriesStack.Push(q);
            }

            MyRender.Log.WriteLine("MyOcclusionQueries.LoadContent - END");
        }

        public override void UnloadContent()
        {
            if (m_device != null)
            {
                System.Diagnostics.Debug.Assert(m_queriesStack.Count == m_existingQueries.Count);
                m_queriesStack.Clear();

                foreach (var q in m_existingQueries)
                {
                    q.UnloadContent();
                }

                m_device = null;
            }
        }
    }


    class MyOcclusionQuery :/* MyOcclusionQueries.Friend,*/ IDisposable
    {
        Query dxQuery;

        // Because Xna OcclusionQuery returns IsComplete = false when query not started
        bool m_started = false;
        bool m_inDraw = false;


        public static MyOcclusionQuery CreateQuery()
        {
            return new MyOcclusionQuery();
        }

        private MyOcclusionQuery()
        {
        }

        public void Begin()
        {
            System.Diagnostics.Debug.Assert(m_inDraw == false);
            m_started = true;
            m_inDraw = true;
            dxQuery.Issue(Issue.Begin);
        }

        public void End()
        {
            System.Diagnostics.Debug.Assert(m_inDraw == true);
            m_inDraw = false;
            dxQuery.Issue(Issue.End);
        }

        public void LoadContent(Device device)
        {
            System.Diagnostics.Debug.Assert(dxQuery == null);
            System.Diagnostics.Debug.Assert(m_inDraw == false);

            dxQuery = new Query(device, QueryType.Occlusion);
        }

        public void UnloadContent()
        {
            if (dxQuery != null && !dxQuery.IsDisposed)
            {
                dxQuery.Dispose();
            }

            dxQuery = null;
            m_started = false;
            m_inDraw = false;
        }

        public int PixelCount
        {
            get
            {
                if (!m_started) return 0;

                System.Diagnostics.Debug.Assert(m_inDraw == false);

                int pixels = 0;

                try
                {
                    if (dxQuery.GetData<int>(out pixels, false))
                        return pixels;
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("ERROR: Query get data failed!");
                    MyLog.Default.WriteLine(e.ToString());
                }

                return 0;
            }
        }

        public bool IsComplete
        {
            get
            {
                if (!m_started) return true; // Because of XNA
                return CheckStatus(false);
            }
        }

        private bool CheckStatus(bool flush)
        {
            System.Diagnostics.Debug.Assert(m_inDraw == false);
            int data;
            try
            {
                if (dxQuery != null) //TODO:can be called from PrepareEntitiesTask...
                    return dxQuery.GetData<int>(out data, flush);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ERROR: Query GetData failed! CheckStatus");
                MyLog.Default.WriteLine(e.ToString());
            }

            return false;
        }

        public object Tag { get; set; }

        public void Dispose()
        {
            UnloadContent();
        }
    }
}
