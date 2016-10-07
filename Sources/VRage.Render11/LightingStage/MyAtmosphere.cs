﻿using System.Collections.Generic;
using System.Linq;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    struct AtmosphereLuts
    {
        internal IUavTexture TransmittanceLut;
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
            m_ps = MyShaders.CreatePs("Transparent/Atmosphere/Atmosphere.hlsl");
            m_psPerSample = MyShaders.CreatePs("Transparent/Atmosphere/Atmosphere.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_precomputeDensity = MyShaders.CreateCs("Transparent/Atmosphere/AtmospherePrecompute.hlsl");

            m_proxyVs = MyShaders.CreateVs("Transparent/Atmosphere/Atmosphere.hlsl");
            m_proxyIL = MyShaders.CreateIL(m_proxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));
        }

        static Dictionary<uint, MyAtmosphere> Atmospheres = new Dictionary<uint, MyAtmosphere>();
        internal static Dictionary<uint, AtmosphereLuts> AtmosphereLUT = new Dictionary<uint, AtmosphereLuts>();

        internal unsafe static void AllocateLuts(ref AtmosphereLuts luts)
        {
            if (luts.TransmittanceLut == null)
            {
                luts.TransmittanceLut = MyManagers.RwTextures.CreateUav("AtmosphereLuts.TransmittanceLut",
                    512, 128, SharpDX.DXGI.Format.R32G32_Float);
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
            RC.ComputeShader.SetConstantBuffer(1, cb);

            var worldMatrix = atmosphere.WorldMatrix;
            worldMatrix.Translation -= MyRender11.Environment.Matrices.CameraPosition;

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
            RC.ComputeShader.SetUav(0, luts.TransmittanceLut);

            RC.ComputeShader.Set(m_precomputeDensity);
            RC.Dispatch(512 / 8, 128 / 8, 1);

            RC.ComputeShader.SetUav(0, null);
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

            AtmosphereLuts luts = new AtmosphereLuts();
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
                IUavTexture uav = AtmosphereLUT[ID].TransmittanceLut;
                MyManagers.RwTextures.DisposeTex(ref uav);
            }

            AtmosphereLUT.Remove(ID);
        }

        internal unsafe static void Render()
        {
            var RC = MyImmediateRC.RC;

            if(Atmospheres.Count == 0 || !Enabled) {
                return;
            }

            var sphereMesh = MyMeshes.GetMeshId(X.TEXT_("Models/Debug/Sphere.mwm"), 1.0f);
            var buffers = MyMeshes.GetLodMesh(sphereMesh, 0).Buffers;
            RC.SetVertexBuffer(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            RC.SetIndexBuffer(buffers.IB.Buffer, buffers.IB.Format);

            RC.VertexShader.Set(m_proxyVs);
            RC.SetInputLayout(m_proxyIL);
            

            RC.SetRasterizerState(null);
            RC.SetBlendState(MyBlendStateManager.BlendAtmosphere);

            RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);

            var cb = MyCommon.GetObjectCB(sizeof(AtmosphereConstants));
            RC.AllShaderStages.SetConstantBuffer(1, cb);
            RC.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);

            // depth, 
            RC.PixelShader.SetSrvs(0, MyGBuffer.Main);

            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);


            var indicesNum = MyMeshes.GetLodMesh(sphereMesh, 0).Info.IndicesNum;

            // sort by distance
            int i = Atmospheres.Count;

            foreach (var atmosphere in Atmospheres.OrderByDescending(x => (x.Value.WorldMatrix.Translation - MyRender11.Environment.Matrices.CameraPosition).LengthSquared()))
            {
                var worldMatrix = atmosphere.Value.WorldMatrix;
                worldMatrix.Translation -= MyRender11.Environment.Matrices.CameraPosition;
                
                var worldViewProj = ((Matrix) worldMatrix) * MyRender11.Environment.Matrices.ViewProjectionAt0;

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
                RC.PixelShader.SetSrvs(5, luts.TransmittanceLut);

                bool inside = worldMatrix.Translation.Length() < atmosphere.Value.AtmosphereRadius * atmosphere.Value.PlanetScaleFactor;

                if (inside)
                {
                    RC.SetRasterizerState(MyRasterizerStateManager.InvTriRasterizerState);

                    if (MyRender11.MultisamplingEnabled)
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0);
                    }
                    else
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
                    }                    
                }
                else
                {
                    RC.SetRasterizerState(null);

                    if (MyRender11.MultisamplingEnabled)
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.TestDepthAndEdgeStencil, 0);
                    }
                    else
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestReadOnly);
                    }
                }

                RC.PixelShader.Set(m_ps);

                RC.DrawIndexed(indicesNum, 0, 0);

                if (MyRender11.MultisamplingEnabled)
                {
                    if (inside)
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0x80);
                    }
                    else
                    {
                        RC.SetDepthStencilState(MyDepthStencilStateManager.TestDepthAndEdgeStencil, 0x80);
                    }

                    RC.PixelShader.Set(m_psPerSample);
                    RC.DrawIndexed(indicesNum, 0, 0);
                    
                }

                i--;
            }

            RC.PixelShader.SetSrvs(5, null, null);
            RC.SetRasterizerState(null);
            RC.SetDepthStencilState(null);
            RC.SetRtv(null);
        }

        internal static uint? GetCurrentAtmosphereId()
        {
            double minDist = double.MaxValue;
            uint? minKey = null;
            foreach (var atmosphere in Atmospheres)
            {
                double sqDistance = (MyRender11.Environment.Matrices.CameraPosition - atmosphere.Value.WorldMatrix.Translation).LengthSquared();
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
