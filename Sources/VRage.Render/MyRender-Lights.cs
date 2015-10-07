#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;

using VRageRender.Effects;
using VRageRender.Lights;
using VRageRender.Graphics;

using VRageMath;
using SharpDX.Direct3D9;

#endregion

namespace VRageRender
{
    internal static partial class MyRender
    {
        #region Lights

        static public void UpdateForwardLights()
        {
            GetRenderProfiler().StartProfilingBlock("Setup lights");

            MyEffectModelsDNS effectDNS = (MyEffectModelsDNS)GetEffect(MyEffects.ModelDNS);
            MyLights.UpdateEffect(effectDNS.DynamicLights, true);
            MyLights.UpdateEffectReflector(effectDNS.Reflector, true);

            MyEffectVoxels effectVoxels = (MyEffectVoxels)GetEffect(MyEffects.VoxelsMRT);
            MyLights.UpdateEffect(effectVoxels.DynamicLights, true);
            MyLights.UpdateEffectReflector(effectVoxels.Reflector, true);

            MyEffectDecals effectDecals = (MyEffectDecals)GetEffect(MyEffects.Decals);
            MyLights.UpdateEffect(effectDecals.DynamicLights, true);
            MyLights.UpdateEffectReflector(effectDecals.Reflector, true);

            GetRenderProfiler().EndProfilingBlock(); //Setup lights
        }


        static void RenderSpotLight(MyLightRenderElement lightElement, MyEffectPointLight effectPointLight)
        {
            MyRenderLight light = lightElement.Light;

            //Matrix lightViewProjectionShadow = Matrix.Identity;

            // Always cull clockwise (render inner parts of object), depth test is done in PS using light radius and cone angle
            RasterizerState.CullClockwise.Apply();
            DepthStencilState.None.Apply();

            //m_device.BlendState = BlendState.Additive;
            //Need to use max because of overshinning places where multiple lights shine
            MyStateObjects.Light_Combination_BlendState.Apply();

            if (lightElement.RenderShadows && lightElement.ShadowMap != null)
            {
                m_spotShadowRenderer.SetupSpotShadowBaseEffect(effectPointLight, lightElement.ShadowLightViewProjectionAtZero, lightElement.ShadowMap);
            }
            effectPointLight.SetNearSlopeBiasDistance(0);

            effectPointLight.SetLightPosition((Vector3)(light.Position - MyRenderCamera.Position));
            effectPointLight.SetLightIntensity(light.Intensity);
            effectPointLight.SetSpecularLightColor(light.SpecularColor);
            effectPointLight.SetFalloff(light.Falloff);

            effectPointLight.SetLightViewProjection((Matrix)(lightElement.ViewAtZero * lightElement.Projection));
            effectPointLight.SetReflectorDirection(light.ReflectorDirection);
            effectPointLight.SetReflectorConeMaxAngleCos(1 - light.ReflectorConeMaxAngleCos);
            effectPointLight.SetReflectorColor(light.ReflectorColor);
            effectPointLight.SetReflectorRange(light.ReflectorRange);
            effectPointLight.SetReflectorIntensity(light.ReflectorIntensity);
            effectPointLight.SetReflectorTexture(light.ReflectorTexture);
            effectPointLight.SetReflectorFalloff(light.ReflectorFalloff);

            if (lightElement.RenderShadows)
                effectPointLight.SetTechnique(effectPointLight.SpotShadowTechnique);
            else
                effectPointLight.SetTechnique(effectPointLight.SpotTechnique);

            MyDebugDraw.DrawConeForLight(effectPointLight, lightElement.World);
        }


        static void RenderSpotLights(List<MyLightRenderElement> spotLightElements, MyEffectPointLight effectPointLight)
        {
            if (spotLightElements.Count == 0 || !MyRender.Settings.EnableSpotLights)
            {
                return;
            }

            GetRenderProfiler().StartProfilingBlock("RenderSpotLightList");
            foreach (var spotElement in spotLightElements)
            {
                RenderSpotLight(spotElement, effectPointLight);
            }
            GetRenderProfiler().EndProfilingBlock();
        }

