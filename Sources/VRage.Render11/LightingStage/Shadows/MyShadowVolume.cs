using System;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    class MyShadowVolume
    {
        MatrixD m_matrixShadowToWorldAt0Space = Matrix.Identity;
        Vector3D m_cameraPosition;

        public Matrix MatrixShadowToWorldAt0Space
        {
            get
            {
                MatrixD matrixTranslation = MatrixD.CreateTranslation(m_cameraPosition - MyRender11.Environment.Matrices.CameraPosition);
                return m_matrixShadowToWorldAt0Space * matrixTranslation;
            }
        }

        public Matrix MatrixWorldAt0ToShadowSpace
        {
            get { return Matrix.Invert(MatrixShadowToWorldAt0Space); }
        }

        public MatrixD MatrixWorldToShadowSpace
        {
            get
            {
                MatrixD matrixTranslation = MatrixD.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition);
                return matrixTranslation * MatrixWorldAt0ToShadowSpace;
            }
        }

        public MatrixD MatrixShadowToWorldSpace
        {
            get { return MatrixD.Invert(MatrixWorldToShadowSpace); }
        }
        public Color DebugShadowVolumeColor = Color.Pink;
        
        static readonly Vector3D[] m_boxVertices = new Vector3D[]
        {
            new Vector3D(-1, -1, 0),
            new Vector3D(-1, 1, 0),
            new Vector3D( 1, 1, 0),
            new Vector3D( 1, -1, 0),

            new Vector3D(-1, -1, 1),
            new Vector3D(-1, 1, 1),
            new Vector3D( 1, 1, 1),
            new Vector3D( 1, -1, 1)
        };
        static Vector3[] m_tmpDrawShadowVolume8 = new Vector3[8];
        static Vector3D[] m_tmpDrawShadowVolume8D = new Vector3D[8];
        
        public void SetMatrixWorldAt0ToShadow(MatrixD matrix)
        {
            m_matrixShadowToWorldAt0Space = MatrixD.Invert(matrix);
            m_cameraPosition = MyRender11.Environment.Matrices.CameraPosition;
        }

        public void DrawShadowVolumeIntoWorld()
        {
            var lineBatch = MyLinesRenderer.CreateBatch();

            MatrixD inverseViewProj = MatrixShadowToWorldAt0Space;
            Vector3D.Transform(m_boxVertices, ref inverseViewProj, m_tmpDrawShadowVolume8D);

            for (int vertexIndex = 0; vertexIndex < 8; ++vertexIndex)
                m_tmpDrawShadowVolume8[vertexIndex] = m_tmpDrawShadowVolume8D[vertexIndex];

            MyPrimitivesRenderer.Draw6FacedConvexZ(m_tmpDrawShadowVolume8, DebugShadowVolumeColor, 0.2f);
            lineBatch.Add6FacedConvex(m_tmpDrawShadowVolume8, Color.Pink);

            lineBatch.Commit();
        }

        public MyShadowmapQuery GetShadowmapQueryForSingleShadow(int index, IDsvBindable dsvBindable)
        {
            MyProjectionInfo projInfo = new MyProjectionInfo();
            projInfo.WorldCameraOffsetPosition = MyRender11.Environment.Matrices.CameraPosition;
            projInfo.WorldToProjection = MatrixWorldAt0ToShadowSpace;
            projInfo.LocalToProjection = Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition) * MatrixWorldAt0ToShadowSpace;

            MyShadowmapQuery query = new MyShadowmapQuery();
            query.DepthBuffer = dsvBindable;
            query.Viewport = new MyViewport(dsvBindable.Size.X, dsvBindable.Size.Y);
            query.ProjectionInfo = projInfo;
            query.ProjectionFactor = (float)Math.Sqrt(dsvBindable.Size.X * dsvBindable.Size.Y / (m_matrixShadowToWorldAt0Space.Left.Length() * m_matrixShadowToWorldAt0Space.Up.Length()));

            query.QueryType = MyFrustumEnum.ShadowProjection;
            query.Index = index;
            return query;
        }

        public MyShadowmapQuery GetShadowmapQueryForCsm(IDsvBindable dsvBindable, int cascadeIndex)
        {
            MyProjectionInfo projInfo = new MyProjectionInfo();
            projInfo.WorldCameraOffsetPosition = MyRender11.Environment.Matrices.CameraPosition;
            projInfo.WorldToProjection = Matrix.CreateTranslation(-MyRender11.Environment.Matrices.CameraPosition) * MatrixWorldAt0ToShadowSpace;
            projInfo.LocalToProjection = MatrixWorldAt0ToShadowSpace;

            MyShadowmapQuery query = new MyShadowmapQuery();
            query.DepthBuffer = dsvBindable;
            query.Viewport = new MyViewport(dsvBindable.Size.X, dsvBindable.Size.Y);
            query.ProjectionInfo = projInfo;
            query.ProjectionFactor = (float)Math.Sqrt(dsvBindable.Size.X * dsvBindable.Size.Y / (m_matrixShadowToWorldAt0Space.Left.Length() * m_matrixShadowToWorldAt0Space.Up.Length()));

            query.QueryType = MyFrustumEnum.ShadowCascade;
            query.Index = cascadeIndex;
            return query;
        }
    }
}
