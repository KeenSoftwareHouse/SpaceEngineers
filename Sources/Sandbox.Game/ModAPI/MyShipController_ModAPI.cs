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

        bool IMyShipController.TryGetPlanetPosition(out Vector3D position)
        {
            var blockPosition = this.PositionComp.GetPosition();
            if (!MyGravityProviderSystem.IsPositionInNaturalGravity(blockPosition))
            {
                position = Vector3D.Zero;
                return false;
            }
            var boundingBox = PositionComp.WorldAABB;
            var nearestPlanet = MyGamePruningStructure.GetClosestPlanet(ref boundingBox);
            if (nearestPlanet == null)
            {
                position = Vector3D.Zero;
                return false;
            }
            position = nearestPlanet.PositionComp.GetPosition();
            return true;
        }

        bool IMyShipController.TryGetPlanetElevation(MyPlanetElevation detail, out double elevation)
        {
            var blockPosition = this.PositionComp.GetPosition();
            if (!MyGravityProviderSystem.IsPositionInNaturalGravity(blockPosition))
            {
                elevation = double.PositiveInfinity;
                return false;
            }            
            var boundingBox = PositionComp.WorldAABB;
            var nearestPlanet = MyGamePruningStructure.GetClosestPlanet(ref boundingBox);
            if (nearestPlanet == null)
            {
                elevation = double.PositiveInfinity;
                return false;
            }

            switch (detail)
            {
                case MyPlanetElevation.Sealevel:
                    elevation = ((boundingBox.Center - nearestPlanet.PositionComp.GetPosition()).Length() - nearestPlanet.AverageRadius);
                    return true;

                case MyPlanetElevation.Surface:
                    var controlledEntityPosition = CubeGrid.Physics.CenterOfMassWorld;
                    Vector3D closestPoint = nearestPlanet.GetClosestSurfacePointGlobal(ref controlledEntityPosition);
                    elevation = Vector3D.Distance(closestPoint, controlledEntityPosition);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException("detail", detail, null);
            }
        }
    }
}
