using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageMath;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Vector4 = VRageMath.Vector4;
using Plane = VRageMath.Plane;
using Ray = VRageMath.Ray;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRage.Voxels;
using VRageRender.Resources;

namespace VRageRender
{
    class MyDebugRenderer : MyImmediateRC
    {
        //static MyShader m_vertexShader = MyShaderCache.CreateFromFile2("primitive.hlsl", "vs", MyShaderProfile.VS_5_0);

        static PixelShaderId m_baseColorShader;
        static PixelShaderId m_baseColorLinearShader;
        static PixelShaderId m_normalShader;
        static PixelShaderId m_glossinessShader;
        static PixelShaderId m_metalnessShader;
        static PixelShaderId m_matIDShader;
        static PixelShaderId m_aoShader;
        static PixelShaderId m_emissiveShader;
        static PixelShaderId m_ambientDiffuseShader;
        static PixelShaderId m_ambientSpecularShader;
        static PixelShaderId m_edgeDebugShader;
        static PixelShaderId m_shadowsDebugShader;
        static PixelShaderId m_NDotLShader;
        private static PixelShaderId m_stencilShader;

        static VertexShaderId m_screenVertexShader;
        static PixelShaderId m_blitTextureShader;
        static PixelShaderId m_blitTexture3DShader;
        static PixelShaderId m_blitTextureArrayShader;
        static InputLayoutId m_inputLayout;

        static VertexBufferId m_quadBuffer;

        internal static void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
            m_screenVertexShader = MyShaders.CreateVs("debug_base_color.hlsl");
            m_baseColorShader = MyShaders.CreatePs("debug_base_color.hlsl");
            m_baseColorLinearShader = MyShaders.CreatePs("debug_base_color_linear.hlsl");
            m_normalShader = MyShaders.CreatePs("debug_normal.hlsl");
            m_glossinessShader = MyShaders.CreatePs("debug_glossiness.hlsl");
            m_metalnessShader = MyShaders.CreatePs("debug_metalness.hlsl");
            m_matIDShader = MyShaders.CreatePs("debug_mat_id.hlsl");
            m_aoShader = MyShaders.CreatePs("debug_ambient_occlusion.hlsl");
            m_emissiveShader = MyShaders.CreatePs("debug_emissive.hlsl");
            m_ambientDiffuseShader = MyShaders.CreatePs("debug_ambient_diffuse.hlsl");
            m_ambientSpecularShader = MyShaders.CreatePs("debug_ambient_specular.hlsl");
            m_edgeDebugShader = MyShaders.CreatePs("debug_edge.hlsl");
            m_shadowsDebugShader = MyShaders.CreatePs("debug_cascades_shadow.hlsl");
            m_NDotLShader = MyShaders.CreatePs("debug_NDotL.hlsl");
            m_stencilShader = MyShaders.CreatePs("debug_Stencil.hlsl");

            m_blitTextureShader = MyShaders.CreatePs("debug_blitTexture.hlsl");
            m_blitTexture3DShader = MyShaders.CreatePs("debug_blitTexture3D.hlsl");
            m_blitTextureArrayShader = MyShaders.CreatePs("debug_blitTextureArray.hlsl");
            m_inputLayout = MyShaders.CreateIL(m_screenVertexShader.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION2, MyVertexInputComponentType.TEXCOORD0));

