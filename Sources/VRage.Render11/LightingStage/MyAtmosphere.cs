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
    }

    struct MyAtmosphere
    {
        internal MatrixD WorldMatrix;
        internal float AtmosphereRadius;
        internal float PlanetRadius;

        internal Vector3 BetaRayleighScattering;
        internal Vector3 BetaMieScattering;
        internal Vector2 HeightScaleRayleighMie;

        internal float PlanetScaleFactor;
        internal float AtmosphereScaleFactor;

        internal MyAtmosphereSettings Settings;
        // precompute hash/id/whatever
    }

    struct AtmospherePrecomputeConstants
    {
        internal Vector3 PlanetCentre;
        internal float RadiusGround;
        internal Vector3 BetaRayleighScattering;
        internal float RadiusAtmosphere;
        internal Vector3 BetaMieScattering;
        internal float MieG;
        internal Vector2 HeightScaleRayleighMie;
        internal float PlanetScaleFactor;
        internal float AtmosphereScaleFactor;
        internal float Intensity;
        internal Vector3 __padding;
    }

    struct AtmosphereConstants
    {
        internal Matrix WorldViewProj;
        internal Vector3 PlanetCentre;
        internal float AtmosphereRadius;
        internal Vector3 BetaRayleighScattering;
        internal float GroundRadius;
        internal Vector3 BetaMieScattering;
        internal float MieG;
        internal Vector2 HeightScaleRayleighMie;
        internal float PlanetScaleFactor;
        internal float AtmosphereScaleFactor;
        internal float Intensity;
        internal float FogIntensity;
        internal Vector2 __padding;
    }

    public class MyAtmosphereRenderer
    {
        internal static bool Enabled = true;

        static ComputeShaderId m_precomputeDensity;
        static PixelShaderId m_ps;
        static PixelShaderId m_psPerSample;
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
            m_ps = MyShaders.CreatePs("atmosphere.hlsl");
            m_psPerSample = MyShaders.CreatePs("atmosphere.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_precomputeDensity = MyShaders.CreateCs("AtmospherePrecompute.hlsl");

            m_proxyVs = MyShaders.CreateVs("atmosphere.hlsl");
            m_proxyIL = MyShaders.CreateIL(m_proxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));
        }

        static Dictionary<uint, MyAtmosphere> Atmospheres = new Dictionary<uint, MyAtmosphere>();
        internal static Dictionary<uint, AtmosphereLuts> AtmosphereLUT = new Dictionary<uint, AtmosphereLuts>();

        internal unsafe static void AllocateLuts(ref AtmosphereLuts luts)
        {
            if (luts.TransmittanceLut == RwTexId.NULL)
            {
                luts.TransmittanceLut = MyRwTextures.CreateUav2D(512, 128, SharpDX.DXGI.Format.R32G32_Float);
            }
        }

        internal static void UpdateSettings(uint id, MyAtmosphereSettings settings)
        {
            MyAtmosphere atmosphere;
            if (settings.MieColorScattering.X == 0.0f)
            {
                settings.MieColorScattering = new Vector3(settings.MieScattering);
            }
            if (settings.Intensity == 0.0f)
            {
                settings.Intensity = 1.0f;
            }
            if (settings.AtmosphereTopModifier == 0.0f)
            {
                settings.AtmosphereTopModifier = 1.0f;
            }
            if (settings.SeaLevelModifier == 0.0f)
            {
                settings.SeaLevelModifier = 1.0f;
            }
            if (settings.RayleighHeightSpace == 0.0f)
            {
                settings.RayleighHeightSpace = settings.RayleighHeight;
            }
            if (settings.RayleighTransitionModifier == 0.0f)
            {
                settings.RayleighTransitionModifier = 1.0f;
            }

            if (Atmospheres.TryGetValue(id, out atmosphere))
            {
                atmosphere.Settings = settings;
                Atmospheres[id] = atmosphere;
                RecomputeAtmospheres();
            }
        }

        internal unsafe static void Precompute1(MyAtmosphere atmosphere, ref AtmosphereLuts luts)
        {
            var RC = MyImmediateRC.RC;

            float radiusGround = atmosphere.PlanetRadius;
            float RadiusAtmosphere = atmosphere.AtmosphereRadius;

            var cb = MyCommon.GetObjectCB(sizeof(AtmospherePrecomputeConstants));
            RC.DeviceContext.ComputeShader.SetConstantBuffer(1, cb);

            var worldMatrix = atmosphere.WorldMatrix;
            worldMatrix.Translation -= MyEnvironment.CameraPosition;

            AtmospherePrecomputeConstants constants = new AtmospherePrecomputeConstants();
            // Raise the ground a bit for better sunsets
            constants.RadiusGround = radiusGround * 1.01f * atmosphere.Settings.SeaLevelModifier;
            constants.RadiusAtmosphere = RadiusAtmosphere * atmosphere.Settings.AtmosphereTopModifier;
            constants.HeightScaleRayleighMie = atmosphere.HeightScaleRayleighMie * new Vector2(atmosphere.Settings.RayleighHeight, atmosphere.Settings.MieHeight);
            constants.BetaRayleighScattering = atmosphere.BetaRayleighScattering / atmosphere.Settings.RayleighScattering;
            constants.BetaMieScattering = atmosphere.BetaMieScattering / atmosphere.Settings.MieColorScattering;
            constants.MieG = atmosphere.Settings.MieG;
            constants.PlanetScaleFactor = atmosphere.PlanetScaleFactor;
            constants.AtmosphereScaleFactor = atmosphere.AtmosphereScaleFactor;
            constants.PlanetCentre = (Vector3)worldMatrix.Translation;
            constants.Intensity = atmosphere.Settings.Intensity;

            var mapping = MyMapping.MapDiscard(cb);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            // transmittance
            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(0, luts.TransmittanceLut.Uav);

            RC.SetCS(m_precomputeDensity);
            RC.DeviceContext.Dispatch(512 / 8, 128 / 8, 1);

            RC.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
        }

        internal static void CreateAtmosphere(uint ID, MatrixD worldMatrix, float planetRadius, float atmosphereRadius, 
                                              Vector3 rayleighScattering, float rayleighHeightScale, Vector3 mieScattering, float mieHeightScale,
                                              float planetScaleFactor, float atmosphereScaleFactor)
        {   
            Atmospheres.Add(ID, new MyAtmosphere { WorldMatrix = worldMatrix, PlanetRadius = planetRadius, AtmosphereRadius = atmosphereRadius,
                                                   BetaRayleighScattering = rayleighScattering,
                                                   BetaMieScattering = mieScattering,
                                                   HeightScaleRayleighMie = new Vector2(rayleighHeightScale, mieHeightScale),
                                                   PlanetScaleFactor = planetScaleFactor, AtmosphereScaleFactor = atmosphereScaleFactor
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
                MyRwTextures.Destroy(AtmosphereLUT[ID].TransmittanceLut);
            }

            AtmosphereLUT.Remove(ID);
        }

        internal unsafe static void Render()
        {
            var RC = MyImmediateRC.RC;

            if(Atmospheres.Count == 0 || !Enabled) {
                return;
            }

            var sphereMesh = MyMeshes.GetMeshId(X.TEXT("Models/Debug/Sphere.mwm"));
            var buffers = MyMeshes.GetLodMesh(sphereMesh, 0).Buffers;
            RC.SetVB(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIB(buffers.IB.Buffer, buffers.IB.Format);

            RC.SetVS(m_proxyVs);
            RC.SetIL(m_proxyIL);
            

            RC.SetRS(null);
            RC.SetBS(MyRender11.BlendAtmosphere);

            var cb = MyCommon.GetObjectCB(sizeof(AtmosphereConstants));
            RC.SetCB(1, cb);
            RC.DeviceContext.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MyRender11.m_shadowmapSamplerState);

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

                double distance = worldMatrix.Translation.Length();
                double atmosphereTop = atmosphere.Value.AtmosphereRadius * atmosphere.Value.Settings.AtmosphereTopModifier * atmosphere.Value.PlanetScaleFactor * atmosphere.Value.Settings.RayleighTransitionModifier;
                float rayleighHeight = atmosphere.Value.Settings.RayleighHeight;
                float t = 0.0f;
                if (distance > atmosphereTop)
                {
                    if (distance > atmosphereTop * 2.0f)
                    {
                        t = 1.0f;
                    }
                    else
                    {
                        t = (float)((distance - atmosphereTop) / atmosphereTop);
                    }
                }
                rayleighHeight = MathHelper.Lerp(atmosphere.Value.Settings.RayleighHeight, atmosphere.Value.Settings.RayleighHeightSpace, t);

                AtmosphereConstants constants = new AtmosphereConstants();
                constants.WorldViewProj = Matrix.Transpose(worldViewProj);
                constants.PlanetCentre = (Vector3)worldMatrix.Translation;
                constants.AtmosphereRadius = atmosphere.Value.AtmosphereRadius * atmosphere.Value.Settings.AtmosphereTopModifier;
                // Raise the ground a bit for better sunsets
                constants.GroundRadius = atmosphere.Value.PlanetRadius * 1.01f * atmosphere.Value.Settings.SeaLevelModifier;
                constants.BetaRayleighScattering = atmosphere.Value.BetaRayleighScattering / atmosphere.Value.Settings.RayleighScattering;
                constants.BetaMieScattering = atmosphere.Value.BetaMieScattering / atmosphere.Value.Settings.MieColorScattering;
                constants.HeightScaleRayleighMie = atmosphere.Value.HeightScaleRayleighMie * new Vector2(rayleighHeight, atmosphere.Value.Settings.MieHeight);
                constants.MieG = atmosphere.Value.Settings.MieG;
                constants.PlanetScaleFactor = atmosphere.Value.PlanetScaleFactor;
                constants.AtmosphereScaleFactor = atmosphere.Value.AtmosphereScaleFactor;
                constants.Intensity = atmosphere.Value.Settings.Intensity;
                constants.FogIntensity = atmosphere.Value.Settings.FogIntensity;

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                var luts = AtmosphereLUT[atmosphere.Key];
                RC.DeviceContext.PixelShader.SetShaderResources(5, luts.TransmittanceLut.ShaderView);

                bool inside = worldMatrix.Translation.Length() < atmosphere.Value.AtmosphereRadius * atmosphere.Value.PlanetScaleFactor;

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

                RC.SetPS(m_ps);

                RC.DeviceContext.DrawIndexed(indicesNum, 0, 0);

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

                    RC.SetPS(m_psPerSample);
                    RC.DeviceContext.DrawIndexed(indicesNum, 0, 0);
                    
                }

                i--;
            }

            RC.DeviceContext.PixelShader.SetShaderResources(5, null, null);
            RC.SetRS(null);
            RC.SetDS(null);
        }

        internal static uint? GetCurrentAtmosphereId()
        {
            double minDist = double.MaxValue;
            uint? minKey = null;
            foreach (var atmosphere in Atmospheres)
            {
                double sqDistance = (MyEnvironment.CameraPosition - atmosphere.Value.WorldMatrix.Translation).LengthSquared();
                if (sqDistance < minDist)
                {
                    minKey = atmosphere.Key;
                    minDist = sqDistance;
                }
            }
            return minKey;
        }

        internal static MyAtmosphere GetAtmosphere(uint id)
        {
            return Atmospheres[id];
        }

        internal static AtmosphereLuts GetAtmosphereLuts(uint id)
        {
            return AtmosphereLUT[id];
        }
    }
}
