using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    class MyRenderVoxelCellBackground : MyRenderVoxelCell, IMyBackgroundDrawableRenderObject
    {
        int m_backgroundProxyData = 0;

        float m_atmosphereRadius = 0.0f;

        Vector3D m_position;

        Vector3 m_leftCornerPositionOffset;

        public float AtmosphereRadius
        {
            get { return m_atmosphereRadius*MyRenderAtmosphere.ATMOSPHERE_SCALE; }
        }

        float m_planetRadius = 0.0f;

        public float PlanetRadius
        {
            get { return m_planetRadius * MyRenderAtmosphere.ATMOSPHERE_SCALE; }
        }

        bool m_hasAtmosphere = false;

        public bool HasAtmosphere
        {
            get 
            {
                return m_hasAtmosphere;
            }
        }

        public Vector3D Position
        {
            get 
            {
                return m_position;
            }
        }

        public Vector3 PositiontoLeftBottomOffset
        {
            get
            {
                return m_leftCornerPositionOffset * MyRenderAtmosphere.ATMOSPHERE_SCALE;
            }
        }

        Vector3 m_atmosphereWavelengths;

        public Vector3 AtmosphereWavelengths
        {
            get
            {
                return m_atmosphereWavelengths;
            }
        }

        public bool IsInside(Vector3D cameraPos)
        {
            return GetRelativeCameraPos(cameraPos).Length() < AtmosphereRadius;
        }

        public Vector3 GetRelativeCameraPos(Vector3D cameraPos)
        {
            return (cameraPos -this.Position) * MyRenderAtmosphere.ATMOSPHERE_SCALE;
        }

        public int BackgroundProxyData { get { return m_backgroundProxyData; } set { m_backgroundProxyData = value; } }

        public MyRenderVoxelCellBackground(MyCellCoord coord, ref MatrixD worldMatrix, Vector3D position, float atmoshpereRadius, float planetRadius, bool hasAtmosphere, Vector3 atmosphereWavelengths) :
            base(MyClipmapScaleEnum.Massive, coord, ref worldMatrix)
        {
            m_atmosphereWavelengths = atmosphereWavelengths;
            m_atmosphereRadius = atmoshpereRadius;
            m_planetRadius = planetRadius;
            m_hasAtmosphere = hasAtmosphere;
            m_position = position;
            m_leftCornerPositionOffset =  worldMatrix.Translation -position;
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            if (MyRender.Settings.SkipVoxels)
                return;

            Debug.Assert(lodTypeEnum == MyLodTypeEnum.LOD0 || lodTypeEnum == MyLodTypeEnum.LOD_BACKGROUND);

            double distanceMin = (MyRenderCamera.Position - m_aabb.Min).Length();
            double distanceMax = (MyRenderCamera.Position - m_aabb.Max).Length();

            distanceMin = Math.Min(distanceMin, distanceMax);
            distanceMax = Math.Max(distanceMin, distanceMax);

            if (distanceMin > MyRenderCamera.FAR_PLANE_DISTANCE && lodTypeEnum == MyLodTypeEnum.LOD0)
            {
                return;
            }
            if (distanceMax < MyRenderCamera.NEAR_PLANE_FOR_BACKGROUND && lodTypeEnum == MyLodTypeEnum.LOD_BACKGROUND)
            {
                return;
            }
            base.GetRenderElements(lodTypeEnum, elements, transparentElements);
        }

        public override void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
            if (MyRender.Settings.SkipVoxels || lodTypeEnum == MyLodTypeEnum.LOD_BACKGROUND)
                return;


            double distance = (MyRenderCamera.Position - m_aabb.Center).Length();
            if (distance > MyRenderCamera.FAR_PLANE_DISTANCE)
            {
                return;
            }

            base.GetRenderElementsForShadowmap(lodTypeEnum, renderElements, transparentRenderElements);

        }
    }
}