        private static void AddSpotLightRenderElement(MyRenderLight light)
        {
            float cosAngle = 1 - light.ReflectorConeMaxAngleCos;

            // Near clip is 5 to prevent cockpit bugs
            float nearClip = 0.5f;
            float c = nearClip / cosAngle;

            // 'a' is "screen size" at near clip (a, c and nearclip makes right triangle)
            float a = (float)Math.Sqrt(c * c - nearClip * nearClip);
            if (nearClip < light.ReflectorRange)
            {
                MatrixD lightView = MatrixD.CreateLookAt(light.Position, light.Position + light.ReflectorDirection * light.ReflectorRange, (Vector3D)light.ReflectorUp);
                MatrixD lightViewAtZero = MatrixD.CreateLookAt(light.Position - MyRenderCamera.Position, light.Position + light.ReflectorDirection * light.ReflectorRange - MyRenderCamera.Position, (Vector3D)light.ReflectorUp);

                var distanceSquared = (MyRenderCamera.Position - light.Position).LengthSquared();

                bool drawShadows = light.CastShadows;
                drawShadows &= distanceSquared < light.ShadowDistance * light.ShadowDistance * MyRenderConstants.RenderQualityProfile.SpotShadowsMaxDistanceMultiplier;                

                var lightProjection = Matrix.CreatePerspectiveOffCenter(-a, a, -a, a, nearClip, light.ReflectorRange);

                bool renderShadows = Settings.EnableSpotShadows && MyRender.CurrentRenderSetup.EnableSmallLightShadows.Value && drawShadows;

                MyLightRenderElement lightElement = null;
                lightElement = m_spotLightsPool.Allocate(true);
                Debug.Assert(lightElement != null, "Out of lights, increase pool");
                if (lightElement != null)
                {
                    MatrixD spotWorld = light.SpotWorld;
                    spotWorld.Translation = light.SpotWorld.Translation - MyRenderCamera.Position;
                    lightElement.Light = light;
                    lightElement.World = spotWorld;
                    lightElement.ViewAtZero = lightViewAtZero;
                    lightElement.Projection = lightProjection;
                    lightElement.RenderShadows = renderShadows;

                    if ((light.SpotQuery != null) && (light.SpotQueryState == QueryState.CheckOcc) && (light.SpotQuery.IsComplete))
                    {
                        light.QueryPixels = light.SpotQuery.PixelCount;
                        if (light.QueryPixels < 0) //ATI
                            light.QueryPixels = 0;
                        light.SpotQueryState = QueryState.WaitOcc;
                    }
                    
                    //lightElement.UseReflectorTexture = (light.LightOwner == MyLight.LightOwnerEnum.SmallShip);

                    if (renderShadows)
                    {
                        var lightViewProjectionShadow = m_spotShadowRenderer.CreateViewProjectionMatrix(lightView, a, nearClip, light.ReflectorRange);
                        var lightViewProjectionShadowAtZero = m_spotShadowRenderer.CreateViewProjectionMatrix(lightViewAtZero, a, nearClip, light.ReflectorRange);
                        lightElement.ShadowLightViewProjection = lightViewProjectionShadow;
                        lightElement.ShadowLightViewProjectionAtZero = lightViewProjectionShadowAtZero;
                    }
                    m_spotLightRenderElements.Add(lightElement);
                }
            }
        }

        private static void PrepareLights()
        {
            GetRenderProfiler().StartProfilingBlock("Prepare lights");

            // Select small lights and do frustum check
            if (MyRender.CurrentRenderSetup.EnableSmallLights.Value)
            {
              /*  List<MyLight> usedLights = null;
                
                //todo
                //if (CurrentRenderSetup.LightsToUse == null)
                {
                    var frustum = MyRenderCamera.GetBoundingFrustum();
                    MyLights.UpdateSortedLights(ref frustum);
                    usedLights = MyLights.GetSortedLights();
                }*/
                /*else
                {
                    usedLights = CurrentRenderSetup.LightsToUse;
                } */

                m_pointLights.Clear();
                m_hemiLights.Clear();
                m_spotLightRenderElements.Clear();
                m_spotLightsPool.DeallocateAll();
                foreach (var light in m_renderLightsForDraw)
                {
                    if (light.LightOn) // Light is on
                    {
                        if ((light.LightType & LightTypeEnum.PointLight) != 0 && (light.LightType & LightTypeEnum.Hemisphere) == 0) // Light is point
                        {
                            m_pointLights.Add(light);
                        }
                        if ((light.LightType & LightTypeEnum.Hemisphere) != 0) // Light is hemi
                        {
                            m_hemiLights.Add(light);
                        }
                        if ((light.LightType & LightTypeEnum.Spotlight) != 0 && light.ReflectorOn) // Light is spot
                        {
                            AddSpotLightRenderElement(light);
                        }
                    }
                }
            }

            GetRenderProfiler().EndProfilingBlock();
        }

