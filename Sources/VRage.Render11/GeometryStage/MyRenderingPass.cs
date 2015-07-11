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
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using ParallelTasks;

namespace VRageRender
{
    // stores preallocated data
    class MyPassLocals
    {
        internal MyMaterialProxyId matTexturesID;
        internal int matConstantsID;
        internal MyObjectData objectData;
        internal Buffer objectBuffer;

        internal void Clear()
        {
            matTexturesID = MyMaterialProxyId.NULL;
            matConstantsID = -1;

            objectData = new MyObjectData();

            objectBuffer = null;
        }
    }

    struct MyPassStats
    {
        internal int Meshes;
        internal int Submeshes;
        internal int Instances;
        internal int Triangles;
        internal int ObjectConstantsChanges;
        internal int MaterialConstantsChanges;

        internal void Clear()
        {
            Meshes = 0;
            Submeshes = 0;
            ObjectConstantsChanges = 0;
            MaterialConstantsChanges = 0;
            Instances = 0;
            Triangles = 0;
        }

        internal void Gather(MyPassStats other)
        {
            Meshes += other.Meshes;
            Submeshes += other.Submeshes;
            Instances += other.Instances;
            Triangles += other.Triangles;
            ObjectConstantsChanges += other.ObjectConstantsChanges;
            MaterialConstantsChanges += other.MaterialConstantsChanges;
        }
    }

    class MyRenderingPass
    {
        #region Local
        internal MyRenderContext m_RC;
        internal MyPassLocals Locals;
        internal MyPassStats Stats;
        internal bool m_joined;

        int m_currentProfilingBlock_renderableType = -1;
        string m_currentProfilingBlock_renderableMaterial = "";
        #endregion

        #region Shared
        bool m_isImmediate;
        internal Matrix ViewProjection;
        internal MyViewport Viewport;
        internal string DebugName;
        #endregion

        internal DeviceContext Context { get { return RC.Context; } }
        internal int ProcessingMask { get; set; }

        internal MyRenderContext RC
        {
            get
            {
                Debug.Assert(!m_isImmediate || (MyRenderProxy.RenderThread.SystemThread == Thread.CurrentThread));
                return m_isImmediate ? MyImmediateRC.RC : m_RC;
            }
        }

        internal void SetImmediate(bool isImmediate)
        {
            m_isImmediate = isImmediate;
        }

        internal void SetContext(MyRenderContext rc)
        {
            Debug.Assert(!m_isImmediate && m_RC == null);
            m_RC = rc;
        }

        internal void Cleanup()
        {
            m_RC = null;
        }

        internal virtual void PerFrame()
        {
            MyCommon.UpdateFrameConstants();
        }

        internal virtual void Begin()
        {
            if (Locals == null)
            {
                Locals = new MyPassLocals();
            }
            Locals.Clear();

            //if (!m_isImmediate)
            //{
            //    //Debug.Assert(m_RC == null);
            //    //m_RC = MyRenderContextPool.AcquireRC();
            //}

            var mapping = MyMapping.MapDiscard(RC.Context, MyCommon.ProjectionConstants);
            mapping.stream.Write(Matrix.Transpose(ViewProjection));
            mapping.Unmap();

            // common settings
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Context.Rasterizer.SetViewport(Viewport.OffsetX, Viewport.OffsetY, Viewport.Width, Viewport.Height);

            Context.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            Context.PixelShader.SetShaderResource(MyCommon.DITHER_8X8_SLOT, MyTextures.Views[MyTextures.Dithering8x8TexId.Index]);

            if (MyBigMeshTable.Table.m_IB != null)
            {
                RC.VSBindSRV(MyCommon.BIG_TABLE_INDICES,
                    MyBigMeshTable.Table.m_IB.Srv,
                    MyBigMeshTable.Table.m_VB_positions.Srv,
                    MyBigMeshTable.Table.m_VB_rest.Srv);
            }
        }

        [Conditional(VRage.ProfilerShort.Symbol)]
        internal void FeedProfiler(ulong nextSortingKey)
        {
            int type = (int)MyRenderableComponent.ExtractTypeFromSortingKey(nextSortingKey);
            var material = MyRenderableComponent.ExtractMaterialNameFromSortingKey(nextSortingKey);

            if (type != m_currentProfilingBlock_renderableType)
            {
                if (m_currentProfilingBlock_renderableType != -1)
                {
                    // close material
                    RC.EndProfilingBlock();
                    // close type
                    RC.EndProfilingBlock();
                }

                // start type
                RC.BeginProfilingBlock(((MyMaterialType)type).ToString());
                // start material
                RC.BeginProfilingBlock(material);

                m_currentProfilingBlock_renderableType = type;
                m_currentProfilingBlock_renderableMaterial = material;
            }

            if (material != m_currentProfilingBlock_renderableMaterial)
            {
                if (m_currentProfilingBlock_renderableMaterial != "")
                {
                    // close material
                    RC.EndProfilingBlock();
                }
                // start material
                RC.BeginProfilingBlock(material);

                m_currentProfilingBlock_renderableMaterial = material;
            }
        }

