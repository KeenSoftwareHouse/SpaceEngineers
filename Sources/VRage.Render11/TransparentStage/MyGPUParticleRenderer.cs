using SharpDX.Direct3D;
using System;
using System.Diagnostics;
using VRageMath;

namespace VRageRender
{
    internal class MyGPUParticleRenderer : MyImmediateRC
    {
        private static bool m_resetSystem;

        // GPUParticle structure is split into two sections for better cache efficiency - could even be SOA but would require creating more vertex buffers.
        private struct Particle
        {
            public Vector4 Params1, Params2, Params3, Params4, Params5;
        };
        private unsafe static readonly int PARTICLE_STRIDE = sizeof(Particle);

        private struct EmitterConstantBuffer
        {
            public int EmittersCount;
            public Vector3 Pad;
        };
        public unsafe static readonly int EMITTERCONSTANTBUFFER_SIZE = sizeof(EmitterConstantBuffer);
        public unsafe static readonly int EMITTERDATA_SIZE = sizeof(MyGPUEmitterData);

        private static VertexShaderId m_vs = VertexShaderId.NULL;
        private static PixelShaderId m_ps = PixelShaderId.NULL;
        private static PixelShaderId m_psOIT = PixelShaderId.NULL;

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

#if DEBUG
        private static MyReadStructuredBuffer m_debugCounterBuffer;
#endif

        //private static int m_numDeadParticlesOnInit;
        //private static int m_numDeadParticlesAfterEmit;

        private static MyGPUEmitterData[] m_emitterData = new MyGPUEmitterData[MyGPUEmitters.MAX_LIVE_EMITTERS];
        private static SharpDX.Direct3D11.ShaderResourceView[] m_emitterTextures = new SharpDX.Direct3D11.ShaderResourceView[MyGPUEmitters.MAX_LIVE_EMITTERS];
        internal static void Run(MyBindableResource depthRead)
        {
            // The maximum number of supported GPU particles
            SharpDX.Direct3D11.ShaderResourceView textureArraySRV;
            int emitterCount = MyGPUEmitters.Gather(m_emitterData, out textureArraySRV);
            if (emitterCount == 0)
                return;

            // Unbind current targets while we run the compute stages of the system
            //RC.DeviceContext.OutputMerger.SetTargets(null);
            // global GPU particles setup
            RC.SetDS(MyDepthStencilState.DefaultDepthState);
            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.SetIL(null);
            RC.CSSetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.DeviceContext.ComputeShader.SetSamplers(0, SamplerStates.StandardSamplers);
            RC.DeviceContext.PixelShader.SetSamplers(0, SamplerStates.StandardSamplers);
            RC.SetCB(4, MyRender11.DynamicShadows.ShadowCascades.CascadeConstantBuffer);
            RC.DeviceContext.VertexShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, SamplerStates.m_shadowmap);
            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.CASCADES_SM_SLOT, MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray.SRV);
            RC.DeviceContext.VertexShader.SetSamplers(0, SamplerStates.StandardSamplers);

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
            RC.DeviceContext.CopyStructureCount(m_activeListConstantBuffer, 0, m_aliveIndexBuffer.m_UAV);
        
            // Only read number of alive and dead particle back to the CPU in debug as we don't want to stall the GPU in release code
#if DEBUG
            int numDeadParticlesAfterSimulation = ReadCounter( m_deadListBuffer );
            int numActiveParticlesAfterSimulation = ReadCounter(m_aliveIndexBuffer);
            int numSkippedParticlesAfterSimulation = ReadCounter(m_skippedParticleCountBuffer);
#endif
    
            MyGpuProfiler.IC_BeginBlock("Render");
            Render(textureArraySRV, depthRead);
            MyGpuProfiler.IC_EndBlock();

            RC.DeviceContext.ComputeShader.SetSamplers(0, SamplerStates.StandardSamplers);
#if DEBUG
            MyRenderStats.Generic.WriteFormat("GPU particles live #: {0}", numActiveParticlesAfterSimulation, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU particles dead #: {0}", numDeadParticlesAfterSimulation, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
            MyRenderStats.Generic.WriteFormat("GPU particles skipped #: {0}", numSkippedParticlesAfterSimulation, VRage.Stats.MyStatTypeEnum.CurrentValue, 300, 0);
#endif
        }

