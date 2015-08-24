using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage.FileSystem;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    struct AtmosphereLuts
    {
        internal RwTexId TransmittanceLut;
        internal RwTexId InscatterLut;
    }

    struct MyAtmosphere
    {
        internal MatrixD WorldMatrix;
        internal float PlanetRadius;
        internal float AtmosphereRadius;

        internal Vector3 BetaRayleighScattering;
        internal Vector3 BetaMieScattering;
        internal Vector2 HeightScaleRayleighMie;

        // precompute hash/id/whatever
    }

    struct AtmospherePrecomputeConstants
    {
        internal float RadiusGround;
        internal float RadiusAtmosphere;
        internal Vector2 HeightScaleRayleighMie;
        internal Vector3 BetaRayleighScattering;
        internal float RadiusLimit;
        internal Vector3 BetaMieScattering;
    }

    struct AtmosphereConstants
    {
        internal Matrix WorldViewProj;
        internal Vector3 PlanetCentre;
        internal float AtmosphereRadius;
        internal Vector3 BetaRayleighScattering;
        internal float GroundRadius;
        internal Vector3 BetaMieScattering;
        internal float RadiusLimit;
        internal Vector2 HeightScaleRayleighMie;
    }

    class MyAtmosphereRenderer
    {
        static ComputeShaderId m_precomputeDensity;
        static ComputeShaderId m_precomputeInscatter1;
        static PixelShaderId m_ps;
        static PixelShaderId m_psT;
        static PixelShaderId m_psPerSample;
        static PixelShaderId m_psTPerSample;
        static VertexShaderId m_proxyVs;
        //static 
        static InputLayoutId m_proxyIL;

        // inscatter texture
        // transmittance texture
        //static RwTexId m_transmittanceLut;
        //static RwTexId m_inscatterLutR;
        //static RwTexId m_inscatterLutM;
        

        internal static void Init()
        {
            m_ps = MyShaders.CreatePs("atmosphere.hlsl", "psAtmosphereInscatter");
            m_psT = MyShaders.CreatePs("atmosphere.hlsl", "psAtmosphereTransmittance");
            m_psPerSample = MyShaders.CreatePs("atmosphere.hlsl", "psAtmosphereInscatter", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));
            m_psTPerSample = MyShaders.CreatePs("atmosphere.hlsl", "psAtmosphereTransmittance", MyShaderHelpers.FormatMacros(MyRender11.ShaderSampleFrequencyDefine()));

            m_precomputeDensity = MyShaders.CreateCs("AtmospherePrecompute.hlsl", "precomputeDensity");
            m_precomputeInscatter1 = MyShaders.CreateCs("AtmospherePrecompute.hlsl", "precomputeInscatter1");

            m_proxyVs = MyShaders.CreateVs("atmosphere.hlsl", "proxyVs");
            m_proxyIL = MyShaders.CreateIL(m_proxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            //Precompute();
        }

        static Dictionary<uint, MyAtmosphere> Atmospheres = new Dictionary<uint, MyAtmosphere>();
        internal static Dictionary<uint, AtmosphereLuts> AtmosphereLUT = new Dictionary<uint, AtmosphereLuts>();

        internal unsafe static void AllocateLuts(ref AtmosphereLuts luts)
        {
            if (luts.TransmittanceLut == RwTexId.NULL)
            {
                luts.TransmittanceLut = MyRwTextures.CreateUav2D(256, 64, SharpDX.DXGI.Format.R16G16B16A16_Float);
                luts.InscatterLut = MyRwTextures.CreateUav3D(32, 128, 32 * 8, SharpDX.DXGI.Format.R16G16B16A16_Float);
            }
        }

        internal unsafe static void Precompute1(MyAtmosphere atmosphere, ref AtmosphereLuts luts)
        {
            var RC = MyImmediateRC.RC;

            float radiusGround = atmosphere.PlanetRadius;
            float RadiusAtmosphere = atmosphere.AtmosphereRadius;

            var cb = MyCommon.GetObjectCB(sizeof(AtmospherePrecomputeConstants));
            RC.Context.ComputeShader.SetConstantBuffer(1, cb);

            AtmospherePrecomputeConstants constants = new AtmospherePrecomputeConstants();
            constants.RadiusGround = radiusGround;
            constants.RadiusAtmosphere = RadiusAtmosphere;
            constants.RadiusLimit = RadiusAtmosphere + 1;
            constants.HeightScaleRayleighMie = atmosphere.HeightScaleRayleighMie;
            constants.BetaRayleighScattering = atmosphere.BetaRayleighScattering;
            constants.BetaMieScattering = atmosphere.BetaMieScattering;

            var mapping = MyMapping.MapDiscard(cb);
            mapping.stream.Write(constants);
            mapping.Unmap();

            // transmittance

            RC.Context.ComputeShader.SetUnorderedAccessView(0, luts.TransmittanceLut.Uav);

            RC.SetCS(m_precomputeDensity);
            RC.Context.Dispatch(256 / 8, 64 / 8, 1);

            RC.Context.ComputeShader.SetUnorderedAccessView(0, null);

            // inscatter 1

            RC.Context.ComputeShader.SetShaderResource(0, luts.TransmittanceLut.ShaderView);

            RC.SetCS(m_precomputeInscatter1);
            RC.Context.ComputeShader.SetUnorderedAccessViews(0, luts.InscatterLut.Uav);

            RC.Context.Dispatch(32 / 8, 128 / 8, 32 * 8);

            RC.Context.ComputeShader.SetUnorderedAccessViews(0, null as UnorderedAccessView, null as UnorderedAccessView);
        }

        internal static void CreateAtmosphere(uint ID, MatrixD worldMatrix, float planetRadius, float atmosphereRadius, Vector3 rayleighScattering, float rayleighHeightScale, Vector3 mieScattering, float mieHeightScale)
        {
            Atmospheres.Add(ID, new MyAtmosphere { WorldMatrix = worldMatrix, PlanetRadius = planetRadius, AtmosphereRadius = atmosphereRadius,
                                                   BetaRayleighScattering = rayleighScattering,
                                                   BetaMieScattering = mieScattering,
                                                   HeightScaleRayleighMie = new Vector2(rayleighHeightScale, mieHeightScale)
            });

            AtmosphereLuts luts = new AtmosphereLuts { TransmittanceLut = RwTexId.NULL };
            AllocateLuts(ref luts);
            Precompute1(Atmospheres[ID], ref luts);
            AtmosphereLUT[ID] = luts;
        }

        internal static void RecomputeAtmospheres()
        {
            foreach (var kv in Atmospheres)
            {
                var luts = AtmosphereLUT[kv.Key];
                Precompute1(kv.Value, ref luts);
                AtmosphereLUT[kv.Key] = luts;
            }
        }

        internal static void RemoveAtmosphere(uint ID)
        {
            Atmospheres.Remove(ID);

            if (AtmosphereLUT.ContainsKey(ID))
            {
                //MyRwTextures.Destroy(AtmosphereLUT[ID].InscatterLutM);
                //MyRwTextures.Destroy(AtmosphereLUT[ID].InscatterLutR);
                MyRwTextures.Destroy(AtmosphereLUT[ID].InscatterLut);
                MyRwTextures.Destroy(AtmosphereLUT[ID].TransmittanceLut);
            }

            AtmosphereLUT.Remove(ID);
        }

        internal unsafe static void Render()
        {
            var RC = MyImmediateRC.RC;

            if(Atmospheres.Count == 0) {
                return;
            }

            var sphereMesh = MyMeshes.GetMeshId(X.TEXT("Models/Debug/Sphere.mwm"));
            var buffers = MyMeshes.GetLodMesh(sphereMesh, 0).Buffers;
            RC.SetVB(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIB(buffers.IB.Buffer, buffers.IB.Format);

            RC.SetVS(m_proxyVs);
            RC.SetIL(m_proxyIL);
            

            RC.SetRS(null);
            RC.SetBS(MyRender11.BlendAdditive);

            var cb = MyCommon.GetObjectCB(sizeof(AtmosphereConstants));
            RC.SetCB(1, cb);
            RC.Context.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);

            // depth, 
            RC.BindGBufferForRead(0, MyGBuffer.Main);

            RC.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));


            var indicesNum = MyMeshes.GetLodMesh(sphereMesh, 0).Info.IndicesNum;

            // sort by distance
            int i = Atmospheres.Count;

            foreach (var atmosphere in Atmospheres.OrderByDescending(x => (x.Value.WorldMatrix.Translation - MyEnvironment.CameraPosition).LengthSquared()))
            {
                var worldMatrix = atmosphere.Value.WorldMatrix;
                worldMatrix.Translation -= MyEnvironment.CameraPosition;
                
                var worldViewProj = ((Matrix) worldMatrix) * MyEnvironment.ViewProjectionAt0;

                AtmosphereConstants constants = new AtmosphereConstants();
                constants.WorldViewProj = Matrix.Transpose(worldViewProj);
                constants.PlanetCentre = (Vector3)worldMatrix.Translation;
                constants.AtmosphereRadius = atmosphere.Value.AtmosphereRadius;
                constants.GroundRadius = atmosphere.Value.PlanetRadius;
                constants.BetaRayleighScattering = atmosphere.Value.BetaRayleighScattering;
                constants.BetaMieScattering = atmosphere.Value.BetaMieScattering;
                constants.HeightScaleRayleighMie = atmosphere.Value.HeightScaleRayleighMie;
                constants.RadiusLimit = constants.AtmosphereRadius + 1;

                var mapping = MyMapping.MapDiscard(cb);
                mapping.stream.Write(constants);
                mapping.Unmap();

                var luts = AtmosphereLUT[atmosphere.Key];
                RC.Context.PixelShader.SetShaderResources(5, luts.TransmittanceLut.ShaderView, luts.InscatterLut.ShaderView);

                bool inside = worldMatrix.Translation.Length() < constants.AtmosphereRadius;

                if (inside)
                {
                    RC.SetRS(MyRender11.m_invTriRasterizerState);

                    if (MyRender11.MultisamplingEnabled)
                    {
                        RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0);
                    }
                    else
                    {
                        RC.SetDS(MyDepthStencilState.IgnoreDepthStencil);
                    }                    
                }
                else
                {
                    RC.SetRS(null);

                    if (MyRender11.MultisamplingEnabled)
                    {
                        RC.SetDS(MyDepthStencilState.TestDepthAndEdgeStencil, 0);
                    }
                    else
                    {
                        RC.SetDS(MyDepthStencilState.DepthTest);
                    }
                }

                if (i == 1)
                {
                    RC.SetBS(MyRender11.BlendOutscatter);

                    RC.SetPS(m_psT);
                    RC.Context.DrawIndexed(indicesNum, 0, 0);

                    RC.SetBS(MyRender11.BlendAdditive);
                }
                RC.SetPS(m_ps);

                RC.Context.DrawIndexed(indicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    if (inside)
                    {
                        RC.SetDS(MyDepthStencilState.TestEdgeStencil, 0x80);
                    }
                    else
                    {
                        RC.SetDS(MyDepthStencilState.TestDepthAndEdgeStencil, 0x80);
                    }

                    if (i == 1)
                    {
                        RC.SetBS(MyRender11.BlendOutscatter);

                        RC.SetPS(m_psTPerSample);
                        RC.Context.DrawIndexed(indicesNum, 0, 0);

                        RC.SetBS(MyRender11.BlendAdditive);
                    }
                    RC.SetPS(m_psPerSample);
                    RC.Context.DrawIndexed(indicesNum, 0, 0);
                    
                }

                i--;
            }

            RC.Context.PixelShader.SetShaderResources(5, null, null);
            RC.SetRS(null);
            RC.SetDS(null);
        }
    }
}
