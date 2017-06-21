using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.FileSystem;
using VRage.Import;
using VRage.Library.Utils;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Import;
using VRageRender.Vertex;


namespace VRageRender
{
    class MyAssetMesh : MyMesh
    {
        internal MyAssetMesh(string assetName)
        {
            m_name = assetName;
        }

        #region Loading internals
        MyRenderMeshInfo LoadMesh(string assetName)
        {
            MyLODDescriptor[] ignoreMe;
            return LoadMesh(assetName, out ignoreMe);
        }

        MyRenderMeshInfo LoadMesh(string assetName, out MyLODDescriptor[] LodDescriptors)
        {
            //Debug.Assert(assetName.EndsWith(".mwm"));
            #region Temporary for mwm endings
            if (!assetName.EndsWith(".mwm"))
            {
                assetName += ".mwm";
            }
            #endregion

            var meshVertexInput = MyVertexInputLayout.Empty;
            LodDescriptors = null;
            MyRenderMeshInfo result = new MyRenderMeshInfo();

            var importer = new MyModelImporter();
            var fsPath = Path.IsPathRooted(assetName) ? assetName : Path.Combine(MyFileSystem.ContentPath, assetName);

            if (!MyFileSystem.FileExists(fsPath))
            {
                System.Diagnostics.Debug.Fail("Model " + assetName + " does not exists!");

                return MyAssetsLoader.GetDebugMesh().LODs[0].m_meshInfo;
            }


            string contentPath = null;
            if (Path.IsPathRooted(assetName) && assetName.ToLower().Contains("models"))
                contentPath = assetName.Substring(0, assetName.ToLower().IndexOf("models"));

            try
            {

                importer.ImportData(fsPath, new string[]
                    {
                        MyImporterConstants.TAG_VERTICES,
                        MyImporterConstants.TAG_BLENDINDICES,
                        MyImporterConstants.TAG_BLENDWEIGHTS,
                        MyImporterConstants.TAG_NORMALS,
                        MyImporterConstants.TAG_TEXCOORDS0,
                        MyImporterConstants.TAG_TANGENTS,
                        MyImporterConstants.TAG_BINORMALS,
                        MyImporterConstants.TAG_BONES,
                        MyImporterConstants.TAG_MESH_PARTS,
                        MyImporterConstants.TAG_BOUNDING_BOX,
                        MyImporterConstants.TAG_BOUNDING_SPHERE,
                        MyImporterConstants.TAG_LODS,
                    });
                Dictionary<string, object> tagData = importer.GetTagData();

                // extract data
                var positions = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];
                System.Diagnostics.Debug.Assert(positions.Length > 0);
                var verticesNum = positions.Length;
                var boneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
                var boneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];
                var normals = (Byte4[])tagData[MyImporterConstants.TAG_NORMALS];
                var texcoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
                var tangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
                var bintangents = (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS];
                var tangentBitanSgn = new Byte4[verticesNum];
                for (int i = 0; i < verticesNum; i++)
                {
                    var N = VF_Packer.UnpackNormal(normals[i].PackedValue);
                    var T = VF_Packer.UnpackNormal(tangents[i].PackedValue);
                    var B = VF_Packer.UnpackNormal(bintangents[i].PackedValue);

                    var tanW = new Vector4(T.X, T.Y, T.Z, 0);

                    tanW.W = T.Cross(N).Dot(B) < 0 ? -1 : 1;
                    tangentBitanSgn[i] = VF_Packer.PackTangentSignB4(ref tanW);
                }
                
                bool hasBonesInfo = boneIndices.Length > 0 && boneWeights.Length > 0;
                var bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];

                //
                var vertexBuffers = new List<IVertexBuffer>();
                IIndexBuffer indexBuffer = null;
                var submeshes = new Dictionary<MyMeshDrawTechnique, List<MyDrawSubmesh>>();
                var submeshes2 = new Dictionary<MyMeshDrawTechnique, List<MySubmeshInfo>>();
                var submeshesMeta = new List<MySubmeshInfo>();

