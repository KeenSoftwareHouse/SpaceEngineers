using System.Collections.Generic;
using VRage.Generics;
using VRage.Utils;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Utils;



//  This class is responsible for holding list of dynamic lights, adding, removing and finally drawing on voxels or other models.

namespace VRageRender.Lights
{
    static class MyLights
    {
        static MySortLightsByDistanceComparer m_sortLightsComparer = new MySortLightsByDistanceComparer();

        static List<MyRenderLight> m_renderLights = new List<MyRenderLight>();

        //  Used to sort lights by their distance to influence bounding sphere
        class MySortLightsByDistanceComparer : IComparer<MyRenderLight>
        {
            public BoundingSphereD BoundingSphere;

            public int Compare(MyRenderLight x, MyRenderLight y)
            {
                double xDist, yDist;
                Vector3D xPos = x.Position;
                Vector3D yPos = y.Position;
                Vector3D.Distance(ref BoundingSphere.Center, ref xPos, out xDist);
                Vector3D.Distance(ref BoundingSphere.Center, ref yPos, out yDist);
                return xDist.CompareTo(yDist);
            }
        }

        static MyLights()
        {
            MyRender.RegisterRenderModule(MyRenderModuleEnum.Lights, "Lights", DebugDraw, MyRenderStage.DebugDraw, false);
        }

        public static void LoadData()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyLights.LoadData");
            MyRender.Log.WriteLine("MyLights.LoadData() - START");
            MyRender.Log.IncreaseIndent();


            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyLights.LoadData() - END");
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UnloadData()
        {
        }

        public static List<MyRenderLight> GetSortedLights()
        {
            return m_renderLights;
        }

        public static void UpdateLightsForEffect(List<MyRenderLight> renderLights)
        {
            m_renderLights.Clear();

            const float RADIUS_FOR_LIGHTS = 400;

            BoundingSphereD sphere = new BoundingSphereD(MyRenderCamera.Position, RADIUS_FOR_LIGHTS);
            int maxLightsForEffect = MyLightsConstants.MAX_LIGHTS_FOR_EFFECT;

            MyUtils.AssertIsValid(sphere.Center);
            MyUtils.AssertIsValid(sphere.Radius);


            //  If number of lights with influence is more than max number of lights allowed in the effect, we sort them by distance (or we can do it by some priority)
            if (renderLights.Count > maxLightsForEffect)
            {
                m_sortLightsComparer.BoundingSphere = sphere;
                renderLights.Sort(m_sortLightsComparer);
            }
            else
                maxLightsForEffect = renderLights.Count;

            for (int i = 0; i < maxLightsForEffect; i++)
            {
                m_renderLights.Add(renderLights[i]);
            }
        }


        //  This method adds lights information into specified effect.
        //  Method gets lights that could have influence on bounding sphere (it is assumed this will be bounding sphere of a phys object or voxel render cell).
        //  Lights that are far from bounding sphere are ignored. But near lights are taken to second step, where we sort them by distance and priority
        //  and set them to the effect.
        //  We assume RemoveKilled() was called before this method, so here we don't check if light isn't killed.
        public static void UpdateEffect(MyEffectDynamicLightingBase effect, bool subtractCameraPosition)
        {
            //  Set lights to effect, but not more than effect can handle
            for (int i = 0; i < m_renderLights.Count; i++)
            {
                SetLightToEffect(effect, i, m_renderLights[i], subtractCameraPosition);
            }

            effect.SetDynamicLightsCount(m_renderLights.Count);

            Vector4 sunColor = MyRender.Sun.Color;
            effect.SetSunColor(new Vector3(sunColor.X, sunColor.Y, sunColor.Z));
            effect.SetDirectionToSun(-MyRender.Sun.Direction);
            effect.SetSunIntensity(MyRender.Sun.Intensity);
                       
            Vector3 ambientColor = MyRender.AmbientColor * MyRender.AmbientMultiplier;
            effect.SetAmbientColor(ambientColor);

        }

