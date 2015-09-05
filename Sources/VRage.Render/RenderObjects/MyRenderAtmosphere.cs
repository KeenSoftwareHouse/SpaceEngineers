using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Import;
using VRageMath;

namespace VRageRender
{
    class MyRenderAtmosphere : MyRenderEntity
    {
        public const float ATMOSPHERE_SCALE = 0.001f;
        float m_atmosphereRadius;
        float m_planetRadius;
        Vector3 m_atmosphereWavelengths;

        public MyRenderAtmosphere(uint id, string debugName, string model, MatrixD worldMatrix, MyMeshDrawTechnique drawTechnique, RenderFlags renderFlags, float atmosphereRadius, float planetRadius, Vector3 atmosphereWavelengths)
            : base(id, debugName,model, worldMatrix,drawTechnique, renderFlags)
        {
            m_atmosphereWavelengths = atmosphereWavelengths;
            m_atmosphereRadius = atmosphereRadius;
            m_planetRadius = planetRadius;
        }

        public MyRenderAtmosphere(uint id, string debugName, MatrixD worldMatrix, MyMeshDrawTechnique drawTechnique, RenderFlags renderFlags)
            : base(id, debugName, worldMatrix,drawTechnique, renderFlags)
        {
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            MyLodTypeEnum lod = lodTypeEnum == MyLodTypeEnum.LOD_BACKGROUND ? MyLodTypeEnum.LOD0:lodTypeEnum;
            base.GetRenderElements(lod, elements, transparentElements);
        }

        public bool IsInside(Vector3D cameraPos)
        {
            return GetRelativeCameraPos(cameraPos).Length() < AtmosphereRadius;
        }

        public Vector3 GetRelativeCameraPos(Vector3D cameraPos)
        {
            return cameraPos * ATMOSPHERE_SCALE -this.Position;
        }

        public float AtmosphereRadius
        {
            get
            {
                return m_atmosphereRadius * ATMOSPHERE_SCALE;
            }
        }

        public float PlanetRadius
        {
            get
            {
                return m_planetRadius * ATMOSPHERE_SCALE;
            }
        }

        public Vector3D Position
        {
            get
            {
                return m_worldMatrix.Translation * ATMOSPHERE_SCALE;
            }
        }

        public bool IsSurface
        {
            get
            {
                return m_drawTechnique == MyMeshDrawTechnique.PLANET_SURFACE;
            }
        }

        public Vector3 AtmosphereWavelengths
        {
            get
            {
                return m_atmosphereWavelengths;
            }
        }
    }
}
