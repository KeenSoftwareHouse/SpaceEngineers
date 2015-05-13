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
using BoundingSphere = VRageMath.BoundingSphere;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRage.Render11.Shaders;

namespace VRageRender
{
    class MyDebugRenderer
    {
        static MyShader m_vertexShader = MyShaderCache.Create("primitive.hlsl", "vs", MyShaderProfile.VS_5_0);
        static MyShader m_baseColorShader = MyShaderCache.Create("debug.hlsl", "base_color", MyShaderProfile.PS_5_0);
        static MyShader m_normalShader = MyShaderCache.Create("debug.hlsl", "normal", MyShaderProfile.PS_5_0);
        static MyShader m_glossinessShader = MyShaderCache.Create("debug.hlsl", "glossiness", MyShaderProfile.PS_5_0);
        static MyShader m_metalnessShader = MyShaderCache.Create("debug.hlsl", "metalness", MyShaderProfile.PS_5_0);

        internal static void Draw()
        {
            var context = MyRender.Context;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);
            context.PixelShader.SetConstantBuffer(MyCommon.FrameSlot, MyCommon.FrameConstants.Buffer);
            context.OutputMerger.SetTargets(null as DepthStencilView, MyRender.Backbuffer.RenderTarget);
            context.PixelShader.SetShaderResources(0, MyRender.MainGbuffer.DepthGbufferViews);
            context.OutputMerger.BlendState = null;
            context.VertexShader.Set(MyCommon.FullscreenShader.VertexShader);