        private static void RenderSpotShadows()
        {
            // Render spot shadows (for first n spot lights, lights are sorted by camera distance)
            if (MyRender.CurrentRenderSetup.EnableSmallLights.Value && MyRender.Settings.EnableSpotShadows)
            {
                GetRenderProfiler().StartProfilingBlock("Render spot shadows");

                m_spotLightRenderElements.Sort(MyLightRenderElement.SpotComparer); 

                int currentShadowTarget = 0;
                foreach (var spotElement in m_spotLightRenderElements)
                {
                    spotElement.Light.ShadowMapIndex = -1;

                    if (currentShadowTarget >= m_spotShadowRenderTargets.Length)
                    {
                        spotElement.ShadowMap = null;
                        continue;
                    }
                    if (spotElement.RenderShadows)
                    {
                        var shadowMapRt = (SharpDX.Direct3D9.Texture)m_spotShadowRenderTargets[currentShadowTarget];
                        var shadowMapDepthRt = (SharpDX.Direct3D9.Texture)m_spotShadowRenderTargetsZBuffers[currentShadowTarget];
                        var position = spotElement.Light.Position;
                        m_spotShadowRenderer.RenderForLight(spotElement.ShadowLightViewProjection, spotElement.ShadowLightViewProjectionAtZero, ref position, shadowMapRt, shadowMapDepthRt, currentShadowTarget, spotElement.Light.ShadowIgnoreObjects);
                        spotElement.ShadowMap = shadowMapRt;

                        spotElement.Light.ShadowMapIndex = currentShadowTarget;

                       //BaseTexture.ToFile(shadowMapRt, "c:\\spot_shadow " + currentShadowTarget.ToString() + ".dds", ImageFileFormat.Dds);

                        currentShadowTarget++;
                    }
                }
                GetRenderProfiler().EndProfilingBlock();
            }
        }

