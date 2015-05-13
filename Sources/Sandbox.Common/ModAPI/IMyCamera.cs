using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyCamera
    {
        Vector3D PreviousPosition { get; }
        Vector2 ViewportOffset { get; }                    //  Current viewport
        Vector2 ViewportSize { get; }
        MatrixD ViewMatrix { get; }                    //  This is view matrix when camera in real position
        MatrixD WorldMatrix { get; }
        MatrixD ProjectionMatrix { get; }
        MatrixD ProjectionMatrixForNearObjects { get; }

        float NearPlaneDistance { get; }
        float FarPlaneDistance { get; } // farplane is now set by MyObjectBuilder_SessionSettings.ViewDistance
        float NearForNearObjects { get; }
        float FarForNearObjects { get; }

        float FieldOfViewAngle { get; }
        float FieldOfViewAngleForNearObjects { get; }
        float FovWithZoom { get; }
        float FovWithZoomForNearObjects { get; }
        double GetDistanceWithFOV(VRageMath.Vector3D position);
        bool IsInFrustum(ref VRageMath.BoundingBoxD boundingBox);
        bool IsInFrustum(ref VRageMath.BoundingSphereD boundingSphere);
        bool IsInFrustum(VRageMath.BoundingBoxD boundingBox);
    }
}
