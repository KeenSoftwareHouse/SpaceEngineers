using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyLaserAntenna : IMyLaserAntenna
    {
        Vector3D ModAPI.Ingame.IMyLaserAntenna.TargetCoords
        {
            get
            {
                return m_targetCoords;
            }
        }

        void ModAPI.Ingame.IMyLaserAntenna.SetTargetCoords(string coords)
        {
            if (coords != null)
            {
                PasteCoordinates(coords);
            }
        }

        void ModAPI.Ingame.IMyLaserAntenna.Connect()
        {
            if (CanConnectToGPS())
            {
                ConnectToGps();
            }
        }

        bool ModAPI.Ingame.IMyLaserAntenna.IsPermanent
        {
            get {  return m_IsPermanent; }
        }

        bool IMyLaserAntenna.RequireLoS
        {
            get { return m_needLineOfSight; }
        }

        bool ModAPI.Ingame.IMyLaserAntenna.IsOutsideLimits
        {
            get { return m_outsideLimits; }
        }
    }
}
