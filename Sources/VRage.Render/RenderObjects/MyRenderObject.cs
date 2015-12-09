#region Using Statements

using System.Collections.Generic;
using VRageMath;
using System;
using ParallelTasks;
using VRage.Utils;
using VRageRender.Shadows;
using VRageRender.Utils;

#endregion

namespace VRageRender
{
    /// <summary>
    /// Sensor element used for sensors
    /// </summary>
    class MyRenderObject : MyElement
    {
        RenderFlags m_renderFlags;

        public string DebugName;

        public int RenderCounter;
        public int ShadowCastUpdateInterval = 0; //frames count to refresh visibility from sun
        public MyCullableRenderObject CullObject;
        public Task CastShadowTask;
        public MyCastShadowJob CastShadowJob;
        public double Distance; //if object was in frustum, distance is uptodate
        public MyManualCullableRenderObject ParentCullObject = null;

        /// <summary>
        /// Volume relative to parent cull object (or world volume when no parent)
        /// </summary>
        protected BoundingSphereD m_volume;

        CullingOptions m_cullingOptions = CullingOptions.Default;

        protected List<IMyRenderMessage> m_billboards = new List<IMyRenderMessage>();


        public MyRenderObject(uint id, string debugName, RenderFlags renderFlags = RenderFlags.Visible | RenderFlags.SkipIfTooSmall | RenderFlags.NeedsResolveCastShadow, CullingOptions cullingOptions = VRageRender.CullingOptions.Default)
            : base(id)
        {
            DebugName = debugName;
            Flags = MyElementFlag.EF_AABB_DIRTY;
            m_renderFlags = renderFlags;
            m_cullingOptions = cullingOptions;
        }

        public bool ShadowBoxLod
        {
            get { return (m_renderFlags & RenderFlags.ShadowLodBox) == RenderFlags.ShadowLodBox; }
        }

        public void SetDirty()
        {
            Flags |= MyElementFlag.EF_AABB_DIRTY;
        }

        public override void UpdateWorldAABB()
        {
            BoundingSphereD.CreateFromBoundingBox(ref m_aabb, out m_volume);

            base.UpdateWorldAABB();
        }

        public unsafe virtual void GetCorners(Vector3D* corners)
        {
            WorldAABB.GetCornersUnsafe(corners);
        }

        #region Properties

        public bool SkipIfTooSmall
        {
            get { return (m_renderFlags & RenderFlags.SkipIfTooSmall) > 0; }
            set
            {
                if (value)
                    m_renderFlags |= RenderFlags.SkipIfTooSmall;
                else
                    m_renderFlags &= ~RenderFlags.SkipIfTooSmall;
            }
        }

        public bool NeedsResolveCastShadow
        {
            get { return (m_renderFlags & RenderFlags.NeedsResolveCastShadow) > 0; }
            set
            {
                if (value)
                    m_renderFlags |= RenderFlags.NeedsResolveCastShadow;
                else
                    m_renderFlags &= ~RenderFlags.NeedsResolveCastShadow;
            }
        }

        public bool FastCastShadowResolve
        {
            get { return (m_renderFlags & RenderFlags.FastCastShadowResolve) > 0; }
            set
            {
                if (value)
                    m_renderFlags |= RenderFlags.FastCastShadowResolve;
                else
                    m_renderFlags &= ~RenderFlags.FastCastShadowResolve;
            }
        }

        public bool CastShadows
        {
            get { return (m_renderFlags & RenderFlags.CastShadows) > 0; }
            set
            {
                if (value)
                    m_renderFlags |= RenderFlags.CastShadows;
                else
                    m_renderFlags &= ~RenderFlags.CastShadows;
            }
        }

        public virtual bool NearFlag
        {
            get
            {
                return (m_renderFlags & RenderFlags.Near) != 0;
            }
            set
            {
                bool hasChanged = value != NearFlag;

                if (value)
                    m_renderFlags |= RenderFlags.Near;
                else
                    m_renderFlags &= ~RenderFlags.Near;
            }
        }

        public virtual bool Visible
        {
            get
            {
                return (m_renderFlags & RenderFlags.Visible) != 0;
            }
            set
            {
                bool hasChanged = value != Visible;

                if (value)
                    m_renderFlags |= RenderFlags.Visible;
                else
                    m_renderFlags &= ~RenderFlags.Visible;
            }
        }

        public CullingOptions CullingOptions
        {
            get { return m_cullingOptions; }
        }


        public bool UseCustomDrawMatrix
        {
            get { return (m_renderFlags & RenderFlags.UseCustomDrawMatrix) > 0; }
            set
            {
                if (value)
                    m_renderFlags |= RenderFlags.UseCustomDrawMatrix;
                else
                    m_renderFlags &= ~RenderFlags.UseCustomDrawMatrix;
            }
        }

        public virtual BoundingSphereD WorldVolume
        {
            get
            {
                return m_volume;
            }
        }

        #endregion


        public virtual void BeforeDraw()
        {
        }

        public virtual bool Draw()
        {
            if (Visible)
            {
                ClearBillboards();
                MyRender.AddRenderObjectToDraw(this);
                return true;
            }

            //Render character need update bones
            if (CastShadows)
                return true;

            return false;
        }

        public virtual void IssueOcclusionQueries()
        {
        }

        void ClearBillboards()
        {
            foreach (var billboardMessage in m_billboards)
            {
                MyRenderProxy.MessagePool.Return(billboardMessage);
            }
            m_billboards.Clear();
        }


        public virtual void LoadContent()
        {
        }

        public virtual void UnloadContent()
        {
            ClearBillboards();
        }

        public virtual void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<VRageRender.MyRender.MyRenderElement> elements, List<VRageRender.MyRender.MyRenderElement> transparentElements)
        {
        }

        public virtual void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
        }

        /// <summary>
        /// Draw debug.  
        /// </summary>
        /// <returns></returns>
        public virtual void DebugDraw()
        {
        }

        public List<IMyRenderMessage> Billboards
        {
            get { return m_billboards; }
        }

        #region Intersection Methods

        //  Calculates intersection of line with object.
        public virtual bool GetIntersectionWithLine(ref LineD line)
        {
            return false;
        }

        #endregion
    }
}
