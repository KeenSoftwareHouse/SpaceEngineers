using SharpDX.Direct3D;
using System;
using System.Diagnostics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    internal class MyGPUParticleRenderer : MyImmediateRC
    {
        private static bool m_resetSystem;

        // GPUParticle structure is split into two sections for better cache efficiency - could even be SOA but would require creating more vertex buffers.
        private struct Particle
        {
            internal Vector4 Params1, Params2, Params3, Params4, Params5;
        };
        private unsafe static readonly int PARTICLE_STRIDE = sizeof(Particle);

        private struct EmitterConstantBuffer
        {
            internal int EmittersCount;
            internal Vector3 Pad;
        };
        internal unsafe static readonly int EMITTERCONSTANTBUFFER_SIZE = sizeof(EmitterConstantBuffer);
        internal unsafe static readonly int EMITTERDATA_SIZE = sizeof(MyGPUEmitterData);

        private static VertexShaderId m_vs = VertexShaderId.NULL;
        private static PixelShaderId m_ps = PixelShaderId.NULL;
        private static PixelShaderId m_psOIT = PixelShaderId.NULL;
        static PixelShaderId m_psDebugUniformAccum;
        static PixelShaderId m_psDebugUniformAccumOIT;

        private static ComputeShaderId m_csSimulate = ComputeShaderId.NULL;
        private static ComputeShaderId m_csInitDeadList = ComputeShaderId.NULL;
        private static ComputeShaderId m_csEmit = ComputeShaderId.NULL;
        private static ComputeShaderId m_csEmitSkipFix = ComputeShaderId.NULL;
        private static ComputeShaderId m_csResetParticles = ComputeShaderId.NULL;

        private static MyRWStructuredBuffer m_particleBuffer;
        private static MyRWStructuredBuffer m_deadListBuffer;
        private static MyRWStructuredBuffer m_skippedParticleCountBuffer;
        private static ConstantsBufferId m_activeListConstantBuffer = ConstantsBufferId.NULL;

        private static ConstantsBufferId m_emitterConstantBuffer = ConstantsBufferId.NULL;
        private static StructuredBufferId m_emitterStructuredBuffer = StructuredBufferId.NULL;

        private static MyRWStructuredBuffer m_aliveIndexBuffer;
        private static MyIndirectArgsBuffer m_indirectDrawArgsBuffer;

        private static IndexBufferId m_ib = IndexBufferId.NULL;

        private static MyReadStructuredBuffer[] m_debugCounterBuffers = new MyReadStructuredBuffer[2];
        static int m_debugCounterBuffersIndex = 0;

        //private static int m_numDeadParticlesOnInit;
        //private static int m_numDeadParticlesAfterEmit;

        private static MyGPUEmitterData[] m_emitterData = new MyGPUEmitterData[MyGPUEmitters.MAX_LIVE_EMITTERS];
        private static ISrvBindable[] m_emitterTextures = new ISrvBindable[MyGPUEmitters.MAX_LIVE_EMITTERS];
        internal static void Run(ISrvBindable depthRead)
        {
            // The maximum number of supported GPU particles
            ISrvBindable textureArraySrv;
            int emitterCount = MyGPUEmitters.Gather(m_emitterData, out textureArraySrv);
            if (emitterCount == 0)
                return;

            // Unbind current targets while we run the compute stages of the system
            //RC.DeviceContext.OutputMerger.SetTargets(null);
            // global GPU particles setup
            RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            RC.SetInputLayout(null);
            RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.ComputeShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.AllShaderStages.SetConstantBuffer(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.VertexShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);
            RC.VertexShader.SetSrv(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray);
            RC.VertexShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            // If we are resetting the particle system, then initialize the dead list
            if (m_resetSystem)
            {
                ResetInternal();
                m_resetSystem = false;
            }

            MyGpuProfiler.IC_BeginBlock("Emit");
            // Emit particles into the system
            Emit(emitterCount, m_emitterData);
            MyGpuProfiler.IC_EndBlock();

            // Run the simulation for this frame
            MyGpuProfiler.IC_BeginBlock("Simulate");
            Simulate(depthRead);
            MyGpuProfiler.IC_EndBlock();

            // Copy the atomic counter in the alive list UAV into a constant buffer for access by subsequent passes
            RC.CopyStructureCount(m_activeListConstantBuffer, 0, m_aliveIndexBuffer.m_uav);
        
            // Only read number of alive and dead particle back to the CPU in debug as we don't want to stall the GPU in release code

            ProfilerShort.Begin("Debug - ReadCounter");
            MyGpuProfiler.IC_BeginBlock("Debug - ReadCounter");
            int numActiveParticlesAfterSimulation = ReadCounter(m_aliveIndexBuffer);
            MyGpuProfiler.IC_EndBlock();
            ProfilerShort.End();
    
            MyGpuProfiler.IC_BeginBlock("Render");
            Render(textureArraySrv, depthRead);
            MyGpuProfiler.IC_EndBlock();

            RC.ComputeShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            MyStatsDisplay.Write("GPU particles", "Live #", numActiveParticlesAfterSimulation);
        }

        // Init the dead list so that all the particles in the system are marked as dead, ready to be spawned.
        private static void InitDeadList()
        {
            RC.ComputeShader.Set(m_csInitDeadList);

            RC.ComputeShader.SetRawUav(0, m_deadListBuffer.m_uav, 0);

            // Disaptch a set of 1d thread groups to fill out the dead list, one thread per particle
            RC.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);
        }

        private static void ResetInternal()
        {
            InitDeadList();

            RC.ComputeShader.SetRawUav(0, m_particleBuffer.m_uav);
            RC.ComputeShader.Set(m_csResetParticles);
            RC.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);
        }
        private const int MAX_PARTICLE_EMIT_THREADS = 128;
        private const int MAX_EMITTERS = 8;

        // Per-frame emission of particles into the GPU simulation
        private static void Emit(int emitterCount, MyGPUEmitterData[] emitterData)
        {
            // update emitter data
            var mapping = MyMapping.MapDiscard(m_emitterStructuredBuffer.Buffer);
            int maxParticlesToEmitThisFrame = 0;
            for (int i = 0; i < emitterCount; i++)
            {
                mapping.WriteAndPosition(ref emitterData[i]);

                if (emitterData[i].NumParticlesToEmitThisFrame > maxParticlesToEmitThisFrame)
                    maxParticlesToEmitThisFrame = emitterData[i].NumParticlesToEmitThisFrame;
            }
            mapping.Unmap();

            int numThreadGroupsX = align(maxParticlesToEmitThisFrame, MAX_PARTICLE_EMIT_THREADS) / MAX_PARTICLE_EMIT_THREADS;
            int numThreadGroupsY = align(emitterCount, MAX_EMITTERS) / MAX_EMITTERS;

            // update emitter count
            mapping = MyMapping.MapDiscard(m_emitterConstantBuffer);
            mapping.WriteAndPosition(ref emitterCount);
            mapping.WriteAndPosition(ref numThreadGroupsX);
            mapping.Unmap();

            if (maxParticlesToEmitThisFrame > 0)
            {
                // Set resources but don't reset any atomic counters
                RC.ComputeShader.SetRawUav(0, m_particleBuffer.m_uav);
                RC.ComputeShader.SetRawUav(1, m_deadListBuffer.m_uav);
                RC.ComputeShader.SetRawUav(2, m_skippedParticleCountBuffer.m_uav, 0);

                RC.ComputeShader.SetConstantBuffer(1, m_emitterConstantBuffer);

                RC.ComputeShader.SetRawSrv(1, m_emitterStructuredBuffer.Srv);

                RC.ComputeShader.Set(m_csEmit);
                RC.Dispatch(numThreadGroupsX, numThreadGroupsY, 1);
                RC.ComputeShader.SetConstantBuffer(1, null);

                RC.ComputeShader.Set(m_csEmitSkipFix);
                // Disaptch a set of 1d thread groups to fill out the dead list, one thread per particle
                RC.Dispatch(1, 1, 1);
            }
        }

        // Per-frame simulation step
        private static void Simulate(ISrvBindable depthRead)
        {
            RC.ComputeShader.SetRawUav(0, m_particleBuffer.m_uav);
            RC.ComputeShader.SetRawUav(1, m_deadListBuffer.m_uav);
            RC.ComputeShader.SetRawUav(2, m_aliveIndexBuffer.m_uav, 0);
            RC.ComputeShader.SetRawUav(3, m_indirectDrawArgsBuffer.m_uav);

            RC.ComputeShader.SetSrv(0, depthRead);
            RC.ComputeShader.SetRawSrv(1, m_emitterStructuredBuffer.Srv);

            RC.ComputeShader.Set(m_csSimulate);

            RC.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);

            RC.ComputeShader.SetUav(0, null);
            RC.ComputeShader.SetUav(1, null);
            RC.ComputeShader.SetUav(2, null);
            RC.ComputeShader.SetUav(3, null);
        }

        private static void Render(ISrvBindable textureArraySRV, ISrvBindable depthRead)
        {
            RC.VertexShader.Set(m_vs);
            if (MyRender11.Settings.DisplayTransparencyHeatMap)
            {
                if (MyRender11.DebugOverrides.OIT)
                    RC.PixelShader.Set(m_psDebugUniformAccumOIT);
                else RC.PixelShader.Set(m_psDebugUniformAccum);
            }
            else
            {
                if (MyRender11.DebugOverrides.OIT)
                    RC.PixelShader.Set(m_psOIT);
                else RC.PixelShader.Set(m_ps);
            }

            RC.SetVertexBuffer(0, null, 0);
            RC.SetIndexBuffer(m_ib.Buffer, SharpDX.DXGI.Format.R32_UInt);
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

            RC.AllShaderStages.SetConstantBuffer(1, m_activeListConstantBuffer);

            RC.AllShaderStages.SetSrv(0, depthRead);
            RC.PixelShader.SetSrvs(1, textureArraySRV);
            
            RC.VertexShader.SetRawSrv(0, m_particleBuffer.m_srv);
            RC.VertexShader.SetRawSrv(1, m_emitterStructuredBuffer.Srv);
            RC.VertexShader.SetRawSrv(2, m_aliveIndexBuffer.m_srv);
            ISrvBindable skybox = MyRender11.IsIntelBrokenCubemapsWorkaround
                ? MyGeneratedTextureManager.IntelFallbackCubeTex
                : (ISrvBindable)MyManagers.EnvironmentProbe.Cubemap;
            RC.VertexShader.SetSrv(MyCommon.SKYBOX_IBL_SLOT, skybox);

            // bind render target?
            if (!MyStereoRender.Enable)
                RC.DrawIndexedInstancedIndirect(m_indirectDrawArgsBuffer.Buffer, 0);
            else
                MyStereoRender.DrawIndexedInstancedIndirectGPUParticles(RC, m_indirectDrawArgsBuffer.Buffer, 0);

            MyRender11.ProcessDebugOutput();
            RC.VertexShader.SetSrv(MyCommon.SKYBOX_IBL_SLOT, null);
            RC.AllShaderStages.SetSrv(0, null);
        }

        internal static void Init()
        {
            MyGPUEmitters.Init();

            m_resetSystem = true;

            m_csInitDeadList = MyShaders.CreateCs("Transparent/GPUParticles/InitDeadList.hlsl", null);
            m_csResetParticles = MyShaders.CreateCs("Transparent/GPUParticles/Reset.hlsl", null);
            m_csEmit = MyShaders.CreateCs("Transparent/GPUParticles/Emit.hlsl", null);
            m_csEmitSkipFix = MyShaders.CreateCs("Transparent/GPUParticles/EmitSkipFix.hlsl", null);
            m_csSimulate = MyShaders.CreateCs("Transparent/GPUParticles/Simulation.hlsl", null);

            var macrosRender = new[] { new ShaderMacro("STREAKS", null), new ShaderMacro("LIT_PARTICLE", null) };
            var macrosRenderOIT = new[] { new ShaderMacro("STREAKS", null), new ShaderMacro("LIT_PARTICLE", null), new ShaderMacro("OIT", null) };
            m_vs = MyShaders.CreateVs("Transparent/GPUParticles/Render.hlsl", macrosRender);
            m_ps = MyShaders.CreatePs("Transparent/GPUParticles/Render.hlsl", macrosRender);
            m_psOIT = MyShaders.CreatePs("Transparent/GPUParticles/Render.hlsl", macrosRenderOIT);

            var macroDebug = new[] { new ShaderMacro("DEBUG_UNIFORM_ACCUM", null)};
            m_psDebugUniformAccum = MyShaders.CreatePs("Transparent/GPUParticles/Render.hlsl", MyShaders.ConcatenateMacros(macrosRender, macroDebug));
            m_psDebugUniformAccumOIT = MyShaders.CreatePs("Transparent/GPUParticles/Render.hlsl", MyShaders.ConcatenateMacros(macrosRenderOIT, macroDebug));

            InitDevice();
        }

        private static void InitDevice()
        {
            m_particleBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, PARTICLE_STRIDE, MyRWStructuredBuffer.UavType.Default, true, "MyGPUParticleRenderer::particleBuffer");
            m_deadListBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, sizeof(uint), MyRWStructuredBuffer.UavType.Append, false, "MyGPUParticleRenderer::deadListBuffer");
            m_skippedParticleCountBuffer = new MyRWStructuredBuffer(1, sizeof(uint), MyRWStructuredBuffer.UavType.Counter, true, "MyGPUParticleRenderer::skippedParticleCountBuffer");
 
            // Create a staging buffer that is used to read GPU atomic counter into that can then be mapped for reading 
            // back to the CPU for debugging purposes
            m_debugCounterBuffers[0] = new MyReadStructuredBuffer(1, sizeof(uint), "MyGPUParticleRenderer::debugCounterBuffers[0]");
            m_debugCounterBuffers[1] = new MyReadStructuredBuffer(1, sizeof(uint), "MyGPUParticleRenderer::debugCounterBuffers[1]");

            var description = new SharpDX.Direct3D11.BufferDescription(4 * sizeof(uint),
                SharpDX.Direct3D11.ResourceUsage.Default, SharpDX.Direct3D11.BindFlags.ConstantBuffer, SharpDX.Direct3D11.CpuAccessFlags.None, 
                SharpDX.Direct3D11.ResourceOptionFlags.None, sizeof(uint));
            m_activeListConstantBuffer = MyHwBuffers.CreateConstantsBuffer(description, "MyGPUParticleRenderer::activeListConstantBuffer");

            m_emitterConstantBuffer = MyHwBuffers.CreateConstantsBuffer(EMITTERCONSTANTBUFFER_SIZE, "MyGPUParticleRenderer::emitterConstantBuffer");
            m_emitterStructuredBuffer = MyHwBuffers.CreateStructuredBuffer(MyGPUEmitters.MAX_LIVE_EMITTERS, EMITTERDATA_SIZE, true, null,
                "MyGPUParticleRenderer::emitterStructuredBuffer");

            m_aliveIndexBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, sizeof(float), MyRWStructuredBuffer.UavType.Counter, true,
                "MyGPUParticleRenderer::aliveIndexBuffer");

            m_indirectDrawArgsBuffer = new MyIndirectArgsBuffer(5, sizeof(uint), "MyGPUParticleRenderer::indirectDrawArgsBuffer");

            unsafe
            {
                uint[] indices = new uint[MyGPUEmitters.MAX_PARTICLES * 6];
                for (uint i = 0, index = 0, vertex = 0; i < MyGPUEmitters.MAX_PARTICLES; i++)
                {
                    indices[index + 0] = vertex + 0;
                    indices[index + 1] = vertex + 1;
                    indices[index + 2] = vertex + 2;

                    indices[index + 3] = vertex + 2;
                    indices[index + 4] = vertex + 1;
                    indices[index + 5] = vertex + 3;

                    vertex += 4;
                    index += 6;
                }
                fixed (uint* ptr = indices)
                {
                    m_ib = MyHwBuffers.CreateIndexBuffer(MyGPUEmitters.MAX_PARTICLES * 6, SharpDX.DXGI.Format.R32_UInt,
                        SharpDX.Direct3D11.BindFlags.IndexBuffer, SharpDX.Direct3D11.ResourceUsage.Immutable, new IntPtr(ptr), "MyGPUParticleRenderer::indexBuffer");
                }
            }

            //MyRender11.BlendAlphaPremult
        }

        internal static void OnDeviceReset()
        {
            DoneDevice();
            InitDevice();
        }
        private static void DoneDevice()
        {
            MyHwBuffers.Destroy(ref m_ib);
            MyHwBuffers.Destroy(ref m_activeListConstantBuffer);

            if (m_indirectDrawArgsBuffer != null)
            {
                m_indirectDrawArgsBuffer.Release(); m_indirectDrawArgsBuffer = null;
            }
            if (m_debugCounterBuffers[0] != null)
            {
                m_debugCounterBuffers[0].Release(); m_debugCounterBuffers[0] = null;
            } 
            if (m_debugCounterBuffers[1] != null)
            {
                m_debugCounterBuffers[1].Release(); m_debugCounterBuffers[1] = null;
            }   
            if (m_aliveIndexBuffer != null)
            {
                m_aliveIndexBuffer.Release(); m_aliveIndexBuffer = null;
            }
            if (m_deadListBuffer != null)
            {
                m_deadListBuffer.Release(); m_deadListBuffer = null;
            }
            if (m_skippedParticleCountBuffer != null)
            {
                m_skippedParticleCountBuffer.Release(); m_skippedParticleCountBuffer = null;
            }
            if (m_particleBuffer != null)
            {
                m_particleBuffer.Release(); m_particleBuffer = null;
            }
            MyHwBuffers.Destroy(ref m_emitterConstantBuffer);
            MyHwBuffers.Destroy(ref m_emitterStructuredBuffer);

        }

        internal static void OnDeviceEnd()
        {
            DoneDevice();

            m_ps = PixelShaderId.NULL;
            m_psOIT = PixelShaderId.NULL;
            m_vs = VertexShaderId.NULL;

            m_csSimulate = ComputeShaderId.NULL;

            m_csResetParticles = ComputeShaderId.NULL;
            m_csInitDeadList = ComputeShaderId.NULL;
            m_csEmit = ComputeShaderId.NULL;
            m_csEmitSkipFix = ComputeShaderId.NULL;

            m_resetSystem = true;
        }

        internal static void OnSessionEnd()
        {
            MyGPUEmitters.OnSessionEnd();
            m_resetSystem = true;
        }

        internal static void Reset()
        {
            m_resetSystem = true;
        }

        // IMPORTANT: This method can be called only once per frame. If it is not, it will stall CPU for quite long time
        // Helper function to read atomic UAV counters back onto the CPU.
        // The method reads in the current frame the value from the previous frame
        private static int ReadCounter(IUnorderedAccessBindable uav)
        {
            int count = 0;

            if (VRage.MyCompilationSymbols.DX11Debug)
            {
                // Copy the UAV counter to a staging resource
                RC.CopyStructureCount(m_debugCounterBuffers[m_debugCounterBuffersIndex].m_resource as SharpDX.Direct3D11.Buffer, 0, uav.Uav);

                m_debugCounterBuffersIndex = m_debugCounterBuffersIndex == 1 ? 0 : 1;
                var mapping = MyMapping.MapRead(m_debugCounterBuffers[m_debugCounterBuffersIndex].m_resource);
                mapping.ReadAndPosition(ref count);
                mapping.Unmap();
            }

            return count;
        }

        // Helper function to align values
        private static int align(int value, int alignment) { return (value + (alignment - 1)) & ~(alignment - 1); }
    }
}
