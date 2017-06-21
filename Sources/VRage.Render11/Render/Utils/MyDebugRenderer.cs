using System;
using System.Linq;
using System.Text;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRageMath;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingBox = VRageMath.BoundingBox;
using Vector4 = VRageMath.Vector4;
using Plane = VRageMath.Plane;
using Ray = VRageMath.Ray;
using VRageRender.Vertex;
using VRage.Render11.Resources;
using VRageRender.Voxels;
using SharpDX.DXGI;
using VRage.Render11.LightingStage;

namespace VRageRender
{
    class MyDebugRenderer : MyImmediateRC
    {
        //static MyShader m_vertexShader = MyShaderCache.CreateFromFile2("primitive.hlsl", "vs", MyShaderProfile.VS_5_0);

        static PixelShaderId m_baseColorShader;
        static PixelShaderId m_baseColorLinearShader;
        static PixelShaderId m_albedoShader;
        static PixelShaderId m_normalShader;
        static PixelShaderId m_normalViewShader;
        static PixelShaderId m_glossinessShader;
        static PixelShaderId m_metalnessShader;
        static PixelShaderId m_aoShader;
        static PixelShaderId m_emissiveShader;
        static PixelShaderId m_ambientDiffuseShader;
        static PixelShaderId m_ambientSpecularShader;
        static PixelShaderId m_edgeDebugShader;
        static PixelShaderId m_shadowsDebugShader;
        static PixelShaderId m_NDotLShader;
        static PixelShaderId m_LODShader;
        private static PixelShaderId m_depthShader;
        private static ComputeShaderId m_depthReprojectionShader;
        private static PixelShaderId m_stencilShader;
        private static PixelShaderId m_rtShader;

        static VertexShaderId m_screenVertexShader;
        static PixelShaderId m_blitTextureShader;
        static PixelShaderId m_blitTexture3DShader;
        static PixelShaderId m_blitTextureArrayShader;
        static InputLayoutId m_inputLayout;

        static IVertexBuffer m_quadBuffer;

        internal static PixelShaderId BlitTextureShader { get { return m_blitTextureShader; } }

        internal static void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_screenVertexShader = MyShaders.CreateVs("Debug/DebugBaseColor.hlsl");
            m_baseColorShader = MyShaders.CreatePs("Debug/DebugBaseColor.hlsl");
            m_albedoShader = MyShaders.CreatePs("Debug/DebugAlbedo.hlsl");
            m_normalShader = MyShaders.CreatePs("Debug/DebugNormal.hlsl");
            m_normalViewShader = MyShaders.CreatePs("Debug/DebugNormalView.hlsl");
            m_glossinessShader = MyShaders.CreatePs("Debug/DebugGlossiness.hlsl");
            m_metalnessShader = MyShaders.CreatePs("Debug/DebugMetalness.hlsl");
            m_aoShader = MyShaders.CreatePs("Debug/DebugAmbientOcclusion.hlsl");
            m_emissiveShader = MyShaders.CreatePs("Debug/DebugEmissive.hlsl");
            m_ambientDiffuseShader = MyShaders.CreatePs("Debug/DebugAmbientDiffuse.hlsl");
            m_ambientSpecularShader = MyShaders.CreatePs("Debug/DebugAmbientSpecular.hlsl");
            m_edgeDebugShader = MyShaders.CreatePs("Debug/DebugEdge.hlsl");
            m_shadowsDebugShader = MyShaders.CreatePs("Debug/DebugCascadesShadow.hlsl");
            m_NDotLShader = MyShaders.CreatePs("Debug/DebugNDotL.hlsl");
            m_LODShader = MyShaders.CreatePs("Debug/DebugLOD.hlsl");
            m_depthShader = MyShaders.CreatePs("Debug/DebugDepth.hlsl");
            m_depthReprojectionShader = MyShaders.CreateCs("Debug/DebugDepthReprojection.hlsl");
            m_stencilShader = MyShaders.CreatePs("Debug/DebugStencil.hlsl");
            m_rtShader = MyShaders.CreatePs("Debug/DebugRt.hlsl");