        internal static void RenderLights()
        {
            PrepareLights();

            RenderSpotShadows();

            GetRenderProfiler().StartProfilingBlock("Render lights");
            MyRender.SetRenderTarget(GetRenderTarget(MyRenderTargets.Auxiliary1), null, SetDepthTargetEnum.RestoreDefault);
            MyRender.GraphicsDevice.Clear(ClearFlags.Target, new SharpDX.ColorBGRA(0.0f), 1, 0);

            SetCorrectViewportSize();

            if (MyRender.CurrentRenderSetup.EnableSmallLights.Value)
            {
                MyEffectPointLight effectPointLight = (MyEffectPointLight) MyRender.GetEffect(MyEffects.PointLight);
                SharpDX.Direct3D9.Texture diffuseRT = MyRender.GetRenderTarget(MyRenderTargets.Diffuse);
                effectPointLight.SetNormalsRT(MyRender.GetRenderTarget(MyRenderTargets.Normals));
                effectPointLight.SetDiffuseRT(diffuseRT);
                effectPointLight.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                effectPointLight.SetHalfPixel(diffuseRT.GetLevelDescription(0).Width, diffuseRT.GetLevelDescription(0).Height);
                effectPointLight.SetScale(GetScaleForViewport(diffuseRT));

                var invViewProjMatrix = Matrix.Invert(MyRenderCamera.ViewProjectionMatrixAtZero);
                var invViewMatrix = Matrix.Invert(MyRenderCamera.ViewMatrixAtZero);

                effectPointLight.SetCameraPosition(Vector3.Zero);
                effectPointLight.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);
                effectPointLight.SetInvViewMatrix(invViewMatrix);

                DepthStencilState.None.Apply();
                MyStateObjects.Light_Combination_BlendState.Apply();

                //Render each light with a model specific to the light
                GetRenderProfiler().StartProfilingBlock("PointLight");

                var cullRationSq = MyRenderConstants.DISTANCE_LIGHT_CULL_RATIO * MyRenderConstants.DISTANCE_LIGHT_CULL_RATIO;

                if (MyRender.Settings.EnablePointLights)
                {
                    foreach (MyRenderLight light in m_pointLights)
                    {
                        var distanceSq = (MyRenderCamera.Position - light.PositionWithOffset).LengthSquared();

                        var hasVolumetricGlare = light.GlareOn && light.Glare.Type == MyGlareTypeEnum.Distant;
                        var isTooFarAway = (light.Range * light.Range) < (distanceSq / cullRationSq);

                        if (!isTooFarAway)
                        {
                            // Always cull clockwise (render inner parts of object), depth test is done is PS using light radius
                            RasterizerState.CullClockwise.Apply();

                            effectPointLight.SetLightPosition((Vector3)(light.PositionWithOffset - MyRenderCamera.Position));
                            effectPointLight.SetLightIntensity(light.Intensity);
                            effectPointLight.SetSpecularLightColor(light.SpecularColor);
                            effectPointLight.SetFalloff(light.Falloff);

                            effectPointLight.SetLightRadius(light.Range);
                            effectPointLight.SetReflectorTexture(light.ReflectorTexture);
                            effectPointLight.SetLightColor(light.Color.ToVector3());

                            effectPointLight.SetTechnique(effectPointLight.PointTechnique);

                            MyDebugDraw.DrawSphereForLight(effectPointLight, light.PositionWithOffset, light.Range, ref Vector3.One, 1);
                            MyPerformanceCounter.PerCameraDrawWrite.LightsCount++;
                        }

                        // if(!isTooFarAway || hasVolumetricGlare)
                        //   light.Draw();
                    }
                }


                GetRenderProfiler().EndProfilingBlock();


                GetRenderProfiler().StartProfilingBlock("Hemisphere");

                if (MyRender.Settings.EnablePointLights)
                {
                    foreach (MyRenderLight light in m_hemiLights)
                    {
                        // compute bounding box
                        //Vector3 center = light.Position;// - light.Range * new Vector3(0,1,0);
                        //Vector3 extend = new Vector3(light.Range, light.Range, light.Range);
                        //m_lightBoundingBox.Min = center - extend;
                        //m_lightBoundingBox.Max = center + extend;
                        // Always cull clockwise (render inner parts of object), depth test is done is PS using light radius
                        if (Vector3D.Dot((Vector3D)light.ReflectorDirection, MyRenderCamera.Position - light.Position) > 0 && light.PointBoundingSphere.Contains(MyRenderCamera.Position) == VRageMath.ContainmentType.Contains)
                        {
                            RasterizerState.CullNone.Apply(); //zevnitr
                        }
                        else
                        {
                            RasterizerState.CullCounterClockwise.Apply(); //zvenku
                        }

                        effectPointLight.SetLightPosition((Vector3)(light.Position - MyRenderCamera.Position));
                        effectPointLight.SetLightIntensity(light.Intensity);
                        effectPointLight.SetSpecularLightColor(light.SpecularColor);
                        effectPointLight.SetFalloff(light.Falloff);

                        effectPointLight.SetLightRadius(light.Range);
                        effectPointLight.SetReflectorTexture(light.ReflectorTexture);
                        effectPointLight.SetLightColor(light.Color.ToVector3());
                        effectPointLight.SetTechnique(effectPointLight.HemisphereTechnique);

                        MatrixD world = MatrixD.CreateScale(light.Range) * MatrixD.CreateWorld(light.Position - MyRenderCamera.Position, light.ReflectorDirection, light.ReflectorUp);
                        MyDebugDraw.DrawHemisphereForLight(effectPointLight, ref world, ref Vector3.One, 1);
                        //light.Draw();

                        MyPerformanceCounter.PerCameraDrawWrite.LightsCount++;
                    }
                }
                GetRenderProfiler().EndProfilingBlock();
                      

                GetRenderProfiler().StartProfilingBlock("Spotlight");
                RenderSpotLights(m_spotLightRenderElements, effectPointLight);

                GetRenderProfiler().EndProfilingBlock();

                foreach (var light in m_renderLightsForDraw)
                {
                    light.Draw();
                }
            }
             
            DepthStencilState.None.Apply();
            RasterizerState.CullCounterClockwise.Apply();
            
            MyStateObjects.Sun_Combination_BlendState.Apply();

            GetRenderProfiler().StartProfilingBlock("Sun light");

