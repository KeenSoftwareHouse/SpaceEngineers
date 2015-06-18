using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D9;
using VRage.Import;

namespace VRageRender
{
    using VRageRender.Graphics;
    using VRageRender.Effects;
    using VRageRender.Techniques;
    using VRageMath;

    static partial class MyRender
    {
        static MySortedElements m_sortedElements = new MySortedElements();
        static MySortedElements m_sortedTransparentElements = new MySortedElements();

        static Dictionary<int, MyDrawTechniqueBase> m_drawTechniques;

        private static void InitDrawTechniques()
        {
            m_drawTechniques = new Dictionary<int, MyDrawTechniqueBase>();
            m_drawTechniques[(int)MyMeshDrawTechnique.MESH] = new MyDrawTechniqueMesh();
            m_drawTechniques[(int)MyMeshDrawTechnique.HOLO] = new MyDrawTechniqueHolo();
            m_drawTechniques[(int)MyMeshDrawTechnique.DECAL] = new MyDrawTechniqueDecal();
            m_drawTechniques[(int)MyMeshDrawTechnique.ALPHA_MASKED] = new MyDrawTechniqueAlphaMasked();
            m_drawTechniques[(int)MyMeshDrawTechnique.VOXELS_DEBRIS] = new MyDrawTechniqueVoxelDebris();
            m_drawTechniques[(int)MyMeshDrawTechnique.ATMOSPHERE] = new MyDrawTechniqueAtmosphere();
            m_drawTechniques[(int)MyMeshDrawTechnique.PLANET_SURFACE] = new MyDrawTechniquePlanetSurface();
            m_drawTechniques[(int)MyMeshDrawTechnique.VOXEL_MAP_SINGLE] = new MyDrawTechniqueVoxelSingle();
            m_drawTechniques[(int)MyMeshDrawTechnique.VOXEL_MAP_MULTI] = new MyDrawTechniqueVoxelMulti();
            m_drawTechniques[(int)MyMeshDrawTechnique.SKINNED] = new MyDrawTechniqueSkinned();
            m_drawTechniques[(int)MyMeshDrawTechnique.MESH_INSTANCED_GENERIC] = new MyDrawTechniqueMeshInstancedGeneric();
            m_drawTechniques[(int)MyMeshDrawTechnique.MESH_INSTANCED_GENERIC_MASKED] = new MyDrawTechniqueMeshInstancedGenericMasked();
            m_drawTechniques[(int)MyMeshDrawTechnique.MESH_INSTANCED] = new MyDrawTechniqueMeshInstanced();
            m_drawTechniques[(int)MyMeshDrawTechnique.MESH_INSTANCED_SKINNED] = new MyDrawTechniqueMeshInstancedSkinned();
            m_drawTechniques[(int)MyMeshDrawTechnique.GLASS] = new MyDrawTechniqueGlass();
        }

        private static MyDrawTechniqueBase GetTechnique(MyMeshDrawTechnique technique)
        {
            return m_drawTechniques[(int)technique];
        }

        private static void DrawRenderElementsAlternative(MySortedElements sortedElements, MyLodTypeEnum lod, out int ibChangesStats)
        {
            m_currentLodDrawPass = lod;

            ibChangesStats = 0;

            BlendState.Opaque.Apply(); //set by default, blend elements are at the end.

            DrawVoxels(sortedElements, lod, MyRenderVoxelBatchType.SINGLE_MATERIAL, ref ibChangesStats);
            DrawVoxels(sortedElements, lod, MyRenderVoxelBatchType.MULTI_MATERIAL, ref ibChangesStats);
            DrawModels(sortedElements, lod, ref ibChangesStats);
        }

