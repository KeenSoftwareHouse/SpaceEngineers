using VRage.Game.Utils;
using VRageMath;

namespace VRage.Game.ModAPI.Interfaces
{
    public interface IMyCameraController
    {
        /// <summary>
        /// Change camera properties now.
        /// Communication: from controller to camera.
        /// </summary>
        /// <param name="currentCamera"></param>
        void ControlCamera(MyCamera currentCamera);

        /// <summary>
        /// Rotate camera controller.
        /// </summary>
        void Rotate(Vector2 rotationIndicator, float rollIndicator);
        /// <summary>
        /// Rotation of camera controller stopped.
        /// </summary>
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
        bool HandlePickUp();

        bool IsInFirstPersonView { get; set; }
        bool ForceFirstPersonCamera { get; set; }

        bool AllowCubeBuilding { get; }
    }
}
