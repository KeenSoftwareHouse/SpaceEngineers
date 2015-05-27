using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;

namespace VRageRender
{
    internal unsafe struct MyObjectData
    {
        internal Vector4 m_row0;
        internal Vector4 m_row1;
        internal Vector4 m_row2;
        internal Vector3 KeyColor;
        internal float CustomAlpha;
        internal Vector3 ColorMul;
        internal float Emissive;
        internal uint MaterialIndex;
        internal MyMaterialFlags MaterialFlags;
        internal float _padding0;
        internal float _padding1;
        internal Matrix LocalMatrix
        {
            get
            {
                var row0 = m_row0;
                var row1 = m_row1;
                var row2 = m_row2;

                return new Matrix(
                    row0.X, row1.X, row2.X, 0,
                    row0.Y, row1.Y, row2.Y, 0,
                    row0.Z, row1.Z, row2.Z, 0,
                    row0.W, row1.W, row2.W, 1);

            }
            set
            {
                m_row0 = new Vector4(value.M11, value.M21, value.M31, value.M41);
                m_row1 = new Vector4(value.M12, value.M22, value.M32, value.M42);
                m_row2 = new Vector4(value.M13, value.M23, value.M33, value.M43);
            }
        }
        internal void Translate(Vector3 v)
        {
            m_row0.W += v.X;
            m_row1.W += v.Y;
            m_row2.W += v.Z;
        }
    }

    struct MyMatrix4x3
    {
        internal Vector4 m_row0;
        internal Vector4 m_row1;
        internal Vector4 m_row2;

        internal Matrix Matrix4x4
        {
            get
            {
                var row0 = m_row0;
                var row1 = m_row1;
                var row2 = m_row2;

                return new Matrix(
                    row0.X, row1.X, row2.X, 0,
                    row0.Y, row1.Y, row2.Y, 0,
                    row0.Z, row1.Z, row2.Z, 0,
                    row0.W, row1.W, row2.W, 1);

            }
            set
            {
                m_row0 = new Vector4(value.M11, value.M21, value.M31, value.M41);
                m_row1 = new Vector4(value.M12, value.M22, value.M32, value.M42);
                m_row2 = new Vector4(value.M13, value.M23, value.M33, value.M43);
            }
        }
    }

    class MyGBufferPass : MyRenderingPass
    {
        internal MyGBuffer GBuffer;

        internal sealed override void Begin()
        {
            RC.BeginProfilingBlock("gbuffer pass");

            base.Begin();

            RC.BindGBufferForWrite(GBuffer);

            RC.SetDS(MyDepthStencilState.DepthTestWrite);
        }