        private static void DrawVoxels(MySortedElements sortedElements, MyLodTypeEnum lod, MyRenderVoxelBatchType batchType, ref int ibChangesStats)
        {
            int index = sortedElements.GetVoxelIndex(lod, batchType);
            var matDict = sortedElements.Voxels[index];

            if (matDict.RenderElementCount == 0)
                return;

            var tech = GetTechnique(batchType == MyRenderVoxelBatchType.SINGLE_MATERIAL ? MyMeshDrawTechnique.VOXEL_MAP_SINGLE : MyMeshDrawTechnique.VOXEL_MAP_MULTI);
           
            var shader = (MyEffectVoxels)tech.PrepareAndBeginShader(m_currentSetup, lod);         
            MyPerformanceCounter.PerCameraDrawWrite.TechniqueChanges[(int)lod]++;
           
            if (lod == MyLodTypeEnum.LOD_BACKGROUND)
            {
                shader.SetAmbientMinimumAndIntensity(new Vector4(AmbientColor * AmbientMultiplier, EnvAmbientIntensity));
                shader.SetSunDirection(m_sun.Direction);
                shader.SetSunColorAndIntensity(new Vector3(m_sun.Color.X, m_sun.Color.Y, m_sun.Color.Z), m_sun.Intensity);
                shader.SetBacklightColorAndIntensity(new Vector3(m_sun.BackColor.X, m_sun.BackColor.Y, m_sun.BackColor.Z), m_sun.BackIntensity);
                var postProcess = MyRender.GetPostProcess(MyPostProcessEnum.VolumetricFog) as MyPostProcessVolumetricFog;
                shader.EnableFog(postProcess.Enabled);
                shader.SetSunSpecularColor(m_sun.SpecularColor);
            }

            foreach (var mat in matDict.Voxels)
            {
                var firstElement = mat.Value.FirstOrDefault();
                if (firstElement == null)
                    continue;

               
                // Setup material
                tech.SetupVoxelMaterial(shader, firstElement.VoxelBatch);
                MyPerformanceCounter.PerCameraDrawWrite.MaterialChanges[(int)lod]++;

                MyRenderObject lastRenderObject = null;
                VertexBuffer lastVertexBuffer = null;

                foreach (var renderElement in mat.Value)
                {
                    if (!object.ReferenceEquals(lastVertexBuffer, renderElement.VertexBuffer))
                    {
                        lastVertexBuffer = renderElement.VertexBuffer;
                        GraphicsDevice.Indices = renderElement.IndexBuffer;
                        GraphicsDevice.SetStreamSource(0, renderElement.VertexBuffer, 0, renderElement.VertexStride);
                        GraphicsDevice.VertexDeclaration = renderElement.VertexDeclaration;
                        MyPerformanceCounter.PerCameraDrawWrite.VertexBufferChanges[(int)lod]++;
                        ibChangesStats++;
                    }

                    if (lastRenderObject != renderElement.RenderObject)
                    {
                        lastRenderObject = renderElement.RenderObject;
                        MyPerformanceCounter.PerCameraDrawWrite.EntityChanges[(int)lod]++;
                        tech.SetupEntity(shader, renderElement);                     
                        shader.D3DEffect.CommitChanges();
                    }

                    GraphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, renderElement.VertexCount, renderElement.IndexStart, renderElement.TriCount);
                    MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
                }
            }

            shader.End();
            // Technique End
        }

