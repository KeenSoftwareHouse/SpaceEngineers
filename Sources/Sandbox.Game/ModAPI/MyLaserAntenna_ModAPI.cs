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
                this.sync.PasteCoordinates(coords);
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

        ModAPI.Ingame.IMyLaserAntenna ModAPI.Ingame.IMyLaserAntenna.OtherAntenna
        {
            get
            {
                MyLaserAntenna otherend = GetOther();

                // perform Los test, and check for ownership
                if (otherend != null && LosTests(otherend) && otherend.HasLocalPlayerAccess())
                    return otherend;

                return null;
            }
        }
    }
}