        internal virtual void RecordCommands(MyRenderableProxy proxy)
        {

        }

        internal virtual void RecordCommands(ref MyRenderableProxy_2 proxy)
        {
        }

        //internal CommandList GrabCommandList()
        //{
        //    if (!m_isImmediate)
        //    {
        //        var result = MyRenderContextPool.FinishFreeRC(m_RC);
        //        m_RC = null;
        //        return result;
        //    }
        //    return null;
        //}

        internal virtual void End()
        {
            if (VRage.MyCompilationSymbols.RenderProfiling)
            {
                if (m_currentProfilingBlock_renderableType != -1)
                {
                    // close material
                    RC.EndProfilingBlock();
                    // close type
                    RC.EndProfilingBlock();
                }
            }
        }

        internal unsafe void SetProxyConstants(MyRenderableProxy proxy)
        {
            RC.SetCB(MyCommon.OBJECT_SLOT, proxy.objectBuffer);

            MyMapping mapping;

            bool constantsChange = true;

            fixed (void* ptr0 = &Locals.objectData)
            {
                fixed (void* ptr1 = &proxy.ObjectData)
                {
                    constantsChange = !SharpDX.Utilities.CompareMemory(new IntPtr(ptr0), new IntPtr(ptr1), sizeof(MyObjectData));
                }
            }

            if (!constantsChange
                && proxy.skinningMatrices == null
                && Locals.objectBuffer == proxy.objectBuffer)
            {

            }
            else
            {
                Locals.objectData = proxy.ObjectData;
                Locals.objectBuffer = proxy.objectBuffer;

                MyObjectData objectData = proxy.ObjectData;
                //objectData.Translate(-MyEnvironment.CameraPosition);

                mapping = MyMapping.MapDiscard(RC.Context, proxy.objectBuffer);
                void* ptr = &objectData;
                mapping.stream.Write(new IntPtr(ptr), 0, sizeof(MyObjectData));

                if (proxy.skinningMatrices != null)
                {
                    if (proxy.Draw.BonesMapping == null)
                    {
                        for (int j = 0; j < Math.Min(MyRender11Constants.SHADER_MAX_BONES, proxy.skinningMatrices.Length); j++)
                        { 
                            mapping.stream.Write(Matrix.Transpose(proxy.skinningMatrices[j]));
                        }
                    }
                    else
                    {
                        for (int j = 0; j < proxy.Draw.BonesMapping.Length; j++)
                        {
                            mapping.stream.Write(Matrix.Transpose(proxy.skinningMatrices[proxy.Draw.BonesMapping[j]]));
                        }
                    }
                }

                mapping.Unmap();

                Stats.ObjectConstantsChanges++;
            }
        }

        internal void BindProxyGeometry(MyRenderableProxy proxy)
        {
            //RC.SetVBs(proxy.geometry.VB, proxy.geometry.VertexStrides);
            //RC.SetIB(proxy.geometry.IB, proxy.geometry.IndexFormat);
            //RC.SetVB()
            var buffers = proxy.Mesh.Buffers;
            Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffers.VB0.Buffer, buffers.VB0.Stride, 0), new VertexBufferBinding(buffers.VB1.Buffer, buffers.VB1.Stride, 0));

            if (proxy.InstancingEnabled)
            {
                Context.InputAssembler.SetVertexBuffers(2, new VertexBufferBinding(proxy.Instancing.VB.Buffer, proxy.Instancing.VB.Stride, 0));
            }

            RC.SetIB(buffers.IB.Buffer, buffers.IB.Format);
        }

        internal virtual MyRenderingPass Fork()
        {
            var copied = (MyRenderingPass)this.MemberwiseClone();

            copied.Locals = null;
            copied.Stats = new MyPassStats();
            copied.m_currentProfilingBlock_renderableType = -1;
            copied.m_currentProfilingBlock_renderableMaterial = "";

            return copied;
        }

        internal MyRenderingPass ForkWithNewContext()
        {
            var result = Fork();
            result.SetContext(MyRenderContextPool.AcquireRC());
            return result;
        }

        internal void Join()
        {
            Debug.Assert(!m_joined);
            m_joined = true;

            MyRender11.GatherStats(Stats);
        }
    }
}
