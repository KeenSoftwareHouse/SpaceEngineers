#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ParallelTasks;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender.Shadows;
using VRageRender.Textures;
using VRageRender.Utils;
using System.IO;
using VRage;
//using MyUtils = VRageRender.Utils.MyUtils;


#endregion

namespace VRageRender
{
    class MyRenderEntity : MyRenderTransformObject
    {
        internal class MyLodModel
        {
            public float Distance; //in meters
            public MyRenderModel Model;
            public MyRenderModel ShadowModel;
            public SharpDX.Direct3D9.VertexDeclaration VertexDeclaration;
            public int VertexStride;
            public List<MyRenderMeshMaterial> MeshMaterials = new List<MyRenderMeshMaterial>();
        }

        protected List<MyLodModel> m_lods = new List<MyLodModel>();

        public MatrixD DrawMatrix;
        protected MyMeshDrawTechnique m_drawTechnique;

        public MyRenderInstanceBuffer m_instanceBuffer;
        int m_instanceStart;
        int m_instanceCount;

        public Vector3 EntityColor = new Vector3(1, 1, 1);
        public float EntityDithering = 0;
        public Vector3 EntityColorMaskHSV;
        public float MaxViewDistance = float.MaxValue;


        bool m_isDataSet;

        public MyRenderEntity(uint id, string debugName, string model, MatrixD worldMatrix, MyMeshDrawTechnique drawTechnique, RenderFlags renderFlags)
            : base(id, debugName, worldMatrix, renderFlags)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(model));

            var renderModel = AddLODs(model);
            if (renderModel != null)
            {
                m_localAABB = (BoundingBoxD)renderModel.BoundingBox;
                m_localVolume = (BoundingSphereD)renderModel.BoundingSphere;
                m_localVolumeOffset = (Vector3D)renderModel.BoundingSphere.Center;
            }

