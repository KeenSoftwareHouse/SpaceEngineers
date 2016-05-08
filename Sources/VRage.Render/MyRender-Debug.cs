#region Using

using System.Collections.Generic;

using VRage.Utils;
using System.Linq;

using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace VRageRender
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRageRender.Graphics;
    using VRageMath;

    internal static partial class MyRender
    {
        #region Debug render

        public static void RenderObjectsDebugDraw()
        {

            //MyDebugDraw.DrawAxis(Matrix.Identity, 10, 1, false);

            foreach (var renderObject in m_renderObjectsToDebugDraw)
            {
                renderObject.DebugDraw();
            }

           // return;

            DebugDrawLights();     
                
        }

        private static void DebugDrawLights()
        {
            return;
            //Debug draw lights
            foreach (MyRenderLight light in m_renderLightsForDraw)
            {
                //if (light.LightOn && light.Glare.Type == TransparentGeometry.MyLightGlare.GlareTypeEnum.Distant)
                {
                    if ((light.LightType & LightTypeEnum.PointLight) != 0)
                    {
                        MyDebugDraw.DrawSphereWireframe(MatrixD.CreateScale(light.Range) * MatrixD.CreateTranslation(light.PositionWithOffset), Color.Red, 1);
                        MyDebugDraw.DrawAxis(MatrixD.CreateWorld(light.Position, light.ReflectorDirection, light.ReflectorUp), 2, 1, false);
                        // MyDebugDraw.DrawText(light.PositionWithOffset, new System.Text.StringBuilder(light.ID.ToString()), Color.White, 1);
                    }
                    //if ((light.LightType & LightTypeEnum.Hemisphere) != 0)
                    //{
                    //    Matrix rotationHotfix = Matrix.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver2);
                    //    Matrix world = Matrix.CreateScale(light.Range) * rotationHotfix * Matrix.CreateWorld(light.Position, light.ReflectorDirection, light.ReflectorUp);
                    //    MyDebugDraw.DrawHemisphereWireframe(world, new Vector3(1, 0, 0), 1);
                    //}
                    if ((light.LightType & LightTypeEnum.Spotlight) != 0)
                    {
                        // Uncomment to show sphere for spot light
                        //MyDebugDraw.DrawSphereWireframe(Matrix.CreateScale(light.ReflectorRange) * Matrix.CreateTranslation(light.Position), new Vector3(color.X, color.Y, color.Z), 0.25f);

                        //MyDebugDraw.DrawCapsule(light.Position, light.Position + light.ReflectorDirection * light.ReflectorRange, 1, new Color(color.X, color.Y, color.Z), false);

                        //float reflectorConeAngle = (float)System.Math.Acos(1 - light.ReflectorConeMaxAngleCos);
                        //float reflectorRadius = (float)System.Math.Tan(reflectorConeAngle) * light.ReflectorRange;
                        //MyDebugDraw.DrawCylinder(light.Position, light.Position + light.ReflectorDirection * light.ReflectorRange, 0, reflectorRadius, new Color(color.X, color.Y, color.Z), false);

//                        MyDebugDraw.DrawText(light.Position, new System.Text.StringBuilder(light.ShadowMapIndex.ToString() + " (" + (light.SpotQuery != null ? light.QueryPixels.ToString() : "") + ")" ), Color.Yellow, 0.8f, false);

                        MyDebugDraw.DrawText(light.Position, new System.Text.StringBuilder(Vector3D.Distance(MyRenderCamera.Position, light.Position).ToString()), Color.Yellow, 0.8f, false);

                        MyStateObjects.WireframeClockwiseRasterizerState.Apply();
                        DepthStencilState.None.Apply();

                        MyDebugDraw.DrawModel(MyDebugDraw.ModelCone, light.SpotWorld, Color.White, false);
                    }

                    //just glare
                    if (light.LightType == LightTypeEnum.None)
                    {
                        MyDebugDraw.DrawSphereWireframe(MatrixD.CreateScale(light.Range) * MatrixD.CreateTranslation(light.PositionWithOffset), Color.Red, 1);
                    }
                }
            }
        }
              
        internal static void DrawDebugEnvironmentRenderTargets()
        {
            BlendState.Opaque.Apply();

            // int cubeSize = GetRenderTargetCube(MyRenderTargets.EnvironmentCube).GetLevelDescription(0).Width;
            int cubeSize = 128;

            Vector2I delta = new Vector2I((int)(MyRenderCamera.Viewport.Height * 0.07f), (int)(MyRenderCamera.Viewport.Height * 0.015f));
            Vector2I size = new Vector2I(cubeSize, cubeSize);

            int heightOffset = size.Y + delta.Y;

            MyRender.BeginSpriteBatch(BlendState.Opaque);

            for (int i = 0; i < 6; i++)
            {
                //var back = MyTextureManager.GetTexture<MyTextureCube>("Textures\\BackgroundCube\\Final\\TestCube", null, AppCode.Game.Managers.LoadingMode.Immediate);
                Vector2 vz = Vector2.Zero;
                Rectangle? rf = null;
                RectangleF dest = new RectangleF(delta.X + size.X * i, delta.Y, size.X, size.Y);
                MyRender.DrawSprite(GetRenderTargetCube(MyRenderTargets.EnvironmentCube), (CubeMapFace)i, ref dest, false, ref rf, Color.White, Vector2.UnitX, ref vz, SpriteEffects.None, 0);

                dest = new RectangleF(delta.X + size.X * i, delta.Y + heightOffset, size.X, size.Y);
                MyRender.DrawSprite(GetRenderTargetCube(MyRenderTargets.EnvironmentCubeAux), (CubeMapFace)i, ref dest, false, ref rf, Color.White, Vector2.UnitX, ref vz, SpriteEffects.None, 0);

                dest = new RectangleF(delta.X + size.X * i, delta.Y + heightOffset * 2, size.X, size.Y);
                MyRender.DrawSprite(GetRenderTargetCube(MyRenderTargets.AmbientCube), (CubeMapFace)i, ref dest, false, ref rf, Color.White, Vector2.UnitX, ref vz, SpriteEffects.None, 0);

                dest = new RectangleF(delta.X + size.X * i, delta.Y + heightOffset * 3, size.X, size.Y);
                MyRender.DrawSprite(GetRenderTargetCube(MyRenderTargets.AmbientCubeAux), (CubeMapFace)i, ref dest, false, ref rf, Color.White, Vector2.UnitX, ref vz, SpriteEffects.None, 0);
            }

            MyRender.EndSpriteBatch();
        }

        internal static void DrawDebugBlendedRenderTargets()
        {
            //  All RT should be of same size, so for size we can use any of them we just pick up depthRT
            float renderTargetAspectRatio = (float)MyRenderCamera.Viewport.Width / (float)MyRenderCamera.Viewport.Height;

            float normalizedSizeY = 0.40f;
            //float normalizedSizeY = MyCamera.Viewport.Height / 1920f;
            float normalizedSizeX = normalizedSizeY * renderTargetAspectRatio;

            Vector2I delta = new Vector2I((int)(MyRenderCamera.Viewport.Height * 0.015f), (int)(MyRenderCamera.Viewport.Height * 0.015f));
            Vector2I size = new Vector2I((int)(MyRenderCamera.Viewport.Height * normalizedSizeX), (int)(MyRenderCamera.Viewport.Height * normalizedSizeY));

            BeginSpriteBatch(BlendState.Opaque);
            DrawSprite(MyRender.GetRenderTarget(MyRenderTargets.Diffuse), new Rectangle(delta.X, delta.Y, size.X, size.Y), Color.White);
            DrawSprite(MyRender.GetRenderTarget(MyRenderTargets.Normals), new Rectangle(delta.X + size.X + delta.X, delta.Y, size.X, size.Y), Color.White);
            EndSpriteBatch();
        }

        internal static void DrawDebugHDRRenderTargets()
        {
            BlendState.Opaque.Apply();

            //  All RT should be of same size, so for size we can use any of them we just pick up depthRT
            float renderTargetAspectRatio = (float)MyRenderCamera.Viewport.Width / (float)MyRenderCamera.Viewport.Height;

            float normalizedSizeY = 0.40f;
            float normalizedSizeX = normalizedSizeY * renderTargetAspectRatio;

            Vector2I delta = new Vector2I((int)(MyRenderCamera.Viewport.Height * 0.015f), (int)(MyRenderCamera.Viewport.Height * 0.015f));
            Vector2I size = new Vector2I((int)(MyRenderCamera.Viewport.Height * normalizedSizeX), (int)(MyRenderCamera.Viewport.Height * normalizedSizeY));

            //MyGuiManager.DrawSpriteFast(MyRender.GetRenderTarget(MyRenderTargets.Diffuse), delta.X, delta.Y, size.X, size.Y, Color.White);
            //MyGuiManager.DrawSpriteFast(MyRender.GetRenderTarget(MyRenderTargets.Normals), delta.X + size.X + delta.X, delta.Y, size.X, size.Y, Color.White);
        }
            
        internal static void DrawDebug()
        {
            GetRenderProfiler().StartProfilingBlock("Draw entity debug");

            RasterizerState.CullNone.Apply();
            DepthStencilState.Default.Apply();
            //DepthStencilState.None.Apply();
            BlendState.Opaque.Apply();

            RenderObjectsDebugDraw();

            GetShadowRenderer().DebugDraw();

           /* if (ShowEnhancedRenderStatsEnabled)
                ShowEnhancedRenderStats();
            */ 
            //if (ShowResourcesStatsEnabled)
              //  MySandboxGame.GraphicsDeviceManager.DebugDrawStatistics();
            //if (ShowTexturesStatsEnabled)
              //  MyTextureManager.DebugDrawStatistics();

            GetRenderProfiler().EndProfilingBlock();
        }

        class MyTypeStats
        {
            public int Count;
            public int Tris;
            public object UserData;
            public string UserString;
        }

        static Dictionary<string, MyTypeStats> m_prefabStats = new Dictionary<string, MyTypeStats>();
        static Dictionary<string, MyTypeStats> m_typesStats = new Dictionary<string, MyTypeStats>();

        public static void ClearEnhancedStats()
        {
            m_typesStats.Clear();
            m_prefabStats.Clear();
        }
                               /*
        static private void ShowEnhancedRenderStats()
        {
            ClearEnhancedStats();

            //m_renderObjectListForDraw.Clear();
            //m_shadowPrunningStructure.OverlapAllFrustum(ref m_cameraFrustum, m_renderObjectListForDraw);
            //m_cameraFrustumBox = new BoundingBox(new Vector3(float.NegativeInfinity), new Vector3(float.PositiveInfinity));
            //m_shadowPrunningStructure.OverlapAllBoundingBox(ref m_cameraFrustumBox, m_renderObjectListForDraw);

            foreach (MyRenderObject ro in m_renderObjectListForDraw)
            {
                string ts = ro.Entity.GetType().Name.ToString();
                if (!m_typesStats.ContainsKey(ts))
                    m_typesStats.Add(ts, new MyTypeStats());
                m_typesStats[ts].Count++;
            }
            

            float topOffset = 100;
            Vector2 offset = new Vector2(100, topOffset);
            MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Detailed render statistics"), Color.Yellow, 2);

            float scale = 0.7f;
            offset.Y += 50;
            MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Prepared entities for draw:"), Color.Yellow, scale);
            offset.Y += 30;
            foreach (var pair in SortByCount(m_typesStats))
            {
                MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.Count.ToString() + "x"), Color.Yellow, scale);
                offset.Y += 20;
            }

            offset = new Vector2(400, topOffset + 50);
            scale = 0.6f;
            MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Prepared prefabs for draw:"), Color.Yellow, 0.7f);
            offset.Y += 30;
            foreach (var pair in SortByCount(m_prefabStats))
            {
                MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.Count.ToString() + "x"), Color.Yellow, scale);
                offset.Y += 14;
            }


            ClearEnhancedStats();
            foreach (MyRenderObject ro in m_debugRenderObjectListForDrawLOD0)
            {
                string pt = ro.Entity.GetType().Name.ToString();
                if (!m_prefabStats.ContainsKey(pt))
                    m_prefabStats.Add(pt, new MyTypeStats());

                m_prefabStats[pt].Count++;
                m_prefabStats[pt].Tris += ro.Entity.ModelLod0.GetTrianglesCount();
            }

            offset = new Vector2(800, topOffset + 50);
            scale = 0.6f;
            MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Prepared entities for LOD0:"), Color.Yellow, 0.7f);
            offset.Y += 30;
            foreach (var pair in SortByCount(m_prefabStats))
            {
                MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.Count.ToString() + "x [" + pair.Value.Tris.ToString() + " tris]"), Color.Yellow, scale);
                offset.Y += 14;
            }

            ClearEnhancedStats();

                                * offset = new Vector2(1200, topOffset + 50);
            scale = 0.6f;
            MyDebugDraw.DrawText(offset, new System.Text.StringBuilder("Prepared entities for LOD1:"), Color.Yellow, 0.7f);
            offset.Y += 30;
            foreach (var pair in SortByCount(m_prefabStats))
            {
                MyDebugDraw.DrawText(offset, new System.Text.StringBuilder(pair.Key + ": " + pair.Value.Count.ToString() + "x [" + pair.Value.Tris.ToString() + " tris]"), Color.Yellow, scale);
                offset.Y += 14;
            }

        }

        static public void DumpAllEntities()
        {
            m_typesStats.Clear();

            MySandboxGame.Log.WriteLine("Dump of all loaded prefabs");
            MySandboxGame.Log.WriteLine("");

            foreach (MyEntity entity in MyEntities.GetEntities())
            {
                MyPrefabContainer container = entity as MyPrefabContainer;

                if (container != null)
                {
                    foreach (var prefab in container.GetPrefabs())
                    {
                        string ts = prefab.ModelLod0.AssetName.ToString();
                        if (!m_typesStats.ContainsKey(ts))
                            m_typesStats.Add(ts, new MyTypeStats());
                        m_typesStats[ts].Count++;
                        m_typesStats[ts].Tris += prefab.ModelLod0.Triangles.Length;
                        m_typesStats[ts].UserData = prefab;
                        m_typesStats[ts].UserString += prefab.EntityId.Value.NumericValue + " ";
                    }
                }
            }


            foreach (var po in SortByCount(m_typesStats))
            {
                var prefab = ((AppCode.Game.Prefabs.MyPrefabBase)po.Value.UserData);
                int verticesSize = prefab.ModelLod0.GetVBSize + prefab.ModelLod0.GetIBSize;

                MySandboxGame.Log.WriteLine(po.Key + "," + po.Value.Count + "," + po.Value.Tris + "," + verticesSize + "," + prefab.ModelLod0.GetBVHSize() + "," + po.Value.UserString);
            }
        }
             */
        static public List<KeyValuePair<string, int>> SortByValue(Dictionary<string, int> stats)
        {
            List<KeyValuePair<string, int>> statsList = stats.ToList();
            statsList.Sort(
                delegate(KeyValuePair<string, int> firstPair,
                KeyValuePair<string, int> nextPair)
                {
                    return nextPair.Value.CompareTo(firstPair.Value);
                }
            );

            return statsList;
        }

        static List<KeyValuePair<string, MyTypeStats>> SortByCount(Dictionary<string, MyTypeStats> stats)
        {
            List<KeyValuePair<string, MyTypeStats>> statsList = stats.ToList();
            statsList.Sort(
                delegate(KeyValuePair<string, MyTypeStats> firstPair,
                KeyValuePair<string, MyTypeStats> nextPair)
                {
                    return nextPair.Value.Count.CompareTo(firstPair.Value.Count);
                }
            );

            return statsList;
        }


        #endregion
    }
}