            m_quadBuffer = MyHwBuffers.CreateVertexBuffer(6, MyVertexFormatPosition2Texcoord.STRIDE, BindFlags.VertexBuffer, ResourceUsage.Dynamic);
        }

        internal static void DrawQuad(float x, float y, float w, float h)
        {
            //RC.Context.PixelShader.Set(m_blitTextureShader);

            RC.DeviceContext.VertexShader.Set(m_screenVertexShader);
            RC.DeviceContext.InputAssembler.InputLayout = m_inputLayout;

            var mapping = MyMapping.MapDiscard(m_quadBuffer.Buffer);
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

            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            RC.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_quadBuffer.Buffer, m_quadBuffer.Stride, 0));
            RC.DeviceContext.Draw(6, 0);
        }

        internal unsafe static void DrawAtmosphereTransmittance(uint ID)
        {
            var tex = MyAtmosphereRenderer.AtmosphereLUT[ID].TransmittanceLut;

            RC.DeviceContext.PixelShader.Set(m_blitTextureShader);
            RC.DeviceContext.PixelShader.SetShaderResource(0, tex.ShaderView);

            DrawQuad(256, 0, 256, 64);

            RC.DeviceContext.PixelShader.SetShaderResource(0, null);
        }

        internal static void DrawEnvProbeQuad(float x, float y, float w, float h, int i)
        {
            RC.DeviceContext.PixelShader.Set(m_blitTextureShader);
            
            RC.DeviceContext.PixelShader.SetShaderResource(0, MyEnvironmentProbe.Instance.cubemapPrefiltered.SubresourceSrv(i, 1));

            DrawQuad(x, y, w, h);
        }

        internal static void DrawCascades(MyShadowCascades cascades, int quadStartX, int quadStartY, int quadSize)
        {
            DrawCascadeArray(cascades.CascadeShadowmapArray, quadStartX, quadStartY, quadSize);
        }

        internal static void DrawCombinedCascades(int quadStartX, int quadStartY, int quadSize)
        {
            DrawCascadeArray(MyShadowCascades.CombineShadowmapArray, quadStartX, quadStartY, quadSize);
        }

        private static void DrawCascadeArray(RwTexId textureArray, int quadStartX, int quadStartY, int quadSize)
        {
            RC.DeviceContext.PixelShader.Set(m_blitTextureArrayShader);
            RC.DeviceContext.PixelShader.SetShaderResource(0, textureArray.ShaderView);

            var cb = MyCommon.GetMaterialCB(sizeof(uint));
            RC.DeviceContext.PixelShader.SetConstantBuffer(5, cb);

            for (uint cascadeIndex = 0; cascadeIndex < MyRender11.Settings.ShadowCascadeCount; cascadeIndex++)
            {
                float index = (float)cascadeIndex;
                var mapping = MyMapping.MapDiscard(cb);
                mapping.WriteAndPosition(ref index);
                mapping.Unmap();
                DrawQuad(quadStartX + (quadSize + quadStartX / 2) * cascadeIndex, quadStartY, quadSize, quadSize * MyRender11.ViewportResolution.Y / MyRender11.ViewportResolution.X);
            }
            RC.DeviceContext.PixelShader.SetShaderResource(0, null);
        }

        internal static void DrawParticles(MyBindableResource particleRenderTarget, int quadStartX, int quadStartY, int quadSize)
        {
            RC.DeviceContext.PixelShader.Set(m_blitTextureShader);
            RC.BindSRV(0, particleRenderTarget);
            RC.SetBS(MyRender11.BlendAlphaPremult);

            DrawQuad(quadStartX, quadStartY, quadSize, quadSize * MyRender11.ViewportResolution.Y / MyRender11.ViewportResolution.X);
            RC.SetBS(null);
            RC.DeviceContext.PixelShader.SetShaderResource(0, null);
        }

        internal static void DrawEnvProbe()
        {
            //MyGeometryRenderer.m_envProbe.cubemap

            DrawEnvProbeQuad(256 * 2, 256 * 1, 256, 256, 0);
            DrawEnvProbeQuad(256 * 0, 256 * 1, 256, 256, 1);
            DrawEnvProbeQuad(256 * 1, 256 * 0, 256, 256, 2);
            DrawEnvProbeQuad(256 * 1, 256 * 2, 256, 256, 3);
            DrawEnvProbeQuad(256 * 1, 256 * 1, 256, 256, 4);
            DrawEnvProbeQuad(256 * 3, 256 * 1, 256, 256, 5);
        }

        internal static void Draw(MyBindableResource renderTarget)
        {
            var context = RC.DeviceContext;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            context.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, renderTarget);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            
            //context.OutputMerger.SetTargets(null as DepthStencilView, MyRender.Backbuffer.RenderTarget);

            //context.PixelShader.SetShaderResources(0, MyRender.MainGbuffer.DepthGbufferViews);

            context.OutputMerger.BlendState = null;

            RC.SetVS(null);
            RC.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            RC.DeviceContext.InputAssembler.InputLayout = null;

            if(MyRender11.Settings.DisplayGbufferColor)
            {
                context.PixelShader.Set(m_baseColorShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            if (MyRender11.Settings.DisplayGbufferColorLinear)
            {
                context.PixelShader.Set(m_baseColorLinearShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferNormal)
            {
                context.PixelShader.Set(m_normalShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferGlossiness)
            {
                context.PixelShader.Set(m_glossinessShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferMetalness)
            {
                context.PixelShader.Set(m_metalnessShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferMaterialID)
            {
                context.PixelShader.Set(m_matIDShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayGbufferAO)
            {
                context.PixelShader.Set(m_aoShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayEmissive)
            {
                context.PixelShader.Set(m_emissiveShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if(MyRender11.Settings.DisplayAmbientDiffuse)
            {
                context.PixelShader.Set(m_ambientDiffuseShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if(MyRender11.Settings.DisplayAmbientSpecular)
            {
                context.PixelShader.Set(m_ambientSpecularShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if(MyRender11.Settings.DisplayEdgeMask)
            {
                context.PixelShader.Set(m_edgeDebugShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayShadowsWithDebug)
            {
                context.PixelShader.Set(m_shadowsDebugShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if (MyRender11.Settings.DisplayNDotL)
            {
                context.PixelShader.Set(m_NDotLShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            else if(MyRender11.Settings.DisplayStencil)
            {
                context.PixelShader.SetShaderResource(4, MyGBuffer.Main.DepthStencil.m_SRV_stencil);
                context.PixelShader.Set(m_stencilShader);
                MyScreenPass.DrawFullscreenQuad();
            }
            //DrawEnvProbe();
            //DrawAtmosphereTransmittance(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);
            //DrawAtmosphereInscatter(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);

            if (MyRender11.Settings.DrawCascadeTextures)
            {
                DrawCascades(MyRender11.DynamicShadows.ShadowCascades, 100, 100, 200);
                if (MyScene.SeparateGeometry)
                {
                    DrawCascades(MyRender11.StaticShadows.ShadowCascades, 100, 300, 200);
                    DrawCombinedCascades(100, 500, 200);
                }
            }

            if (MyRender11.Settings.DisplayIDs || MyRender11.Settings.DisplayAabbs)
            {
                DrawHierarchyDebug();
            }

            if (false)
            {
                var batch = MyLinesRenderer.CreateBatch();

                foreach (var light in MyLightRendering.VisiblePointlights)
                {
                    batch.AddSphereRing(new BoundingSphere(light.Position, 0.5f), Color.White, Matrix.Identity);
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

                //        BoundingBox bb = new BoundingBox(renderable.m_owner.Aabb.Min - MyEnvironment.CameraPosition,renderable.m_owner.Aabb.Max - MyEnvironment.CameraPosition);

                //        batch.AddBoundingBox(bb, new Color(LOD_COLORS[renderable.m_voxelLod]));


                //        if (renderable.m_lods != null && renderable.m_voxelLod != renderable.m_lods[0].RenderableProxies[0].ObjectData.CustomAlpha)
                //        {

                //        }
                //    }
                //}

                //batch.Commit();

                MyClipmap.DebugDrawClipmaps();
            }

            if (MyRender11.Settings.EnableVoxelMerging && MyRender11.Settings.DebugRenderMergedCells)
                MyClipmap.DebugDrawMergedCells();

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
            if(false)
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

       private static readonly Vector4[]  LOD_COLORS = 
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
            var worldToClip = MyEnvironment.ViewProjection;
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

                    if(r!= null)
                    {
                        position = r.Owner.WorldMatrix.Translation;
                        ID = r.Owner.ID;
                    }
                    else if(h != null) {
                        position = h.Owner.WorldMatrix.Translation;
                        ID = h.Owner.ID;
                    }
                    else {
                        continue;
                    }

                    var clipPosition = Vector3.Transform(position, ref worldToClip);
                    clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                    clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;

                    if (clipPosition.Z > 0 && clipPosition.Z < 1)
                    {
                        displayString.AppendFormat("{0}", ID);
                        MySpritesRenderer.DrawText(new Vector2(clipPosition.X, clipPosition.Y) * MyRender11.ViewportResolution,
                            displayString, Color.DarkCyan, 0.5f);
                    }

                    displayString.Clear();
                }
            }

            if(MyRender11.Settings.DisplayAabbs)
            {
                foreach(var actor in MyActorFactory.GetAll())
                {
                    var h = actor.GetGroupRoot();
                    var r = actor.GetRenderable();

                    if(h != null)
                    {
                        var bb = BoundingBoxD.CreateInvalid();

                        foreach (var child in h.m_children)
                        {
                            if(child.IsVisible)
                            { 
                                bb.Include(child.Aabb);
                            }
                        }

                        batch.AddBoundingBox((BoundingBox)bb, Color.Red);
                        MyPrimitivesRenderer.Draw6FacedConvexZ(bb.GetCorners().Select(x=>(Vector3)x).ToArray(), Color.Red, 0.1f);
                    }
                    else if(r!=null && actor.GetGroupLeaf() == null)
                    {
                        batch.AddBoundingBox((BoundingBox)r.Owner.Aabb, Color.Green);
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
            //    //m_proj = MyEnvironment.Projection;
            //    //m_vp = MyEnvironment.ViewProjection;
            //    //m_invvp = MyEnvironment.InvViewProjection;

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

                foreach(var r in MyComponentFactory<MyRenderableComponent>.GetAll())
                {
                    if(r.Owner.GetInstanceLod() != null)
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
                for(int i=0; i<4; i++)
                {
                    float levelCellSize = cellSize * (float)Math.Pow(2, i);
                    var position = MyEnvironment.CameraPosition;
                    //var position = Vector3.Zero;
                    position.Y = 0;
                    var positionG = position.Snap(levelCellSize * 2);

                    float radiusMin = radius[i];
                    float radiusMax = radius[i+1];

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
