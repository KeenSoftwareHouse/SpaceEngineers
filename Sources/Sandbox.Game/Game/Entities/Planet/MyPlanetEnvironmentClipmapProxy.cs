using System.Diagnostics;
using Sandbox.Game.WorldEnvironment;
using VRage.Game.Entity;
using VRageMath;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.World;
using VRageRender;

namespace Sandbox.Game.Entities.Planet
{
    internal class MyPlanetEnvironmentClipmapProxy : IMy2DClipmapNodeHandler
    {
        public long Id;

        public int Face;
        public int Lod;
        public Vector2I Coords;

        private int m_lodSet = -1;

        public MyEnvironmentSector EnvironmentSector;

        private MyPlanetEnvironmentComponent m_manager;
        private bool m_split;
        private bool m_closed;

        private bool m_stateCommited = false;

        private MyPlanetEnvironmentClipmapProxy m_parent;
        private readonly MyPlanetEnvironmentClipmapProxy[] m_children = new MyPlanetEnvironmentClipmapProxy[4];

        public int LodSet
        {
            get { return m_lodSet; }
            protected set
            {
                m_lodSet = value;
                m_stateCommited = false;
            }
        }

        public void Init(IMy2DClipmapManager parent, int x, int y, int lod, ref BoundingBox2D bounds)
        {
            m_manager = (MyPlanetEnvironmentComponent)parent;

            var bounds3D = new BoundingBoxD(new Vector3D(bounds.Min, 0), new Vector3D(bounds.Max, 0));
            Lod = lod;

            Face = m_manager.ActiveFace;

            var matrix = m_manager.ActiveClipmap.WorldMatrix;

            bounds3D = bounds3D.TransformFast(matrix);

            Coords = new Vector2I(x, y);

            Id = MyPlanetSectorId.MakeSectorId(x, y, m_manager.ActiveFace, lod);

            m_manager.RegisterProxy(this);

            MyEnvironmentSectorParameters sectorParams;

            matrix.Translation = Vector3D.Zero;

            sectorParams.SurfaceBasisX = Vector3.Transform(new Vector3(bounds.Width / 2, 0, 0), matrix);
            sectorParams.SurfaceBasisY = Vector3.Transform(new Vector3(0, bounds.Height / 2, 0), matrix);
            sectorParams.Center = bounds3D.Center;

            if (lod > m_manager.MaxLod) return;

            if (!m_manager.TryGetSector(Id, out EnvironmentSector))
            {
                sectorParams.SectorId = Id;
                sectorParams.EntityId = MyPlanetSectorId.MakeSectorId(x, y, m_manager.ActiveFace, lod);

                sectorParams.Bounds = m_manager.GetBoundingShape(ref sectorParams.Center, ref sectorParams.SurfaceBasisX, ref sectorParams.SurfaceBasisY); ;

                sectorParams.Environment = m_manager.EnvironmentDefinition;

                sectorParams.DataRange = new BoundingBox2I(Coords << lod, ((Coords + 1) << lod) - 1);

                sectorParams.Provider = m_manager.Providers[m_manager.ActiveFace];

                EnvironmentSector = m_manager.EnvironmentDefinition.CreateSector();
                EnvironmentSector.Init(m_manager, ref sectorParams);

                m_manager.Planet.AddChildEntity((MyEntity)EnvironmentSector);
            }

            m_manager.EnqueueOperation(this, lod);
            LodSet = lod;

            EnvironmentSector.OnLodCommit += sector_OnMyLodCommit;
        }

        public void Close()
        {
            if (m_closed) return;

            m_closed = true;

            if (EnvironmentSector != null)
            {
                m_manager.MarkProxyOutgoingProxy(this);

                NotifyDependants(true);

                if (m_split)
                {
                    for (int i = 0; i < 4; ++i)
                        WaitFor(m_children[i]);
                }
                else if (m_parent != null)
                {
                    WaitFor(m_parent);
                }

                // This because we may have different enqueued
                if (m_manager.IsQueued(this) || m_dependencies.Count == 0)
                    EnqueueClose(true);

                // If no dependencies we can finish
                if (m_dependencies.Count == 0)
                    CloseCommit(true);
            }
            else
            {
                m_manager.UnregisterProxy(this);
            }
        }

        private void EnqueueClose(bool clipmapUpdate)
        {
            if (EnvironmentSector.IsClosed) return;

            Debug.Assert(m_closed);
            if (clipmapUpdate)
            {
                Debug.Assert(m_manager.ActiveFace == Face);
                m_manager.EnqueueOperation(this, -1, !m_split);
                LodSet = -1;
            }
            else
            {
                Debug.Assert(m_manager.ActiveFace != Face);
                EnvironmentSector.SetLod(-1);
                LodSet = -1;
                if (!m_split)
                    m_manager.CheckOnGraphicsClose(EnvironmentSector);
            }
        }
        
