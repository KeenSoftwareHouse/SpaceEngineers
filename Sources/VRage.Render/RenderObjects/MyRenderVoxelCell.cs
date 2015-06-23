#region Using Statements

using System.Collections.Generic;
using SharpDX.Direct3D9;
using VRage;
using VRage.Import;
using VRageMath;
using VRageRender.Graphics;
using VRageRender.Utils;
using VRage.Voxels;
using System.Diagnostics;


#endregion

namespace VRageRender
{
    class MyRenderVoxelCell : MyRenderObject, IMyClipmapCell
    {
        public struct EffectArgs
        {
            public Vector3 CellOffset;
            public Vector3 CellScale;
            public Vector3 CellRelativeCamera;
            public Vector2 Bounds;
            public float MorphDebug;
        }

        public static readonly Color[] LOD_COLORS = new Color[]
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Purple,
            Color.Cyan,
            Color.White,
            Color.CornflowerBlue,
            Color.Chartreuse,
            Color.Coral,
        };

        private readonly List<MyRenderVoxelBatch> m_batches = new List<MyRenderVoxelBatch>();

        private MyCellCoord m_coord;
        private BoundingBox m_localAabb;
        private MatrixD m_worldMatrix;

        private Vector3D m_cellOffset;
        private Vector3 m_cellScale;
        private readonly MyClipmapScaleEnum m_scaleGroup;

        static MyRenderMeshMaterial m_fakeVoxelMaterial = new MyRenderMeshMaterial("VoxelMaterial", "", null, null, null);

        public MyRenderVoxelCell(MyClipmapScaleEnum scaleGroup, MyCellCoord coord, ref MatrixD worldMatrix)
            : base(0, "MyRenderVoxelCell", RenderFlags.Visible | RenderFlags.CastShadows, CullingOptions.VoxelMap)
        {
            m_scaleGroup = scaleGroup;
            m_coord = coord;
            m_worldMatrix = worldMatrix;
            m_fakeVoxelMaterial.DrawTechnique = MyMeshDrawTechnique.VOXEL_MAP;
        }

        public override void UpdateWorldAABB()
        {
            m_aabb = (BoundingBoxD)m_localAabb;
            m_aabb = m_aabb.Transform(ref m_worldMatrix);

            base.UpdateWorldAABB();
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            if (MyRender.Settings.SkipVoxels)
                return;

            Debug.Assert(lodTypeEnum == MyLodTypeEnum.LOD0 || lodTypeEnum == MyLodTypeEnum.LOD_BACKGROUND);

            foreach (MyRenderVoxelBatch batch in m_batches)
            {
                if (batch.IndexBuffer == null)
                    continue;

                MyRender.MyRenderElement renderElement;
                MyRender.AllocateRenderElement(out renderElement);

                if (!MyRender.IsRenderOverloaded)
                {
                    //renderElement.DebugName = this.Name;
                    SetupRenderElement(batch, renderElement);

                    renderElement.DrawTechnique = MyMeshDrawTechnique.VOXEL_MAP;
                    renderElement.Material = m_fakeVoxelMaterial;

                    elements.Add(renderElement);
                }
                else if (renderElement == null)
                {
                    break;
                }

            }
        }

        private void SetupRenderElement(MyRenderVoxelBatch batch, MyRender.MyRenderElement renderElement)
        {
            renderElement.RenderObject = this;

            renderElement.VertexDeclaration = MyVertexFormatVoxelSingleMaterial.VertexDeclaration;
            renderElement.VertexStride = MyVertexFormatVoxelSingleMaterial.Stride;
            renderElement.VertexCount = batch.VertexCount;
            renderElement.VertexBuffer = batch.VertexBuffer;

            renderElement.InstanceBuffer = null;

            renderElement.IndexBuffer = batch.IndexBuffer;
            renderElement.IndexStart = 0;
            if (renderElement.IndexBuffer != null)
            {
                renderElement.TriCount = batch.IndexCount / 3;
            }
            renderElement.WorldMatrix = m_worldMatrix;
            renderElement.WorldMatrix.Translation += m_cellOffset;
            renderElement.VoxelBatch = batch;
        }

        public override void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
            if (MyRender.Settings.SkipVoxels)
                return;


            //  Get non-empty render cells visible to the frustum and sort them by distance to camera
            MyRender.GetRenderProfiler().StartProfilingBlock("GetElements from MyVoxelMap");

