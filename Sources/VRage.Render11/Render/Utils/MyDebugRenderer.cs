using System;
using System.Collections.Generic;
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

        static VertexShaderId m_screenVertexShader;
        static PixelShaderId m_blitTextureShader;
        static PixelShaderId m_blitTexture3DShader;
        static PixelShaderId m_blitTextureArrayShader;
        static InputLayoutId m_inputLayout;

        static VertexBufferId m_quadBuffer;

        internal static void Init()
        {
            //MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
			m_baseColorShader = MyShaders.CreatePs("debug.hlsl", "base_color");
            m_baseColorLinearShader = MyShaders.CreatePs("debug.hlsl", "base_color_linear");
            m_normalShader = MyShaders.CreatePs("debug.hlsl", "normal");
            m_glossinessShader = MyShaders.CreatePs("debug.hlsl", "glossiness");
            m_metalnessShader = MyShaders.CreatePs("debug.hlsl", "metalness");
            m_matIDShader = MyShaders.CreatePs("debug.hlsl", "mat_id");
            m_aoShader = MyShaders.CreatePs("debug.hlsl", "ambient_occlusion");
            m_emissiveShader = MyShaders.CreatePs("debug.hlsl", "emissive");
            m_ambientDiffuseShader = MyShaders.CreatePs("debug.hlsl", "debug_ambient_diffuse");
            m_ambientSpecularShader = MyShaders.CreatePs("debug.hlsl", "debug_ambient_specular");
            m_edgeDebugShader = MyShaders.CreatePs("debug.hlsl", "debug_edge");
			m_shadowsDebugShader = MyShaders.CreatePs("debug.hlsl", "cascades_shadow", MyRender11.ShaderCascadesNumberHeader());
            m_NDotLShader = MyShaders.CreatePs("debug.hlsl", "NDotL");


            m_screenVertexShader = MyShaders.CreateVs("debug.hlsl", "screenVertex");
            m_blitTextureShader = MyShaders.CreatePs("debug.hlsl", "blitTexture");
            m_blitTexture3DShader = MyShaders.CreatePs("debug.hlsl", "blitTexture3D");
            m_blitTextureArrayShader = MyShaders.CreatePs("debug.hlsl", "blitTextureArray");
            m_inputLayout = MyShaders.CreateIL(m_screenVertexShader.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION2, MyVertexInputComponentType.TEXCOORD0));

            m_quadBuffer = MyHwBuffers.CreateVertexBuffer(6, MyVertexFormatPosition2Texcoord.STRIDE, BindFlags.VertexBuffer, ResourceUsage.Dynamic);
        }

        internal static void DrawQuad(float x, float y, float w, float h)
        {
            //RC.Context.PixelShader.Set(m_blitTextureShader);

            RC.Context.VertexShader.Set(m_screenVertexShader);
            RC.Context.InputAssembler.InputLayout = m_inputLayout;

            var mapping = MyMapping.MapDiscard(m_quadBuffer.Buffer);

            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x, y), new Vector2(0, 0)));
            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y + h), new Vector2(1, 1)));
            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x, y + h), new Vector2(0, 1)));

            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x, y), new Vector2(0, 0)));
            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y), new Vector2(1, 0)));
            mapping.stream.Write(new MyVertexFormatPosition2Texcoord(new Vector2(x + w, y + h), new Vector2(1, 1)));

            mapping.Unmap();

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            RC.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_quadBuffer.Buffer, m_quadBuffer.Stride, 0));
            RC.Context.Draw(6, 0);
        }

        internal static void DrawEnvProbeQuad(float x, float y, float w, float h, int i)
        {
            RC.Context.PixelShader.Set(m_blitTextureShader);
            
            RC.Context.PixelShader.SetShaderResource(0, MyGeometryRenderer.m_envProbe.cubemapPrefiltered.SubresourceSrv(i, 1));

            DrawQuad(x, y, w, h);
        }

        internal static void DrawCascades()
        {
            RC.Context.PixelShader.Set(m_blitTextureArrayShader);
            RC.Context.PixelShader.SetShaderResource(0, MyShadows.m_cascadeShadowmapArray.ShaderView);

            var cb = MyCommon.GetMaterialCB(sizeof(uint));
            RC.Context.PixelShader.SetConstantBuffer(5, cb);

            for (uint i = 0; i < 4; i++)
            {
                var mapping = MyMapping.MapDiscard(cb);
                mapping.stream.Write((float)i);
                mapping.Unmap();
                DrawQuad(0, 256 * i, 410, 256);

            }
            RC.Context.PixelShader.SetShaderResource(0, null);
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

        internal static void Draw()
        {
            var context = RC.Context;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            context.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);

            RC.BindDepthRT(null, DepthStencilAccess.ReadWrite, MyRender11.Backbuffer);
            RC.BindGBufferForRead(0, MyGBuffer.Main);
            
            //context.OutputMerger.SetTargets(null as DepthStencilView, MyRender.Backbuffer.RenderTarget);

            //context.PixelShader.SetShaderResources(0, MyRender.MainGbuffer.DepthGbufferViews);

            context.OutputMerger.BlendState = null;

            RC.SetVS(null);
            RC.Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            RC.Context.InputAssembler.InputLayout = null;

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
            else if (MyRender11.Settings.DisplayAO)
            {
                context.PixelShader.SetShaderResource(12, MyScreenDependants.m_ambientOcclusion.m_SRV);

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
            //DrawEnvProbe();
            //DrawAtmosphereTransmittance(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);
            //DrawAtmosphereInscatter(MyAtmosphereRenderer.AtmosphereLUT.Keys.ToArray()[0]);
            //DrawCascades();

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


        //internal static void BeginQuadRendering()
        //{
        //    var context = MyRender.Context;

        //    context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        //    context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);
        //    context.OutputMerger.SetTargets(null as DepthStencilView, MyRender.Backbuffer.RenderTarget);
        //    context.OutputMerger.BlendState = null;
        //}

        //internal static void DrawQuad()
        //{

        //}

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
                        position = r.m_owner.WorldMatrix.Translation;
                        ID = r.m_owner.ID;
                    }
                    else if(h != null) {
                        position = h.m_owner.WorldMatrix.Translation;
                        ID = h.m_owner.ID;
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
                //foreach (var h in MyComponentFactory<MyHierarchyComponent>.GetAll())
                //{
                //    if (h.IsParent)
                //    {
                //        batch.AddBoundingBox(h.m_owner.Aabb, Color.Red);
                //    }
                //    else
                //    {
                //        batch.AddBoundingBox(h.m_owner.Aabb, Color.Orange);
                //    }
                //}

                foreach(var actor in MyActorFactory.GetAll())
                {
                    var h = actor.GetGroupRoot();
                    var r = actor.GetRenderable();

                    if(h != null)
                    {
                        var bb = BoundingBoxD.CreateInvalid();

                        foreach (var child in h.m_children)
                        {
                            if(child.m_visible)
                            { 
                                bb.Include(child.Aabb);
                            }
                        }

                        batch.AddBoundingBox((BoundingBox)bb, Color.Red);
                        MyPrimitivesRenderer.Draw6FacedConvex(bb.GetCorners().Select(x=>(Vector3)x).ToArray(), Color.Red, 0.1f);
                    }
                    else if(r!=null && actor.GetGroupLeaf() == null)
                    {
                        batch.AddBoundingBox((BoundingBox)r.m_owner.Aabb, Color.Green);
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
                    if(r.m_owner.GetComponent(MyActorComponentEnum.Instancing) != null)
                    {
                        batch.AddBoundingBox((BoundingBox)r.m_owner.Aabb, Color.Blue);
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
