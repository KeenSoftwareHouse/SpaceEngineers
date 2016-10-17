using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    class MyCutsceneCamera : MyEntity, IMyCameraController
    {
        public float FOV = 70;

        public MyCutsceneCamera()
        {
            base.Init(null);
        }

        public void ControlCamera(MyCamera currentCamera)
        {
            currentCamera.FieldOfViewDegrees = FOV;
            currentCamera.SetViewMatrix(MatrixD.Invert(WorldMatrix));
        }

        public void Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
        }

        public void RotateStopped()
        {
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        public bool HandleUse()
        {
            return false;
        }

        public bool HandlePickUp()
        {
            return false;
        }

        public bool IsInFirstPersonView
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public bool ForceFirstPersonCamera
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public bool AllowCubeBuilding
        {
            get { return false; }
        }
    }
}