            if (Settings.EnableSun && CurrentRenderSetup.EnableSun.Value && MyRender.Sun.Direction != Vector3.Zero)
            {
                //Sun light
                MyEffectDirectionalLight effectDirectionalLight = MyRender.GetEffect(MyEffects.DirectionalLight) as MyEffectDirectionalLight;
                Texture diffuseRTSun = MyRender.GetRenderTarget(MyRenderTargets.Diffuse);
                effectDirectionalLight.SetNormalsRT(MyRender.GetRenderTarget(MyRenderTargets.Normals));
                effectDirectionalLight.SetDiffuseRT(diffuseRTSun);
                effectDirectionalLight.SetDepthsRT(MyRender.GetRenderTarget(MyRenderTargets.Depth));
                effectDirectionalLight.SetHalfPixelAndScale(diffuseRTSun.GetLevelDescription(0).Width, diffuseRTSun.GetLevelDescription(0).Height, GetScaleForViewport(diffuseRTSun));

                effectDirectionalLight.SetCameraMatrix(Matrix.Invert(MyRenderCamera.ViewMatrixAtZero));

                effectDirectionalLight.SetAmbientMinimumAndIntensity(new Vector4(AmbientColor * AmbientMultiplier, EnvAmbientIntensity));
                
                effectDirectionalLight.SetTextureEnvironmentMain(MyEnvironmentMap.EnvironmentMainMap);
                effectDirectionalLight.SetTextureEnvironmentAux(MyEnvironmentMap.EnvironmentAuxMap);
                effectDirectionalLight.SetTextureAmbientMain(MyEnvironmentMap.AmbientMainMap);
                effectDirectionalLight.SetTextureAmbientAux(MyEnvironmentMap.AmbientAuxMap);
                effectDirectionalLight.SetTextureEnvironmentBlendFactor(MyEnvironmentMap.BlendFactor);

                effectDirectionalLight.SetCameraPosition(Vector3.Zero);

                //Set distance where no slope bias will be applied (because of cockpit artifacts)
                effectDirectionalLight.SetNearSlopeBiasDistance(3);

                effectDirectionalLight.ShowSplitColors(Settings.ShowShadowCascadeSplits);
                

                effectDirectionalLight.SetShadowBias(MyRenderCamera.FieldOfView * 0.0001f * MyRenderConstants.RenderQualityProfile.ShadowBiasMultiplier);
                effectDirectionalLight.SetSlopeBias(0.00002f * MyRenderConstants.RenderQualityProfile.ShadowSlopeBiasMultiplier);
                effectDirectionalLight.SetSlopeCascadeMultiplier(20.0f); //100 makes artifacts in prefabs

                MyRender.GetShadowRenderer().SetupShadowBaseEffect(effectDirectionalLight);

                effectDirectionalLight.SetLightDirection(m_sun.Direction); 
                effectDirectionalLight.SetLightColorAndIntensity(new Vector3(m_sun.Color.X, m_sun.Color.Y, m_sun.Color.Z), m_sun.Intensity);
                effectDirectionalLight.SetBacklightColorAndIntensity(new Vector3(m_sun.BackColor.X, m_sun.BackColor.Y, m_sun.BackColor.Z), m_sun.BackIntensity);
                //m_sun.SpecularColor = {X:0,9137255 Y:0,6078432 Z:0,2078431} //nice yellow
                effectDirectionalLight.SetSpecularLightColor(m_sun.SpecularColor);
                effectDirectionalLight.EnableCascadeBlending(MyRenderConstants.RenderQualityProfile.EnableCascadeBlending);

                effectDirectionalLight.SetFrustumCorners(MyRender.GetShadowRenderer().GetFrustumCorners());

                effectDirectionalLight.SetEnableAmbientEnvironment(Settings.EnableEnvironmentMapAmbient && MyRenderConstants.RenderQualityProfile.EnableEnvironmentals && CurrentRenderSetup.EnableEnvironmentMapping.Value);
                effectDirectionalLight.SetEnableReflectionEnvironment(Settings.EnableEnvironmentMapReflection && MyRenderConstants.RenderQualityProfile.EnableEnvironmentals && CurrentRenderSetup.EnableEnvironmentMapping.Value);

                if (Settings.EnableShadows && MyRender.CurrentRenderSetup.ShadowRenderer != null)
                    effectDirectionalLight.SetTechnique(effectDirectionalLight.DefaultTechnique);
                else
                    effectDirectionalLight.SetTechnique(effectDirectionalLight.DefaultWithoutShadowsTechnique);

                GetFullscreenQuad().Draw(effectDirectionalLight);
            }
            GetRenderProfiler().EndProfilingBlock();

         //  var st = MyRender.GetRenderTarget(MyRenderTargets.ShadowMap);
         //  Texture.ToFile(st, "c:\\sm.dds", ImageFileFormat.Dds);

