using System;
using VRage;
using VRageMath;
using VRageRender.Effects;

namespace VRageRender.Techniques
{
    class MyDrawTechniqueVoxelSingle : MyDrawTechniqueBase
    {
        MyLodTypeEnum m_currentLod;

        public override MyEffectBase PrepareAndBeginShader(MyRender.MyRenderSetup setup, MyLodTypeEnum lodType)
        {
            var shader = MyRender.GetEffect(MyEffects.VoxelsMRT) as MyEffectVoxels;
            SetupBaseEffect(shader, setup, lodType);

            if (lodType == MyLodTypeEnum.LOD_BACKGROUND)
            {
                shader.ApplyFar();
            }
            else
            {
                shader.Apply();
            }
            m_currentLod = lodType;
            shader.Begin(0, SharpDX.Direct3D9.FX.None);
            return shader;
        }

        public override void SetupVoxelMaterial(MyEffectVoxels effect, MyRenderVoxelBatch batch)
        {
            effect.UpdateVoxelTextures(MyRender.OverrideVoxelMaterial ?? batch.Material0);
        }

        public override void SetupEntity(MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            SetupVoxelEntity(m_currentLod,shader, renderElement);
        }

        public static void SetupVoxelEntity(MyLodTypeEnum lod,MyEffectBase shader, MyRender.MyRenderElement renderElement)
        {
            MyEffectVoxels effectVoxels = shader as MyEffectVoxels;

            {
                MatrixD worldMatrixD = renderElement.WorldMatrix;
                worldMatrixD.Translation -= MyRenderCamera.Position;
                var worldMatrix = (Matrix)worldMatrixD;
                effectVoxels.SetWorldMatrix(ref worldMatrix);
            }

            //effectVoxels.SetVoxelMapPosition((Vector3)(renderElement.WorldMatrix.Translation - MyRenderCamera.Position));
            //effectVoxels.SetPositionLocalOffset((Vector3)(renderElement.WorldMatrix.Right));
            //effectVoxels.SetPositionLocalScale((Vector3)(renderElement.WorldMatrix.Up));
            //effectVoxels.SetLodBounds(new Vector2((float)renderElement.WorldMatrix.M14, (float)renderElement.WorldMatrix.M24));
            effectVoxels.SetDiffuseColor(Vector3.One);
            if (MyRenderSettings.DebugClipmapLodColor && renderElement.VoxelBatch.Lod < MyRenderVoxelCell.LOD_COLORS.Length)
            {
                effectVoxels.SetDiffuseColor(MyRenderVoxelCell.LOD_COLORS[renderElement.VoxelBatch.Lod].ToVector3());
            }
            effectVoxels.EnablePerVertexAmbient(MyRender.Settings.EnablePerVertexVoxelAmbient);

            if (lod == MyLodTypeEnum.LOD_BACKGROUND && renderElement.RenderObject is MyRenderVoxelCellBackground)
            {
                SetupAtmosphere(effectVoxels, renderElement.RenderObject as MyRenderVoxelCellBackground);
            }
        }

        public override void SetupMaterial(MyEffectBase shader, MyRenderMeshMaterial material)
        {
            throw new InvalidOperationException();
        }

        static void SetupAtmosphere(MyEffectVoxels shader, MyRenderVoxelCellBackground element)
        {
            shader.SetHasAtmosphere(element.HasAtmosphere);

            if (element.HasAtmosphere)
            {
                float depthScale = 0.15f;

                shader.SetInnerRadius(element.PlanetRadius);
                shader.SetOutherRadius(element.AtmosphereRadius);

                float scaleAtmosphere = 1.0f / (element.AtmosphereRadius - element.PlanetRadius);

                shader.SetScaleAtmosphere(scaleAtmosphere);
                shader.SetScaleAtmosphereOverScaleDepth(scaleAtmosphere / depthScale);

                Vector3 cameraToCenter = element.GetRelativeCameraPos(MyRenderCamera.Position);

                shader.SetRelativeCameraPos(cameraToCenter);

                shader.SetLightPos(-MySunGlare.GetSunDirection());
                shader.SetIsInside(element.IsInside(MyRenderCamera.Position));

                shader.SetScaleDepth(depthScale);

                shader.SetPositonToLeftBottomOffset(element.PositiontoLeftBottomOffset);
            }
        }
    }
}
