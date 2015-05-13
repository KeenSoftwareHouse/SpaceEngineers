using System.Collections.Generic;
using VRageMath;
using VRage.Utils;
using System;
using VRageRender.Graphics;
using VRageRender.Textures;
using VRageRender.Utils;

//  Decals manager. It holds lists of decal triangles, draws them, removes decals after explosion, etc.
//  I can't use texture atlas for holding all decal textures, because I need clamping, and if using atlas, 
//  texture sampler will get neighbour textures too.
//
//  We have two decal buffers. One for model instances, the other for voxels. Each one manages separate 
//  triangleVertexes buffers. One triangleVertexes buffer for one model/texture or voxel render cell and texture.

namespace VRageRender
{
    //  IMPORTANT: This is class, not struct!!!
    //  Reason is, we need to be able to overwrite values inside even if stored in queue, stack or list. If it
    //  was struct, we won't be able to modify it without enque/deque... and that's bad.
    class MyDecalTriangle
    {
        public Vector3 Position0;
        public Vector3 Position1;
        public Vector3 Position2;
        public Vector2 TexCoord0;
        public Vector2 TexCoord1;
        public Vector2 TexCoord2;
        public Vector4 Color0;
        public Vector4 Color1;
        public Vector4 Color2;
        public Vector3 Normal0;
        public Vector3 Normal1;
        public Vector3 Normal2;
        /*
        public Vector3 Tangent0;
        public Vector3 Tangent1;
        public Vector3 Tangent2;
         */
        public bool Draw;
        public int RemainingTrianglesOfThisDecal;       //  This number tells us how many triangles of one decal are in the buffer after this triangleVertexes. If zero, this is the last triangleVertexes of a decal.
        public float RandomOffset;

        public MyRenderLight Light;
        public float Emissivity;
        public Vector3D Position;

        public void Start(float lightSize)
        {  /*
            if (lightSize > 0)
            {
                Light = MyLights.AddLight();
                Light.Color = Vector4.One;
                Light.Start(MyLight.LightTypeEnum.PointLight, 1.0f);
                Light.LightOn = lightSize > 0;
                Light.Intensity = 10;
            }*/
        }

        public void Close()
        {      /*
            if (Light != null)
            {
                MyLights.RemoveLight(Light);
                Light = null;
            }    */
        }
    }

