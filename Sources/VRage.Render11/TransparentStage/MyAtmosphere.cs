using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D;
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

    struct AtmosphereConstants
    {
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
        internal Matrix WorldViewProj;
    }

    public class MyAtmosphereRenderer
    {
        internal static bool Enabled = true;

        static ComputeShaderId m_precomputeDensity;
        static PixelShaderId m_ps;
        static PixelShaderId m_psPerSample;
        private static PixelShaderId m_psEnvPerSample;
        private static PixelShaderId m_psEnv;
        static VertexShaderId m_proxyVs;
        //static 
        static InputLayoutId m_proxyIL;
        
        private static IConstantBuffer m_cb;
        private static MeshId m_sphereMesh;

        // inscatter texture
        // transmittance texture
        //static RwTexId m_transmittanceLut;
        //static RwTexId m_inscatterLutR;
        //static RwTexId m_inscatterLutM;

        internal static unsafe void Init()
        {
            m_ps = MyShaders.CreatePs("Transparent/Atmosphere/AtmosphereGBuffer.hlsl");
            m_psPerSample = MyShaders.CreatePs("Transparent/Atmosphere/AtmosphereGBuffer.hlsl", MyRender11.ShaderSampleFrequencyDefine());

            m_psEnv = MyShaders.CreatePs("Transparent/Atmosphere/AtmosphereEnv.hlsl");
            m_psEnvPerSample = MyShaders.CreatePs("Transparent/Atmosphere/AtmosphereEnv.hlsl", MyRender11.ShaderSampleFrequencyDefine());
            
            m_precomputeDensity = MyShaders.CreateCs("Transparent/Atmosphere/AtmospherePrecompute.hlsl");

            m_proxyVs = MyShaders.CreateVs("Transparent/Atmosphere/AtmosphereVS.hlsl");
            m_proxyIL = MyShaders.CreateIL(m_proxyVs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION_PACKED));

            m_cb = MyManagers.Buffers.CreateConstantBuffer("CommonObjectCB" + sizeof(AtmosphereConstants), sizeof(AtmosphereConstants),
                usage: SharpDX.Direct3D11.ResourceUsage.Dynamic);
        }

        public static void OnSessionStart()
        {
            m_sphereMesh = MyMeshes.GetMeshId(X.TEXT_("Models/Debug/Sphere.mwm"), 1.0f);
        }

        static readonly Dictionary<uint, MyAtmosphere> m_atmospheres = new Dictionary<uint, MyAtmosphere>();
        internal static readonly Dictionary<uint, AtmosphereLuts> AtmosphereLUT = new Dictionary<uint, AtmosphereLuts>();

        private static void AllocateLuts(ref AtmosphereLuts luts)
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

            if (m_atmospheres.TryGetValue(id, out atmosphere))
            {
                atmosphere.Settings = settings;
                m_atmospheres[id] = atmosphere;
                RecomputeAtmospheres();
            }
        }

        private static unsafe void Precompute1(MyAtmosphere atmosphere, ref AtmosphereLuts luts)
        {
            var RC = MyImmediateRC.RC;

            var cb = MyCommon.GetObjectCB(sizeof(AtmosphereConstants));
            RC.ComputeShader.SetConstantBuffer(1, cb);

            var worldMatrix = atmosphere.WorldMatrix;
            worldMatrix.Translation -= MyRender11.Environment.Matrices.CameraPosition;

            var constants = FillAtmosphereConstants(MyRender11.Environment.Matrices.CameraPosition, atmosphere);

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
            m_atmospheres.Add(ID, new MyAtmosphere { WorldMatrix = worldMatrix, PlanetRadius = planetRadius, AtmosphereRadius = atmosphereRadius,
                                                   BetaRayleighScattering = rayleighScattering,
                                                   BetaMieScattering = mieScattering,
                                                   HeightScaleRayleighMie = new Vector2(rayleighHeightScale, mieHeightScale),
                                                   PlanetScaleFactor = planetScaleFactor, AtmosphereScaleFactor = atmosphereScaleFactor
            });

            AtmosphereLuts luts = new AtmosphereLuts();
            AllocateLuts(ref luts);
            Precompute1(m_atmospheres[ID], ref luts);
            AtmosphereLUT[ID] = luts;
        }

        internal static void RecomputeAtmospheres()
        {
            foreach (var kv in m_atmospheres)
            {
                var luts = AtmosphereLUT[kv.Key];
                Precompute1(kv.Value, ref luts);
                AtmosphereLUT[kv.Key] = luts;
            }
        }

        internal static void RemoveAtmosphere(uint ID)
        {
            m_atmospheres.Remove(ID);

            if (AtmosphereLUT.ContainsKey(ID))
            {
                IUavTexture uav = AtmosphereLUT[ID].TransmittanceLut;
                MyManagers.RwTextures.DisposeTex(ref uav);
            }

            AtmosphereLUT.Remove(ID);
        }

        internal static void RenderGBuffer()
        {
            var RC = MyImmediateRC.RC;
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, MyGBuffer.Main.LBuffer);
            RC.PixelShader.SetSrvs(0, MyGBuffer.Main);
            RenderAll(MyRender11.Environment.Matrices.CameraPosition, ref MyRender11.Environment.Matrices.ViewProjectionAt0, m_ps, m_psPerSample);
        }

        internal static void RenderEnvProbe(Vector3D cameraPosition, ref Matrix viewProj, uint atmosphereId)
        {
            RenderBegin();
            RenderOne(cameraPosition, ref viewProj, m_psEnv, m_psEnvPerSample, atmosphereId);
            RenderEnd();
        }

        private static void RenderBegin()
        {
            var RC = MyImmediateRC.RC;
            var buffers = MyMeshes.GetLodMesh(m_sphereMesh, 0).Buffers;
            RC.SetVertexBuffer(0, buffers.VB0);
            RC.SetIndexBuffer(buffers.IB);

            RC.VertexShader.Set(m_proxyVs);
            RC.SetInputLayout(m_proxyIL);

            RC.SetRasterizerState(null);
            RC.SetBlendState(MyBlendStateManager.BlendAtmosphere);

            RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.AllShaderStages.SetConstantBuffer(1, m_cb);
            RC.PixelShader.SetSampler(MyCommon.SHADOW_SAMPLER_SLOT, MySamplerStateManager.Shadowmap);
        }

        private static void RenderEnd()
        {
            var RC = MyImmediateRC.RC;

            RC.PixelShader.SetSrvs(5, null, null);
            RC.SetRasterizerState(null);
            RC.SetDepthStencilState(null);
            RC.SetRtv(null);
        }

        static unsafe void RenderAll(Vector3D cameraPosition, ref Matrix viewProj, 
            PixelShaderId ps, PixelShaderId psPerSample)
        {
            if(m_atmospheres.Count == 0 || !Enabled)
                return;
            
            RenderBegin();

            // sort by distance
            int i = m_atmospheres.Count;
            var atmospheres = m_atmospheres.OrderByDescending((x) => (x.Value.WorldMatrix.Translation - cameraPosition).LengthSquared());
            foreach (var atmosphere in atmospheres)
            {
                RenderOne(cameraPosition, ref viewProj, ps, psPerSample, atmosphere.Key);
                i--;
            }

            RenderEnd();
        }

        private static void RenderOne(Vector3D cameraPosition, ref Matrix viewProj, 
            PixelShaderId ps, PixelShaderId psPerSample, uint atmosphereId)
        {
            if (!Enabled)
                return;

            var RC = MyImmediateRC.RC;
            var setup = m_atmospheres[atmosphereId];

            var worldMatrix = setup.WorldMatrix;
            worldMatrix.Translation -= cameraPosition;

            var worldViewProj = ((Matrix)worldMatrix) * viewProj;

            var constants = FillAtmosphereConstants(cameraPosition, setup);
            constants.WorldViewProj = Matrix.Transpose(worldViewProj);

            var mapping = MyMapping.MapDiscard(m_cb);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            var luts = AtmosphereLUT[atmosphereId];
            RC.PixelShader.SetSrvs(5, luts.TransmittanceLut);

            bool inside = worldMatrix.Translation.Length() < setup.AtmosphereRadius * setup.PlanetScaleFactor;

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

            RC.PixelShader.Set(ps);

            var indicesNum = MyMeshes.GetLodMesh(m_sphereMesh, 0).Info.IndicesNum;
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

                RC.PixelShader.Set(psPerSample);
                RC.DrawIndexed(indicesNum, 0, 0);
            }
        }

        internal static uint? GetNearestAtmosphereId()
        {
            double minDist = double.MaxValue;
            uint? minKey = null;
            foreach (var atmosphere in m_atmospheres)
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

        private static AtmosphereConstants FillAtmosphereConstants(Vector3D cameraPosition, MyAtmosphere atmosphere)
        {
            var position = atmosphere.WorldMatrix.Translation - cameraPosition;
            double distance = position.Length();
            double atmosphereTop = atmosphere.AtmosphereRadius * atmosphere.Settings.AtmosphereTopModifier * 
                atmosphere.PlanetScaleFactor * atmosphere.Settings.RayleighTransitionModifier;
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
            } var rayleighHeight = MathHelper.Lerp(atmosphere.Settings.RayleighHeight, atmosphere.Settings.RayleighHeightSpace, t);
            return new AtmosphereConstants
            {
                PlanetCentre = position,
                AtmosphereRadius = atmosphere.AtmosphereRadius * atmosphere.Settings.AtmosphereTopModifier,
                GroundRadius = atmosphere.PlanetRadius * 1.01f * atmosphere.Settings.SeaLevelModifier,
                BetaRayleighScattering = atmosphere.BetaRayleighScattering / atmosphere.Settings.RayleighScattering,
                BetaMieScattering = atmosphere.BetaMieScattering / atmosphere.Settings.MieColorScattering,
                HeightScaleRayleighMie = atmosphere.HeightScaleRayleighMie * new Vector2(rayleighHeight, atmosphere.Settings.MieHeight),
                // precompute:
                //HeightScaleRayleighMie = atmosphere.HeightScaleRayleighMie * new Vector2(atmosphere.Settings.RayleighHeight, atmosphere.Settings.MieHeight),
                MieG = atmosphere.Settings.MieG,
                PlanetScaleFactor = atmosphere.PlanetScaleFactor,
                AtmosphereScaleFactor = atmosphere.AtmosphereScaleFactor,
                Intensity = atmosphere.Settings.Intensity,
                FogIntensity = atmosphere.Settings.FogIntensity
            };
        }
    }
}
