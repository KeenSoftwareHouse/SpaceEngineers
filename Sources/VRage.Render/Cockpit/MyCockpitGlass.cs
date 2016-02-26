using VRageMath;

using SharpDX.Direct3D9;
using VRageRender.Graphics;
using VRageRender.Effects;
using VRageRender.Utils;
using VRageRender.Lights;
using VRage.Utils;
using VRage;
using VRage.Library.Utils;


//  Draws cockpit glass (scratches, etc). Left, front and right window.

namespace VRageRender
{
    class MyCockpitGlass : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.CockpitGlass;
        }

        //Bounding sphere used to calculate nearby lights
        static BoundingSphereD m_boundingSphereForLights = new BoundingSphereD();

        private static MyInterpolationQueue<MatrixD> m_interpolation = new MyInterpolationQueue<MatrixD>(8, MatrixD.Slerp);

        private static MatrixD m_playerHeadForCockpit;

        public static bool Visible;
        public static float GlassDirtAlpha;
        public static MatrixD PlayerHeadForCockpitInteriorWorldMatrix
        {
            get { return m_playerHeadForCockpit; }
            set
            {
                MyRender.AddAndInterpolateObjectMatrix(m_interpolation, ref value);
                m_playerHeadForCockpit = value;
            }
        }
        public static string Model;

        static MyCockpitGlass()
        {
            // Cockpit interior drawn like entity under small ship
            MyRender.RegisterRenderModule(MyRenderModuleEnum.CockpitGlass, "Cockpit glass", Draw, MyRenderStage.AlphaBlend, 200, true);
        }

        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyCockpitGlass.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyCockpitGlass::LoadContent");

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyCockpitGlass.LoadContent() - END");
        }

        public override void UnloadContent()
        {
            MyRender.Log.WriteLine("MyCockpitGlass.UnloadContent - START");
            MyRender.Log.IncreaseIndent();

            Visible = false;

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyCockpitGlass.UnloadContent - END");
        }

        public static void Draw()
        {
            if (!Visible)
                return;

            MyRenderModel model = MyRenderModels.GetModel(Model);//MySession.Static.PlayerShip.CockpitGlassModelEnum);

            RasterizerState.CullNone.Apply();

            MyStateObjects.DepthStencil_StencilReadOnly.Apply();

            BlendState.NonPremultiplied.Apply();

            MyEffectCockpitGlass effect = (MyEffectCockpitGlass)MyRender.GetEffect(MyEffects.CockpitGlass);

            effect.SetGlassDirtLevelAlpha(new Vector4(GlassDirtAlpha, 0, 0, 0));

            var inMatrix = PlayerHeadForCockpitInteriorWorldMatrix;
            MatrixD drawMatrix;
            MatrixD.Multiply(ref inMatrix, ref MyRenderCamera.InversePositionTranslationMatrix, out drawMatrix);

            effect.SetWorldMatrix((Matrix)drawMatrix);
            effect.SetViewMatrix((Matrix)MyRenderCamera.ViewMatrix);

            Matrix projection = MyRenderCamera.ProjectionMatrixForNearObjects;
            effect.SetWorldViewProjectionMatrix((Matrix)(drawMatrix * MyRenderCamera.ViewMatrixAtZero * projection));

            MyRenderMeshMaterial cockpitMaterial = model.GetMeshList()[0].Material;
            cockpitMaterial.PreloadTexture();
            effect.SetCockpitGlassTexture(cockpitMaterial.DiffuseTexture);

            Texture depthRT = MyRender.GetRenderTarget(MyRenderTargets.Depth);
            effect.SetDepthTexture(depthRT);

            effect.SetHalfPixel(MyUtilsRender9.GetHalfPixel(depthRT.GetLevelDescription(0).Width, depthRT.GetLevelDescription(0).Height));

            Vector4 sunColor = MyRender.Sun.Color;
            effect.SetSunColor(new Vector3(sunColor.X, sunColor.Y, sunColor.Z));

            effect.SetDirectionToSun(-MyRender.Sun.Direction);

            effect.SetAmbientColor(Vector3.Zero);
            effect.SetReflectorPosition((Vector3)((Vector3)MyRenderCamera.Position - 4 * MyRenderCamera.ForwardVector));

            if (MyRender.RenderLightsForDraw.Count > 0)
            {
                effect.SetNearLightColor(MyRender.RenderLightsForDraw[0].Color);
                effect.SetNearLightRange(MyRender.RenderLightsForDraw[0].Range);
            } 

            MyRender.GetShadowRenderer().SetupShadowBaseEffect(effect);
            effect.SetShadowBias(0.001f);

            MyLights.UpdateEffect(effect, true);

            effect.Begin();
            model.Render();
            effect.End();

            //MyDebugDraw.DrawSphereWireframe(PlayerHeadForCockpitInteriorWorldMatrix, Vector3.One, 1);
        }
    }
}