        public void InitJoin(IMy2DClipmapNodeHandler[] children)
        {
            m_split = false;
            m_closed = false;

            if (EnvironmentSector != null)
            {
                m_manager.UnmarkProxyOutgoingProxy(this);
                m_manager.EnqueueOperation(this, Lod);
                LodSet = Lod;

                for (int i = 0; i < 4; ++i)
                    m_children[i] = null;
            }
            else
            {
                m_manager.RegisterProxy(this);
            }
        }

        public unsafe void Split(BoundingBox2D* childBoxes, ref IMy2DClipmapNodeHandler[] children)
        {
            m_split = true;

            for (int i = 0; i < 4; ++i)
                children[i].Init(m_manager, (Coords.X << 1) + (i & 1), (Coords.Y << 1) + ((i >> 1) & 1), Lod - 1, ref childBoxes[i]);

            if (EnvironmentSector != null) // if we have so do our children.
            {
                for (int i = 0; i < 4; ++i)
                {
                    m_children[i] = (MyPlanetEnvironmentClipmapProxy)children[i];
                    m_children[i].m_parent = this;
                }
            }
        }

        #region Dependency Management

        private readonly HashSet<MyPlanetEnvironmentClipmapProxy> m_dependencies = new HashSet<MyPlanetEnvironmentClipmapProxy>();
        private readonly HashSet<MyPlanetEnvironmentClipmapProxy> m_dependants = new HashSet<MyPlanetEnvironmentClipmapProxy>();

        private void WaitFor(MyPlanetEnvironmentClipmapProxy proxy)
        {
            if (proxy.LodSet == -1) return;

            m_dependencies.Add(proxy);
            proxy.m_dependants.Add(this);
        }

        private void sector_OnMyLodCommit(MyEnvironmentSector sector, int lod)
        {
            Debug.Assert(sector == EnvironmentSector);
            Debug.Assert((m_closed == (lod == -1)) || m_dependencies.Count > 0, "Lod set does not match expected lod.");

            if (lod == LodSet)
            {
                m_stateCommited = true;

                if (m_dependencies.Count == 0)
                {
                    if (lod == -1 && m_closed)
                        CloseCommit(false);
                    else
                        NotifyDependants(false);
                }
            } // if dependants then we actually commited a different lod, in that case we just ignore this
        }

        private void CloseCommit(bool clipmapUpdate)
        {
            if (!m_split)
            {
                m_manager.UnregisterOutgoingProxy(this);
                EnvironmentSector.OnLodCommit -= sector_OnMyLodCommit;

                Debug.Assert((m_closed && LodSet == -1 && (EnvironmentSector.LodLevel == -1 || m_manager.QueuedLod(this) == -1)) || EnvironmentSector.Closed);
            }

            // when we skip lodding in because we were immediately hidden
            NotifyDependants(clipmapUpdate);
        }

        private void NotifyDependants(bool clipmapUpdate)
        {
            Debug.Assert(m_dependencies.Count == 0);

            foreach (var dep in m_dependants)
            {
                Debug.Assert(dep != this);
                dep.Notify(this, clipmapUpdate);
            }

            m_dependants.Clear();
        }

        private void ClearDependencies()
        {
            foreach (var dep in m_dependencies)
            {
                dep.m_dependants.Remove(this);
            }

            m_dependencies.Clear();
        }

        private void Notify(MyPlanetEnvironmentClipmapProxy proxy, bool clipmapUpdate)
        {
            if (m_dependencies.Count == 0) return;

            m_dependencies.Remove(proxy);

            if (m_dependencies.Count == 0 && m_closed)
            {
                EnqueueClose(clipmapUpdate);
                if (EnvironmentSector.IsClosed || EnvironmentSector.LodLevel == -1)
                {
                    CloseCommit(clipmapUpdate);
                }
            }
        }

        #endregion

        internal void DebugDraw(bool outgoing = false)
        {
            if (EnvironmentSector != null)
            {
                var center = (EnvironmentSector.Bounds[4] + EnvironmentSector.Bounds[7]) / 2;

                var offset = MySector.MainCamera.UpVector * 2 * (1 << Lod);

                var desc = string.Format("Lod: {4}; Dependants: {0}; Dependencies: {1}\nSplit: {2}; Closed:{3}", m_dependants.Count, m_dependencies.Count, m_split, m_closed, Lod);

                MyRenderProxy.DebugDrawText3D(center += offset, desc, outgoing ? Color.Yellow : Color.White, 1, true);

                ((MyEntity) EnvironmentSector).DebugDraw();
            }
        }
    }
}