            m_drawTechnique = drawTechnique;
            m_isDataSet = true;
        }

        public MyRenderEntity(uint id, string debugName, MatrixD worldMatrix, MyMeshDrawTechnique drawTechnique, RenderFlags renderFlags)
            : base(id, debugName, worldMatrix, renderFlags)
        {
            m_drawTechnique = drawTechnique;
            m_isDataSet = false;
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            if (MyRender.CurrentRenderSetup.CallerID.HasValue && MyRender.CurrentRenderSetup.CallerID.Value == MyRenderCallerEnum.EnvironmentMap && this.EntityDithering > 0)
            {
                return;
            }

            Debug.Assert(m_isDataSet, "Data is not set, have you forgotten to send SetRenderEntityData message?");

            MyRenderModel currentModel = m_lods[0].Model;
            List<MyRenderMeshMaterial> currentMaterials = m_lods[0].MeshMaterials;

            var volume = WorldVolume;
            var lodIndex = 0;

            if (lodTypeEnum == MyLodTypeEnum.LOD_NEAR)
            {

            }
            else
                if (lodTypeEnum == MyLodTypeEnum.LOD0)
                {
                    if (m_lods.Count > 1)
                    {
                        var distance = MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref MyRenderCamera.Position, ref volume);

                        if (distance > MyRenderConstants.RenderQualityProfile.LodTransitionDistanceBackgroundEnd)
                            return;

                        for (int i = 1; i < m_lods.Count; i++)
                        {
                            var lod = m_lods[i];
                            if (distance < lod.Distance)
                                break;

                            currentModel = lod.Model;
                            currentMaterials = lod.MeshMaterials;
                            lodIndex = i;
                        }
                    }
                }
                else
                    return; //nothing to render in LOD1

            if (currentModel == null)
                return;

            int instanceCount = m_instanceBuffer != null ? m_instanceCount : 1;

            MyRender.ModelTrianglesCountStats += currentModel.GetTrianglesCount() * instanceCount;

            MyPerformanceCounter.PerCameraDrawWrite.EntitiesRendered++;

            CollectRenderElements(elements, transparentElements, currentModel, currentMaterials, lodIndex);
        }

        internal void SetInstanceData(MyRenderInstanceBuffer buffer, int instanceStart, int instanceCount, BoundingBoxD localAabb)
        {
            m_localAABB = localAabb;
            m_localVolume = BoundingSphereD.CreateFromBoundingBox(m_localAABB);
            m_localVolumeOffset = m_localVolume.Center;
            SetDirty();

            m_instanceStart = instanceStart;
            m_instanceCount = instanceCount;

            if (m_instanceBuffer != buffer)
            {
                m_instanceBuffer = buffer;
                UnloadVertexDeclaration();
                CreateVertexDeclaration();
            }
        }

        internal void CollectRenderElements(List<VRageRender.MyRender.MyRenderElement> renderElements, List<VRageRender.MyRender.MyRenderElement> transparentRenderElements, MyRenderModel model, List<MyRenderMeshMaterial> materials, int lodIndex)
        {
            if (model.LoadState == LoadState.Unloaded)
            {
                //model.LoadInDraw(LoadingMode.Background);
                model.LoadInDraw(LoadingMode.Immediate);
                return;
            }
            if (model.LoadState == LoadState.Loading)
                return;

            if (m_instanceBuffer != null && m_instanceCount == 0)
                return;

            var drawMatrix = GetWorldMatrixForDraw();

            int meshCount = model.GetMeshList().Count;
            for (int i = 0; i < meshCount; i++)
            {
                MyRenderMesh mesh = model.GetMeshList()[i];

                MyRenderMeshMaterial material = model.HasSharedMaterials ? mesh.Material : materials[i];

                if (!material.Enabled)
                    continue;

                if (material.DrawTechnique == MyMeshDrawTechnique.GLASS && EntityDithering == 0)
                {
                    m_drawTechnique = MyMeshDrawTechnique.GLASS;
                    continue;
                }

                //Preload needs to be here because of reloadcontent
                material.PreloadTexture(LoadingMode.Background);

                VRageRender.MyRender.MyRenderElement renderElement;
                VRageRender.MyRender.AllocateRenderElement(out renderElement);

                if (!MyRender.IsRenderOverloaded)
                {
                    //renderElement.DebugName = entity.Name;
                    renderElement.RenderObject = this;

                    renderElement.VertexBuffer = model.VertexBuffer;
                    renderElement.IndexBuffer = model.IndexBuffer;
                    renderElement.VertexCount = model.GetVerticesCount();
                    renderElement.VertexDeclaration = model.GetVertexDeclaration();
                    renderElement.VertexStride = model.GetVertexStride();
                    renderElement.InstanceBuffer = null;
                    renderElement.BonesUsed = mesh.BonesUsed;

                    renderElement.IndexStart = mesh.IndexStart;
                    renderElement.TriCount = mesh.TriCount;

                    renderElement.WorldMatrixForDraw = drawMatrix;
                    renderElement.WorldMatrix = WorldMatrix;

                    renderElement.Material = material;
                    renderElement.DrawTechnique = m_drawTechnique == MyMeshDrawTechnique.MESH || m_drawTechnique == MyMeshDrawTechnique.GLASS ? material.DrawTechnique : m_drawTechnique;
                    renderElement.Color = EntityColor * material.DiffuseColor;
                    renderElement.Dithering = mesh.GlassDithering == 0 ? EntityDithering : mesh.GlassDithering;
                    renderElement.ColorMaskHSV = EntityColorMaskHSV;

                    if (m_instanceBuffer != null)
                    {
                        renderElement.VertexStride = m_lods[lodIndex].VertexStride;
                        renderElement.VertexDeclaration = m_lods[lodIndex].VertexDeclaration;
                        renderElement.InstanceBuffer = m_instanceBuffer.InstanceBuffer;
                        renderElement.InstanceStart = m_instanceStart;
                        renderElement.InstanceCount = m_instanceCount;
                        renderElement.InstanceStride = m_instanceBuffer.Stride;

                        if (m_instanceBuffer.Type == MyRenderInstanceBufferType.Generic)
                            renderElement.DrawTechnique = renderElement.DrawTechnique == MyMeshDrawTechnique.ALPHA_MASKED ? MyMeshDrawTechnique.MESH_INSTANCED_GENERIC_MASKED : MyMeshDrawTechnique.MESH_INSTANCED_GENERIC;
                        else
                            renderElement.DrawTechnique = model.BoneIndices.Length > 0 ? MyMeshDrawTechnique.MESH_INSTANCED_SKINNED : MyMeshDrawTechnique.MESH_INSTANCED;
                    }

                    Debug.Assert(renderElement.VertexBuffer != null, "Vertex buffer cannot be null!");
                    Debug.Assert(renderElement.IndexBuffer != null, "Index buffer cannot be null!");

                    if (material.DrawTechnique == MyMeshDrawTechnique.HOLO)
                    {
                        if (transparentRenderElements != null)
                            transparentRenderElements.Add(renderElement);
                    }
                    else
                    {
                        renderElements.Add(renderElement);
                    }
                }
            }
        }


        public override void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
            MyRenderModel model;

            int lodIndex = m_lods.Count > (int)lodTypeEnum ? (int)lodTypeEnum : 0;

            if (ShadowBoxLod && lodTypeEnum == MyLodTypeEnum.LOD1)
            {
                model = MyDebugDraw.ModelBoxLowRes;
            }
            else
            {
                model = m_lods[lodIndex].ShadowModel == null ? m_lods[lodIndex].Model : m_lods[lodIndex].ShadowModel;
            }

            if (model == null || model.LoadState != LoadState.Loaded)
                return;

            var drawMatrix = GetWorldMatrixForDraw();

            if (m_drawTechnique == MyMeshDrawTechnique.GLASS)
            {
                int meshCount = model.GetMeshList().Count;
                for (int i = 0; i < meshCount; i++)
                {
                    MyRenderMesh mesh = model.GetMeshList()[i];

                    MyRenderMeshMaterial material = mesh.Material;

                    if (!material.Enabled)
                        continue;

                    VRageRender.MyRender.MyRenderElement renderElement;
                    VRageRender.MyRender.AllocateRenderElement(out renderElement);

                    if (!MyRender.IsRenderOverloaded)
                    {
                        //renderElement.DebugName = entity.Name;
                        renderElement.RenderObject = this;

                        renderElement.VertexBuffer = model.VertexBuffer;
                        renderElement.IndexBuffer = model.IndexBuffer;
                        renderElement.VertexCount = model.GetVerticesCount();
                        renderElement.VertexDeclaration = model.GetVertexDeclaration();
                        renderElement.VertexStride = model.GetVertexStride();
                        renderElement.InstanceBuffer = null;
                        renderElement.BonesUsed = mesh.BonesUsed;

                        renderElement.IndexStart = mesh.IndexStart;
                        renderElement.TriCount = mesh.TriCount;

                        renderElement.WorldMatrixForDraw = drawMatrix;
                        renderElement.WorldMatrix = WorldMatrix;

                        renderElement.Material = material;
                        renderElement.DrawTechnique = m_drawTechnique == MyMeshDrawTechnique.MESH || m_drawTechnique == MyMeshDrawTechnique.GLASS ? material.DrawTechnique : m_drawTechnique;
                        renderElement.Color = EntityColor * material.DiffuseColor;
                        renderElement.Dithering = EntityDithering;
                        renderElement.ColorMaskHSV = EntityColorMaskHSV;


                        if (material.DrawTechnique == MyMeshDrawTechnique.GLASS)
                        {
                            renderElement.Dithering = mesh.GlassDithering;
                        }
                        else
                            renderElement.Dithering = 0;


                        if (m_instanceBuffer != null)
                        {
                            renderElement.VertexStride = m_lods[lodIndex].VertexStride;
                            renderElement.VertexDeclaration = m_lods[lodIndex].VertexDeclaration;
                            renderElement.InstanceBuffer = m_instanceBuffer.InstanceBuffer;
                            renderElement.InstanceStart = m_instanceStart;
                            renderElement.InstanceCount = m_instanceCount;
                            renderElement.InstanceStride = m_instanceBuffer.Stride;
                            renderElement.DrawTechnique = model.BoneIndices.Length > 0 ? MyMeshDrawTechnique.MESH_INSTANCED_SKINNED : MyMeshDrawTechnique.MESH_INSTANCED;
                        }

                        Debug.Assert(renderElement.VertexBuffer != null, "Vertex buffer cannot be null!");
                        Debug.Assert(renderElement.IndexBuffer != null, "Index buffer cannot be null!");

                        if (material.DrawTechnique == MyMeshDrawTechnique.HOLO)
                        {
                            if (transparentRenderElements != null)
                                transparentRenderElements.Add(renderElement);
                        }
                        else
                        {
                            renderElements.Add(renderElement);
                        }
                    }
                }
            }
            else
            {
                if (!MyRender.IsRenderOverloaded)
                {
                    int meshCount = model.GetMeshList().Count;
                    bool separateMeshes = false;
                    for (int i = 0; i < meshCount; i++)
                    {
                        MyRenderMesh mesh = model.GetMeshList()[i];
                        if (mesh.BonesUsed != null)
                        {
                            separateMeshes = true;
                            break;
                        }
                    }

                    if (!separateMeshes)
                    {
                        MyRender.MyRenderElement renderElement;
                        MyRender.AllocateRenderElement(out renderElement);

                        renderElement.RenderObject = this;

                        renderElement.VertexBuffer = model.VertexBuffer;
                        renderElement.IndexBuffer = model.IndexBuffer;
                        renderElement.VertexCount = model.GetVerticesCount();
                        renderElement.VertexDeclaration = model.GetVertexDeclaration();
                        renderElement.VertexStride = model.GetVertexStride();
                        renderElement.InstanceBuffer = null;
                        renderElement.Dithering = 0;
                        renderElement.BonesUsed = null;

                        if (m_instanceBuffer != null)
                        {
                            renderElement.VertexStride = m_lods[lodIndex].VertexStride;
                            renderElement.VertexDeclaration = m_lods[lodIndex].VertexDeclaration;
                            renderElement.InstanceBuffer = m_instanceBuffer.InstanceBuffer;
                            renderElement.InstanceStart = m_instanceStart;
                            renderElement.InstanceCount = m_instanceCount;
                            renderElement.InstanceStride = m_instanceBuffer.Stride;
                        }

                        renderElement.IndexStart = 0;
                        if (renderElement.IndexBuffer != null)
                        {
                            renderElement.TriCount = model.GetTrianglesCount();
                        }

                        //renderElement.DebugName = entity.Name;
                        renderElement.WorldMatrix = WorldMatrix;
                        renderElement.WorldMatrixForDraw = drawMatrix;

                        renderElements.Add(renderElement);
                    }
                    else
                    {
                        for (int i = 0; i < meshCount; i++)
                        {
                            MyRenderMesh mesh = model.GetMeshList()[i];

                            VRageRender.MyRender.MyRenderElement renderElement;
                            VRageRender.MyRender.AllocateRenderElement(out renderElement);

                            renderElement.RenderObject = this;

                            renderElement.VertexBuffer = model.VertexBuffer;
                            renderElement.IndexBuffer = model.IndexBuffer;
                            renderElement.VertexCount = model.GetVerticesCount();
                            renderElement.VertexDeclaration = model.GetVertexDeclaration();
                            renderElement.VertexStride = model.GetVertexStride();
                            renderElement.InstanceBuffer = null;
                            renderElement.Dithering = 0;

                            renderElement.BonesUsed = mesh.BonesUsed;

                            renderElement.IndexStart = mesh.IndexStart;
                            renderElement.TriCount = mesh.TriCount;

                            if (m_instanceBuffer != null)
                            {
                                renderElement.VertexStride = m_lods[lodIndex].VertexStride;
                                renderElement.VertexDeclaration = m_lods[lodIndex].VertexDeclaration;
                                renderElement.InstanceBuffer = m_instanceBuffer.InstanceBuffer;
                                renderElement.InstanceStart = m_instanceStart;
                                renderElement.InstanceCount = m_instanceCount;
                                renderElement.InstanceStride = m_instanceBuffer.Stride;
                                renderElement.DrawTechnique = model.BoneIndices.Length > 0 ? MyMeshDrawTechnique.MESH_INSTANCED_SKINNED : MyMeshDrawTechnique.MESH_INSTANCED;
                            }

                            renderElement.WorldMatrix = WorldMatrix;
                            renderElement.WorldMatrixForDraw = drawMatrix;

                            renderElements.Add(renderElement);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the world matrix for draw.
        /// </summary>
        /// <returns></returns>
        public override MatrixD GetWorldMatrixForDraw()
        {
            if (UseCustomDrawMatrix)
                return DrawMatrix;

            return base.GetWorldMatrixForDraw();
        }

        /// <summary>
        /// Draw debug.  
        /// </summary>
        /// <returns></returns>
        public override void DebugDraw()
        {
            return;
            //if (MyMwcFinalBuildConstants.DrawHelperPrimitives)
            {
                // DebugDrawVolume();
                DebugDrawOBB();
                MyDebugDraw.DrawAxis(WorldMatrix, (float)m_localVolume.Radius, 1, false);

            }

            /*
            if (MyMwcFinalBuildConstants.DrawJLXCollisionPrimitives)
            {
                DebugDrawPhysics();
            }

            if (MyMwcFinalBuildConstants.DrawNormalVectors)
            {
                DebugDrawNormalVectors();
            } */

        }

        #region Intersection Methods

        //  Calculates intersection of line with object.
        public override bool GetIntersectionWithLine(ref LineD line)
        {
            var t = m_aabb.Intersects(new RayD(line.From, line.Direction));
            if (t.HasValue && t.Value < line.Length && t.Value > 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        public override void LoadContent()
        {
            base.LoadContent();

            foreach (var lod in m_lods)
            {
                if (lod.Model != null)
                {
                    lod.Model.LoadData();
                    lod.Model.LoadContent();
                    lod.Model.LoadInDraw();
                }
            }

            CreateVertexDeclaration();
        }

        private void CreateVertexDeclaration()
        {
            if (m_instanceBuffer != null)
            {
                foreach (var lod in m_lods)
                {
                    if (lod.Model != null)
                    {
                        lod.VertexDeclaration = CreateInstanceDeclaration(lod.Model);
                        lod.VertexStride = lod.Model.GetVertexStride();
                    }
                }
            }
        }

        // This is more hack than correct solution, we have to find lods in same dir as models, because of MODs
        private string GetLodPath(string modelPath, string lodPath)
        {
            return Path.Combine(Path.GetDirectoryName(modelPath), Path.GetFileName(lodPath));
        }

        MyRenderModel AddLODs(string model)
        {
            MyLodModel lodModel = AddLOD(0, model);

            if (lodModel == null)
                return null;

            foreach (var lodDesc in lodModel.Model.LODs)
            {
                string lodModelPath = lodDesc.Model;

                if (String.IsNullOrEmpty(Path.GetExtension(lodModelPath)))
                {
                    //Debug.Fail("Missing LOD file extension: " + lodModelPath);
                    lodModelPath += ".mwm";
                }

                lodModelPath = GetLodPath(model, lodModelPath);

                if (lodDesc.RenderQualityList == null || lodDesc.RenderQualityList.Contains((int)MyRenderConstants.RenderQualityProfile.RenderQuality))
                    AddLOD(lodDesc.Distance, lodModelPath);
            }

            return lodModel.Model;
        }

        internal MyLodModel AddLOD(float distance, string model)
        {
            try
            {
                MyLodModel lodModel = new MyLodModel();
                if (!string.IsNullOrEmpty(model))
                {
                    lodModel.Model = MyRenderModels.GetModel(model);
                    if (lodModel.Model.LoadState == LoadState.Error)
                    {
                        return null;
                    }

                    lodModel.Model.CloneMaterials(lodModel.MeshMaterials);
                }
                lodModel.Distance = distance;

                m_lods.Add(lodModel);

                return lodModel;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error adding lod: " + model + ", distance: " + distance, e);
            }
        }

        public MyLodModel AddData(MyRenderMessageSetRenderEntityData msg)
        {
            System.Diagnostics.Debug.Assert(msg.ModelData.Sections.Count > 0, "Invalid data");

            MyLodModel lodModel = new MyLodModel();

            lodModel.Model = new MyRenderModel(MyMeshDrawTechnique.MESH);
            ProfilerShort.Begin("LoadBuffers");
            lodModel.Model.LoadBuffers(msg.ModelData);
            ProfilerShort.BeginNextBlock("CloneMaterials");
            lodModel.Model.CloneMaterials(lodModel.MeshMaterials);
            ProfilerShort.End();
            lodModel.Distance = 0;

            m_localAABB         = (BoundingBoxD)lodModel.Model.BoundingBox;
            m_localVolume       = (BoundingSphereD)lodModel.Model.BoundingSphere;
            m_localVolumeOffset = (Vector3D)lodModel.Model.BoundingSphere.Center;
            m_volume            = m_localVolume;

            m_lods.Add(lodModel);
            m_isDataSet = true;

            return lodModel;
        }

        SharpDX.Direct3D9.VertexDeclaration CreateInstanceDeclaration(MyRenderModel model)
        {
            var modelElements = model.GetVertexDeclaration().Elements;
            List<SharpDX.Direct3D9.VertexElement> elements = new List<SharpDX.Direct3D9.VertexElement>(modelElements.Take(modelElements.Length - 1)); // Remove decl end
            elements.AddList(m_instanceBuffer.VertexElements);
            elements.Add(SharpDX.Direct3D9.VertexElement.VertexDeclarationEnd);
            return new SharpDX.Direct3D9.VertexDeclaration(MyRender.GraphicsDevice, elements.ToArray());
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            UnloadVertexDeclaration();
        }

        private void UnloadVertexDeclaration()
        {
            foreach (var lod in m_lods)
            {
                if (lod.VertexDeclaration != null)
                {
                    lod.VertexDeclaration.Dispose();
                    lod.VertexDeclaration = null;
                }
            }
        }

        public override bool Draw()
        {
            if (Visible)
            {
                foreach (var billboardMessage in m_billboards)
                {
                    MyRenderMessageAddLineBillboardLocal lineBillboard = billboardMessage as MyRenderMessageAddLineBillboardLocal;
                    if (lineBillboard != null)
                    {
                        Vector3D position = Vector3.Transform(lineBillboard.LocalPos, WorldMatrix);
                        Vector3D dir = Vector3.TransformNormal(lineBillboard.LocalDir, WorldMatrix);

                        MyTransparentGeometry.AddLineBillboard(
                            lineBillboard.Material,
                            lineBillboard.Color, position,
                            dir,
                            lineBillboard.Length,
                            lineBillboard.Thickness,
                            lineBillboard.Priority,
                            lineBillboard.Near);
                    }
                    else
                    {
                        MyRenderMessageAddPointBillboardLocal pointBillboard = billboardMessage as MyRenderMessageAddPointBillboardLocal;
                        if (pointBillboard != null)
                        {
                            Vector3D position = Vector3D.Transform(pointBillboard.LocalPos, WorldMatrix);

                            MyTransparentGeometry.AddPointBillboard(pointBillboard.Material, pointBillboard.Color, position, pointBillboard.Radius, pointBillboard.Angle
                                , pointBillboard.Priority, pointBillboard.Colorize, pointBillboard.Near, pointBillboard.Lowres);
                        }
                    }
                }
            }

            return base.Draw();
        }

        public void ChangeModels(int lodIndex, string model)
        {
            if (model != null)
            {
                m_lods[lodIndex].Model = MyRenderModels.GetModel(model);
                m_lods[lodIndex].Model.CloneMaterials(m_lods[lodIndex].MeshMaterials);
            }
        }

        public void ChangeShadowModels(int lodIndex, string model)
        {
            if (model != null)
            {
                m_lods[lodIndex].ShadowModel = MyRenderModels.GetModel(model);
            }
        }

        public List<MyLodModel> Lods
        {
            get { return m_lods; }
        }
    }
}