                int indicesNum = 0;
                bool missingMaterial = false;
                if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
                {
                    var indices = new List<uint>(positions.Length);
                    uint maxIndex = 0;

                    var meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                    foreach (MyMeshPartInfo meshPart in meshParts)
                    {
                        # region Bones indirection
                        int[] bonesRemapping = null;
                        if (boneIndices.Length > 0 && bones.Length > MyRender11Constants.SHADER_MAX_BONES)
                        {
                            Dictionary<int, int> vertexChanged = new Dictionary<int, int>();

                            Dictionary<int, int> bonesUsed = new Dictionary<int, int>();

                            int trianglesNum = meshPart.m_indices.Count / 3;
                            for (int i = 0; i < trianglesNum; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    int index = meshPart.m_indices[i * 3 + j];
                                    if (boneWeights[index].X > 0)
                                        bonesUsed[boneIndices[index].X] = 1;
                                    if (boneWeights[index].Y > 0)
                                        bonesUsed[boneIndices[index].Y] = 1;
                                    if (boneWeights[index].Z > 0)
                                        bonesUsed[boneIndices[index].Z] = 1;
                                    if (boneWeights[index].W > 0)
                                        bonesUsed[boneIndices[index].W] = 1;
                                }
                            }

                            if (bonesUsed.Count > MyRender11Constants.SHADER_MAX_BONES)
                            {
                                Debug.Assert(bonesUsed.Count <= MyRender11Constants.SHADER_MAX_BONES, "Model \"" + assetName + "\"'s part uses more than 60 bones, please split model on more parts");
                            }

                            var partBones = new List<int>(bonesUsed.Keys);
                            partBones.Sort();
                            if (partBones.Count > 0 && partBones[partBones.Count - 1] >= MyRender11Constants.SHADER_MAX_BONES)
                            {
                                for (int i = 0; i < partBones.Count; i++)
                                {
                                    bonesUsed[partBones[i]] = i;
                                }

                                Dictionary<int, int> vertexTouched = new Dictionary<int, int>();

                                for (int i = 0; i < trianglesNum; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        int index = meshPart.m_indices[i * 3 + j];
                                        if (!vertexTouched.ContainsKey(index))
                                        {
                                            if (boneWeights[index].X > 0)
                                                boneIndices[index].X = bonesUsed[boneIndices[index].X];
                                            if (boneWeights[index].Y > 0)
                                                boneIndices[index].Y = bonesUsed[boneIndices[index].Y];
                                            if (boneWeights[index].Z > 0)
                                                boneIndices[index].Z = bonesUsed[boneIndices[index].Z];
                                            if (boneWeights[index].W > 0)
                                                boneIndices[index].W = bonesUsed[boneIndices[index].W];

                                            vertexTouched[index] = 1;

                                            int changes = 0;
                                            vertexChanged.TryGetValue(index, out changes);
                                            vertexChanged[index] = changes + 1;
                                        }
                                    }
                                }

                                bonesRemapping = partBones.ToArray();
                            }

                            if (vertexChanged.Values.Count > 0)
                                Debug.Assert(vertexChanged.Values.Max() < 2, "Vertex shared between model parts, will likely result in wrong skinning");
                        }

                        #endregion

                        int startIndex = indices.Count;
                        int indexCount = meshPart.m_indices.Count;

                        uint minIndex = (uint)meshPart.m_indices[0];
                        foreach (var i in meshPart.m_indices)
                        {
                            indices.Add((uint)i);
                            minIndex = Math.Min(minIndex, (uint)i);
                        }

                        uint baseVertex = minIndex;

                        for (int i = startIndex; i < startIndex + indexCount; i++)
                        {
                            indices[i] -= minIndex;
                            maxIndex = Math.Max(maxIndex, indices[i]);
                        }

