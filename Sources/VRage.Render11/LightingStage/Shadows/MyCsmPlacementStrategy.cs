using System;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    interface ICsmPlacementStrategy
    {
        void Update(MyShadowVolume[] volumes, ref MyShadowsSettings settings, float shadowmapResolution);
    }

    static class MyCsmPlacementStrategyUtil
    {
        static Vector3D[] m_tmpCutPyramidExt4 = new Vector3D[4];
        static Vector3D[] m_tmpCutPyramidExt12 = new Vector3D[12];
        public static Vector3D[] CreateCutPyramidExt(double unitWidth, double unitHeight, double distBase, double distCut, double heightExt)
        {
            m_tmpCutPyramidExt4[0] = new Vector3D(-unitWidth, -unitHeight, -1);
            m_tmpCutPyramidExt4[1] = new Vector3D(-unitWidth, unitHeight, -1);
            m_tmpCutPyramidExt4[2] = new Vector3D(unitWidth, unitHeight, -1);
            m_tmpCutPyramidExt4[3] = new Vector3D(unitWidth, -unitHeight, -1);
            for (int vertexIndex = 0; vertexIndex < 4; ++vertexIndex)
            {
                m_tmpCutPyramidExt12[vertexIndex] = m_tmpCutPyramidExt4[vertexIndex] * distCut;
                m_tmpCutPyramidExt12[vertexIndex + 4] = m_tmpCutPyramidExt4[vertexIndex] * distBase;
                m_tmpCutPyramidExt12[vertexIndex + 8] = m_tmpCutPyramidExt4[vertexIndex] * distBase;
                m_tmpCutPyramidExt12[vertexIndex + 8].Z -= heightExt;
            }
            return m_tmpCutPyramidExt12;
        }

        public const uint VERTICES_IN_CONE_BASE = 16; // cone is used in stabilization of rotation
        static Vector3D[] m_tmpCutConeVerts = new Vector3D[VERTICES_IN_CONE_BASE * 3];
        public static Vector3D[] CreateCutConeExt(double unitRadius, double distBase, double distCut, double heightExt)
        {
            for (uint i = 0; i < VERTICES_IN_CONE_BASE; i++)
            {
                double angle = ((double)i) / VERTICES_IN_CONE_BASE * Math.PI * 2;
                Vector3D vertMult = new Vector3D(Math.Sin(angle) * unitRadius, Math.Cos(angle) * unitRadius, -1);
                m_tmpCutConeVerts[i * 3] = vertMult * distCut;
                m_tmpCutConeVerts[i * 3 + 1] = vertMult * distBase;
                m_tmpCutConeVerts[i * 3 + 2] = vertMult * distBase;
                m_tmpCutConeVerts[i * 3 + 2].Z -= heightExt;
            }
            return m_tmpCutConeVerts;
        }
    }

    class MyCsmOldPlacementStrategy : ICsmPlacementStrategy
    {
        static Vector3D[] m_cornersCS = new Vector3D[8]
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

        static float[] m_shadowCascadeSplitDepths;
        static readonly MyTuple<int, int>[] m_shadowCascadeUpdateIntervals = new MyTuple<int, int>[8] {
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(4, 3),
            MyTuple.Create(8, 5),
            MyTuple.Create(8, 6),
            MyTuple.Create(8, 7),};
        static Vector3D[] m_shadowCascadeUpdatePositions;
        static Vector3D[] m_frustumVerticesWS = new Vector3D[8];

        static int[] m_shadowCascadeFramesSinceLightUpdate;
        static Vector3D[] m_shadowCascadeLightDirections;

        public unsafe void Update(MyShadowVolume[] volumes, ref MyShadowsSettings settings, float shadowmapResolution)
        {
            bool stabilize = true;
            float cascadesNearClip = 1f;
            float shadowChangeDelayMultiplier = 180;
            const float directionDifferenceThreshold = 0.0175f;

            float backOffset = MyRender11.Settings.User.ShadowQuality.BackOffset();
            float shadowmapSize = MyRender11.Settings.User.ShadowQuality.ShadowCascadeResolution();

            Array.Resize(ref m_shadowCascadeSplitDepths, volumes.Length + 1);
            Array.Resize(ref m_shadowCascadeUpdatePositions, volumes.Length);
            Array.Resize(ref m_shadowCascadeFramesSinceLightUpdate, volumes.Length);
            Array.Resize(ref m_shadowCascadeLightDirections, volumes.Length);

            for (int cascadeIndex = 0; cascadeIndex < volumes.Length; ++cascadeIndex)
                m_shadowCascadeSplitDepths[cascadeIndex] = MyRender11.Settings.User.ShadowQuality.ShadowCascadeSplit(cascadeIndex);

            double unitWidth = 1.0 / MyRender11.Environment.Matrices.Projection.M11;
            double unitHeight = 1.0 / MyRender11.Environment.Matrices.Projection.M22;

            Vector3D* untransformedVertices = stackalloc Vector3D[4];
            untransformedVertices[0] = new Vector3D(-unitWidth, -unitHeight, -1);
            untransformedVertices[1] = new Vector3D(-unitWidth, unitHeight, -1);
            untransformedVertices[2] = new Vector3D(unitWidth, unitHeight, -1);
            untransformedVertices[3] = new Vector3D(unitWidth, -unitHeight, -1);

            MatrixD* cascadesMatrices = stackalloc MatrixD[volumes.Length];

            for (int cascadeIndex = 0; cascadeIndex < volumes.Length; ++cascadeIndex)
            {
                ++m_shadowCascadeFramesSinceLightUpdate[cascadeIndex];

                if (m_shadowCascadeFramesSinceLightUpdate[cascadeIndex] > cascadeIndex * shadowChangeDelayMultiplier ||
                    MyRender11.Environment.Data.EnvironmentLight.SunLightDirection.Dot(m_shadowCascadeLightDirections[cascadeIndex]) < (1 - directionDifferenceThreshold))
                {
                    m_shadowCascadeLightDirections[cascadeIndex] = MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
                    m_shadowCascadeFramesSinceLightUpdate[cascadeIndex] = 0;
                }
            }

            for (int cascadeIndex = 0; cascadeIndex < volumes.Length; ++cascadeIndex)
            {
                for (int vertexIndex = 0; vertexIndex < 4; ++vertexIndex)
                {
                    m_frustumVerticesWS[vertexIndex] = untransformedVertices[vertexIndex] * m_shadowCascadeSplitDepths[cascadeIndex];
                    m_frustumVerticesWS[vertexIndex + 4] = untransformedVertices[vertexIndex] * m_shadowCascadeSplitDepths[cascadeIndex + 1];
                }

                bool skipCascade = MyCommon.FrameCounter % (ulong)m_shadowCascadeUpdateIntervals[cascadeIndex].Item1 != (ulong)m_shadowCascadeUpdateIntervals[cascadeIndex].Item2;
                bool forceUpdate = m_shadowCascadeSplitDepths[cascadeIndex] > 1000f && Vector3D.DistanceSquared(m_shadowCascadeUpdatePositions[cascadeIndex], MyRender11.Environment.Matrices.CameraPosition) > Math.Pow(1000, 2);
                // 
                if (!forceUpdate && skipCascade && !settings.Data.UpdateCascadesEveryFrame)
                    continue;
                //if (settings.ShadowCascadeFrozen[cascadeIndex])
                //    continue;

                m_shadowCascadeUpdatePositions[cascadeIndex] = MyRender11.Environment.Matrices.CameraPosition;

                MatrixD invView = MyRender11.Environment.Matrices.InvView;
                Vector3D.Transform(m_frustumVerticesWS, ref invView, m_frustumVerticesWS);

                var bSphere = BoundingSphereD.CreateFromPoints(m_frustumVerticesWS);
                if (stabilize)
                {
                    bSphere.Center = bSphere.Center.Round();
                    bSphere.Radius = Math.Ceiling(bSphere.Radius);
                }

                var shadowCameraPosWS = bSphere.Center + m_shadowCascadeLightDirections[cascadeIndex] * (bSphere.Radius + cascadesNearClip);

                var lightView = VRageMath.MatrixD.CreateLookAt(shadowCameraPosWS, shadowCameraPosWS - m_shadowCascadeLightDirections[cascadeIndex], Math.Abs(Vector3.UnitY.Dot(m_shadowCascadeLightDirections[cascadeIndex])) < 0.99f ? Vector3.UnitY : Vector3.UnitX);
                var offset = bSphere.Radius + cascadesNearClip + backOffset;

                Vector3D vMin = new Vector3D(-bSphere.Radius, -bSphere.Radius, cascadesNearClip);
                Vector3D vMax = new Vector3D(bSphere.Radius, bSphere.Radius, offset + bSphere.Radius);

                var cascadeProjection = MatrixD.CreateOrthographicOffCenter(vMin.X, vMax.X, vMin.Y, vMax.Y, vMax.Z, vMin.Z);
                cascadesMatrices[cascadeIndex] = lightView * cascadeProjection;

                var transformed = Vector3D.Transform(Vector3D.Zero, cascadesMatrices[cascadeIndex]) * shadowmapSize / 2;
                var smOffset = (transformed.Round() - transformed) * 2 / shadowmapSize;

                // stabilize 1st cascade only
                if (stabilize)
                {
                    cascadeProjection.M41 += smOffset.X;
                    cascadeProjection.M42 += smOffset.Y;
                    cascadesMatrices[cascadeIndex] = lightView * cascadeProjection;
                }

                Matrix matrixTranslation = Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition);
                cascadesMatrices[cascadeIndex] = matrixTranslation * cascadesMatrices[cascadeIndex];
                volumes[cascadeIndex].SetMatrixWorldAt0ToShadow(cascadesMatrices[cascadeIndex]);
            }
        }
    }

    class MyCsmRigidPlacementStrategy : ICsmPlacementStrategy
    {
        bool STABILIZE_MOVEMENT = true;
        float GetStableShadowVolumeSize(Vector3D[] verticesPos, float shadowmapResolution)
        {
            BoundingSphere boundingSphere = new BoundingSphere(Vector3.Zero, float.MinValue);
            BoundingBox boundingBox = BoundingBox.CreateInvalid();

            foreach (var pos in verticesPos)
            {
                boundingBox.Min = Vector3.Min(boundingBox.Min, pos);
                boundingBox.Max = Vector3.Max(boundingBox.Max, pos);
            }

            boundingSphere = BoundingSphere.CreateFromBoundingBox(boundingBox);

            float baseSize = boundingSphere.Radius * 2;

            baseSize *= 1 + 1.0f / shadowmapResolution;

            return baseSize;
        }

        Vector3 m_sunDirection = Vector3.Forward;

        Vector3D[] m_tmpCreateOrthoVertsLS = new Vector3D[MyCsmPlacementStrategyUtil.VERTICES_IN_CONE_BASE*3];

        double RoundToPrecision(double value, double precision)
        {
            double modif = value / precision;
            double floored = Math.Round(modif);
            double output = floored * precision;
            return output;
        }

        Vector3D RoundToPrecision(Vector3D value, double precision)
        {
            return new Vector3D(RoundToPrecision(value.X, precision), RoundToPrecision(value.Y, precision), RoundToPrecision(value.Z, precision));
        }

        //double IntersectionLineAndSphere(Vector3D centerPoint, double radius, Vector3D lineOrigin, Vector3D lineDir)
        //{
        //    // https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection
        //    Vector3D c = centerPoint;
        //    double r = radius;
        //    Vector3D o = lineOrigin;
        //    Vector3D l = lineDir;

        //    double sq1 = Math.Pow(Vector3D.Dot((o - c)*(o - c), l), 2);
        //    double sq2 = l.LengthSquared() * ((o - c).LengthSquared() - r*r);
        //    double sq = sq1 - sq2;
        //    MyRenderProxy.Assert(sq >= 0, "the line needs to be crossing sphere! Some internal bug...");
        //    double d1 = Vector3D.Dot(l, o - c);
        //    return -d1 + Math.Sqrt(sq);

        //}

        MatrixD CreateRigidLightMatrix(Vector3D[] verts, Vector3D lightDir, double zOffset)
        {
            // Create orthoghonal vectors:
            Vector3D lightUp = Math.Abs(Vector3.UnitX.Dot(lightDir)) < 0.99f ? Vector3.UnitX : Vector3.UnitY;
            Vector3D lightRight = Vector3.Cross(lightDir, lightUp);
            lightUp = Vector3D.Cross(lightRight, lightDir);
            lightUp.Normalize();
            lightRight.Normalize();

            // Look for the most compact encapsulation of verts: ... vMin and vMax will contain corners of encapsulation box with sides defined lightUp, lightRight,and lightDir
            var lightMatrix = MatrixD.CreateLookAt(new Vector3D(0, 0, 0), lightDir, lightUp);
            var invLightMatrix = MatrixD.Invert(lightMatrix);
            Vector3D.Transform(verts, ref lightMatrix, m_tmpCreateOrthoVertsLS);
            Vector3D vMinLS = m_tmpCreateOrthoVertsLS[0];
            Vector3D vMaxLS = m_tmpCreateOrthoVertsLS[0];
            for (uint i = 1; i < verts.Length; i++)
            {
                vMinLS = Vector3D.Min(vMinLS, m_tmpCreateOrthoVertsLS[i]);
                vMaxLS = Vector3D.Max(vMaxLS, m_tmpCreateOrthoVertsLS[i]);
            }
            Vector3D vMinWS = Vector3D.Transform(vMinLS, ref invLightMatrix);
            Vector3D vMaxWS = Vector3D.Transform(vMaxLS, ref invLightMatrix);
 
            // Create matrices:
            Vector3D vMinOffsetLS = -(vMaxLS - vMinLS)/2;
            Vector3D vMaxOffsetLS = (vMaxLS - vMinLS)/2;
            Vector3D vCenterWS = (vMaxWS + vMinWS) / 2;

            MatrixD matrixLightPlacer = MatrixD.CreateLookAt(vCenterWS, vCenterWS - lightDir, lightUp);
            MatrixD matrixLightSizer = MatrixD.CreateOrthographicOffCenter(vMinOffsetLS.X, vMaxOffsetLS.X,
                vMinOffsetLS.Y, vMaxOffsetLS.Y, vMaxOffsetLS.Z + zOffset, vMinOffsetLS.Z);

            return matrixLightPlacer*matrixLightSizer;
        }

        Vector3D StabilizePosition(Vector3D pos, Vector3D lightDir, Vector3D lightUp, double shadowVolumeSize, double shadowmapResolution)
        {
            // stabilize position:
            Vector3D volumeRight = Vector3D.Cross(lightDir, lightUp);
            volumeRight.Normalize();
            Vector3D volumeUp = Vector3D.Cross(lightDir, volumeRight);
            volumeUp.Normalize();
            Vector3D volumeForward = Vector3D.Cross(volumeRight, volumeUp);
            volumeForward.Normalize();

            double right = Vector3D.Dot(pos, volumeRight);
            double up = Vector3D.Dot(pos, volumeUp);
            double forward = Vector3D.Dot(pos, volumeForward);
            double texelSize = shadowVolumeSize / shadowmapResolution;

            up = RoundToPrecision(up, texelSize);
            right = RoundToPrecision(right, texelSize);
            forward = RoundToPrecision(forward, texelSize);

            return volumeRight * right + volumeUp * up + volumeForward * forward;
        }

        static Vector3D[] m_tmpMainDir = new Vector3D[3] {Vector3D.Right, Vector3D.Up, Vector3D.Forward};
        MatrixD CreateStableLightMatrix(Vector3D[] verts, Vector3D lightDir, double zOffset, double shadowVolumeSize, double shadowmapResolution)
        {
            // Create orthoghonal vectors:
            Vector3D lightUp = Math.Abs(Vector3.UnitX.Dot(lightDir)) < 0.99f ? Vector3.UnitX : Vector3.UnitY;
            Vector3D lightRight = Vector3.Cross(lightDir, lightUp);
            lightUp = Vector3D.Cross(lightRight, lightDir);

            // Look for the most compact encapsulation of verts: ... vMin and vMax will contain corners of encapsulation box with sides defined lightUp, lightRight,and lightDir
            var lightMatrix = MatrixD.CreateLookAt(new Vector3D(0, 0, 0), lightDir, lightUp);
            var invLightMatrix = MatrixD.Invert(lightMatrix);
            Vector3D.Transform(verts, ref lightMatrix, m_tmpCreateOrthoVertsLS);
            Vector3D vMinLS = m_tmpCreateOrthoVertsLS[0];
            Vector3D vMaxLS = m_tmpCreateOrthoVertsLS[0];
            for (uint i = 1; i < verts.Length; i++)
            {
                vMinLS = Vector3D.Min(vMinLS, m_tmpCreateOrthoVertsLS[i]);
                vMaxLS = Vector3D.Max(vMaxLS, m_tmpCreateOrthoVertsLS[i]);
            }
            Vector3D vMinWS = Vector3D.Transform(vMinLS, ref invLightMatrix);
            Vector3D vMaxWS = Vector3D.Transform(vMaxLS, ref invLightMatrix);

            // Create matrices:
            Vector3D vMinOffsetLS = -(vMaxLS - vMinLS) / 2;
            Vector3D vMaxOffsetLS = (vMaxLS - vMinLS) / 2;
            Vector3D vCenterWS = (vMaxWS + vMinWS) / 2;

            // this is screwed. it is required to implement it more stable
            Vector3D vMinOffsetStable = new Vector3D(-shadowVolumeSize / 2, -shadowVolumeSize / 2, -shadowVolumeSize / 2);
            Vector3D vMaxOffsetStable = new Vector3D(shadowVolumeSize / 2, shadowVolumeSize / 2, shadowVolumeSize / 2);

            Vector3D viewDirWS = MyRender11.Environment.Matrices.InvViewAt0.Forward;
            Vector3D viewDirLS = Vector3D.Transform(viewDirWS, lightMatrix);

            Vector3D missingSizeLS = new Vector3D(shadowVolumeSize, shadowVolumeSize, shadowVolumeSize) - (vMaxLS - vMinLS);
            missingSizeLS *= 0.99f; // <- fixing bugs caused by imprecise arithmetic calculation
            for (int i = 0; i < m_tmpMainDir.Length; i++)
            {
                Vector3D dir = m_tmpMainDir[i];
                double offsetScalar = Vector3D.Dot(viewDirLS, dir*missingSizeLS);
                offsetScalar = Math.Abs(offsetScalar);
                Vector3D offsetVector = m_tmpMainDir[i] * offsetScalar / 2;
                if (Vector3D.Dot(offsetVector, viewDirLS) < 0)
                    offsetVector = -offsetVector;
                vMinOffsetStable -= offsetVector;
                vMaxOffsetStable -= offsetVector;
            }

            vCenterWS = StabilizePosition(vCenterWS, lightDir, lightUp, shadowVolumeSize, shadowmapResolution);
            vMinOffsetLS = RoundToPrecision(vMinOffsetStable, shadowVolumeSize / shadowmapResolution);
            vMaxOffsetLS = RoundToPrecision(vMaxOffsetStable, shadowVolumeSize / shadowmapResolution);

            MatrixD matrixLightPlacer = MatrixD.CreateLookAt(vCenterWS, vCenterWS - lightDir, lightUp);
            MatrixD matrixLightSizer = MatrixD.CreateOrthographicOffCenter(vMinOffsetLS.X, vMaxOffsetLS.X,
                vMinOffsetLS.Y, vMaxOffsetLS.Y, vMaxOffsetLS.Z + zOffset, vMinOffsetLS.Z);

            return matrixLightPlacer * matrixLightSizer; 
        }

        public void Update(MyShadowVolume[] volumes, ref MyShadowsSettings settings, float shadowmapResolution)
        {
            // Update sun position:
            if (!settings.NewData.FreezeSunDirection)
            {
                Vector3D currentSunLightDir = MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
                Vector3D prevSunLightDir = m_sunDirection;
                currentSunLightDir.Normalize();
                prevSunLightDir.Normalize();
                double diffAngle = 360.0 / Math.PI * Math.Acos(Vector3D.Dot(prevSunLightDir, currentSunLightDir));
                if (diffAngle >= settings.NewData.SunAngleThreshold)
                    m_sunDirection = MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
            }

            double unitWidth = 1.0/MyRender11.Environment.Matrices.Projection.M11;
            double unitHeight = 1.0/MyRender11.Environment.Matrices.Projection.M22;
            for (int cascadeIndex = 0; cascadeIndex < volumes.Length; ++cascadeIndex)
            {
                // Update primitive vertices that are inside of frustrum matrix:
                float baseDist = settings.Cascades[cascadeIndex].FullCoverageDepth;
                float baseCut = (cascadeIndex == 0) ? 0 : settings.Cascades[cascadeIndex - 1].FullCoverageDepth;
                Vector3D[] verts;
                float extDepth = settings.Cascades[cascadeIndex].ExtendedCoverageDepth;

                if (!settings.NewData.StabilizeRotation)
                    verts = MyCsmPlacementStrategyUtil.CreateCutPyramidExt(unitWidth, unitHeight, baseDist, baseCut, extDepth);
                else
                {
                    double coneRadius = Math.Sqrt((unitWidth * unitWidth) + (unitHeight * unitHeight));
                    verts = MyCsmPlacementStrategyUtil.CreateCutConeExt(coneRadius, baseDist, baseCut, extDepth);
                }
                double shadowVolumeSize = GetStableShadowVolumeSize(verts, shadowmapResolution);

                MatrixD invView = MyRender11.Environment.Matrices.InvView;
                Vector3D.Transform(verts, ref invView, verts);

                float zOffset = settings.NewData.ZOffset;

                MatrixD shadowMatrixWorld;
                if (!settings.NewData.StabilizeMovement)
                    shadowMatrixWorld = CreateRigidLightMatrix(verts, m_sunDirection, zOffset);
                else
                    shadowMatrixWorld = CreateStableLightMatrix(verts, m_sunDirection, zOffset, shadowVolumeSize, shadowmapResolution);

                Matrix matrixTranslation = Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition);
                Matrix shadowMatrixWorldAt0 = matrixTranslation*shadowMatrixWorld;
                volumes[cascadeIndex].SetMatrixWorldAt0ToShadow(shadowMatrixWorldAt0);
            }
        }
    }
}
