using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyCameraController
    {
        MatrixD GetViewMatrix();

        void Rotate(Vector2 rotationIndicator, float rollIndicator);

        void RotateStopped();

        void OnAssumeControl(IMyCameraController previousCameraController);
        void OnReleaseControl(IMyCameraController newCameraController);


        /// <summary>
        /// Used to send "use" commands to camera controller
        /// </summary>
        /// <returns>
        /// Return value indicates if the camera controller handled the use button.
        /// If not, it should fall to ControlledObject
        /// </returns>
        bool HandleUse();

        bool IsInFirstPersonView { get; set; }
        bool ForceFirstPersonCamera { get; set; }

        bool AllowCubeBuilding { get; }
    }
}
