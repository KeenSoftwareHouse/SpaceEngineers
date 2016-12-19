using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    internal class MyJetpackThrustComponent : MyEntityThrustComponent
    {
        public new MyCharacter Entity { get { return base.Entity as MyCharacter; } }
        public MyCharacter Character { get { return Entity; } }
        public MyCharacterJetpackComponent Jetpack { get { return Character.JetpackComp; } }

        protected override void UpdateThrusts(bool enableDampers)
        {
            base.UpdateThrusts(enableDampers);

            ProfilerShort.Begin("MyJetpackThrustComponent.UpdateThrusts");
            if (Character != null &&
                Character.Physics != null &&
                Jetpack.TurnedOn)
            {
                if (FinalThrust.LengthSquared() > 0.0001f)
                {
                    if (Character.Physics.IsInWorld)
                    {
                        Character.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, FinalThrust, null, null);

                        Vector3 velocity = Character.Physics.LinearVelocity;
                        float maxCharacterSpeedRelativeToShip = Math.Max(Character.Definition.MaxSprintSpeed, Math.Max(Character.Definition.MaxRunSpeed, Character.Definition.MaxBackrunSpeed));
                        float maxSpeed = (MyGridPhysics.ShipMaxLinearVelocity() + maxCharacterSpeedRelativeToShip);
                        if (velocity.LengthSquared() > maxSpeed * maxSpeed)
                        {
                            velocity.Normalize();
                            velocity *= maxSpeed;
                            Character.Physics.LinearVelocity = velocity;
                        }
                    }
                }

                const float stoppingVelocitySq = 0.001f * 0.001f;
                if (Character.Physics.Enabled)
                {
                    if (Character.Physics.LinearVelocity != Vector3.Zero && Character.Physics.LinearVelocity.LengthSquared() < stoppingVelocitySq)
                    {
                        Character.Physics.LinearVelocity = Vector3.Zero;
                        ControlThrustChanged = true;
                    }
                }
            }
            ProfilerShort.End();
        }

        public override void Register(MyEntity entity, Vector3I forwardVector, Func<bool> onRegisteredCallback = null)
        {
            var character = entity as MyCharacter;
            if (character == null)
                return;

            base.Register(entity, forwardVector);

            MyDefinitionId fuelType = FuelType(entity);
            float efficiency = 1.0f;
            if (MyFakes.ENABLE_HYDROGEN_FUEL)
                efficiency = Jetpack.FuelConverterDefinition.Efficiency;

            m_lastFuelTypeData.Efficiency = efficiency;
            m_lastFuelTypeData.EnergyDensity = Jetpack.FuelDefinition.EnergyDensity;
            m_lastSink.SetMaxRequiredInputByType(fuelType, m_lastSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, Jetpack.MaxPowerConsumption, m_lastGroup));

            SlowdownFactor = Math.Max(character.Definition.Jetpack.ThrustProperties.SlowdownFactor, SlowdownFactor);
        }

        protected override bool RecomputeOverriddenParameters(MyEntity thrustEntity, FuelTypeData fuelData)
        {
            return false;
        }

        protected override bool IsUsed(MyEntity thrustEntity)
        {
            return Enabled;
        }

        protected override float ForceMagnitude(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere)
        {
            return Jetpack.ForceMagnitude * CalculateForceMultiplier(thrustEntity, planetaryInfluence, inAtmosphere);
        }

        protected override float CalculateForceMultiplier(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere)
        {
            Debug.Assert(planetaryInfluence >= 0f);

            float forceMultiplier = 1.0f;

            if (Jetpack.MaxPlanetaryInfluence != Jetpack.MinPlanetaryInfluence && (inAtmosphere && Jetpack.NeedsAtmosphereForInfluence || !inAtmosphere))
                forceMultiplier = MathHelper.Lerp(Jetpack.EffectivenessAtMinInfluence, Jetpack.EffectivenessAtMaxInfluence, MathHelper.Clamp((planetaryInfluence - Jetpack.MinPlanetaryInfluence) / (Jetpack.MaxPlanetaryInfluence - Jetpack.MinPlanetaryInfluence), 0f, 1f));
            else if (Jetpack.NeedsAtmosphereForInfluence && !inAtmosphere)
                forceMultiplier = Jetpack.EffectivenessAtMinInfluence;

            return forceMultiplier;
        }

        protected override float CalculateConsumptionMultiplier(MyEntity thrustEntity, float naturalGravityStrength)
        {
            return 1f + Jetpack.ConsumptionFactorPerG * (naturalGravityStrength / MyGravityProviderSystem.G);
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

        protected override bool IsThrustEntityType(MyEntity thrustEntity)
        {
            return thrustEntity is MyCharacter;
        }

        protected override void RemoveFromGroup(MyEntity thrustEntity, MyEntityThrustComponent.MyConveyorConnectedGroup group)
        {
        }

        protected override void AddToGroup(MyEntity thrustEntity, MyEntityThrustComponent.MyConveyorConnectedGroup group)
        {
        }

        protected override Vector3 ApplyThrustModifiers(ref MyDefinitionId fuelType, ref Vector3 thrust, ref Vector3 thrustOverride, MyResourceSinkComponentBase resourceSink)
        {
            thrust += thrustOverride;
            if (Character.ControllerInfo.Controller == null || MySession.Static.CreativeToolsEnabled(Character.ControllerInfo.Controller.Player.Id.SteamId) == false ||
                (MySession.Static.LocalCharacter != Character && Sync.IsServer == false))
            {
                thrust *= resourceSink.SuppliedRatioByType(fuelType);
            }
            thrust *= MyFakes.THRUST_FORCE_RATIO;

            return thrust;
        }
    }
}