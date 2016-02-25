using System.Diagnostics;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyClipmapCellProxy : IMyClipmapCell
    {
        MyActor m_actor;
        MeshId m_mesh;
        MatrixD m_worldMatrix;
        Vector3 m_scale;
        Vector3 m_translation;
        readonly int m_lod;
        internal MyClipmapScaleEnum ScaleGroup;
        BoundingBox m_localAabb;

        internal Vector3 Translation { get { return m_translation; } }
        internal MatrixD WorldMatrix { get { return m_worldMatrix; } }
        internal BoundingBox LocalAabb { get { return m_localAabb; } }
        internal MeshId MeshId { get { return m_mesh; } }
        internal int Lod { get { return m_lod; } }
        internal MyActor Actor { get { return m_actor; } }

        void IMyClipmapCell.UpdateMesh(MyRenderMessageUpdateClipmapCell msg)
        {
            MyMeshes.UpdateVoxelCell(m_mesh, msg.Metadata, msg.Batches);

            m_scale = msg.Metadata.PositionScale;
            m_translation = msg.Metadata.PositionOffset;
            m_localAabb = msg.Metadata.LocalAabb;

            UpdateActorInfo(true);
        }

        internal bool UpdateMergedMesh(MyRenderMessageUpdateMergedVoxelMesh msg)
        {
            if (!MyMeshes.UpdateMergedVoxelCell(MeshId, ref msg.Metadata, msg.MergedBatches))
                return false;
            
            m_scale = msg.Metadata.PositionScale;
            m_translation = msg.Metadata.PositionOffset;
            m_localAabb = msg.Metadata.LocalAabb;

            UpdateActorInfo();

            return true;
        }

        private void UpdateActorInfo(bool refreshFoliage = false)
        {
            MyVoxelRenderableComponent renderableComponent = m_actor.GetRenderable() as MyVoxelRenderableComponent;
            var matrix = MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix;

            renderableComponent.m_voxelScale = m_scale;
            renderableComponent.m_voxelOffset = m_translation;

            m_actor.SetMatrix(ref matrix);
            m_actor.SetAabb(m_localAabb.Transform(m_worldMatrix));
            renderableComponent.SetVoxelLod(m_lod, ScaleGroup);
            m_actor.MarkRenderDirty();

            UpdateFoliage(refreshFoliage);
        }

        void IMyClipmapCell.UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortIntoCullObjects)
        {
            m_worldMatrix = worldMatrix;

            MatrixD m = MatrixD.CreateScale(m_scale) * MatrixD.CreateTranslation(m_translation) * m_worldMatrix;
            m_actor.SetMatrix(ref m);
            m_actor.SetAabb(m_localAabb.Transform(m_worldMatrix));
        }

        void IMyClipmapCell.SetDithering(float dithering)
        {
            if (m_actor.GetRenderable() != null)
                m_actor.GetRenderable().SetDithering(dithering);
        }

        bool IMyClipmapCell.IsValid()
        {
            //System.Diagnostics.Debug.Assert(m_actor != null, "Cell already destroyed");
            //System.Diagnostics.Debug.Assert(!m_actor.IsDestroyed, "Cell already destroyed");
            return m_actor != null && !m_actor.IsDestroyed;
        }

		internal MyClipmapCellProxy(MyCellCoord cellCoord, ref MatrixD worldMatrix, Vector3D massiveCenter, float massiveRadius, RenderFlags additionalFlags = 0, bool mergedMesh = false)
        {
            m_worldMatrix = worldMatrix;

            m_actor = MyActorFactory.CreateVoxelCell();
            m_actor.SetMatrix(ref worldMatrix);

            m_lod = cellCoord.Lod;

            MyVoxelRenderableComponent renderableComponent = m_actor.GetRenderable() as MyVoxelRenderableComponent;

            m_mesh = !mergedMesh ? MyMeshes.CreateVoxelCell(cellCoord.CoordInLod, cellCoord.Lod) : MyMeshes.CreateMergedVoxelCell(cellCoord.CoordInLod, cellCoord.Lod);
            renderableComponent.SetVoxelLod(m_lod, ScaleGroup);
            renderableComponent.SetModel(m_mesh);
            renderableComponent.m_massiveCenter = massiveCenter;
            renderableComponent.m_massiveRadius = massiveRadius;
            renderableComponent.m_additionalFlags = MyProxiesFactory.GetRenderableProxyFlags(additionalFlags);
        }

        private void UpdateFoliage(bool refresh = false)
        {
            var foliageComponent = m_actor.GetFoliage();

            bool removeComponent = m_lod > MyFoliageComponent.LodLimit || !m_actor.IsVisible;
            bool shouldInitializeFoliage = false;

            if(refresh && foliageComponent != null)
                foliageComponent.InvalidateStreams();

            if (foliageComponent == null && !removeComponent)
                shouldInitializeFoliage = m_mesh.ShouldHaveFoliage();

            if (shouldInitializeFoliage)
                m_actor.AddComponent<MyFoliageComponent>(MyComponentFactory<MyFoliageComponent>.Create());
            else if (foliageComponent != null && removeComponent)
            {
                foliageComponent.InvalidateStreams();
                m_actor.RemoveComponent<MyFoliageComponent>(foliageComponent);
            }
        }

        internal void SetVisibility(bool value)
        {
            if (m_actor != null)
            {
                m_actor.SetVisibility(value);
                UpdateFoliage(!value);

                if (value)
                {
                    MyMeshes.ReloadVoxelCell(m_mesh);
                }
                else
                {
                    MyMeshes.UnloadVoxelCell(m_mesh);
                }
            }
        }

        internal void Unload()
        {
            if (m_actor != null && !m_actor.IsDestroyed)
            {
                MyActorFactory.Destroy(m_actor);
            }
            m_actor = null;
            MyMeshes.RemoveVoxelCell(m_mesh);
        }
    }
}