        static void SetLightToEffect(MyEffectDynamicLightingBase effect, int index, MyRenderLight light, bool subtractCameraPosition)
        {
            if (subtractCameraPosition == true)
            {
                effect.SetDynamicLightsPosition(index, (Vector3)(light.Position - MyRenderCamera.Position));
            }
            else
            {
                effect.SetDynamicLightsPosition(index, (Vector3)light.Position);
            }

            //Cannot use *light.Intensity because it makes visual artifacts
            effect.SetDynamicLightsColor(index, light.Color * MathHelper.Clamp(light.Intensity, 0, 1));
            effect.SetDynamicLightsFalloff(index, light.Falloff);
            effect.SetDynamicLightsRange(index, light.Range);
        }

        public static void UpdateEffectReflector(MyEffectReflectorBase effect, bool subtractCameraPosition)
        {
            MyRenderLight reflectorLight = null;

            foreach (MyRenderLight light in m_renderLights)
            {
                if (light.ReflectorOn)
                {
                    reflectorLight = light;
                    break;
                }
            }

            if (reflectorLight != null && reflectorLight.ReflectorOn)
            {
                effect.SetReflectorDirection(reflectorLight.ReflectorDirection);
                effect.SetReflectorConeMaxAngleCos(reflectorLight.ReflectorConeMaxAngleCos);
                effect.SetReflectorColor(reflectorLight.ReflectorColor);
                effect.SetReflectorRange(reflectorLight.ReflectorRange);
            }
            else
            {
                effect.SetReflectorRange(0);
            }

            if (subtractCameraPosition)
                effect.SetCameraPosition(Vector3.Zero);
            else
                effect.SetCameraPosition((Vector3)MyRenderCamera.Position); 
        }


        public static void DebugDraw()
        {
            /*
            MyLights.UpdateSortedLights(ref MyCamera.BoundingSphere, false);

            foreach (MyLight light in m_sortedLights)
            {
                //if (light.LightOn && light.Glare.Type == TransparentGeometry.MyLightGlare.GlareTypeEnum.Distant)
                {
                    if ((light.LightType & MyLight.LightTypeEnum.PointLight) != 0)
                    {
                        MyDebugDraw.DrawSphereWireframe(Matrix.CreateScale(light.Range) * Matrix.CreateTranslation(light.PositionWithOffset), new Vector3(1, 0, 0), 1);
                    }
                    if ((light.LightType & MyLight.LightTypeEnum.Hemisphere) != 0)
                    {
                        Matrix rotationHotfix = Matrix.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver2);
                        Matrix world = Matrix.CreateScale(light.Range) * rotationHotfix * Matrix.CreateWorld(light.Position, light.ReflectorDirection, light.ReflectorUp);
                        MyDebugDraw.DrawHemisphereWireframe(world, new Vector3(1, 0, 0), 1);
                    }
                    if ((light.LightType & MyLight.LightTypeEnum.Spotlight) != 0)
                    {
                        Vector4 color = Color.Aqua.ToVector4();
                        // MyDebugDraw.DrawAABB(ref bb, ref color, 1.0f);

                        MyDebugDraw.DrawAxis(Matrix.CreateWorld(light.Position, Vector3.Up, Vector3.Forward), 2, 1);
                        MyDebugDraw.DrawSphereWireframe(Matrix.CreateScale(light.Range) * Matrix.CreateTranslation(light.PositionWithOffset), new Vector3(1, 0, 0), 1);

                        // Uncomment to show sphere for spot light
                        //MyDebugDraw.DrawSphereWireframe(Matrix.CreateScale(light.ReflectorRange) * Matrix.CreateTranslation(light.Position), new Vector3(color.X, color.Y, color.Z), 0.25f);
                        //MySimpleObjectDraw.DrawConeForLight();
                        MyStateObjects.WireframeRasterizerState.Apply();
                        SharpDX.Toolkit.Graphics.DepthStencilState.None.Apply();
                        
                        MyDebugDraw.DrawModel(MySimpleObjectDraw.ModelCone, light.SpotWorld, Vector3.One, 1);
                    }
                } 
            }        */
        }
    }
}
