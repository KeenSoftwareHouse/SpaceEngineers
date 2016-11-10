using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender
{
    class MyEnvironmentMatrices
    {
        internal Vector3D CameraPosition;
        internal Matrix ViewAt0;
        internal Matrix InvViewAt0;
        internal Matrix ViewProjectionAt0;
        internal Matrix InvViewProjectionAt0;

        internal Matrix View;
        internal Matrix InvView;
        internal Matrix Projection;
        internal Matrix InvProjection;
        internal Matrix ViewProjection;
        internal Matrix InvViewProjection;

        internal MatrixD ViewD;
        //internal MatrixD InvViewD;
        internal MatrixD OriginalProjectionD;
        //internal MatrixD InvProjectionD;
        internal MatrixD ViewProjectionD;
        //internal MatrixD InvViewProjectionD;

        internal BoundingFrustumD ViewFrustumD;
        internal BoundingFrustumD ViewFrustumClippedD;

        internal float NearClipping;
        internal float LargeDistanceFarClipping;
        internal float FarClipping;

        /// <summary>Field of view Horizontal</summary>
        internal float FovH;

        /// <summary>Field of view Vertical</summary>
        internal float FovV;
    }

    class MyEnvironment
    {
        internal MyEnvironmentMatrices Matrices = new MyEnvironmentMatrices();
        internal MyEnvironmentData Data;
        internal MyRenderFogSettings Fog;
    }
}