    class MyDecals : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.Decals;
        }

        static MyVertexFormatDecal[] m_vertices;
        static MyTexture2D[] m_texturesDiffuse;
        static MyTexture2D[] m_texturesNormalMap;
        
        //static List<MyTriangle_Vertex_Normals> m_neighbourTriangles;
        static MyDecalsForRenderObjects m_decalsForModels;
        static MyDecalsForVoxels m_decalsForVoxels;

        static MyDecals()
        {
            MyRender.RegisterRenderModule(MyRenderModuleEnum.Decals, "Decals", Draw, MyRenderStage.LODDrawEnd);
        }

        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyDecals.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyDecals::LoadContent");

            //  Reason is that if count of neighbour triangles is more then decal triangles buffer, we won't be able to add any triangleVertexes to the buffer.
            MyDebug.AssertRelease(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER > MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES);

            MyDebug.AssertRelease(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_LARGE <= MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);
            MyDebug.AssertRelease(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER_SMALL <= MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER);

            //  Reason is that if count of neighbour triangles is more then decal triangles buffer, we won't be able to add any triangleVertexes to the buffer.
            MyDebug.AssertRelease(MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER > MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES);
            
            //  Reason is that if count of neighbour triangles is more then this fade limit, we won't be able to add decals that lay on more triangles, because buffer will be never released to us.
            MyDebug.AssertRelease(MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES < (MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER * MyDecalsConstants.TEXTURE_LARGE_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT));

            //  Reason is that if count of neighbour triangles is more then this fade limit, we won't be able to add decals that lay on more triangles, because buffer will be never released to us.
            MyDebug.AssertRelease(MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES < (MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER * MyDecalsConstants.TEXTURE_SMALL_FADING_OUT_MINIMAL_TRIANGLE_COUNT_PERCENT));

            //  Large must be bigger than small
            MyDebug.AssertRelease(MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES > MyDecalsConstants.TEXTURE_SMALL_MAX_NEIGHBOUR_TRIANGLES);

            m_vertices = new MyVertexFormatDecal[MyDecalsConstants.MAX_DECAL_TRIANGLES_IN_BUFFER * MyDecalsConstants.VERTEXES_PER_DECAL];
           // m_neighbourTriangles = new List<MyTriangle_Vertex_Normals>(MyDecalsConstants.TEXTURE_LARGE_MAX_NEIGHBOUR_TRIANGLES);
            
            m_decalsForModels = new MyDecalsForRenderObjects(MyDecalsConstants.DECAL_BUFFERS_COUNT);
            m_decalsForVoxels = new MyDecalsForVoxels(MyDecalsConstants.DECAL_BUFFERS_COUNT);

            //  Decal textures
            int texturesCount = MyEnumsToStrings.Decals.Length;
            m_texturesDiffuse = new MyTexture2D[texturesCount];
            m_texturesNormalMap = new MyTexture2D[texturesCount];
            
            for (int i = 0; i < texturesCount; i++)
            {
                //MyRender.Log.WriteLine("textureManager " + i.ToString() + "Textures\\Decals\\" + MyEnumsToStrings.Decals[i] + "_Diffuse", SysUtils.LoggingOptions.MISC_RENDER_ASSETS);
                m_texturesDiffuse[i] = MyTextureManager.GetTexture<MyTexture2D>("Textures\\Decals\\" + MyEnumsToStrings.Decals[i] + "_Diffuse.dds", "", null, LoadingMode.Immediate);
                //MyRender.Log.WriteLine("textureManager " + i.ToString() + "Textures\\Decals\\" + MyEnumsToStrings.Decals[i] + "_NormalMap", SysUtils.LoggingOptions.MISC_RENDER_ASSETS);
                m_texturesNormalMap[i] = MyTextureManager.GetTexture<MyTexture2D>("Textures\\Decals\\" + MyEnumsToStrings.Decals[i] + "_NormalMap.dds", "", CheckTexture, LoadingMode.Immediate);
                
                MyUtilsRender9.AssertTexture(m_texturesNormalMap[i]);
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyDecals.LoadContent() - END");
        }

        /// <summary>
        /// Checks the normal map.
        /// </summary>
        /// <param name="texture">The texture.</param>
        private static void CheckTexture(MyTexture texture)
        {
            MyUtilsRender9.AssertTexture((MyTexture2D)texture);

            texture.TextureLoaded -= CheckTexture;
        }

        /// <summary>
        /// Unloads the content.
        /// </summary>
        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("MyDecals.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            if (m_decalsForModels != null)
                m_decalsForModels.Clear();

            if (m_decalsForVoxels != null)
                m_decalsForVoxels.Clear();

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyDecals.UnloadContent - END");
        }

        
        //  Add decal and all surounding triangles for model intersection
        internal static void AddDecal(MyRenderObject renderObject, ref MyDecalTriangle_Data triangle, int trianglesToAdd, MyDecalTexturesEnum decalTexture, 
            Vector3D position, float lightSize, float emissivity)
        {
            IMyDecalsBuffer decalsBuffer = null;
            if (renderObject is MyRenderVoxelCell)
                    //  If we get null, buffer is full so no new decals can't be placed
                decalsBuffer = m_decalsForVoxels.GetTrianglesBuffer(renderObject as MyRenderVoxelCell, decalTexture);
            else
                if (renderObject is MyRenderTransformObject)
                    decalsBuffer = m_decalsForModels.GetTrianglesBuffer(renderObject, decalTexture);

            if (renderObject is MyManualCullableRenderObject)
            {
                decalsBuffer = m_decalsForModels.GetTrianglesBuffer(renderObject, decalTexture);
            }
                

            //  If we get null, buffer is full so no new decals can't be placed
            if (decalsBuffer == null) return;

            if (decalsBuffer.CanAddTriangles(trianglesToAdd))
            {
                Vector3 normalSum = Vector3.Zero;

                decalsBuffer.Add(triangle, trianglesToAdd, position, lightSize, emissivity);
            }
        }


        //  Blends-out triangles affected by explosion (radius + some safe delta). Triangles there have zero alpha are flaged to not-draw at all.
        public static void HideTrianglesAfterExplosion(MyRenderVoxelCell voxelCell, ref BoundingSphere explosionSphere)
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyDecals::HideTrianglesAfterExplosion");
            m_decalsForVoxels.HideTrianglesAfterExplosion(voxelCell.ID, ref explosionSphere);
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        /// <summary>
        /// Removes decals from the specified entity (NOT voxel map).
        /// E.g. when the entity is destroyed (destructible prefab).
        /// </summary>
        /// <param name="renderObject">The entity from which we want to remove decals. NOT MyVoxelMap!</param>
        public static void RemoveModelDecals(MyRenderTransformObject renderObject)
        {
            m_decalsForModels.ReturnTrianglesBuffer(renderObject);
        }    

        public static float GetMaxDistanceForDrawingDecals()
        {
            return MyDecalsConstants.MAX_DISTANCE_FOR_DRAWING_DECALS;
        }

        public static void Draw()
        {
            //if (m_currentLodDrawPass == MyLodTypeEnum.LOD0)
            //{
            //    MyStateObjects.DepthStencil_TestFarObject_DepthReadOnly.Apply();
            //    MyStateObjects.Dynamic_Decals_BlendState.Apply();
            //    MyStateObjects.BiasedRasterizer_Decals.Apply();

            //    Effects.MyEffectDecals effect = (Effects.MyEffectDecals)MyRender.GetEffect(MyEffects.Decals);

            //    //  Draw voxel decals
            //    m_decalsForVoxels.Draw(m_vertices, effect, m_texturesDiffuse, m_texturesNormalMap);

            //    //  Draw model decals
            //    m_decalsForModels.Draw(m_vertices, effect, m_texturesDiffuse, m_texturesNormalMap);
            //} 
        }

        public static float UpdateDecalEmissivity(MyDecalTriangle decalTriangle, float alpha, MyRenderObject renderObject)
        {
            float emisivity = 0;
            if (decalTriangle.Emissivity > 0)
            {
                //emisivity = (float)(Math.Sin(decalTriangle.RandomOffset + decalTriangle.RandomOffset * MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000.0f)) * 0.4f + 0.7f;

                // 2 seconds default, more emissive lit longer
                float stableLength = 2000 * decalTriangle.Emissivity;
                if ((MyRender.RenderTimeInMS - decalTriangle.RandomOffset) < stableLength)
                    emisivity = 1;
                else
                {
                    emisivity = (float)(500 - (MyRender.RenderTimeInMS - stableLength - decalTriangle.RandomOffset)) / 500.0f;
                    if (emisivity < 0)
                        emisivity = 0;
                }

                emisivity *= decalTriangle.Emissivity;

                if (emisivity > 0)
                {
                    Color color = MyDecalsConstants.PROJECTILE_DECAL_COLOR;

                    Vector3D position;
                    MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                    if (transformObject != null)
                    {
                        position = Vector3D.Transform(decalTriangle.Position, transformObject.WorldMatrix);
                    }
                    else
                        if (renderObject is MyManualCullableRenderObject)
                        {
                            position = Vector3D.Transform(decalTriangle.Position, (renderObject as MyManualCullableRenderObject).WorldMatrix);
                        }
                        else
                        {
                            position = decalTriangle.Position;
                        }

                    MyTransparentGeometry.AddPointBillboard(
                            "DecalGlare",
                            color * alpha,
                           (Vector3)position,
                           1.5f * emisivity,
                           0);                    

                    if (decalTriangle.Light != null)
                    {
                        decalTriangle.Light.Color = color;
                        decalTriangle.Light.SetPosition(position);

                        float range = Math.Max(3 * emisivity * alpha, 0.1f);
                        decalTriangle.Light.Range = range;
                    }
                }
            }

            return emisivity;
        }
    }
}
