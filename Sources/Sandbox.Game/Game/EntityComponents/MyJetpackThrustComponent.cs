using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using VRage.Components;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
	class MyJetpackThrustComponent : MyEntityThrustComponent
	{
		public new MyCharacter Entity { get { return base.Entity as MyCharacter; } }
		public MyCharacter Character { get { return Entity; } }
		public MyCharacterJetpackComponent Jetpack { get { return Character.JetpackComp; } }

		protected override void UpdateThrusts()
		{
			base.UpdateThrusts();

			if (Character != null &&
				Character.Physics != null &&
				Jetpack.TurnedOn && 
				(MySession.LocalCharacter == Character ||
				(MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity && (MySector.MainCamera.IsInFrustum(Character.PositionComp.WorldAABB) ||
				(Character.PositionComp.GetPosition() - MySector.MainCamera.Position).LengthSquared() < 100f))))
			{
				if (FinalThrust.LengthSquared() > 0.001f)
				{
					Character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, FinalThrust, null, null);
				}

				const float stoppingVelocitySq = 0.001f * 0.001f;
				if (Character.Physics.Enabled)
				{
					if (Character.Physics.LinearVelocity != Vector3.Zero && Character.Physics.LinearVelocity.LengthSquared() < stoppingVelocitySq)
						Character.Physics.LinearVelocity = Vector3.Zero;
				}
			}
		}

	    public override void Register(MyEntity entity, Vector3I forwardVector)
	    {
            var character = entity as MyCharacter;
            if (character == null)
                return;

            base.Register(entity, forwardVector);

	        MyDefinitionId fuelType = FuelType(entity);
	        int typeIndex = GetTypeIndex(ref fuelType);
            float efficiency = 1.0f;
            if (MyFakes.ENABLE_HYDROGEN_FUEL)
	            efficiency = Jetpack.FuelConverterDefinition.Efficiency;

	        m_dataByFuelType[typeIndex].Efficiency = efficiency;
	        m_dataByFuelType[typeIndex].EnergyDensity = Jetpack.FuelDefinition.EnergyDensity;
            ResourceSink.SetMaxRequiredInputByType(fuelType, ResourceSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, Jetpack.MaxPowerConsumption));

            SlowdownFactor = Math.Max(character.Definition.Jetpack.ThrustProperties.SlowdownFactor, SlowdownFactor);
	    }

	    protected override bool RecomputeOverriddenParameters(MyEntity thrustEntity)
	    {
	        return false;
	    }

	    protected override bool IsUsed(MyEntity thrustEntity)
	    {
	        return Enabled;
	    }

	    protected override float ForceMagnitude(MyEntity thrustEntity)
	    {
            return Jetpack.ForceMagnitude * CalculateForceMultiplier(thrustEntity);
	    }

	    protected override float CalculateForceMultiplier(MyEntity thrustEntity)
	    {


	        float forceMultiplier = 1.0f;

	        return forceMultiplier;
	    }

	    protected override float MaxPowerConsumption(MyEntity thrustEntity)
	    {
	        return Jetpack.MaxPowerConsumption;
	    }

	    protected override float MinPowerConsumption(MyEntity thrustEntity)
	    {
	        return Jetpack.MinPowerConsumption;
	    }

	    protected override void UpdateThrustStrength(HashSet<MyEntity> entities, float thrustForce)
		{
			ControlThrust = Vector3.Zero;
		}

	    protected override MyDefinitionId FuelType(MyEntity thrustEntity)
	    {
	        return Jetpack.FuelDefinition != null ? Jetpack.FuelDefinition.Id : MyResourceDistributorComponent.ElectricityId;
	    }
	}
}