            foreach (MyRenderVoxelBatch batch in m_batches)
            {
                if (batch.IndexCount == 0)
                    continue;

                MyRender.MyRenderElement renderElement;
                MyRender.AllocateRenderElement(out renderElement);

                if (!MyRender.IsRenderOverloaded)
                {
                    SetupRenderElement(batch, renderElement);
                    renderElement.Dithering = 0;

                    renderElements.Add(renderElement);
                }
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        private unsafe void AddBatch(MyClipmapCellBatch batch)
        {
            //  This will just preload textures used by this material - so they are ready in memory when first time drawn
            MyRenderVoxelMaterials.Get((byte)batch.Material0).GetTextures();

            MyRenderVoxelBatch newBatch = new MyRenderVoxelBatch();
            string debugName = "VoxelBatchSingle";

            newBatch.Type = MyRenderVoxelBatchType.SINGLE_MATERIAL;
            newBatch.Lod = m_coord.Lod;
            newBatch.Material0 = (byte)batch.Material0;
            newBatch.Material1 = batch.Material1 == -1 ? (byte?)null : (byte)batch.Material1;
            newBatch.Material2 = batch.Material2 == -1 ? (byte?)null : (byte)batch.Material2;
            if (newBatch.Material1 != null || newBatch.Material2 != null)
            {
                newBatch.Type = MyRenderVoxelBatchType.MULTI_MATERIAL;
                debugName = "VoxelBatchMulti";
            }


            //  Vertex buffer
            int vbSize = sizeof(MyVertexFormatVoxelSingleData) * batch.Vertices.Length;
            newBatch.VertexCount = batch.Vertices.Length;

            // When Usage.Dynamic was not there, it crashed on nVidia cards
            newBatch.VertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, vbSize, Usage.WriteOnly | Usage.Dynamic, VertexFormat.None, Pool.Default);
            newBatch.VertexBuffer.SetData(batch.Vertices, LockFlags.Discard);
            newBatch.VertexBuffer.Tag = newBatch;
            newBatch.VertexBuffer.DebugName = debugName;
            MyPerformanceCounter.PerAppLifetime.VoxelVertexBuffersSize += vbSize;

            //  Index buffer
            int ibSize = sizeof(short) * batch.Indices.Length;
            newBatch.IndexCount = batch.Indices.Length;

            // When Usage.Dynamic was not there, it crashed on nVidia cards
            newBatch.IndexBuffer = new IndexBuffer(MyRender.GraphicsDevice, ibSize, Usage.WriteOnly | Usage.Dynamic, Pool.Default, true);
            newBatch.IndexBuffer.SetData(batch.Indices, LockFlags.Discard);
            newBatch.IndexBuffer.DebugName = debugName;
            MyPerformanceCounter.PerAppLifetime.VoxelIndexBuffersSize += ibSize;


            newBatch.UpdateSortOrder();

            m_batches.Add(newBatch);
        }

        /// <summary>
        /// Draw debug.  
        /// </summary>
        /// <returns></returns>
        public override void DebugDraw()
        {
            if (!MyRender.Settings.DebugRenderClipmapCells)
                return;

            if (m_coord.Lod >= LOD_COLORS.Length)
                return;

            const double DRAW_DIST = 8.0;
            const double TARGET_DIST = 7.0;
            var targetPoint = MyRenderCamera.Position + (Vector3D)MyRenderCamera.ForwardVector * TARGET_DIST;

            var worldAabb = m_aabb;
            if (true)
            {
                MyDebugDraw.DrawAABBLine(ref m_aabb, ref LOD_COLORS[m_coord.Lod], 1f, true);
            }

            if (worldAabb.Distance(targetPoint) < DRAW_DIST && m_coord.Lod == 0)
            {
                if (false)
                {
                    MyDebugDraw.DrawAABBLine(ref m_aabb, ref LOD_COLORS[m_coord.Lod], 1f, true);
                }
            }
        }

        public override void UnloadContent()
        {
            Dispose();
        }

        private void Dispose()
        {
            foreach (MyRenderVoxelBatch batch in m_batches)
            {
                batch.Dispose();
            }
            m_batches.Clear();
        }

        public Vector3D PositionLeftBottomCorner
        {
            get { return m_worldMatrix.Translation; }
            set { m_worldMatrix.Translation = value; }
        }

        #region Intersection Methods

        //  Calculates intersection of line with object.
        public override bool GetIntersectionWithLine(ref VRageMath.LineD line)
        {
            double? t = m_aabb.Intersects(new RayD(line.From, line.Direction));
            if (t.HasValue && t.Value < line.Length && t.Value > 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        void IMyClipmapCell.UpdateMesh(MyRenderMessageUpdateClipmapCell msg)
        {
            Dispose();
            m_cellOffset = msg.PositionOffset;
            m_cellScale = msg.PositionScale;
            m_localAabb = msg.MeshAabb;
            foreach (var batch in msg.Batches)
            {
                AddBatch(batch);
            }

            SetDirty();
            UpdateWorldAABB();
            MyRender.UpdateRenderObject(this, true);
        }

        void IMyClipmapCell.UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortIntoCullObjects)
        {
            m_worldMatrix = worldMatrix;
            SetDirty();
            UpdateWorldAABB();
            MyRender.UpdateRenderObject(this, sortIntoCullObjects);
        }

        internal void GetEffectArgs(out EffectArgs effectParams)
        {
            effectParams.CellOffset = (Vector3)m_cellOffset;
            effectParams.CellScale = m_cellScale;
            effectParams.CellRelativeCamera = (Vector3)(MyRenderCamera.Position - (m_worldMatrix.Translation + m_cellOffset));
            effectParams.MorphDebug = MathHelper.Clamp(MyClipmap.DebugClipmapMostDetailedLod - m_coord.Lod, 0f, 1f);

            Vector2 lodBounds;
            MyClipmap.ComputeLodViewBounds(m_scaleGroup, m_coord.Lod, out lodBounds.X, out lodBounds.Y);
            effectParams.Bounds = lodBounds;
        }
    }



}
