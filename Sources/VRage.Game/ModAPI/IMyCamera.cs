using System;
using VRageMath;

namespace VRage.ModAPI
{
    public interface IMyCamera
    {
        Vector3D Position { get; }
        Vector3D PreviousPosition { get; }
        Vector2 ViewportOffset { get; }                    //  Current viewport
        Vector2 ViewportSize { get; }
        MatrixD ViewMatrix { get; }                    //  This is view matrix when camera in real position
        MatrixD WorldMatrix { get; }
        MatrixD ProjectionMatrix { get; }
        
        float NearPlaneDistance { get; }
        float FarPlaneDistance { get; } // farplane is now set by MyObjectBuilder_SessionSettings.ViewDistance
        float NearForNearObjects { get; }
        float FarForNearObjects { get; }

        float FieldOfViewAngle { get; }
        float FovWithZoom { get; }

        double GetDistanceWithFOV(VRageMath.Vector3D position);  // GetDistanceFromPoint
        bool IsInFrustum(ref VRageMath.BoundingBoxD boundingBox);
        bool IsInFrustum(ref VRageMath.BoundingSphereD boundingSphere);
        bool IsInFrustum(VRageMath.BoundingBoxD boundingBox);
        /// <summary>
        /// Gets screen coordinates of 3d world pos in 0 - 1 distance where 1.0 is screen width(for X) or height(for Y).
        /// WARNING: Y is from bottom to top.
        /// </summary>
        /// <param name="worldPos">World position.</param>
        /// <returns>Screen coordinate in 0-1 distance.</returns>
        Vector3D WorldToScreen(ref Vector3D worldPos);
        LineD WorldLineFromScreen(Vector2 screenCoords);

        [Obsolete]
        MatrixD ProjectionMatrixForNearObjects { get; }
        [Obsolete]
        float FieldOfViewAngleForNearObjects { get; }
        [Obsolete]
        float FovWithZoomForNearObjects { get; }
    }
}