            m_blitTextureShader = MyShaders.CreatePs("Debug/DebugBlitTexture.hlsl");
            m_blitTexture3DShader = MyShaders.CreatePs("Debug/DebugBlitTexture3D.hlsl");
            m_blitTextureArrayShader = MyShaders.CreatePs("Debug/DebugBlitTextureArray.hlsl");
            m_inputLayout = MyShaders.CreateIL(m_screenVertexShader.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION2, MyVertexInputComponentType.TEXCOORD0));

            m_quadBuffer = MyManagers.Buffers.CreateVertexBuffer(
                "MyDebugRenderer quad", 6, MyVertexFormatPosition2Texcoord.STRIDE,
                usage: ResourceUsage.Dynamic);
        }

        internal static void DrawQuad(float x, float y, float w, float h, VertexShaderId? customVertexShader = null)
        {
            //RC.Context.PixelShader.Set(m_blitTextureShader);

            VertexShaderId usedVertexShader;
            if (!customVertexShader.HasValue)
                usedVertexShader = m_screenVertexShader;
            else
                usedVertexShader = customVertexShader.Value;

            RC.VertexShader.Set(usedVertexShader);
            RC.SetInputLayout(m_inputLayout);

            var mapping = MyMapping.MapDiscard(m_quadBuffer);
            var tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x, y), new Vector2(0, 0));
            mapping.WriteAndPosition(ref tmpFormat);

            tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y + h), new Vector2(1, 1));
            mapping.WriteAndPosition(ref tmpFormat);

            tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x, y + h), new Vector2(0, 1));
            mapping.WriteAndPosition(ref tmpFormat);

            tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x, y), new Vector2(0, 0));
            mapping.WriteAndPosition(ref tmpFormat);

            tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y), new Vector2(1, 0));
            mapping.WriteAndPosition(ref tmpFormat);

            tmpFormat = new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y + h), new Vector2(1, 1));
            mapping.WriteAndPosition(ref tmpFormat);

            mapping.Unmap();

            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

            RC.SetVertexBuffer(0, m_quadBuffer);
            RC.Draw(6, 0);
        }

        internal unsafe static void DrawAtmosphereTransmittance(uint ID)
        {
            var tex = MyAtmosphereRenderer.AtmosphereLUT[ID].TransmittanceLut;

            RC.PixelShader.Set(m_blitTextureShader);
            RC.PixelShader.SetSrv(0, tex);

            DrawQuad(256, 0, 256, 64);

            RC.PixelShader.SetSrv(0, null);
        }

        internal static void DrawSrvTexture(float x, float y, float w, float h, ISrvBindable tex)
        {
            RC.PixelShader.Set(m_blitTextureShader);

            RC.PixelShader.SetSrv(0, tex);

            DrawQuad(x, y, w, h);
        }

        internal static void DrawDepthArrayTexture(IDepthArrayTexture tex, int quadStartX, int quadStartY, int quadSize)
        {
            DrawSrvArrayTexture(tex, tex.NumSlices, quadStartX, quadStartY, quadSize);
        }

        private static void DrawSrvArrayTexture(ISrvBindable textureArray, int texCount, int quadStartX, int quadStartY, int quadSize)
        {
            RC.PixelShader.Set(m_blitTextureArrayShader);
            RC.PixelShader.SetSrv(0, textureArray);

            var cb = MyCommon.GetMaterialCB(sizeof(uint));
            RC.PixelShader.SetConstantBuffer(5, cb);

            for (uint cascadeIndex = 0; cascadeIndex < texCount; cascadeIndex++)
            {
                float index = (float)cascadeIndex;
                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref index);
                mapping.Unmap();
                DrawQuad(quadStartX + (quadSize + quadStartX / 2) * cascadeIndex, quadStartY, quadSize, quadSize * MyRender11.ViewportResolution.Y / MyRender11.ViewportResolution.X);
            }
            RC.PixelShader.SetSrv(0, null);
        }

        internal static void DrawParticles(ISrvBindable particleRenderTarget, int quadStartX, int quadStartY, int quadSize)
        {
            RC.PixelShader.Set(m_blitTextureShader);
            RC.AllShaderStages.SetSrv(0, particleRenderTarget);
            RC.SetBlendState(MyBlendStateManager.BlendAlphaPremult);

            DrawQuad(quadStartX, quadStartY, quadSize, quadSize * MyRender11.ViewportResolution.Y / MyRender11.ViewportResolution.X);
            RC.SetBlendState(null);
            RC.PixelShader.SetSrv(0, null);
        }

        internal static void DrawEnvProbe()
        {
            int colorMipmap = 0;
            IUavArrayTexture colorTexture = MyManagers.EnvironmentProbe.Cubemap;
            DrawSrvTexture(256 * 2, 256 * 1, 256, 256, colorTexture.SubresourceSrv(0, colorMipmap));
            DrawSrvTexture(256 * 0, 256 * 1, 256, 256, colorTexture.SubresourceSrv(1, colorMipmap));
            DrawSrvTexture(256 * 1, 256 * 0, 256, 256, colorTexture.SubresourceSrv(2, colorMipmap));
            DrawSrvTexture(256 * 1, 256 * 2, 256, 256, colorTexture.SubresourceSrv(3, colorMipmap));
            DrawSrvTexture(256 * 1, 256 * 1, 256, 256, colorTexture.SubresourceSrv(4, colorMipmap));
            DrawSrvTexture(256 * 3, 256 * 1, 256, 256, colorTexture.SubresourceSrv(5, colorMipmap));
        }

        internal static void Draw(IRtvBindable renderTarget, IRtvTexture ambientOcclusion)
        {
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);

            RC.SetRtv(renderTarget);
            RC.PixelShader.SetSrvs(0, MyGBuffer.Main);

            RC.SetBlendState(null);

            if (MyRender11.Settings.DisplayGbufferColor)
            {
                RC.PixelShader.Set(m_baseColorShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferAlbedo)
            {
                RC.PixelShader.Set(m_albedoShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferNormal)
            {
                RC.PixelShader.Set(m_normalShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferNormalView)
            {
                RC.PixelShader.Set(m_normalViewShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferGlossiness)
            {
                RC.PixelShader.Set(m_glossinessShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferMetalness)
            {
                RC.PixelShader.Set(m_metalnessShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferAO)
            {
                RC.PixelShader.Set(m_aoShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayEmissive)
            {
                RC.PixelShader.Set(m_emissiveShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayAmbientDiffuse)
            {
                RC.PixelShader.Set(m_ambientDiffuseShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayAmbientSpecular)
            {
                RC.PixelShader.Set(m_ambientSpecularShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayEdgeMask)
            {
                RC.PixelShader.Set(m_edgeDebugShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayShadowsWithDebug)
            {
                RC.PixelShader.Set(m_shadowsDebugShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayNDotL)
            {
                RC.PixelShader.Set(m_NDotLShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferLOD)
            {
                RC.PixelShader.Set(m_LODShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayMipmap)
            {
                RC.PixelShader.Set(m_baseColorShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayDepth)
            {
                RC.PixelShader.Set(m_depthShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayReprojectedDepth)
            {
                var dst = MyManagers.RwTexturesPool.BorrowUav("DebugRender.DepthReprojection", Format.R32_Float);
                MyRender11.RC.ClearUav(dst, SharpDX.Int4.Zero);

                RC.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.ComputeShader.SetSrv(0, MyGBuffer.Main.DepthStencil.SrvDepth);
                RC.ComputeShader.SetUav(0, dst);
                RC.ComputeShader.Set(m_depthReprojectionShader);
                int numThreadGroupsX = align(MyRender11.ResolutionI.X, 32) / 32;
                int numThreadGroupsY = align(MyRender11.ResolutionI.Y, 32) / 32;
                RC.Dispatch(numThreadGroupsX, numThreadGroupsY, 1);
                RC.ComputeShader.SetSrv(0, null);
                RC.ComputeShader.SetUav(0, null);

                RC.PixelShader.SetSrv(0, dst);
                RC.PixelShader.Set(m_depthShader);
                MyScreenPass.DrawFullscreenQuad();

                RC.PixelShader.SetSrv(0, MyGBuffer.Main.DepthStencil.SrvDepth);
            }
            else if (MyRender11.Settings.DisplayStencil)
            {
                RC.PixelShader.Set(m_stencilShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayAO)
            {
                RC.PixelShader.SetSrv(0, ambientOcclusion);
                RC.PixelShader.SetSampler(0, MySamplerStateManager.Linear);
                RC.PixelShader.Set(m_rtShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayEnvProbe)
            {
                DrawEnvProbe();
            }

            //DrawEnvProbe();
            //DrawAtmosphereTransmittance(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);
            //DrawAtmosphereInscatter(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);

            if (MyRender11.Settings.DrawCascadeTextures)
            {
                //DrawDepthArrayTexture(MyManagers.Shadow.GetCsmForGbuffer(), 100, 100, 200);
                DrawDepthArrayTexture(MyRender11.DynamicShadows.ShadowCascades.CascadeShadowmapArray, 100, 100, 200);
                if (MyScene.SeparateGeometry)
                {
                    DrawDepthArrayTexture(MyRender11.StaticShadows.ShadowCascades.CascadeShadowmapArray, 100, 300, 200);
                    DrawDepthArrayTexture(MyShadowCascades.CombineShadowmapArray, 100, 500, 200);
                }
            }

            if (MyRender11.Settings.DisplayIDs || MyRender11.Settings.DisplayAabbs)
            {
                DrawHierarchyDebug();
            }

            if (false)
            {
                var batch = MyLinesRenderer.CreateBatch();

                foreach (var light in MyLightsRendering.VisiblePointlights)
                {
                    batch.AddSphereRing(new BoundingSphere(light.PointPosition, 0.5f), Color.White, Matrix.Identity);
                }
                batch.Commit();
            }

            // draw terrain lods
            if (MyRender11.Settings.DebugRenderClipmapCells)
            {
                //var batch = MyLinesRenderer.CreateBatch();

                //foreach (var renderable in MyComponentFactory<MyRenderableComponent>.GetAll().Where(x => (MyMeshes.IsVoxelMesh(x.Mesh))))
                //{
                //    if (renderable.IsVisible)
                //    {
                //        if (renderable.m_lod >= LOD_COLORS.Length)
                //            return;

                //        BoundingBox bb = new BoundingBox(renderable.m_owner.Aabb.Min - MyRender11.Environment.CameraPosition,renderable.m_owner.Aabb.Max - MyRender11.Environment.CameraPosition);

                //        batch.AddBoundingBox(bb, new Color(LOD_COLORS[renderable.m_voxelLod]));


                //        if (renderable.m_lods != null && renderable.m_voxelLod != renderable.m_lods[0].RenderableProxies[0].ObjectData.CustomAlpha)
                //        {

                //        }
                //    }
                //}

                //batch.Commit();

                MyClipmap.DebugDrawClipmaps();
            }

            //if(true)
            //{
            //    var batch = MyLinesRenderer.CreateBatch();

            //    foreach(var id in MyLights.DirtySpotlights)
            //    {
            //        var info = MyLights.Spotlights[id.Index];

            //        if(info.ApertureCos > 0)
            //        {
            //            var D = info.Direction * info.Range;
            //            //batch.AddCone(MyLights.Lights.Data[id.Index].Position + D, -D, info.Up.Cross(info.Direction) * info.BaseRatio * info.Range, 32, Color.OrangeRed);

            //            //var bb = MyLights.AabbFromCone(info.Direction, info.ApertureCos, info.Range).Transform(Matrix.CreateLookAt(MyLights.Lights.Data[id.Index].Position, info.Direction, info.Up));


            //            //batch.AddBoundingBox(bb, Color.Green);

            //            batch.AddCone(MyLights.Lights.Data[id.Index].Position + D, -D, info.Up.Cross(info.Direction) * info.BaseRatio * info.Range, 32, Color.OrangeRed);

            //            var bb = MyLights.AabbFromCone(info.Direction, info.ApertureCos, info.Range, MyLights.Lights.Data[id.Index].Position, info.Up);
            //            batch.AddBoundingBox(bb, Color.Green);
            //        } 
            //    }



            //    batch.Commit();
            //}

            // draw lods
            if (false)
            {
                var batch = MyLinesRenderer.CreateBatch();

                //foreach (var renderable in MyComponentFactory<MyRenderableComponent>.GetAll().Where(x => ((x.GetMesh() as MyVoxelMesh) == null)))
                //{

                //    if (renderable.CurrentLodNum >= LOD_COLORS.Length || renderable.m_lods.Length == 1)
                //        continue;

                //    batch.AddBoundingBox(renderable.m_owner.Aabb, new Color(LOD_COLORS[renderable.CurrentLodNum]));
                //}

                batch.Commit();
            }
        }

        private static int align(int value, int alignment) { return (value + (alignment - 1)) & ~(alignment - 1); }
        private static readonly Vector4[] LOD_COLORS = 
        {
	        new Vector4( 1, 0, 0, 1 ),
	        new Vector4(  0, 1, 0, 1 ),
	        new Vector4(  0, 0, 1, 1 ),

	        new Vector4(  1, 1, 0, 1 ),
	        new Vector4(  0, 1, 1, 1 ),
	        new Vector4(  1, 0, 1, 1 ),

	        new Vector4(  0.5f, 0, 1, 1 ),
	        new Vector4(  0.5f, 1, 0, 1 ),
	        new Vector4(  1, 0, 0.5f, 1 ),
	        new Vector4(  0, 1, 0.5f, 1 ),

	        new Vector4(  1, 0.5f, 0, 1 ),
	        new Vector4(  0, 0.5f, 1, 1 ),

	        new Vector4(  0.5f, 1, 1, 1 ),
	        new Vector4(  1, 0.5f, 1, 1 ),
	        new Vector4(  1, 1, 0.5f, 1 ),
	        new Vector4(  0.5f, 0.5f, 1, 1 ),	
        };

        internal static void DrawHierarchyDebug()
        {
            var worldToClip = MyRender11.Environment.Matrices.ViewProjection;
            var displayString = new StringBuilder();

            var batch = MyLinesRenderer.CreateBatch();

            if (MyRender11.Settings.DisplayIDs)
            {
                foreach (var actor in MyActorFactory.GetAll())
                {
                    var h = actor.GetGroupLeaf();
                    var r = actor.GetRenderable();

                    Vector3 position;
                    uint ID;

                    if (r != null)
                    {
                        position = r.Owner.WorldMatrix.Translation;
                        ID = r.Owner.ID;
                    }
                    else if (h != null)
                    {
                        position = h.Owner.WorldMatrix.Translation;
                        ID = h.Owner.ID;
                    }
                    else
                    {
                        continue;
                    }

                    var clipPosition = Vector3.Transform(position, ref worldToClip);
                    clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                    clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                    if (clipPosition.Z > 0 && clipPosition.Z < 1)
                    {
                        displayString.AppendFormat("{0}", ID);
                        MyDebugTextHelpers.DrawText(new Vector2(clipPosition.X, clipPosition.Y) * MyRender11.ViewportResolution,
                            displayString, Color.DarkCyan, 0.5f);
                    }

                    displayString.Clear();
                }
            }

            if (MyRender11.Settings.DisplayAabbs)
            {
                foreach (var actor in MyActorFactory.GetAll())
                {
                    var h = actor.GetGroupRoot();
                    var r = actor.GetRenderable();

                    if (h != null)
                    {
                        var bb = BoundingBoxD.CreateInvalid();

                        foreach (var child in h.m_children)
                        {
                            if (child.IsVisible)
                            {
                                bb.Include(child.Aabb);
                            }
                        }

                        batch.AddBoundingBox(((BoundingBox)bb).Translate(-MyRender11.Environment.Matrices.CameraPosition), Color.Red);
                        MyPrimitivesRenderer.Draw6FacedConvexZ(bb.GetCorners().Select(x => (Vector3)x).ToArray(), Color.Red, 0.1f);
                    }
                    else if (r != null && actor.GetGroupLeaf() == null)
                    {
                        batch.AddBoundingBox(((BoundingBox)r.Owner.Aabb).Translate(-MyRender11.Environment.Matrices.CameraPosition), Color.Green);
                    }
                }
            }

            batch.Commit();
        }
    }

    partial class MyRender11
    {
        private static Ray ComputeIntersectionLine(ref Plane p1, ref Plane p2)
        {
            Ray ray = new Ray();
            ray.Direction = Vector3.Cross(p1.Normal, p2.Normal);
            float num = ray.Direction.LengthSquared();
            ray.Position = Vector3.Cross(-p1.D * p2.Normal + p2.D * p1.Normal, ray.Direction) / num;
            return ray;
        }

        private static void TransformRay(ref Ray ray, ref Matrix matrix)
        {
            ray.Direction = Vector3.Transform(ray.Position + ray.Direction, ref matrix);
            ray.Position = Vector3.Transform(ray.Position, ref matrix);
            ray.Direction = ray.Direction - ray.Position;
        }

        static Matrix m_proj;
        static Matrix m_vp;
        static Matrix m_invvp;

        internal static void DrawSceneDebug()
        {
            //if(true)
            //{
            //    //m_proj = MyRender11.Environment.Projection;
            //    //m_vp = MyRender11.Environment.ViewProjection;
            //    //m_invvp = MyRender11.Environment.InvViewProjection;

            //    Vector2 groupDim = new Vector2(256, 256);
            //    Vector2 tileScale = new Vector2(1600, 900) / (2 * groupDim);
            //    Vector2 tileBias = tileScale - new Vector2(1, 1);


            //    //Vector4 c1 = new Vector4(m_proj.M11 * tileScale.X, 0, tileBias.X, 0);
            //    //Vector4 c2 = new Vector4(0, -m_proj.M22 * tileScale.Y, tileBias.Y, 0);
            //    Vector4 c1 = new Vector4(m_proj.M11, 0, 0, 0);
            //    Vector4 c2 = new Vector4(0, m_proj.M22, 0, 0);
            //    Vector4 c4 = new Vector4(0, 0, 1, 0);

            //    var frustumPlane0 = new VRageMath.Plane(c4 - c1);
            //    var frustumPlane1 = new VRageMath.Plane(c4 + c1);
            //    var frustumPlane2 = new VRageMath.Plane(c4 - c2);
            //    var frustumPlane3 = new VRageMath.Plane(c4 + c2);
            //    frustumPlane0.Normalize();
            //    frustumPlane1.Normalize();
            //    frustumPlane2.Normalize();
            //    frustumPlane3.Normalize();


            //    var ray0 = ComputeIntersectionLine(ref frustumPlane2, ref frustumPlane0);
            //    var ray1 = ComputeIntersectionLine(ref frustumPlane1, ref frustumPlane2);
            //    var ray2 = ComputeIntersectionLine(ref frustumPlane3, ref frustumPlane1);
            //    var ray3 = ComputeIntersectionLine(ref frustumPlane0, ref frustumPlane3);


            //    TransformRay(ref ray0, ref m_invvp);
            //    TransformRay(ref ray1, ref m_invvp);
            //    TransformRay(ref ray2, ref m_invvp);
            //    TransformRay(ref ray3, ref m_invvp);


            //    var batch = MyLinesRenderer.CreateBatch();

            //    batch.Add(ray0.Position, ray0.Position + ray0.Direction * 100, Color.Red);
            //    batch.Add(ray1.Position, ray1.Position + ray1.Direction * 100, Color.Red);
            //    batch.Add(ray2.Position, ray2.Position + ray2.Direction * 100, Color.Red);
            //    batch.Add(ray3.Position, ray3.Position + ray3.Direction * 100, Color.Red);

            //    batch.AddFrustum(new BoundingFrustum(m_vp), Color.Green);

            //    batch.Commit();

            //}

            // draw lights
            //if(false)
            //{
            //    MyLinesBatch batch = MyLinesRenderer.CreateBatch();

            //    foreach (var light in MyLight.Collection)
            //    {
            //        if (light.PointLightEnabled)
            //        {
            //            var position = light.GetPosition();
            //            //batch.AddBoundingBox(new BoundingBox(position - light.Pointlight.Range, position + light.Pointlight.Range), Color.Red);

            //            batch.AddSphereRing(new BoundingSphere(position, light.Pointlight.Range), new Color(light.Pointlight.Color), Matrix.Identity);
            //            batch.AddSphereRing(new BoundingSphere(position, light.Pointlight.Range), new Color(light.Pointlight.Color), Matrix.CreateRotationX((float)Math.PI * 0.5f));
            //            batch.AddSphereRing(new BoundingSphere(position, light.Pointlight.Range), new Color(light.Pointlight.Color), Matrix.CreateRotationZ((float)Math.PI * 0.5f));

            //            batch.AddSphereRing(new BoundingSphere(position, light.Pointlight.Radius), new Color(light.Pointlight.Color), Matrix.Identity);
            //            batch.AddSphereRing(new BoundingSphere(position, light.Pointlight.Radius), new Color(light.Pointlight.Color), Matrix.CreateRotationX((float)Math.PI * 0.5f));
            //        }
            //    }

            //    batch.Commit();
            //}

            //
            if (false)
            {
                MyLinesBatch batch = MyLinesRenderer.CreateBatch();

                foreach (var r in MyComponentFactory<MyRenderableComponent>.GetAll())
                {
                    if (r.Owner.GetInstanceLod() != null)
                    {
                        batch.AddBoundingBox((BoundingBox)r.Owner.Aabb, Color.Blue);
                    }
                }

                batch.Commit();
            }

            if (false)
            {
                MyLinesBatch batch = MyLinesRenderer.CreateBatch();
                //var radius = new [] { 0, 40, 72, 128, 256 , 512 };
                var radius = new[] { 0, 50, 80, 128, 256, 512 };
                float cellSize = 8;

                var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Pink, Color.MediumVioletRed };

                var prevPositionG = Vector3.PositiveInfinity;
                for (int i = 0; i < 4; i++)
                {
                    float levelCellSize = cellSize * (float)Math.Pow(2, i);
                    var position = MyRender11.Environment.Matrices.CameraPosition;
                    //var position = Vector3.Zero;
                    position.Y = 0;
                    var positionG = position.Snap(levelCellSize * 2);

                    float radiusMin = radius[i];
                    float radiusMax = radius[i + 1];

                    // naive

                    var pmin = (positionG - radiusMax - levelCellSize * 2).Snap(levelCellSize * 2);
                    var pmax = (positionG + radiusMax + levelCellSize * 2).Snap(levelCellSize * 2);

                    //if(i==0)
                    //{
                    //    for (var x = pmin.X; x < pmax.X; x += levelCellSize)
                    //    {
                    //        for (var y = pmin.Y; y < pmax.Y; y += levelCellSize)
                    //        {
                    //            for (var z = pmin.Z; z < pmax.Z; z += levelCellSize)
                    //            {
                    //                var cell = new Vector3(x, y, z);
                    //                var rep = cell.Snap(levelCellSize * 2);

                    //                var inLevelGrid = (rep - positionG).Length() < radiusMax;
                    //                if(inLevelGrid)
                    //                {
                    //                    batch.AddBoundingBox(new BoundingBox(cell, cell + levelCellSize), colors[i]);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //else 
                    {
                        for (var x = pmin.X; x < pmax.X; x += levelCellSize)
                        {
                            for (var z = pmin.Z; z < pmax.Z; z += levelCellSize)
                            {
                                var cell = new Vector3(x, positionG.Y, z);
                                var rep = cell.Snap(levelCellSize * 2);

                                var inPrevLevelGrid = (cell - prevPositionG).Length() < radiusMin;
                                var inLevelGrid = (rep - positionG).Length() < radiusMax;

                                if (inLevelGrid && !inPrevLevelGrid)
                                {
                                    batch.AddBoundingBox(new BoundingBox(cell, cell + levelCellSize), colors[i]);
                                }
                            }
                        }
                    }

                    prevPositionG = positionG;
                }


                batch.Commit();
            }
        }
    }
}