        // Init the dead list so that all the particles in the system are marked as dead, ready to be spawned.
        private static void InitDeadList()
        {
            RC.SetCS(m_csInitDeadList);

            RC.BindUAV(0, m_deadListBuffer, 0);

            // Disaptch a set of 1d thread groups to fill out the dead list, one thread per particle
            RC.DeviceContext.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);

#if DEBUG
            //m_numDeadParticlesOnInit = ReadCounter(m_deadListBuffer);
#endif
        }

        private static void ResetInternal()
        {
            InitDeadList();

            RC.BindUAV(0, m_particleBuffer);
            RC.SetCS(m_csResetParticles);
            RC.DeviceContext.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);
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
                RC.BindUAV(0, m_particleBuffer);
                RC.BindUAV(1, m_deadListBuffer);
                RC.BindUAV(2, m_skippedParticleCountBuffer, 0);

                RC.CSSetCB(1, m_emitterConstantBuffer);

                RC.CSBindRawSRV(0, Resources.MyTextures.RandomTexId);
                RC.CSBindRawSRV(1, m_emitterStructuredBuffer);

                RC.SetCS(m_csEmit);
                RC.DeviceContext.Dispatch(numThreadGroupsX, numThreadGroupsY, 1);
                RC.CSSetCB(1, null);

                RC.SetCS(m_csEmitSkipFix);
                // Disaptch a set of 1d thread groups to fill out the dead list, one thread per particle
                RC.DeviceContext.Dispatch(1, 1, 1);
            }

#if DEBUG
            //m_numDeadParticlesAfterEmit = ReadCounter(m_deadListBuffer);
#endif
        }

        // Per-frame simulation step
        private static void Simulate(MyBindableResource depthRead)
        {
            RC.BindUAV(0, m_particleBuffer);
            RC.BindUAV(1, m_deadListBuffer);
            RC.BindUAV(2, m_aliveIndexBuffer, 0);
            RC.BindUAV(3, m_indirectDrawArgsBuffer);

            RC.BindSRV(0, depthRead);
            RC.CSBindRawSRV(1, m_emitterStructuredBuffer);

            RC.SetCS(m_csSimulate);

            RC.DeviceContext.Dispatch(align(MyGPUEmitters.MAX_PARTICLES, 256) / 256, 1, 1);

            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null, -1);
            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(1, null, -1);
            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(2, null, -1);
            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(3, null, -1);
        }

        private static void Render(SharpDX.Direct3D11.ShaderResourceView textureArraySRV, MyBindableResource depthRead)
        {
            RC.SetVS(m_vs);
            if (MyRender11.DebugOverrides.OIT)
                RC.SetPS(m_psOIT);
            else RC.SetPS(m_ps);

            RC.SetVB(0, null, 0);
            RC.SetIB(m_ib.Buffer, SharpDX.DXGI.Format.R32_UInt);
            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            RC.SetCB(1, m_activeListConstantBuffer);

            RC.BindSRV(0, depthRead);
            RC.DeviceContext.PixelShader.SetShaderResources(1, textureArraySRV);
            
            RC.VSBindRawSRV(0, m_particleBuffer);
            RC.VSBindRawSRV(1, m_aliveIndexBuffer);
            RC.VSBindRawSRV(2, m_emitterStructuredBuffer);
            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT,
                MyRender11.IsIntelBrokenCubemapsWorkaround ? Resources.MyTextures.GetView(Resources.MyTextures.IntelFallbackCubeTexId) : MyEnvironmentProbe.Instance.cubemapPrefiltered.SRV);
            RC.DeviceContext.VertexShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT,
                Resources.MyTextures.GetView(Resources.MyTextures.GetTexture(MyRender11.Environment.NightSkyboxPrefiltered, Resources.MyTextureEnum.CUBEMAP, true)));

            // bind render target?
            if (!MyStereoRender.Enable)
                RC.DeviceContext.DrawIndexedInstancedIndirect(m_indirectDrawArgsBuffer.Buffer, 0);
            else
                MyStereoRender.DrawIndexedInstancedIndirectGPUParticles(RC, m_indirectDrawArgsBuffer.Buffer, 0);

            MyRender11.ProcessDebugOutput();
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX_IBL_SLOT, null);
            RC.DeviceContext.PixelShader.SetShaderResource(MyCommon.SKYBOX2_IBL_SLOT, null);
        }

        internal static void Init()
        {
            MyGPUEmitters.Init();

            m_resetSystem = true;

            m_csInitDeadList = MyShaders.CreateCs("Particles/InitDeadList.hlsl", null);
            m_csResetParticles = MyShaders.CreateCs("Particles/Reset.hlsl", null);
            m_csEmit = MyShaders.CreateCs("Particles/Emit.hlsl", null);
            m_csEmitSkipFix = MyShaders.CreateCs("Particles/EmitSkipFix.hlsl", null);
            m_csSimulate = MyShaders.CreateCs("Particles/Simulation.hlsl", null);

            var macrosRender = new[] { new ShaderMacro("STREAKS", null), new ShaderMacro("LIT_PARTICLE", null) };
            var macrosRenderOIT = new[] { new ShaderMacro("STREAKS", null), new ShaderMacro("LIT_PARTICLE", null), new ShaderMacro("OIT", null) };
            m_vs = MyShaders.CreateVs("Particles/Render.hlsl", macrosRender);
            m_ps = MyShaders.CreatePs("Particles/Render.hlsl", macrosRender);
            m_psOIT = MyShaders.CreatePs("Particles/Render.hlsl", macrosRenderOIT);

            InitDevice();
        }

        private static void InitDevice()
        {
            m_particleBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, PARTICLE_STRIDE, MyRWStructuredBuffer.UAVType.Default, true, "MyGPUParticleRenderer::particleBuffer");
            m_deadListBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, sizeof(uint), MyRWStructuredBuffer.UAVType.Append, false, "MyGPUParticleRenderer::deadListBuffer");
            m_skippedParticleCountBuffer = new MyRWStructuredBuffer(1, sizeof(uint), MyRWStructuredBuffer.UAVType.Counter, true, "MyGPUParticleRenderer::skippedParticleCountBuffer");