                        #region Material
                        var materialDesc = meshPart.m_MaterialDesc;

                        var matId = MyMeshMaterials1.GetMaterialId(materialDesc, contentPath);
                        var partKey = MyMeshMaterials1.Table[matId.Index].Technique;
                        var materialName = MyMeshMaterials1.Table[matId.Index].Name;

                        var list = submeshes.SetDefault(partKey, new List<MyDrawSubmesh>());
                        list.Add(new MyDrawSubmesh(indexCount, startIndex, (int)baseVertex, MyMeshMaterials1.GetProxyId(matId), bonesRemapping));

                        submeshesMeta.Add(new MySubmeshInfo
                        {
                            IndexCount = indexCount,
                            StartIndex = startIndex,
                            BaseVertex = (int)baseVertex,
                            BonesMapping = bonesRemapping,
                            Material = materialName.ToString(),
                            Technique = partKey
                        });

                        var list2 = submeshes2.SetDefault(partKey, new List<MySubmeshInfo>());
                        list2.Add(submeshesMeta[submeshesMeta.Count - 1]);

                        #endregion

                    }
                    indicesNum = indices.Count;

                    #region Fill gpu buffes
                    unsafe
                    {
                        if (maxIndex <= ushort.MaxValue)
                        {
                            // create 16 bit indices
                            var indices16 = new ushort[indices.Count];
                            for (int i = 0; i < indices.Count; i++)
                            {
                                indices16[i] = (ushort)indices[i];
                            }

                            result.Indices = indices16;

                            fixed (ushort* I = indices16)
                            {
                                indexBuffer = MyManagers.Buffers.CreateIndexBuffer(assetName + " index buffer", indices16.Length, new IntPtr(I), MyIndexBufferFormat.UShort, ResourceUsage.Immutable);
                            }
                        }
                        else
                        {
                            var indicesArray = indices.ToArray();
                            fixed (uint* I = indicesArray)
                            {
                                indexBuffer = MyManagers.Buffers.CreateIndexBuffer(assetName + " index buffer", indices.Count, new IntPtr(I), MyIndexBufferFormat.UInt, ResourceUsage.Immutable);
                            }
                        }
                    }
                    unsafe
                    {
                        if (!hasBonesInfo)
                        {
                            var vertices = new MyVertexFormatPositionH4[verticesNum];

                            for (int i = 0; i < verticesNum; i++)
                            {
                                vertices[i] = new MyVertexFormatPositionH4(positions[i]);
                            }
                            meshVertexInput = meshVertexInput.Append(MyVertexInputComponentType.POSITION_PACKED);

                            result.VertexPositions = vertices;

                            fixed (MyVertexFormatPositionH4* V = vertices)
                            {
                                vertexBuffers.Add(
                                    MyManagers.Buffers.CreateVertexBuffer(
                                        assetName + " vertex buffer " + vertexBuffers.Count, verticesNum,
                                        sizeof(MyVertexFormatPositionH4), new IntPtr(V), ResourceUsage.Immutable));
                            }
                        }
                        else
                        {
                            var vertices = new MyVertexFormatPositionSkinning[verticesNum];
                            for (int i = 0; i < verticesNum; i++)
                            {
                                vertices[i] = new MyVertexFormatPositionSkinning(
                                    positions[i],
                                    new Byte4(boneIndices[i].X, boneIndices[i].Y, boneIndices[i].Z, boneIndices[i].W),
                                    boneWeights[i]);
                            }
                            meshVertexInput = meshVertexInput.Append(MyVertexInputComponentType.POSITION_PACKED)
                                .Append(MyVertexInputComponentType.BLEND_WEIGHTS)
                                .Append(MyVertexInputComponentType.BLEND_INDICES);

                            fixed (MyVertexFormatPositionSkinning* V = vertices)
                            {
                                vertexBuffers.Add(MyManagers.Buffers.CreateVertexBuffer(
                                    assetName + " vertex buffer " + vertexBuffers.Count, verticesNum,
                                    sizeof(MyVertexFormatPositionSkinning), new IntPtr(V), ResourceUsage.Immutable));
                            }
                        }
                        // add second stream
                        {
                            var vertices = new MyVertexFormatTexcoordNormalTangent[verticesNum];
                            for (int i = 0; i < verticesNum; i++)
                            {
                                vertices[i] = new MyVertexFormatTexcoordNormalTangent(texcoords[i], normals[i], tangentBitanSgn[i]);
                            }

                            fixed (MyVertexFormatTexcoordNormalTangent* V = vertices)
                            {
                                vertexBuffers.Add(MyManagers.Buffers.CreateVertexBuffer(
                                    assetName + " vertex buffer " + vertexBuffers.Count, verticesNum,
                                    sizeof(MyVertexFormatTexcoordNormalTangent), new IntPtr(V), ResourceUsage.Immutable));
                            }

                            result.VertexExtendedData = vertices;

                            meshVertexInput = meshVertexInput
                                .Append(MyVertexInputComponentType.NORMAL, 1)
                                .Append(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1)
                                .Append(MyVertexInputComponentType.TEXCOORD0_H, 1);
                        }
                    }
                    #endregion
                }
                #region Extract lods
                if (tagData.ContainsKey(MyImporterConstants.TAG_LODS))
                {
                    var tagLODs = tagData[MyImporterConstants.TAG_LODS];
                    if (((MyLODDescriptor[])tagLODs).Length > 0)
                    {
                    }
                    LodDescriptors = (MyLODDescriptor[])((MyLODDescriptor[])tagLODs).Clone();
                }
                #endregion

                if (missingMaterial)
                {
                    Debug.WriteLine(String.Format("Mesh {0} has missing material", assetName));
                }

                //indexBuffer.SetDebugName(assetName + " index buffer");
                int c = 0;
                //vertexBuffers.ForEach(x => x.SetDebugName(assetName + " vertex buffer " + c++));

                //
                result.BoundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                result.BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
                result.VerticesNum = verticesNum;
                result.IndicesNum = indicesNum;
                result.VertexLayout = meshVertexInput;
                result.IB = indexBuffer;
                result.VB = vertexBuffers.ToArray();
                result.IsAnimated = hasBonesInfo;
                result.Parts = submeshes.ToDictionary(x => x.Key, x => x.Value.ToArray());
                result.PartsMetadata = submeshes2.ToDictionary(x => x.Key, x => x.Value.ToArray());
                result.m_submeshes = submeshesMeta;

                IsAnimated |= result.IsAnimated;

                importer.Clear();
                return result;
            }
            catch (Exception e)
            {
                return MyAssetsLoader.GetDebugMesh().LODs[0].m_meshInfo;
            }
        }

        internal void SetMaterial_SLOW(MyMaterialProxyId materialId)
        {
            foreach (var lod in LODs)
            {
                if (lod.m_meshInfo == null)
                    continue;

                foreach (var kv in lod.m_meshInfo.Parts)
                {
                    for (int i = 0; i < kv.Value.Length; i++)
                    {
                        var submesh = kv.Value[i];
                        submesh.MaterialId = materialId;

                        kv.Value[i] = submesh;
                    }
                }
            }

            MyRenderableComponent.MarkAllDirty();
        }

        //internal static MyMaterialDescription LoadMaterial(VRage.Common.Import.MyMaterialDescriptor materialDesc)
        //{


        //    if (materialDesc != null)
        //    {
        //        var descriptor = new MyMaterialDescription();
        //        descriptor.TextureColorMetalPath =
        //            materialDesc.Textures.Get("ColorMetalTexture", "");
        //        descriptor.TextureNormalGlossPath =
        //            materialDesc.Textures.Get("NormalGlossTexture", "");
        //        descriptor.TextureAmbientOcclusionPath =
        //            materialDesc.Textures.Get("AddMapsTexture", "");
        //        descriptor.TextureAlphamaskPath =
        //            materialDesc.Textures.Get("AlphamaskTexture", null);
        //        descriptor.Technique = materialDesc.Technique;

        //        if (materialDesc.MaterialName.ToLower().Contains("debug"))
        //        {
        //            descriptor = GetDebugMaterialDescriptor();
        //        }

        //        MyMaterials.RegisterMaterial(materialDesc.MaterialName, descriptor);

        //        return descriptor;
        //    }

        //    return new MyMaterialDescription();
        //}

        //internal static MyMaterialDescription GetDebugMaterialDescriptor()
        //{
        //    return new MyMaterialDescription { TextureColorMetalPath = "DEBUG_PINK", TextureAmbientOcclusionPath = "", TextureNormalGlossPath = "", TextureAlphamaskPath = "", Technique = MyMesh.DEFAULT_MESH_TECHNIQUE };
        //}

        internal static void LoadMaterials(string assetName)
        {
            //string contentFolder = null;
            //if (Path.IsPathRooted(file) && file.ToLower().Contains("models"))
            //{
            //    contentFolder = file.Substring(0, file.ToLower().IndexOf("models"));
            //}

            //Debug.Assert(assetName.EndsWith(".mwm"));
            #region Temporary for mwm endings
            if (!assetName.EndsWith(".mwm"))
            {
                assetName += ".mwm";
            }
            #endregion


            string contentPath = null;
            if (Path.IsPathRooted(assetName) && assetName.ToLower().Contains("models"))
                contentPath = assetName.Substring(0, assetName.ToLower().IndexOf("models"));


            MyRenderMeshInfo result = new MyRenderMeshInfo();

            var importer = new MyModelImporter();
            var fsPath = Path.IsPathRooted(assetName) ? assetName : Path.Combine(MyFileSystem.ContentPath, assetName);

            importer.ImportData(fsPath, new string[] { MyImporterConstants.TAG_MESH_PARTS });
            Dictionary<string, object> tagData = importer.GetTagData();

            if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
            {
                var meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                foreach (MyMeshPartInfo meshPart in meshParts)
                {
                    #region Material
                    //LoadMaterial(meshPart.m_MaterialDesc);
                    MyMeshMaterials1.GetMaterialId(meshPart.m_MaterialDesc, contentPath);
                    #endregion
                }
            }

            importer.Clear();
        }
        #endregion

        internal void LoadAsset()
        {
            LODs = null;
            IsAnimated = false;
            // serialized now
            m_loadingStatus = MyAssetLoadingEnum.Waiting;

            Debug.Assert(m_loadingStatus == MyAssetLoadingEnum.Waiting);

            MyLODDescriptor[] lodsDesc;
            var mostDetailed = new MyRenderLodInfo();
            mostDetailed.LodNum = 0;
            mostDetailed.m_meshInfo = LoadMesh(m_name, out lodsDesc);
            mostDetailed.Distance = 0;

            int lodsNum = 1;
            if (lodsDesc != null)
                lodsNum += lodsDesc.Length;

            LODs = new MyRenderLodInfo[lodsNum];
            LODs[0] = mostDetailed;

            if (lodsDesc != null)
            {
                int num = 1;
                foreach (var lodDesc in lodsDesc)
                {
                    var lod = new MyRenderLodInfo();
                    lod.Distance = lodDesc.Distance;
                    lod.LodNum = num;
                    lod.m_meshInfo = LoadMesh(lodDesc.GetModelAbsoluteFilePath(m_name));
                    LODs[num] = lod;
                    num++;
                }
            }

            //Marshal.Alloc
            //SharpDX.Utilities.

            m_loadingStatus = MyAssetLoadingEnum.Ready;
        }
    }
}
