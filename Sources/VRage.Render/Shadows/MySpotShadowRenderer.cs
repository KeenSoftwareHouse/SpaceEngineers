using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D9;


namespace VRageRender.Shadows
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
    using VRageRender.Effects;
    using VRageRender.Graphics;
    using VRageRender.Textures;
    using VRageMath;

    class MySpotShadowRenderer : MyShadowRendererBase
    {
        public const int SpotShadowMapSize = 256;

        private BoundingFrustumD m_spotFrustum = new BoundingFrustumD(MatrixD.Identity);

        public void RenderForLight(MatrixD lightViewProjection, Matrix lightViewProjectionAtZero, ref Vector3D lightPosition, Texture shadowRenderTarget, Texture shadowDepth, int spotIndex, List<uint> ignoredObjects)
        {             
            m_renderElementsForShadows.Clear();
            m_castingRenderObjectsUnique.Clear();
            
            m_spotFrustum.Matrix = lightViewProjection;
            
            //MyRender.GetEntitiesFromPrunningStructure(ref lightBoundingBox, m_castingRenderObjects);
            //MyRender.GetEntitiesFromShadowStructure(ref lightBoundingBox, m_castingRenderObjects);

            int occludedItemsStats = 0; //always 0 for shadows
            MyRender.PrepareEntitiesForDraw(ref m_spotFrustum, lightPosition, (MyOcclusionQueryID)(1), m_castingRenderObjects, null, m_castingCullObjects, m_castingManualCullObjects, null, ref occludedItemsStats);

            foreach (MyElement element in m_castingRenderObjects)
            {
                MyRenderObject renderObject = (MyRenderObject)element;

                if (ignoredObjects.Contains(renderObject.ID))
                    continue;

                renderObject.GetRenderElementsForShadowmap(MyLodTypeEnum.LOD0, m_renderElementsForShadows, null);
            }

            // Set our targets
            MyRender.SetRenderTarget(shadowRenderTarget, shadowDepth);
            MyRender.GraphicsDevice.Clear(ClearFlags.All, new ColorBGRA(1.0f), 1.0f, 0);

            DepthStencilState.Default.Apply();
            RasterizerState.CullNone.Apply();
            BlendState.Opaque.Apply();

            RenderShadowMap(lightViewProjectionAtZero);

            MyRender.TakeScreenshot("ShadowMapSpot", shadowRenderTarget, MyEffectScreenshot.ScreenshotTechniqueEnum.Color); 
        }

        protected void RenderShadowMap(Matrix lightViewProjectionAtZero)
        {         
            // Set up the effect
            MyEffectShadowMap shadowMapEffect = MyRender.GetEffect(MyEffects.ShadowMap) as MyEffectShadowMap;
            shadowMapEffect.SetViewProjMatrix(lightViewProjectionAtZero);
            shadowMapEffect.SetDitheringTexture((SharpDX.Direct3D9.Texture)MyTextureManager.GetTexture<MyTexture2D>(@"Textures\Models\Dither.png"));
            shadowMapEffect.SetHalfPixel(MySpotShadowRenderer.SpotShadowMapSize, MySpotShadowRenderer.SpotShadowMapSize);
            
            // Draw the models 
            DrawElements(m_renderElementsForShadows, shadowMapEffect, true, MyRenderCamera.Position, MyShadowRenderer.NumSplits, false);   
        }

        public MatrixD CreateViewProjectionMatrix(MatrixD lightView, float halfSize, float nearClip, float farClip)
        {
            float a = halfSize;

            MatrixD lightShadowProjection = MatrixD.CreatePerspectiveOffCenter(-a, a, -a, a, nearClip, farClip);

            return lightView * lightShadowProjection;
        }

        public void SetupSpotShadowBaseEffect(MyEffectSpotShadowBase effect, Matrix lightViewProjectionShadowAtZero, Texture shadowRenderTarget)
        {
            // Set shadow properties
            var shadowMap = shadowRenderTarget;
            effect.SetShadowMap(shadowMap);
            effect.SetShadowMapSize(new Vector2(shadowMap.GetLevelDescription(0).Width, shadowMap.GetLevelDescription(0).Height));
            effect.SetShadowBias(0.05f);
            effect.SetSlopeBias(0.004f);
            effect.SetLightViewProjectionShadow(lightViewProjectionShadowAtZero);
        }
    }
}
