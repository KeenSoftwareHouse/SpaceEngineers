using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using VRage.Components;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
	class MyThrusterBlockThrustComponent : MyEntityThrustComponent
	{
		new MyCubeGrid Entity { get { return base.Entity as MyCubeGrid; } }
		MyCubeGrid CubeGrid { get { return Entity; } }

		// Levitation period length in seconds
		private float m_levitationPeriodLength = 1.3f;
		private float m_levitationTorqueCoeficient = 0.25f;

		protected override void UpdateThrusts()
		{
			base.UpdateThrusts();

			if (CubeGrid != null && CubeGrid.Physics != null &&
				(CubeGrid.GridSystems.ControlSystem.IsLocallyControlled
				|| (!CubeGrid.GridSystems.ControlSystem.IsControlled)
				|| (CubeGrid.GridSystems.ControlSystem.IsControlled && (MySector.MainCamera.IsInFrustum(CubeGrid.PositionComp.WorldAABB) || (CubeGrid.PositionComp.GetPosition() - MySector.MainCamera.Position).LengthSquared() < 100f))))
			{
                if (CubeGrid.Physics.Enabled)
                {
                    if (FinalThrust.LengthSquared() > 0.001f)
                    {
                        var thrust = FinalThrust;
                        if (CubeGrid.Physics.IsWelded) //only reliable variant
                        {
                            thrust = Vector3.TransformNormal(thrust, CubeGrid.WorldMatrix);
                            thrust = Vector3.TransformNormal(thrust, Matrix.Invert(CubeGrid.Physics.RigidBody.GetRigidBodyMatrix()));
                        }
                        CubeGrid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, thrust, null, null);
                    }

                    const float stoppingVelocitySq = 0.001f * 0.001f;
                    if (CubeGrid.Physics.LinearVelocity != Vector3.Zero && CubeGrid.Physics.LinearVelocity.LengthSquared() < stoppingVelocitySq && CubeGrid.Physics.RigidBody.IsActive)
                    {
                        CubeGrid.Physics.LinearVelocity = Vector3.Zero;
                    }
                }
			}
		}

	    public override void Register(MyEntity entity, Vector3I forwardVector)
		{
			var thrust = entity as MyThrust;
			if (thrust == null)
				return;

			base.Register(entity, forwardVector);

	        MyDefinitionId fuelType = FuelType(entity);
	        int typeIndex = GetTypeIndex(ref fuelType);
	        m_dataByFuelType[typeIndex].EnergyDensity = thrust.FuelDefinition.EnergyDensity;
	        m_dataByFuelType[typeIndex].Efficiency = thrust.BlockDefinition.FuelConverter.Efficiency;

            ResourceSink.SetMaxRequiredInputByType(fuelType, ResourceSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, thrust.MaxPowerConsumption));

			thrust.EnabledChanged += thrust_EnabledChanged;
			thrust.SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
		    SlowdownFactor = Math.Max(thrust.BlockDefinition.SlowdownFactor, SlowdownFactor);
		}

		public override void Unregister(MyEntity entity, Vector3I forwardVector)
		{
			base.Unregister(entity, forwardVector);
			var thrust = entity as MyThrust;
			if (thrust == null)
				return;

			thrust.EnabledChanged -= thrust_EnabledChanged;
			thrust.SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;

            // Need to recalculate the slowdown factor. Maybe save different levels of the factors and just revert back to previous one
		    SlowdownFactor = 0f;
		    foreach (var direction in Base6Directions.IntDirections)
		    {
		        foreach (var dataByType in m_dataByFuelType)
		        {
		            foreach (var entityInDirection in dataByType.ThrustsByDirection[direction])
		            {
		                var thrustInDirection = entityInDirection as MyThrust;
		                if (thrustInDirection == null)
		                    continue;

		                SlowdownFactor = Math.Max(thrustInDirection.BlockDefinition.SlowdownFactor, SlowdownFactor);
		            }
		        }
		    }
		}

		protected override void UpdateThrustStrength(HashSet<MyEntity> thrusters, float thrustForce)
		{
			foreach (MyEntity thrustEntity in thrusters)
			{
				var thrust = thrustEntity as MyThrust;
                if (thrust == null)
					continue;

			    float forceMultiplier = CalculateForceMultiplier(thrust);

				if (IsOverridden(thrust))
					thrust.CurrentStrength = forceMultiplier * thrust.ThrustOverride * ResourceSink.SuppliedRatioByType(thrust.FuelDefinition.Id) / thrust.ThrustForce.Length();
				else if (IsUsed(thrust))
                    thrust.CurrentStrength = forceMultiplier * thrustForce * ResourceSink.SuppliedRatioByType(thrust.FuelDefinition.Id);
				else
					thrust.CurrentStrength = 0;
			}
		}

	/*	void UpdateLevitation()
		{
			if (!MyFakes.SMALL_SHIP_LEVITATION)
				return;

			if (ControlThrust.LengthSquared() <= float.Epsilon * float.Epsilon)
			{
				if (ResourceSink.SuppliedRatio > 0)
				{
					float globalOffset = MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000.0f;
					float x = (float)Math.Sin(globalOffset * m_levitationPeriodLength);
					float y = 0;// (float)Math.Sin(globalOffset * periodLength);
					float z = (float)Math.Sin(globalOffset * m_levitationPeriodLength + m_levitationPeriodLength / 2);

					var torque = new Vector3(x, y, z) * m_levitationTorqueCoeficient * Entity.Physics.Mass * ResourceSink.SuppliedRatio;
					Entity.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Vector3.Zero, null, torque);
				}
			}
		}

		void StopLevitation()
		{
			if (!MyFakes.SMALL_SHIP_LEVITATION)
				return;

			var max = m_levitationTorqueCoeficient * Entity.Physics.Mass;
			var maxTorque = new Vector3(max, 0, max);
			var maxVelocity = maxTorque / Entity.Physics.RigidBody.InertiaTensor.Scale * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
			maxVelocity *= 1 / (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * m_levitationPeriodLength);

			if (Entity.Physics.RigidBody.AngularVelocity.LengthSquared() < maxVelocity.LengthSquared())
			{
				Entity.Physics.AngularVelocity = Vector3.Zero;
			}
		}*/

		private void thrust_EnabledChanged(MyTerminalBlock obj)
		{
			m_thrustsChanged = true;
		}

		private void ComponentStack_IsFunctionalChanged()
		{
			m_thrustsChanged = true;
		}

        private bool IsOverridden(MyThrust thrust)
        {
            var thruster = thrust as MyThrust;
            if (thruster == null)
                return false;

            bool autopilotEnabled = false;
            MyEntityThrustComponent thrustComp;
            if (thruster.CubeGrid.Components.TryGet(out thrustComp))
                autopilotEnabled = thrustComp.AutopilotEnabled;

            return thruster.Enabled && thruster.IsFunctional && thruster.ThrustOverride > 0 && !autopilotEnabled;
        }

        protected override bool RecomputeOverriddenParameters(MyEntity thrustEntity)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return false;

            bool autopilotEnabled = false;
            MyEntityThrustComponent thrustComp;
            if (thruster.CubeGrid.Components.TryGet(out thrustComp))
                autopilotEnabled = thrustComp.AutopilotEnabled;

            bool isOverridden = thruster.Enabled && thruster.IsFunctional && thruster.ThrustOverride > 0 && !autopilotEnabled;
            if (!isOverridden)
                return false;

            m_totalThrustOverride += thruster.ThrustOverride * -thruster.ThrustForwardVector;
            m_totalThrustOverridePower += thruster.ThrustOverride / thruster.ThrustForce.Length() * thruster.MaxPowerConsumption;
            return true;
        }

        protected override bool IsUsed(MyEntity thrustEntity)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return false;

            return thruster.Enabled && thruster.IsFunctional && thruster.ThrustOverride == 0;
        }

        protected override float CalculateForceMultiplier(MyEntity thrustEntity)
        {
            var thruster = thrustEntity as MyThrust;


            float forceMultiplier = 1.0f;

            var def = thruster.BlockDefinition;
            

	        return forceMultiplier;
	    }

        protected override float ForceMagnitude(MyEntity thrustEntity)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return 0f;

            return thruster.BlockDefinition.ForceMagnitude * CalculateForceMultiplier(thruster);
        }

        protected override float MaxPowerConsumption(MyEntity thrustEntity)
        {
            return (thrustEntity as MyThrust).MaxPowerConsumption;
        }

        protected override float MinPowerConsumption(MyEntity thrustEntity)
        {
            return (thrustEntity as MyThrust).MinPowerConsumption;
        }

	    protected override MyDefinitionId FuelType(MyEntity thrustEntity)
	    {
	        var thrust = thrustEntity as MyThrust;
	        return thrust.FuelDefinition != null ? thrust.FuelDefinition.Id : MyResourceDistributorComponent.ElectricityId;
	    }
	}
}