        internal unsafe override sealed void RecordCommands(MyRenderableProxy proxy)
        {
            if (proxy.Mesh.Buffers.IB == IndexBufferId.NULL || proxy.Draw.IndexCount == 0 || (proxy.flags & MyRenderableProxyFlags.SkipInMainView) > 0)
            {
                return;
            }

            Stats.Meshes++;

            SetProxyConstants(proxy);
            BindProxyGeometry(proxy);

            Debug.Assert(proxy.Shaders.VS != null);

            RC.BindShaders(proxy.Shaders);

            if ((proxy.flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                RC.SetRS(MyRender11.m_nocullRasterizerState);
            else
                RC.SetRS(null);

//#if DEBUG
            if (MyRender11.Settings.Wireframe)
            {
                if ((proxy.flags & MyRenderableProxyFlags.DisableFaceCulling) > 0)
                    RC.SetRS(MyRender11.m_nocullWireframeRasterizerState);
                else
                    RC.SetRS(MyRender11.m_wireframeRasterizerState);
            }
//#endif

            //for (int i = 0; i < proxy.submeshes.Length; i++)
            //{
                Stats.Submeshes++;
                var submesh = proxy.Draw;

                //if (submesh.Material != null && submesh.Material.TexturesHash != Locals.matTexturesID)
                //{
                //    Locals.matTexturesID = submesh.Material.TexturesHash;
                //    RC.BindRawSRV(submesh.Material.SRVs);
                //}

                if (submesh.MaterialId != Locals.matTexturesID)
                {
                    Locals.matTexturesID = submesh.MaterialId;
                    var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                    RC.MoveConstants(ref material.MaterialConstants);
                    RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                    RC.SetSRVs(ref material.MaterialSRVs);
                }

                //if (submesh.Material != null && submesh.Material.ConstantsHash != Locals.matConstantsID && submesh.Material.ConstantsBuffer != null)
                //{
                //    Stats.MaterialConstantsChanges++;
                //    Locals.matConstantsID = submesh.Material.ConstantsHash;

                //    RC.SetCB(MyCommon.MATERIAL_SLOT, submesh.Material.ConstantsBuffer);

                //    var mapping = MyMapping.MapDiscard(RC.Context, submesh.Material.ConstantsBuffer);
                //    mapping.stream.WriteRange(submesh.Material.Constants);
                //    mapping.Unmap();
                //}

                if (proxy.skinningMatrices != null)
                {
                    Stats.ObjectConstantsChanges++;

                    MyObjectData objectData = proxy.ObjectData;
                    objectData.Translate(-MyEnvironment.CameraPosition);

                    MyMapping mapping;
                    mapping = MyMapping.MapDiscard(RC.Context, proxy.objectBuffer);
                    void* ptr = &objectData;
                    mapping.stream.Write(new IntPtr(ptr), 0, sizeof(MyObjectData));

                    if (proxy.skinningMatrices != null)
                    {
                        if (submesh.BonesMapping == null)
                        {
                            for (int j = 0; j < Math.Min(MyRender11Constants.SHADER_MAX_BONES, proxy.skinningMatrices.Length); j++)
                            { 
                                mapping.stream.Write(Matrix.Transpose(proxy.skinningMatrices[j]));
                            }
                        }
                        else
                        {
                            for (int j = 0; j < submesh.BonesMapping.Length; j++)
                            {
                                mapping.stream.Write(Matrix.Transpose(proxy.skinningMatrices[submesh.BonesMapping[j]]));
                            }
                        }
                    }

                    mapping.Unmap();
                }

                if (proxy.instanceCount == 0 && submesh.IndexCount > 0) {
                    RC.Context.DrawIndexed(submesh.IndexCount, submesh.StartIndex, submesh.BaseVertex);
                    RC.Stats.DrawIndexed++;
                    Stats.Instances++;
                    Stats.Triangles += submesh.IndexCount / 3;
                }
                else if (submesh.IndexCount > 0) { 
                    RC.Context.DrawIndexedInstanced(submesh.IndexCount, proxy.instanceCount, submesh.StartIndex, submesh.BaseVertex, proxy.startInstance);
                    RC.Stats.DrawIndexedInstanced++;
                    Stats.Instances += proxy.instanceCount;
                    Stats.Triangles += proxy.instanceCount * submesh.IndexCount / 3;
                }
            //}
        }

        internal override void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
            RC.SetSRVs(ref proxy.ObjectSRVs);
            RC.BindVertexData(ref proxy.VertexData);

            Debug.Assert(proxy.Shaders.VS != null);

            RC.BindShaders(proxy.Shaders);

            for (int i = 0; i < proxy.Submeshes.Length; i++)
            {
                var submesh = proxy.Submeshes[i];
                var material = MyMaterials1.ProxyPool.Data[submesh.MaterialId.Index];
                RC.MoveConstants(ref material.MaterialConstants);
                RC.SetConstants(ref material.MaterialConstants, MyCommon.MATERIAL_SLOT);
                RC.SetSRVs(ref material.MaterialSRVs);

                if (proxy.InstanceCount == 0)
                {
                    switch (submesh.DrawCommand)
                    {
                        case MyDrawCommandEnum.DrawIndexed:
                            RC.Context.DrawIndexed(submesh.Count, submesh.Start, submesh.BaseVertex);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.Context.Draw(submesh.Count, submesh.Start);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (submesh.DrawCommand)
                    {
                        case MyDrawCommandEnum.DrawIndexed:
                            RC.Context.DrawIndexedInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, submesh.BaseVertex, proxy.StartInstance);
                            break;
                        case MyDrawCommandEnum.Draw:
                            RC.Context.DrawInstanced(submesh.Count, proxy.InstanceCount, submesh.Start, proxy.StartInstance);
                            break;
                        default:
                            break;
                    }
                }
            }

            base.RecordCommands(ref proxy);
        }

        internal override void End()
        {
            base.End();

            RC.EndProfilingBlock();
        }
    }
}
