using System;
using VRageMath;

namespace VRage.ModAPI
{
    /// <summary>
    /// Links to modapi actions. Delegates are set inside MyAPIGateway.
    /// VRAGE TODO: This is probably a temporary class helping us to remove sandbox.
    /// </summary>
    public static class MyAPIGatewayShortcuts
    {
        // TODO: REMOVE THESE TWO, USE MyEntitiesInterface.Register/Unregister...
        /// <summary>
        /// Registers entity in update loop.
        /// Parameters: IMyEntity entity (ref to entity to be registered)
        /// </summary>
        public static Action<IMyEntity> RegisterEntityUpdate = null;
        /// <summary>
        /// Unregisters entity from update loop.
        /// Parameters: IMyEntity entity (ref to entity to be unregistered), bool immediate (default is false)
        /// </summary>
        public static Action<IMyEntity, bool> UnregisterEntityUpdate = null;

        // Obtain current camera. Calls IMySession.GetCamera
        public delegate IMyCamera GetMainCameraCallback();
        public static GetMainCameraCallback GetMainCamera = null;

        // Obtain world bounding box.
        public delegate BoundingBoxD GetWorldBoundariesCallback();
        public static GetWorldBoundariesCallback GetWorldBoundaries = null;

        // Obtain world position of local player.
        public delegate Vector3D GetLocalPlayerPositionCallback();
        public static GetLocalPlayerPositionCallback GetLocalPlayerPosition = null;
    }
}