        private static void DrawModels(MySortedElements sortedElements, MyLodTypeEnum lod, ref int ibChangesStats)
        {          
            for (int i = 0; i < MySortedElements.DrawTechniqueCount; i++)
            {
                var technique = (MyMeshDrawTechnique)i;
                int index = sortedElements.GetModelIndex(lod, technique);
                var matDict = sortedElements.Models[index];

                if (matDict.RenderElementCount == 0)
                    continue;

                // Technique start
                var tech = GetTechnique(technique);
                var shader = tech.PrepareAndBeginShader(m_currentSetup, m_currentLodDrawPass);
               
                RasterizerState currentRasterizer = RasterizerState.Current;
                bool doubleSided = tech.GetType() == typeof(MyDrawTechniqueAlphaMasked) || tech.GetType() == typeof(MyDrawTechniqueMeshInstancedGenericMasked);              
                if (doubleSided)
                {
                    RasterizerState alphaMaskedRasterizer = new RasterizerState { CullMode = Cull.None };
                    alphaMaskedRasterizer.Apply();
                }

                MyPerformanceCounter.PerCameraDrawWrite.TechniqueChanges[(int)lod]++;

                foreach (var mat in matDict.Models)
                {
                    if (mat.Value.RenderElementCount == 0)
                        continue;

                    // Setup material
                    tech.SetupMaterial(shader, mat.Key);
                    MyPerformanceCounter.PerCameraDrawWrite.MaterialChanges[(int)lod]++;

#if !ATI_INSTANCES

                    foreach (var vb in mat.Value.Models)
                    {
                        // Set vb
                        var firstElement = vb.Value.FirstOrDefault();
                        if (firstElement == null)
                            continue;

                        GraphicsDevice.Indices = firstElement.IndexBuffer;
                        GraphicsDevice.SetStreamSource(0, firstElement.VertexBuffer, 0, firstElement.VertexStride);
                        GraphicsDevice.VertexDeclaration = firstElement.VertexDeclaration;
                        MyPerformanceCounter.PerCameraDrawWrite.VertexBufferChanges[(int)lod]++;
                        ibChangesStats++;

                        MyRenderObject lastRenderObject = null;
                        int[] lastBonesSet = null;

                        foreach (var renderElement in vb.Value)
                        {
                            if (renderElement.InstanceBuffer == null)
                            {
                                MyRender.GraphicsDevice.ResetStreamSourceFrequency(0);
                                MyRender.GraphicsDevice.ResetStreamSourceFrequency(1);
                            }
                            else
                            {
                                GraphicsDevice.VertexDeclaration = renderElement.VertexDeclaration;
                                GraphicsDevice.SetStreamSourceFrequency(0, renderElement.InstanceCount, StreamSource.IndexedData);
                                GraphicsDevice.SetStreamSource(0, renderElement.VertexBuffer, 0, renderElement.VertexStride);

                                GraphicsDevice.SetStreamSourceFrequency(1, 1, StreamSource.InstanceData);
                                GraphicsDevice.SetStreamSource(1, renderElement.InstanceBuffer, renderElement.InstanceStride * renderElement.InstanceStart, renderElement.InstanceStride);
                            }
                            
                            if (lastRenderObject != renderElement.RenderObject || lastBonesSet != renderElement.BonesUsed)
                            {
                                lastRenderObject = renderElement.RenderObject;
                                lastBonesSet = renderElement.BonesUsed;
                                MyPerformanceCounter.PerCameraDrawWrite.EntityChanges[(int)lod]++;
                                tech.SetupEntity(shader, renderElement);
                            
                                if (technique == MyMeshDrawTechnique.ATMOSPHERE)
                                {
                                    RasterizerState.CullClockwise.Apply();
                                    BlendState.Additive.Apply();
                                }
                                else if(technique == MyMeshDrawTechnique.PLANET_SURFACE)
                                {
                                    if ((lastRenderObject as MyRenderAtmosphere).IsInside(MyRenderCamera.Position))
                                    {
                                        RasterizerState.CullClockwise.Apply();
                                    }
                                }
                                else if(doubleSided == false)
                                {
                                    currentRasterizer.Apply();
                                    BlendState.Opaque.Apply();
                                }

                                shader.D3DEffect.CommitChanges();
                            }
                     
                            GraphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, renderElement.VertexCount, renderElement.IndexStart, renderElement.TriCount);
                            MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
                        }
#else

                    foreach (var vb in mat.Value.Models)
                    {
                        // Set vb
                        var firstElement = vb.Value.FirstOrDefault();
                        if (firstElement == null)
                            continue;

                        GraphicsDevice.Indices = firstElement.IndexBuffer;
                        GraphicsDevice.SetStreamSource(0, firstElement.VertexBuffer, 0, firstElement.VertexStride);
                        GraphicsDevice.VertexDeclaration = firstElement.VertexDeclaration;
                        MyPerformanceCounter.PerCameraDrawWrite.VertexBufferChanges[(int)lod]++;
                        ibChangesStats++;

                        MyRenderObject lastRenderObject = null;

                        foreach (var renderElement in vb.Value)
                        {
                            if (renderElement.InstanceBuffer == null)
                            {
                                MyRender.GraphicsDevice.ResetStreamSourceFrequency(0);
                                MyRender.GraphicsDevice.ResetStreamSourceFrequency(1);
                            }
                            else
                            {
                                          GraphicsDevice.VertexDeclaration = renderElement.VertexDeclaration;

                                int totalInstCount = renderElement.InstanceCount;
                                int maxbuff = 1024;
                                int offset = 0;

                                while (totalInstCount > 0)
                                {
                                    int count = totalInstCount >= maxbuff ? maxbuff : totalInstCount;
                                    totalInstCount -= count;

                                    GraphicsDevice.SetStreamSourceFrequency(0, count, StreamSource.IndexedData);
                                    GraphicsDevice.SetStreamSource(0, renderElement.VertexBuffer, 0, renderElement.VertexStride);

                                    GraphicsDevice.SetStreamSourceFrequency(1, 1, StreamSource.InstanceData);
                                    GraphicsDevice.SetStreamSource(1, renderElement.InstanceBuffer, renderElement.InstanceStride * (renderElement.InstanceStart + offset), renderElement.InstanceStride);

                                    offset += count;

                                    if (lastRenderObject != renderElement.RenderObject)
                                    {
                                        lastRenderObject = renderElement.RenderObject;
                                        MyPerformanceCounter.PerCameraDrawWrite.EntityChanges[(int)lod]++;
                                        tech.SetupEntity(shader, renderElement);
                                        shader.D3DEffect.CommitChanges();
                                    }

                                    GraphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, renderElement.VertexCount, renderElement.IndexStart, renderElement.TriCount);

                                    MyPerformanceCounter.PerCameraDrawWrite.TotalDrawCalls++;
                                }
                            }
                        
                        }
#endif
                    }
                }

                MyRender.GraphicsDevice.ResetStreamSourceFrequency(0);
                MyRender.GraphicsDevice.ResetStreamSourceFrequency(1);

                shader.End();
                // Technique End

                if (doubleSided)
                {
                    currentRasterizer.Apply();
                }
            }
        }
    }
}