            if(MyRender.Settings.EnableDebugGbufferColor)
            {
                context.PixelShader.Set(m_baseColorShader.PixelShader);
                context.Draw(3, 0);
            }
            else if (MyRender.Settings.EnableDebugGbufferNormal)
            {
                context.PixelShader.Set(m_normalShader.PixelShader);
                context.Draw(3, 0);
            }
            else if (MyRender.Settings.EnableDebugGbufferGlossiness)
            {
                context.PixelShader.Set(m_glossinessShader.PixelShader);
                context.Draw(3, 0);
            }
            else if (MyRender.Settings.EnableDebugGbufferMetalness)
            {
                context.PixelShader.Set(m_metalnessShader.PixelShader);
                context.Draw(3, 0);
            }
        }
    }


    partial class MyRender
    {
        internal static void DrawSceneDebug()
        {

            //ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            //ImmediateContext.Rasterizer.SetViewport(0, 0, m_settings.BackBufferWidth, m_settings.BackBufferHeight);
            //ImmediateContext.OutputMerger.ResetTargets();
            //ShaderResourceView[] shaderViews = m_gbuffer.ShaderViews;
            //ImmediateContext.OutputMerger.SetTargets(Backbuffer.RenderTarget);
            //ImmediateContext.PixelShader.SetConstantBuffer(0, m_frameCB.Buffer);
            //ImmediateContext.PixelShader.SetConstantBuffer(2, m_shadowsCB.Buffer);
            //ImmediateContext.PixelShader.SetShaderResources(0, shaderViews);

            //if (Settings.ShowGbufferDepth)
            //{
            //    ImmediateContext.PixelShader.Set(m_debugGbufferDepth.GetShader());
            //    DrawFullscreen();
            //}
            //else if (Settings.ShowGbufferAlbedo)
            //{
            //    ImmediateContext.PixelShader.Set(m_debugGbufferAlbedo.GetShader());
            //    DrawFullscreen();
            //}
            //else if (Settings.ShowGbufferNormals)
            //{
            //    ImmediateContext.PixelShader.Set(m_debugGbufferNormals.GetShader());
            //    DrawFullscreen();
            //}
            //else if (Settings.ShowCascadesRange)
            //{
            //    ImmediateContext.PixelShader.Set(m_debugContainingCascade.GetShader());
            //    DrawFullscreen();
            //}
            //if (Settings.ShowCascadesDepth)
            //{
            //    /*
            //    SetupSpritePipeline();
            //    ImmediateContext.PixelShader.Set(m_debugCascadeDepth.GetShader());
            //    ImmediateContext.PixelShader.SetConstantBuffer(3, m_debugCB.Buffer);
            //    ImmediateContext.PixelShader.SetShaderResource(7, m_cascadesTexture.ShaderView);

            //    const int horizontalCutoff = (MyRenderConstants.CASCADES_NUM + 1) / 2;
            //    var size = Math.Min(ViewportResolution.X / horizontalCutoff, ViewportResolution.Y / 2) - 3;

            //    for (int i = 0; i < MyRenderConstants.CASCADES_NUM; i++)
            //    {
            //        var stream = MapCB(m_debugCB);
            //        stream.Write(i);
            //        UnmapCB(m_debugCB);

            //        var gridX = i % horizontalCutoff;
            //        var gridY = i / horizontalCutoff;

            //        Vector2 csScale;
            //        Vector2 csOffset;
            //        CalculateSpriteClipspace(new RectangleF(new Vector2I(gridX * size + gridX, gridY * size + gridY), new Vector2I(size, size)), ViewportResolution, out csOffset, out csScale);
                    
            //        DrawSprite(null, csOffset, csScale, Vector2.Zero, Vector2.One);    
            //    }
            //     * */
            //}
            if (false)
            {
                /*
                var linesBatch = CreateLinesBatch();

                int objectsNum = MyRenderObjectPool.Size();
                for (int i = 0; i < objectsNum; i++)
                {
                    var bb = MyRenderObjectPool.m_cullinfos[i].worldAABB;

                    var v0 = bb.Center - bb.HalfExtents;
                    var v1 = v0 + new Vector3(bb.HalfExtents.X *2, 0, 0);
                    var v2 = v0 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v3 = v0 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var v4 = v0 + new Vector3(0, 0, bb.HalfExtents.Z * 2);
                    var v5 = v4 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
                    var v6 = v4 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v7 = v4 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var color = new Byte4(255, 255, 255, 0);

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                }

                linesBatch.Commit();
                 * */
                /*
                var linesBatch = CreateLinesBatch();

                int objectsNum = MyRenderObjectPool.Size();
                for (int i = 0; i < objectsNum; i++)
                {
                    var bb = MyRenderObjectPool.m_cullinfos[i].worldAABB;

                    var v0 = bb.Center - bb.HalfExtents;
                    var v1 = v0 + new Vector3(bb.HalfExtents.X *2, 0, 0);
                    var v2 = v0 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v3 = v0 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var v4 = v0 + new Vector3(0, 0, bb.HalfExtents.Z * 2);
                    var v5 = v4 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
                    var v6 = v4 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v7 = v4 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var color = new Byte4(255, 255, 255, 0);

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                }

                linesBatch.Commit();
                 * */
                /*
                var linesBatch = CreateLinesBatch();

                int objectsNum = MyRenderObjectPool.Size();
                for (int i = 0; i < objectsNum; i++)
                {
                    var bb = MyRenderObjectPool.m_cullinfos[i].worldAABB;

                    var v0 = bb.Center - bb.HalfExtents;
                    var v1 = v0 + new Vector3(bb.HalfExtents.X *2, 0, 0);
                    var v2 = v0 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v3 = v0 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var v4 = v0 + new Vector3(0, 0, bb.HalfExtents.Z * 2);
                    var v5 = v4 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
                    var v6 = v4 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
                    var v7 = v4 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

                    var color = new Byte4(255, 255, 255, 0);

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));

                    linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
                    linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
                }

                linesBatch.Commit();
                 * */
            }

            //if(true)
            //{
            //    /*
            //    var linesBatch = MyRender.CreateLinesBatch();

            //    var list = new List<MySpatialObject>();
            //    var bsphere = new BoundingSphere(Vector3.Zero, 25.0f);
            //    MySpatialManager.Tree.OverlapAllBoundingSphere<MySpatialObject>(ref bsphere, list);
            //    var inIDs = new Dictionary<uint, object>();
            //    foreach(var item in list)
            //    {
            //        inIDs[item.ID] = null;
            //    }

            //    foreach (var item in MySpatialManager.AllValues())
            //    {
            //        var bb = item.m_aabb;

            //        var v0 = bb.Center - bb.HalfExtents;
            //        var v1 = v0 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
            //        var v2 = v0 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
            //        var v3 = v0 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

            //        var v4 = v0 + new Vector3(0, 0, bb.HalfExtents.Z * 2);
            //        var v5 = v4 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
            //        var v6 = v4 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
            //        var v7 = v4 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

            //        var color = new Byte4(255, 0, 0, 0);
            //        if (inIDs.ContainsKey(item.ID))
            //        {
            //            color = new Byte4(0, 255, 0, 0);
            //        }

            //        linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v3, color));

            //        linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v7, color));

            //        linesBatch.Add(new MyVertexFormatPositionColor(v0, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v4, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v1, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v5, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v2, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v6, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v3, color));
            //        linesBatch.Add(new MyVertexFormatPositionColor(v7, color));
            //    }

            //    linesBatch.Commit();
            //     * */
            //}

            // draw culling structure
            if (false)
            {
                var linesBatch = MyLinesRenderer.CreateLinesBatch();

                List<MyRenderObjectProxy> list = new List<MyRenderObjectProxy>();
            
                foreach(var obj in MySceneObject.Collection)
                {
                    linesBatch.AddBoundingBox(obj.AABB, Color.AntiqueWhite);
                }

                

                linesBatch.Commit();
            }

            if(false)
            {
                var linesBatch = MyLinesRenderer.CreateLinesBatch();

                var worldToClip = m_viewMatrix * m_projectionMatrix;

                var displayString = new StringBuilder();

                var screenPositions = new List<Vector2>(10000);
                var display = new List<bool>(10000);
                foreach (var obj in MySceneObject.Collection)
                {
                    var position = obj.m_spatial.m_worldMatrix.Translation;
                    var clipPosition = Vector3.Transform(position, ref worldToClip);
                    clipPosition.X = clipPosition.X * 0.5f + 0.5f;
                    clipPosition.Y = clipPosition.Y * -0.5f + 0.5f;
                    screenPositions.Add(new Vector2(clipPosition.X, clipPosition.Y) * ViewportResolution);
                    display.Add(clipPosition.Z > 0 && clipPosition.Z < 1);
                }

                int i = 0;
                foreach (var obj in MySceneObject.Collection)
                {
                    if (display[i])
                    {
                        //displayString.AppendFormat("ID: {0}, proxy ID: {1}", obj.ID, obj.m_spatial.proxyID);
                      
                        var v = obj.m_spatial.m_aabb.Center;
                        var vv = obj.m_spatial.m_aabb.HalfExtents;

                        //var vv = obj.m_spatial.m_localAabb.HasValue ? obj.m_spatial.m_localAabb.Value.Center : Vector3.Zero;
                        displayString.AppendFormat("<{0}, {1}, {2}> <{3}, {4}, {5}>", v.X, v.Y, v.Z, vv.X, vv.Y, vv.Z);
                        if (obj.m_spatial.m_localAabb.HasValue)
                        {
                            v = obj.m_spatial.m_localAabb.Value.Center;
                            vv = obj.m_spatial.m_localAabb.Value.HalfExtents;
                            displayString.AppendFormat("local: <{0}, {1}, {2}> <{3}, {4}, {5}>", v.X, v.Y, v.Z, vv.X, vv.Y, vv.Z);
                        }

                        //if (v.X != 0 && v.Y != 0 && v.Z != 0)
                        {   
                            if (obj.m_spatial.m_parent != null)
                            {
                                MySpritesRenderer.DrawText(screenPositions[i], displayString, Color.DarkCyan, 0.5f);
                            }
                            else
                            {
                                MySpritesRenderer.DrawText(screenPositions[i] + new Vector2(0, -25.0f), displayString, Color.LightSkyBlue, 1);
                            }
                        }
                        
                    }
                  
                    if (obj.m_spatial.m_parent != null)
                    {
                        linesBatch.Add(new MyVertexFormatPositionColor(obj.m_spatial.m_parent.m_worldMatrix.Translation, new Byte4(0, 0, 128, 25)));
                        linesBatch.Add(new MyVertexFormatPositionColor(obj.m_spatial.m_worldMatrix.Translation, new Byte4(0, 0, 128, 25)));
                    }

                    displayString.Clear();

                    i++;
                }

                linesBatch.Commit();
            }

            //UpdateDepthBias();

            //ImmediateContext.ClearState();
        }
    }
}
