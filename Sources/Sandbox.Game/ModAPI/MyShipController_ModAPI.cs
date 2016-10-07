using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyShipController
    {
        IMyEntity VRage.Game.ModAPI.Interfaces.IMyControllableEntity.Entity
        {
            get { return Entity; }
        }

        void VRage.Game.ModAPI.Interfaces.IMyControllableEntity.DrawHud(IMyCameraController camera, long playerId)
        {
            if (camera != null)
            {
                DrawHud(camera, playerId);
            }
        }

        public Vector3D GetNaturalGravity()
        {
            return MyGravityProviderSystem.CalculateNaturalGravityInPoint(WorldMatrix.Translation);
        }

        public Vector3D GetArtificialGravity()
        {
            return MyGravityProviderSystem.CalculateArtificialGravityInPoint(WorldMatrix.Translation);
        }

        public Vector3D GetTotalGravity()
        {
            return MyGravityProviderSystem.CalculateTotalGravityInPoint(WorldMatrix.Translation);
        }

        double IMyShipController.GetShipSpeed()
        {
            var physics = Parent != null ? Parent.Physics : null;
            var linearVelocity = physics == null ? Vector3D.Zero : new Vector3D(physics.LinearVelocity);
            return linearVelocity.Length();
        }

        MyShipVelocities ModAPI.Ingame.IMyShipController.GetShipVelocities()
        {
            var physics = Parent != null ? Parent.Physics : null;
            var linearVelocity = physics == null ? Vector3D.Zero : new Vector3D(physics.LinearVelocity);
            var angularVelocity = physics == null ? Vector3D.Zero : new Vector3D(physics.AngularVelocity);
            return new MyShipVelocities(linearVelocity, angularVelocity);
        }

        public MyShipMass CalculateShipMass()
        {
            int baseMass;
            var totalMass = CubeGrid.GetCurrentMass(out baseMass, Pilot);
            return new MyShipMass(baseMass, totalMass);
        }
    }
}
