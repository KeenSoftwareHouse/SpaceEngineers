using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    partial class MyLaserAntenna : IMyLaserAntenna
    {
        Vector3D IMyLaserAntenna.TargetCoords
        {
            get
            {
                return m_targetCoords;
            }
        }

        void IMyLaserAntenna.SetTargetCoords(string coords)
        {
            if (coords != null)
            {
                this.sync.PasteCoordinates(coords);
            }
        }

        void IMyLaserAntenna.Connect()
        {
            if (CanConnectToGPS())
            {
                ConnectToGps();
            }
        }

        bool IMyLaserAntenna.IsPermanent
        {
            get {  return m_IsPermanent; }
        }
    }
}
