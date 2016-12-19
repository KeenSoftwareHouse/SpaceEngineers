using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCameraBlock:IMyFunctionalBlock
    {
        /// <summary>
        /// Does a raycast in the direction the camera is facing. Pitch and Yaw are in degrees. 
        /// Will return an empty struct if distance or angle are out of bounds.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <returns></returns>
        MyDetectedEntityInfo Raycast(double distance, float pitch = 0, float yaw = 0);

        /// <summary>
        /// Does a raycast to the given point. 
        /// Will return an empty struct if distance or angle are out of bounds.
        /// </summary>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        MyDetectedEntityInfo Raycast(Vector3D targetPos);

        /// <summary>
        /// Does a raycast in the given direction. 
        /// Will return an empty struct if distance or angle are out of bounds.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="targetDirection"></param>
        /// <returns></returns>
        MyDetectedEntityInfo Raycast(double distance, Vector3D targetDirection);

        /// <summary>
        /// The maximum distance that this camera can scan, based on the time since the last scan.
        /// </summary>
        double AvailableScanRange { get; }

        /// <summary>
        /// When this is true, the available raycast distance will count up, and power usage is increased.
        /// </summary>
        bool EnableRaycast { get; set; }

        /// <summary>
        /// Checks if the camera can scan the given distance.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        bool CanScan(double distance);

        /// <summary>
        /// Returns the number of milliseconds until the camera can do a raycast of the given distance.
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        int TimeUntilScan(double distance);
        
        /// <summary>
        /// Returns the maximum positive angle you can apply for pitch and yaw.
        /// </summary>
        float RaycastConeLimit { get; }

        /// <summary>
        /// Returns the maximum distance you can request a raycast. -1 means infinite.
        /// </summary>
        double RaycastDistanceLimit { get; }
    }
}