            // Blend in background
            if (true) // blend background
            {                  
                GetRenderProfiler().StartProfilingBlock("Blend background");
                
                //todo
                /*if (MyFakes.RENDER_PREVIEWS_WITH_CORRECT_ALPHA)
                {
                    // for some reason the other option does not give 0 alpha for the background when rendering gui preview images
                    MyStateObjects.Additive_NoAlphaWrite_BlendState.Apply();
                }
                else */
                {
                    MyStateObjects.NonPremultiplied_NoAlphaWrite_BlendState.Apply();
                    //BlendState.NonPremultiplied.Apply();
                }
                DepthStencilState.None.Apply();
                RasterizerState.CullCounterClockwise.Apply();
                         
                MyEffectBlendLights effectBlendLights = MyRender.GetEffect(MyEffects.BlendLights) as MyEffectBlendLights;
                Texture diffuseRT = GetRenderTarget(MyRenderTargets.Diffuse);
                MyRenderCamera.SetupBaseEffect(effectBlendLights, m_currentLodDrawPass, m_currentSetup.FogMultiplierMult);
                effectBlendLights.SetDiffuseTexture(diffuseRT);
                effectBlendLights.SetNormalTexture(GetRenderTarget(MyRenderTargets.Normals));
                effectBlendLights.SetDepthTexture(GetRenderTarget(MyRenderTargets.Depth));
                effectBlendLights.SetHalfPixel(diffuseRT.GetLevelDescription(0).Width, diffuseRT.GetLevelDescription(0).Height);
                effectBlendLights.SetScale(GetScaleForViewport(diffuseRT));
                effectBlendLights.SetBackgroundTexture(GetRenderTarget(MyRenderTargets.Auxiliary0));

                effectBlendLights.SetTechnique(effectBlendLights.DefaultTechnique);

                GetFullscreenQuad().Draw(effectBlendLights);
                GetRenderProfiler().EndProfilingBlock();

                // Blend in emissive light, overwrite emissivity (alpha)
                GetRenderProfiler().StartProfilingBlock("Copy emisivity");

                //todo
               // if (MyPostProcessHDR.RenderHDRThisFrame())
               //     MyStateObjects.AddEmissiveLight_BlendState.Apply();
               // else
                    MyStateObjects.AddEmissiveLight_NoAlphaWrite_BlendState.Apply();

                effectBlendLights.SetTechnique(effectBlendLights.CopyEmissivityTechnique);
                GetFullscreenQuad().Draw(effectBlendLights);


                bool showDebugLighting = false;

                if (Settings.ShowSpecularIntensity)
                {
                    effectBlendLights.SetTechnique(MyEffectBlendLights.Technique.OnlySpecularIntensity);
                    showDebugLighting = true;
                }
                else
                    if (Settings.ShowSpecularPower)
                    {
                        effectBlendLights.SetTechnique(MyEffectBlendLights.Technique.OnlySpecularPower);
                        showDebugLighting = true;
                    }
                    else
                        if (Settings.ShowEmissivity)
                        {
                            effectBlendLights.SetTechnique(MyEffectBlendLights.Technique.OnlyEmissivity);
                            showDebugLighting = true;
                        }
                        else
                            if (Settings.ShowReflectivity)
                            {
                                effectBlendLights.SetTechnique(MyEffectBlendLights.Technique.OnlyReflectivity);
                                showDebugLighting = true;
                            }

                if (showDebugLighting)
                {
                    BlendState.Opaque.Apply();
                    GetFullscreenQuad().Draw(effectBlendLights);
                }

                GetRenderProfiler().EndProfilingBlock();
            }

            //TakeScreenshot("Accumulated_lights", GetRenderTarget(MyRenderTargets.Lod0Depth), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            /*TakeScreenshot("EnvironmentMap_1", GetRenderTargetCube(MyRenderTargets.EnvironmentCube), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            TakeScreenshot("EnvironmentMap_2", GetRenderTargetCube(MyRenderTargets.EnvironmentCubeAux), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            TakeScreenshot("AmbientMap_1", GetRenderTargetCube(MyRenderTargets.AmbientCube), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
            TakeScreenshot("AmbientMap_2", GetRenderTargetCube(MyRenderTargets.AmbientCubeAux), MyEffectScreenshot.ScreenshotTechniqueEnum.Default);
              */
            GetRenderProfiler().EndProfilingBlock();
        }

        #endregion
    }
}