#if DEBUG
            // Create a staging buffer that is used to read GPU atomic counter into that can then be mapped for reading 
            // back to the CPU for debugging purposes
            m_debugCounterBuffer = new MyReadStructuredBuffer(1, sizeof(uint), "MyGPUParticleRenderer::debugCounterBuffer");
#endif
            var description = new SharpDX.Direct3D11.BufferDescription(4 * sizeof(uint),
                SharpDX.Direct3D11.ResourceUsage.Default, SharpDX.Direct3D11.BindFlags.ConstantBuffer, SharpDX.Direct3D11.CpuAccessFlags.None, 
                SharpDX.Direct3D11.ResourceOptionFlags.None, sizeof(uint));
            m_activeListConstantBuffer = MyHwBuffers.CreateConstantsBuffer(description, "MyGPUParticleRenderer::activeListConstantBuffer");

            m_emitterConstantBuffer = MyHwBuffers.CreateConstantsBuffer(EMITTERCONSTANTBUFFER_SIZE, "MyGPUParticleRenderer::emitterConstantBuffer");
            m_emitterStructuredBuffer = MyHwBuffers.CreateStructuredBuffer(MyGPUEmitters.MAX_LIVE_EMITTERS, EMITTERDATA_SIZE, true, null, 
                "MyGPUParticleRenderer::emitterStructuredBuffer");

            m_aliveIndexBuffer = new MyRWStructuredBuffer(MyGPUEmitters.MAX_PARTICLES, sizeof(float), MyRWStructuredBuffer.UAVType.Counter, true,
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

#if DEBUG
            if (m_debugCounterBuffer != null)
            {
                m_debugCounterBuffer.Release(); m_debugCounterBuffer = null;
            }
#endif
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

        // Helper function to read atomic UAV counters back onto the CPU. This will cause a stall so only use in debug
#if DEBUG
        private static int ReadCounter(IUnorderedAccessBindable uav)
        {
            int count = 0;

            if (VRage.MyCompilationSymbols.DX11Debug)
            {
                // Copy the UAV counter to a staging resource
                RC.DeviceContext.CopyStructureCount(m_debugCounterBuffer.m_resource as SharpDX.Direct3D11.Buffer, 0, uav.UAV);

                var mapping = MyMapping.MapRead(m_debugCounterBuffer.m_resource);
                mapping.ReadAndPosition(ref count);
                mapping.Unmap();
            }

            return count;
        }
#endif

        // Helper function to align values
        private static int align(int value, int alignment) { return (value + (alignment - 1)) & ~(alignment - 1); }
    }
}
