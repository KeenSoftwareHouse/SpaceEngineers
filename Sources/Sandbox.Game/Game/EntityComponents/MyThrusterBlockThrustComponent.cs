using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    internal class MyThrusterBlockThrustComponent : MyEntityThrustComponent
    {
        private new MyCubeGrid Entity
        { get { return base.Entity as MyCubeGrid; } }
        private MyCubeGrid CubeGrid
        { get { return Entity; } }

        // Levitation period length in seconds
        private float m_levitationPeriodLength = 1.3f;

        private float m_levitationTorqueCoeficient = 0.25f;
        private int m_index;
        protected override void UpdateThrusts(bool enableDampeners)
        {
            base.UpdateThrusts(enableDampeners);

            ProfilerShort.Begin("ThrusterBlockComponent.UpdateThrusts");
            if (CubeGrid != null && CubeGrid.Physics != null)
            {
                if (CubeGrid.Physics.Enabled)
                {
                    if (FinalThrust.LengthSquared() > 0.0001f)
                    {
                        var thrust = FinalThrust;
                        if (CubeGrid.Physics.IsWelded) //only reliable variant
                        {
                            thrust = Vector3.TransformNormal(thrust, CubeGrid.WorldMatrix);
                            thrust = Vector3.TransformNormal(thrust, Matrix.Invert(CubeGrid.Physics.RigidBody.GetRigidBodyMatrix()));
                        }

                        CubeGrid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, thrust, null, null);
                        m_index++;

                        Vector3 velocity = CubeGrid.Physics.LinearVelocity;
                        float maxSpeed = CubeGrid.GridSizeEnum == MyCubeSize.Large ? MyGridPhysics.LargeShipMaxLinearVelocity() : MyGridPhysics.SmallShipMaxLinearVelocity();

                        if (velocity.LengthSquared() > maxSpeed * maxSpeed)
                        {
                            velocity.Normalize();
                            velocity *= maxSpeed;
                            CubeGrid.Physics.LinearVelocity = velocity;
                        }
                    }

                }
            }
            ProfilerShort.End();
        }

        public override void Register(MyEntity entity, Vector3I forwardVector, Func<bool> onRegisteredCallback)
        {
            var thrust = entity as MyThrust;
            if (thrust == null)
                return;

            // As this is called in entity creation thread, the CubeGrid being in scene means the thrust component can and will get updated at the same time as Register is happening, which will cause crashes due to conveyor pathfinding
            //if (CubeGrid.InScene)
            m_thrustEntitiesPending.Enqueue(new MyTuple<MyEntity, Vector3I, Func<bool>>(entity, forwardVector, onRegisteredCallback));
            //else
            //    RegisterLazy(entity, forwardVector, onRegisteredCallback);
        }

        protected override bool RegisterLazy(MyEntity entity, Vector3I forwardVector, Func<bool> onRegisteredCallback)
        {
            base.RegisterLazy(entity, forwardVector, onRegisteredCallback);
            base.Register(entity, forwardVector, onRegisteredCallback);

            var thrust = entity as MyThrust;

            MyDefinitionId fuelType = FuelType(entity);
            m_lastFuelTypeData.EnergyDensity = thrust.FuelDefinition.EnergyDensity;
            m_lastFuelTypeData.Efficiency = thrust.BlockDefinition.FuelConverter.Efficiency;

            m_lastSink.SetMaxRequiredInputByType(fuelType, m_lastSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, thrust.MaxPowerConsumption, m_lastGroup));

            thrust.EnabledChanged += thrust_EnabledChanged;
            thrust.ThrustOverrideChanged += MyThrust_ThrustOverrideChanged;
            thrust.SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            SlowdownFactor = Math.Max(thrust.BlockDefinition.SlowdownFactor, SlowdownFactor);

            if (onRegisteredCallback != null)
                onRegisteredCallback();

            return true;
        }

        public override void Unregister(MyEntity entity, Vector3I forwardVector)
        {
            base.Unregister(entity, forwardVector);
            var thrust = entity as MyThrust;
            if (thrust == null)
                return;

            thrust.SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;
            thrust.ThrustOverrideChanged -= MyThrust_ThrustOverrideChanged;
            thrust.EnabledChanged -= thrust_EnabledChanged;

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

                foreach (var group in ConnectedGroups)
                {
                    foreach (var dataByType in group.DataByFuelType)
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
        }

        protected override void UpdateThrustStrength(HashSet<MyEntity> thrusters, float thrustForce)
        {
            foreach (MyEntity thrustEntity in thrusters)
            {
                var thrust = thrustEntity as MyThrust;
                if (thrust == null)
                    continue;

                float forceMultiplier = CalculateForceMultiplier(thrust, m_lastPlanetaryInfluence, m_lastPlanetaryInfluenceHasAtmosphere);

                if (IsOverridden(thrust))
                {
                    if (MySession.Static.CreativeMode && thrust.IsWorking)
                        thrust.CurrentStrength = forceMultiplier * thrust.ThrustOverride / thrust.ThrustForce.Length();
                    else
                        thrust.CurrentStrength = forceMultiplier * thrust.ThrustOverride * ResourceSink(thrustEntity).SuppliedRatioByType(thrust.FuelDefinition.Id) / thrust.ThrustForce.Length();
                }
                else if (IsUsed(thrust))
                    thrust.CurrentStrength = forceMultiplier * thrustForce * ResourceSink(thrustEntity).SuppliedRatioByType(thrust.FuelDefinition.Id);
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
                var maxVelocity = maxTorque / Entity.Physics.RigidBody.InertiaTensor.Scale * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                maxVelocity *= 1 / (VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * m_levitationPeriodLength);

                if (Entity.Physics.RigidBody.AngularVelocity.LengthSquared() < maxVelocity.LengthSquared())
                {
                    Entity.Physics.AngularVelocity = Vector3.Zero;
                }
            }*/

        private void MyThrust_ThrustOverrideChanged(float newValue)
        {
            MarkDirty();
        }

        private void thrust_EnabledChanged(MyTerminalBlock obj)
        {
            MarkDirty();
            if (CubeGrid.Physics != null && !CubeGrid.Physics.RigidBody.IsActive)
                CubeGrid.ActivatePhysics();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            MarkDirty();
            if (CubeGrid.Physics != null && !CubeGrid.Physics.RigidBody.IsActive)
                CubeGrid.ActivatePhysics();
        }

        private static bool IsOverridden(MyThrust thrust)
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

        protected override bool RecomputeOverriddenParameters(MyEntity thrustEntity, FuelTypeData fuelData)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return false;

            if (!IsOverridden(thruster))
                return false;

            var thrustOverride = thruster.ThrustOverride * -thruster.ThrustForwardVector * CalculateForceMultiplier(thrustEntity, m_lastPlanetaryInfluence, m_lastPlanetaryInfluenceHasAtmosphere);
            var thrustOverridePower = thruster.ThrustOverride / thruster.ThrustForce.Length() * thruster.MaxPowerConsumption;

            if (fuelData.ThrustsByDirection[thruster.ThrustForwardVector].Contains(thrustEntity))
            {
                fuelData.ThrustOverride += thrustOverride;
                fuelData.ThrustOverridePower += thrustOverridePower;
            }

            return true;
        }

        protected override bool IsUsed(MyEntity thrustEntity)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return false;

            return thruster.Enabled && thruster.IsFunctional && thruster.ThrustOverride == 0;
        }

        protected override float CalculateForceMultiplier(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere)
        {
            var thruster = thrustEntity as MyThrust;
            Debug.Assert(planetaryInfluence >= 0f);

            float forceMultiplier = 1.0f;

            var def = thruster.BlockDefinition;

            if (def.NeedsAtmosphereForInfluence && !inAtmosphere)
                forceMultiplier = def.EffectivenessAtMinInfluence;
            else if (def.MaxPlanetaryInfluence != def.MinPlanetaryInfluence)
                forceMultiplier = MathHelper.Lerp(def.EffectivenessAtMinInfluence, def.EffectivenessAtMaxInfluence, MathHelper.Clamp((planetaryInfluence - def.MinPlanetaryInfluence) / (def.MaxPlanetaryInfluence - def.MinPlanetaryInfluence), 0f, 1f));

            return forceMultiplier;
        }

        protected override float CalculateConsumptionMultiplier(MyEntity thrustEntity, float naturalGravityStrength)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return 1f;

            return 1f + thruster.BlockDefinition.ConsumptionFactorPerG * (naturalGravityStrength / MyGravityProviderSystem.G);
        }

        protected override float ForceMagnitude(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return 0f;
            float thrustMultiplayer = (thrustEntity is ModAPI.IMyThrust) ? (thrustEntity as ModAPI.IMyThrust).ThrustMultiplier : 1f;
            return thruster.BlockDefinition.ForceMagnitude * thrustMultiplayer * CalculateForceMultiplier(thruster, planetaryInfluence, inAtmosphere);
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

        protected override bool IsThrustEntityType(MyEntity thrustEntity)
        {
            return thrustEntity is MyThrust;
        }

        protected override void AddToGroup(MyEntity thrustEntity, MyEntityThrustComponent.MyConveyorConnectedGroup group)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return;

            group.ResourceSink.IsPoweredChanged += thruster.Sink_IsPoweredChanged;
        }

        protected override void RemoveFromGroup(MyEntity thrustEntity, MyEntityThrustComponent.MyConveyorConnectedGroup group)
        {
            var thruster = thrustEntity as MyThrust;
            if (thruster == null)
                return;

            group.ResourceSink.IsPoweredChanged -= thruster.Sink_IsPoweredChanged;
        }

        protected override float CalculateMass()
        {
            var group = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(Entity);

            MyGridPhysics body = Entity.Physics;
            float gridMass = body.WeldedRigidBody != null ? body.WeldedRigidBody.Mass : Entity.Physics.Mass;

            MyCubeGrid biggestGrid = null;
            float deathWeight = 0.0f;

            if (group != null)
            {
                float maxRadius = 0;

                foreach (var node in group.Nodes)
                {
                    MyCubeGrid grid = node.NodeData;
                    if (grid.IsStatic || grid.Physics == null)
                        continue;

                    var thrustComp = grid.Components.Get<MyEntityThrustComponent>();

                    bool hasThrust = thrustComp != null && thrustComp.Enabled && thrustComp.HasPower;

                    if (hasThrust == false)
                    {
                        deathWeight += grid.Physics.WeldedRigidBody != null ? grid.Physics.WeldedRigidBody.Mass : grid.Physics.Mass;
                    }
                    else
                    {
                        var rad = grid.PositionComp.LocalVolume.Radius;
                        if (rad > maxRadius || (rad == maxRadius && (biggestGrid == null || grid.EntityId > biggestGrid.EntityId)))
                        {
                            maxRadius = rad;
                            biggestGrid = grid;
                        }
                    }
                }
            }

            if (biggestGrid == CubeGrid)
            {
                gridMass += deathWeight;
            }

            return gridMass;
        }
    }
}