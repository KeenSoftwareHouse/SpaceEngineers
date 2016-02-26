using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    struct MyCloudLayer
    {
        public struct MyCloudTextureInfo
        {
            public TexId ColorMetalTexture;
            public TexId AlphaTexture;
            public TexId NormalGlossTexture;
        }

        internal Vector3D CenterPoint;
        internal double Altitude;
        internal double MinScaledAltitude;
        internal bool ScalingEnabled;

        internal double MaxPlanetHillRadius;

        internal MeshId Mesh;
        internal MyCloudTextureInfo TextureInfo;

        internal Vector3D RotationAxis;
        internal float AngularVelocity;

        public double FadeOutRelativeAltitudeStart;
        public double FadeOutRelativeAltitudeEnd;
        public float ApplyFogRelativeDistance;
    }

    class MyModifiableCloudLayerData
    {
        internal double RadiansAroundAxis;
        internal int LastGameplayFrameUpdate;
    }

    struct CloudsConstants
    {
        internal Matrix World;
        internal Matrix ViewProj;
        internal Vector4 Color;
    }

    struct FogConstants
    {
        internal float LayerAltitude;
        internal float CameraAltitude;
        internal float LayerThickness;
        internal uint CameraTexelX;
        internal uint CameraTexelY;
        internal Vector3 _padding;
    }

    public class MyCloudRenderer
    {
        static VertexShaderId m_proxyVs;
        static PixelShaderId m_cloudPs;
        static ComputeShaderId m_fogShader;
        static InputLayoutId m_proxyIL;
        static SamplerId m_textureSampler;

        const int m_numFogThreads = 8;

        public static bool DrawFog = false;

        internal static void Init()
        {
            m_proxyVs = MyShaders.CreateVs("clouds.hlsl");
            m_cloudPs = MyShaders.CreatePs("clouds.hlsl");
            m_proxyIL = MyShaders.CreateIL(m_proxyVs.BytecodeId, MyVertexLayouts.GetLayout(
                    new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED, 0),
                    new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1),
                    new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1),
                    new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1)));

            m_fogShader = MyShaders.CreateCs("clouds.hlsl", new [] {new ShaderMacro("NUMTHREADS", m_numFogThreads)});

            SamplerStateDescription description = new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = System.Single.MaxValue
            };
            m_textureSampler = MyPipelineStates.CreateSamplerState(description);
        }

        private static readonly Dictionary<uint, MyCloudLayer> m_cloudLayers = new Dictionary<uint, MyCloudLayer>();
        private static readonly List<uint> m_closestCloudsIndexList = new List<uint>();
        private static readonly Dictionary<uint, MyModifiableCloudLayerData> m_modifiableCloudLayerData = new Dictionary<uint, MyModifiableCloudLayerData>();

        internal static void CreateCloudLayer(
            uint ID,
            Vector3D centerPoint,
            double altitude,
            double minScaledAltitude,
            bool scalingEnabled,
            double fadeOutRelativeAltitudeStart,
            double fadeOutRelativeAltitudeEnd,
            float applyFogRelativeDistance,
            double maxPlanetHillRadius,
            string model,
            List<string> textures,
            Vector3D rotationAxis,
            float angularVelocity,
            float radiansAroundAxis)
        {
            MeshId mesh = MyMeshes.GetMeshId(X.TEXT(model));
            MyCloudLayer.MyCloudTextureInfo textureInfo;
            if (textures != null && textures.Count > 0) // TODO: Multiple textures
            {
                var cmTexture = textures[0].Insert(textures[0].LastIndexOf('.'), "_cm");
                var alphaTexture = textures[0].Insert(textures[0].LastIndexOf('.'), "_alphamask");
                var normalGlossTexture = textures[0].Insert(textures[0].LastIndexOf('.'), "_ng");
                textureInfo = new MyCloudLayer.MyCloudTextureInfo
                {
                    ColorMetalTexture = MyTextures.GetTexture(cmTexture, MyTextureEnum.COLOR_METAL),
                    AlphaTexture = MyTextures.GetTexture(alphaTexture, MyTextureEnum.ALPHAMASK),
                    NormalGlossTexture = MyTextures.GetTexture(normalGlossTexture, MyTextureEnum.NORMALMAP_GLOSS),
                };
            }
            else
                textureInfo = new MyCloudLayer.MyCloudTextureInfo
                {
                    ColorMetalTexture = MyTextures.GetTexture(MyMeshes.GetMeshPart(mesh, 0, 0).Info.Material.Info.ColorMetal_Texture.ToString(), MyTextureEnum.COLOR_METAL),
                    AlphaTexture = MyTextures.GetTexture(MyMeshes.GetMeshPart(mesh, 0, 0).Info.Material.Info.Alphamask_Texture.ToString(), MyTextureEnum.ALPHAMASK),
                    NormalGlossTexture = MyTextures.GetTexture(MyMeshes.GetMeshPart(mesh, 0, 0).Info.Material.Info.NormalGloss_Texture.ToString(), MyTextureEnum.NORMALMAP_GLOSS),
                };

            m_cloudLayers.Add(ID, new MyCloudLayer
            {
                CenterPoint = centerPoint,
                Altitude = altitude,
                MinScaledAltitude = minScaledAltitude,
                ScalingEnabled = scalingEnabled,
                FadeOutRelativeAltitudeStart = fadeOutRelativeAltitudeStart,
                FadeOutRelativeAltitudeEnd = fadeOutRelativeAltitudeEnd,
                ApplyFogRelativeDistance = applyFogRelativeDistance,
                MaxPlanetHillRadius = maxPlanetHillRadius,
                Mesh = mesh,
                TextureInfo = textureInfo,
                RotationAxis = rotationAxis,
                AngularVelocity = angularVelocity,
            });
            m_modifiableCloudLayerData.Add(ID, new MyModifiableCloudLayerData { RadiansAroundAxis = radiansAroundAxis, LastGameplayFrameUpdate = MyRender11.Settings.GameplayFrame });
        }

        internal static void RemoveCloud(uint ID)
        {
            m_cloudLayers.Remove(ID);
            m_modifiableCloudLayerData.Remove(ID);
        }

        internal unsafe static void Render()
        {
            if (m_cloudLayers.Count == 0)
                return;

            var immediateContext = MyImmediateRC.RC;

            immediateContext.SetVS(m_proxyVs);
            immediateContext.SetPS(m_cloudPs);
            immediateContext.SetIL(m_proxyIL);

            immediateContext.SetRS(MyRender11.m_nocullRasterizerState);
            immediateContext.SetBS(MyRender11.BlendTransparent);
            immediateContext.SetDS(MyDepthStencilState.DepthTest);

            var cb = MyCommon.GetObjectCB(sizeof(CloudsConstants));
            immediateContext.SetCB(1, cb);
            immediateContext.DeviceContext.PixelShader.SetSamplers(0, m_textureSampler);

            immediateContext.BindDepthRT(MyGBuffer.Main.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadOnly, MyGBuffer.Main.Get(MyGbufferSlot.LBuffer));

            m_cloudLayers.OrderByDescending(x =>
            {
                MyCloudLayer cloudLayer = x.Value;
                Vector3D cameraToLayer = cloudLayer.CenterPoint - MyEnvironment.CameraPosition;
                Vector3D layerToCameraDirection = -Vector3D.Normalize(cameraToLayer);
                return (cameraToLayer + layerToCameraDirection * cloudLayer.Altitude).Length();
            });

            foreach (var cloudLayer in m_cloudLayers)
            {
                var modifiableData = m_modifiableCloudLayerData[cloudLayer.Key];
                int currentGameplayFrame = MyRender11.Settings.GameplayFrame;
                var increment = cloudLayer.Value.AngularVelocity * (float)(currentGameplayFrame - modifiableData.LastGameplayFrameUpdate) / 10.0f;
                modifiableData.RadiansAroundAxis += increment; // Constant for backward compatibility
                if (modifiableData.RadiansAroundAxis >= 2 * Math.PI)
                    modifiableData.RadiansAroundAxis -= 2 * Math.PI;

                modifiableData.LastGameplayFrameUpdate = currentGameplayFrame;

                double scaledAltitude = cloudLayer.Value.Altitude;
                Vector3D centerPoint = cloudLayer.Value.CenterPoint;
                Vector3D cameraPosition = MyEnvironment.CameraPosition;
                double cameraDistanceFromCenter = (centerPoint - cameraPosition).Length();

                if (cloudLayer.Value.ScalingEnabled)
                {
                    double threshold = cloudLayer.Value.Altitude * 0.95;
                    if (cameraDistanceFromCenter > threshold)
                    {
                        scaledAltitude = MathHelper.Clamp(scaledAltitude * (1 - MathHelper.Clamp((cameraDistanceFromCenter - threshold) / (threshold * 1.5), 0.0, 1.0)), cloudLayer.Value.MinScaledAltitude, cloudLayer.Value.Altitude);
                    }
                }

                MatrixD worldMatrix = MatrixD.CreateScale(scaledAltitude) * MatrixD.CreateFromAxisAngle(cloudLayer.Value.RotationAxis, (float)modifiableData.RadiansAroundAxis);
                worldMatrix.Translation = cloudLayer.Value.CenterPoint;
                worldMatrix.Translation -= MyEnvironment.CameraPosition;

                float layerAlpha = 1.0f;

                double currentRelativeAltitude = (cameraDistanceFromCenter - cloudLayer.Value.MinScaledAltitude) / (cloudLayer.Value.MaxPlanetHillRadius - cloudLayer.Value.MinScaledAltitude);
                if (cloudLayer.Value.FadeOutRelativeAltitudeStart > cloudLayer.Value.FadeOutRelativeAltitudeEnd)
                {
                    layerAlpha = (float)MathHelper.Clamp(1.0 - (cloudLayer.Value.FadeOutRelativeAltitudeStart - currentRelativeAltitude) / (cloudLayer.Value.FadeOutRelativeAltitudeStart - cloudLayer.Value.FadeOutRelativeAltitudeEnd), 0.0, 1.0);
                }
                else if (cloudLayer.Value.FadeOutRelativeAltitudeStart < cloudLayer.Value.FadeOutRelativeAltitudeEnd)
                {
                    layerAlpha = (float)MathHelper.Clamp(1.0 - (currentRelativeAltitude - cloudLayer.Value.FadeOutRelativeAltitudeStart) / (cloudLayer.Value.FadeOutRelativeAltitudeEnd - cloudLayer.Value.FadeOutRelativeAltitudeStart), 0.0, 1.0);
                }

                Vector4 layerColor = new Vector4(1, 1, 1, layerAlpha);

                var constants = new CloudsConstants();
                constants.World = MatrixD.Transpose(worldMatrix);
                constants.ViewProj = MatrixD.Transpose(MyEnvironment.ViewProjectionAt0);
                constants.Color = layerColor;

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                MeshId sphereMesh = cloudLayer.Value.Mesh;
                LodMeshId lodMesh = MyMeshes.GetLodMesh(sphereMesh, 0);
                MyMeshBuffers buffers = lodMesh.Buffers;
                immediateContext.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffers.VB0.Buffer, buffers.VB0.Stride, 0));
                immediateContext.DeviceContext.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(buffers.VB1.Buffer, buffers.VB1.Stride, 0));
                immediateContext.SetIB(buffers.IB.Buffer, buffers.IB.Format);

                immediateContext.DeviceContext.PixelShader.SetShaderResource(0, MyTextures.GetView(cloudLayer.Value.TextureInfo.ColorMetalTexture));
                immediateContext.DeviceContext.PixelShader.SetShaderResource(1, MyTextures.GetView(cloudLayer.Value.TextureInfo.AlphaTexture));
                immediateContext.DeviceContext.PixelShader.SetShaderResource(2, MyTextures.GetView(cloudLayer.Value.TextureInfo.NormalGlossTexture));

                immediateContext.DeviceContext.DrawIndexed(lodMesh.Info.IndicesNum, 0, 0);
            }

            immediateContext.DeviceContext.PixelShader.SetShaderResource(0, null);
            immediateContext.DeviceContext.PixelShader.SetShaderResource(1, null);
            immediateContext.DeviceContext.PixelShader.SetShaderResource(2, null);
            immediateContext.SetDS(null);
            immediateContext.SetBS(null);
            immediateContext.SetRS(null);
        }

        private static void FindClosestClouds(List<uint> indexList)
        {
            m_closestCloudsIndexList.Clear();
            Vector3D cameraPosition = MyEnvironment.CameraPosition;

            foreach (KeyValuePair<uint, MyCloudLayer> cloudLayer in m_cloudLayers)
            {
                Vector3D centerPoint = cloudLayer.Value.CenterPoint;
                double distanceToCenterSq = (centerPoint - cameraPosition).LengthSquared();
                double cutoffDistance = cloudLayer.Value.Altitude * 2;
                if (distanceToCenterSq > cutoffDistance * cutoffDistance)
                    continue;

                indexList.Add(cloudLayer.Key);
            }
        }
        /*
        internal unsafe static void ApplyFog(MyBindableResource targetUav)
        {
            if (!DrawFog)
                return;

            FindClosestClouds(m_closestCloudsIndexList);

            var immediateContext = MyImmediateRC.RC;

            immediateContext.BindSRV(0, MyGBuffer.Main.DepthStencil.Depth);

            Vector3D cameraPosition = MyEnvironment.CameraPosition;
            foreach (var cloudLayerIndex in m_closestCloudsIndexList)
            {
                var cloudLayer = m_cloudLayers[cloudLayerIndex];
                if (cloudLayer.ApplyFogRelativeDistance == 0)
                    continue;

                Vector3D centerPoint = cloudLayer.CenterPoint;
                double distanceToCenter = (cameraPosition - centerPoint).Length();
                double altitudeRatio = distanceToCenter / cloudLayer.Altitude - 1;
                if (altitudeRatio > cloudLayer.ApplyFogRelativeDistance / 2.0 ||
                    altitudeRatio < -cloudLayer.ApplyFogRelativeDistance / 2.0)
                    continue;

                MyBindableResource sourceTexture = MyGBuffer.Main.Get(MyGbufferSlot.LBuffer);
                MyBindableResource destinationTexture = targetUav;

                var textureSize = sourceTexture.GetSize();

                immediateContext.BindUAV(0, destinationTexture);

                immediateContext.BindSRV(1, sourceTexture);
                immediateContext.DeviceContext.ComputeShader.SetShaderResource(2, MyTextures.GetView(cloudLayer.TextureInfo.AlphaTexture));
                immediateContext.SetCS(m_fogShader);
                var cb = MyCommon.GetObjectCB(sizeof(FogConstants));
                immediateContext.CSSetCB(1, cb);

                var modifiableData = m_modifiableCloudLayerData[cloudLayerIndex];
                MatrixD worldMatrix = MatrixD.CreateFromAxisAngle(cloudLayer.RotationAxis, (float)modifiableData.RadiansAroundAxis);
                worldMatrix.Translation = cloudLayer.CenterPoint;
                Vector3D cameraInCloudLocal = Vector3D.Normalize(Vector3D.Transform(cameraPosition, MatrixD.Invert(worldMatrix)));

                            //double theta = Math.Acos(cameraInCloudLocal.Y);
                            //double phi = Math.Atan2(cameraInCloudLocal.X, - cameraInCloudLocal.Z);
                            //if (phi < 0)
                            //    phi += 2.0 * Math.PI;

                            //Vector2I texel = new Vector2I((int)Math.Round(phi / (2.0 * Math.PI)*textureSize.X), (int)Math.Round((theta / Math.PI * textureSize.Y)));
        
                var constants = new FogConstants();
                constants.CameraAltitude = (float)distanceToCenter;
                constants.LayerAltitude = (float)cloudLayer.Altitude;
                constants.LayerThickness = (float)(cloudLayer.ApplyFogRelativeDistance * cloudLayer.Altitude);
                //constants.cameraTexelX = (uint)texel.X;
                //constants.cameraTexelY = (uint)texel.Y;

                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                immediateContext.DeviceContext.Dispatch((textureSize.X + m_numFogThreads - 1) / m_numFogThreads, (textureSize.Y + m_numFogThreads - 1) / m_numFogThreads, 1);

                immediateContext.SetCS(null);

                immediateContext.DeviceContext.CopySubresourceRegion(destinationTexture.m_resource, 0, new ResourceRegion(0, 0, 0, textureSize.X, textureSize.Y, 1), sourceTexture.m_resource, 0); // TODO: Combine this with something else instead

                break;	// Only do one cloud's fog
            }
        }*/
    }
}
