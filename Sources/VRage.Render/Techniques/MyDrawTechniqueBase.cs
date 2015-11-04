using VRage;
using VRageMath;
using VRageRender.Effects;
using VRageRender.Graphics;

namespace VRageRender.Techniques
{
    abstract class MyDrawTechniqueBase
    {
        protected RasterizerState SolidRasterizerState = RasterizerState.CullCounterClockwise;
        protected RasterizerState WireframeRasterizerState = MyStateObjects.WireframeCounterClockwiseRasterizerState;

        protected void SetupBaseEffect(MyEffectBase shader, MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            MyRenderCamera.SetupBaseEffect(shader, lodType,  setup.FogMultiplierMult);

            if (lodType == MyLodTypeEnum.LOD_NEAR)
            {
                shader.SetProjectionMatrix(ref MyRenderCamera.ProjectionMatrixForNearObjects);
            }
            else
            {
                shader.SetProjectionMatrix(ref MyRenderCamera.ProjectionMatrix);
            }

            shader.SetViewMatrix(ref MyRenderCamera.ViewMatrixAtZero);

            var rasterizerState = MyRender.Settings.Wireframe ? WireframeRasterizerState : SolidRasterizerState;
            rasterizerState.Apply();
        }

        /// <summary>
        /// Caller is responsible to call End when done with shader.
        /// </summary>
        public abstract MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType);

        public abstract void SetupVoxelMaterial(MyEffectVoxels shader, MyRenderVoxelBatch batch);
        public abstract void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material);
        public abstract void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement);
    }
}
