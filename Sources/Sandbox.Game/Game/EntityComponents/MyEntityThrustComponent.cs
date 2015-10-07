using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using VRage;
using VRage.Components;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    public abstract class MyEntityThrustComponent : MyEntityComponentBase
    {
        private class DirectionComparer : IEqualityComparer<Vector3I>
        {
            public bool Equals(Vector3I x, Vector3I y)
            {
                return x == y;
            }

            public int GetHashCode(Vector3I obj)
            {
                Debug.Assert(
                    obj == Vector3I.Forward ||
                    obj == Vector3I.Backward ||
                    obj == Vector3I.Left ||
                    obj == Vector3I.Right ||
                    obj == Vector3I.Up ||
                    obj == Vector3I.Down);
                return obj.X + (8 * obj.Y) + (64 * obj.Z);
            }
        }

        private static readonly DirectionComparer m_directionComparer = new DirectionComparer();

        protected class FuelTypeData
        {
            public Dictionary<Vector3I, HashSet<MyEntity>> ThrustsByDirection;
            public Dictionary<Vector3I, float> MaxRequirementsByDirection;

            public float CurrentPowerFromFuel;
            public float CurrentRequiredFuelInput;

            public Vector3 MaxNegativeThrust;
            public Vector3 MaxPositiveThrust;

            public float MinRequiredPowerInput;
            public float MaxRequiredPowerInput;

            public float Efficiency;
            public float EnergyDensity;

            public bool FuelConversionDirty;
        }

        protected struct MyConveyorConnectedGroup
        {
            public List<FuelTypeData> DataByFuelType;

            public MyResourceSinkComponent ResourceSink;
        }

        #region Fields
        private const int m_maxInfluenceRecalculationInterval = 10000;

        private Vector3 m_maxNegativeThrust;
        private Vector3 m_maxPositiveThrust;

        protected readonly List<FuelTypeData> m_dataByFuelType = new List<FuelTypeData>();
        protected readonly List<MyDefinitionId> m_fuelTypes = new List<MyDefinitionId>(); 
        private readonly Dictionary<MyDefinitionId, int> m_fuelTypeToIndex = new Dictionary<MyDefinitionId, int>(); 
        
        protected Vector3 m_totalThrustOverride;
        protected float m_totalThrustOverridePower;

        /// <summary>
        /// True whenever thrust was added or removed.
        /// </summary>
        protected bool m_thrustsChanged;
        protected bool m_enabled;

        public MyResourceSinkComponent ResourceSink;

        #endregion

        #region Properties

        /// <summary>
        /// For now just the maximum slowdown factor of any thruster registered to the component
        /// </summary>
        public float SlowdownFactor { get; protected set; }
		public float MaxRequiredPowerInput { get; private set; }
        public float MinRequiredPowerInput { get; private set; }
		public new MyEntity Entity { get { return base.Entity as MyEntity; } }
		public int ThrustCount { get; private set; }
		public bool DampenersEnabled { get; set; }

        private float m_suppliedPowerRatio;

        /// <summary>
        /// Torque and thrust wanted by player (from input).
        /// </summary>
        public Vector3 ControlThrust { get; set; }

        /// <summary>
        /// Final thrust (clamped by available power, added anti-gravity, slowdown).
        /// </summary>
        public Vector3 FinalThrust { get; private set; }

        /// <summary>
        /// Thrust wanted by AutoPilot
        /// </summary>
        public Vector3 AutoPilotControlThrust;
        public bool AutopilotEnabled;

        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                    m_enabled = value;
            }
        }

        #endregion

        private int InitializeType(MyDefinitionId fuelType)
        {
            m_dataByFuelType.Add(new FuelTypeData
            {
                ThrustsByDirection = new Dictionary<Vector3I, HashSet<MyEntity>>(6, m_directionComparer),
                MaxRequirementsByDirection = new Dictionary<Vector3I, float>(6, m_directionComparer),
                FuelConversionDirty = true,
                CurrentPowerFromFuel = 0f,
                CurrentRequiredFuelInput = 0f,
                Efficiency = 0f,
                EnergyDensity = 0f,
            });

            int typeIndex = m_dataByFuelType.Count - 1;
            m_fuelTypeToIndex.Add(fuelType, typeIndex);
            m_fuelTypes.Add(fuelType);

            foreach (var direction in Base6Directions.IntDirections)
            {
                m_dataByFuelType[typeIndex].ThrustsByDirection[direction] = new HashSet<MyEntity>();
            }

            var sinkData = new MyResourceSinkInfo
            {
                ResourceTypeId = fuelType,
                MaxRequiredInput = 0,
                RequiredInputFunc = () => RequiredFuelInput(ref fuelType)
            };

            if (m_fuelTypes.Count == 1)
            {
                ResourceSink.Init(MyStringHash.GetOrCompute("Thrust"), sinkData);
                ResourceSink.IsPoweredChanged += Sink_IsPoweredChanged;
                ResourceSink.CurrentInputChanged += Sink_CurrentInputChanged;

                var cubeGrid = Container.Entity as MyCubeGrid;
                if (cubeGrid != null)
                {
                    var gridSystems = cubeGrid.GridSystems;
                    if (gridSystems != null)
                    {
                        if (gridSystems.ResourceDistributor != null)
                            gridSystems.ResourceDistributor.AddSink(ResourceSink);
                    }
                }
            }
            else
                ResourceSink.AddType(ref sinkData);

            return typeIndex;
        }

	    protected MyEntityThrustComponent()
        {
	        ResourceSink = new MyResourceSinkComponent();
        }

		public virtual void Init()
	    {
		    Enabled = true;
            m_thrustsChanged = true;
            ThrustCount = 0;
            DampenersEnabled = true;
	    }

	    public virtual void Register(MyEntity entity, Vector3I forwardVector)
	    {
		    Debug.Assert(entity != null);
			Debug.Assert(!IsRegistered(entity, forwardVector));
	        MyDefinitionId fuelType = FuelType(entity);
	        int typeIndex;
	        if (!TryGetTypeIndex(ref fuelType, out typeIndex))
	            typeIndex = InitializeType(fuelType);

	        m_dataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Add(entity);
			m_thrustsChanged = true;

			++ThrustCount;
	    }

        public bool IsRegistered(MyEntity entity, Vector3I forwardVector)
        {
            Debug.Assert(entity != null);
            MyDefinitionId fuelType = FuelType(entity);
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return false;

            return m_dataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Contains(entity);
        }

        public virtual void Unregister(MyEntity entity, Vector3I forwardVector)
        {
            Debug.Assert(entity != null);
            Debug.Assert(IsRegistered(entity, forwardVector));
            MyDefinitionId fuelType = FuelType(entity);
            int typeIndex = GetTypeIndex(ref fuelType);
            ResourceSink.SetMaxRequiredInputByType(fuelType, ResourceSink.MaxRequiredInputByType(fuelType) - PowerAmountToFuel(ref fuelType, MaxPowerConsumption(entity)));
            m_dataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Remove(entity);
            m_thrustsChanged = true;
            --ThrustCount;
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
                return;

            m_dataByFuelType[typeIndex].FuelConversionDirty = true;
        }

        private void Sink_IsPoweredChanged()
        {
            MarkDirty();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            var cubeGrid = Container.Entity as MyCubeGrid;
            if (cubeGrid == null)
                return;

            var gridSystems = cubeGrid.GridSystems;
            if (gridSystems == null)
                return;

            if(gridSystems.ResourceDistributor != null)
                gridSystems.ResourceDistributor.RemoveSink(ResourceSink);
        }

        public virtual void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("EntityThrustComponent.UpdateBeforeSimulation");
	        if (ThrustCount == 0)
	        {
		        Entity.Components.Remove<MyEntityThrustComponent>();
		        ProfilerShort.End();
		        return;
	        }

            var frameCounter = MySession.Static.GameplayFrameCounter;

            ProfilerShort.Begin("RecomputeThrustParameters");
            if (m_thrustsChanged)
                RecomputeThrustParameters();
            ProfilerShort.End();

            ProfilerShort.Begin("UpdateThrusts");
            if (Enabled && Entity.Physics != null)
                UpdateThrusts();
            ProfilerShort.End();

            ProfilerShort.End();
        }

        private void RecomputeThrustParameters() // Only gets called when m_thrustsChanged is set
        {
            m_totalThrustOverride = Vector3.Zero;
            m_totalThrustOverridePower = 0;

            m_maxPositiveThrust = new Vector3();
            m_maxNegativeThrust = new Vector3();
            MaxRequiredPowerInput = 0.0f;
            MinRequiredPowerInput = 0.0f;
            foreach (FuelTypeData dataByType in m_dataByFuelType)
            {
                dataByType.MaxRequiredPowerInput = 0f;
                dataByType.MaxPositiveThrust = new Vector3();
                dataByType.MaxNegativeThrust = new Vector3();
                dataByType.MaxRequirementsByDirection.Clear();

                foreach (var dir in dataByType.ThrustsByDirection)
                {
                    if (!dataByType.MaxRequirementsByDirection.ContainsKey(dir.Key))
                        dataByType.MaxRequirementsByDirection[dir.Key] = 0f;

                    float maxRequiredPower = 0;
                    foreach (MyEntity thrustEntity in dir.Value)
                    {
                        if (RecomputeOverriddenParameters(thrustEntity))
                            continue;

                        if (!IsUsed(thrustEntity))
                            continue;

                        var forceMagnitude = ForceMagnitude(thrustEntity);
                        var forceMultiplier = CalculateForceMultiplier(thrustEntity);

                        dataByType.MaxPositiveThrust += Vector3.Clamp(-dir.Key * forceMagnitude, Vector3.Zero, Vector3.PositiveInfinity);
                        dataByType.MaxNegativeThrust += -Vector3.Clamp(-dir.Key * forceMagnitude, Vector3.NegativeInfinity, Vector3.Zero);
                        maxRequiredPower += MaxPowerConsumption(thrustEntity) * forceMultiplier;
                        dataByType.MinRequiredPowerInput = MinPowerConsumption(thrustEntity);
                    }
                    dataByType.MaxRequirementsByDirection[dir.Key] += maxRequiredPower;
                }

                dataByType.MaxRequiredPowerInput += Math.Max(dataByType.MaxRequirementsByDirection[Vector3I.Forward], dataByType.MaxRequirementsByDirection[Vector3I.Backward]);
                dataByType.MaxRequiredPowerInput += Math.Max(dataByType.MaxRequirementsByDirection[Vector3I.Left], dataByType.MaxRequirementsByDirection[Vector3I.Right]);
                dataByType.MaxRequiredPowerInput += Math.Max(dataByType.MaxRequirementsByDirection[Vector3I.Up], dataByType.MaxRequirementsByDirection[Vector3I.Down]);

                MaxRequiredPowerInput += dataByType.MaxRequiredPowerInput;
                MinRequiredPowerInput += dataByType.MinRequiredPowerInput;

                m_maxPositiveThrust += dataByType.MaxPositiveThrust;
                m_maxNegativeThrust += dataByType.MaxNegativeThrust;
            }

            m_thrustsChanged = false;
        }

        protected virtual void UpdateThrusts()
        {
            //if (direction != Vector3.Zero)
            //{

            //}
            //if (Container.Entity.Physics.IsWelded)
            //    direction = Vector3.TransformNormal(ControlThrust, m_grid.Physics.WeldInfo.Transform);
            //if (!Vector3.IsZero(ControlThrust))
            //    Debugger.Break();
            Vector3 thrust = AutopilotEnabled ? ComputeAiThrust(AutoPilotControlThrust) : ComputeBaseThrust(ControlThrust);

            ProfilerShort.Begin("UpdatePowerAndThrustStrength");
            UpdatePowerAndThrustStrength(thrust, true);
            ProfilerShort.End();
            FinalThrust = new Vector3();
            for(int typeIndex = 0; typeIndex < m_dataByFuelType.Count; ++typeIndex)
            {
                MyDefinitionId fuelType = m_fuelTypes[typeIndex];
                Vector3 thrustBeforeApply;
                var maxThrust = (m_maxPositiveThrust + m_maxNegativeThrust);
                thrustBeforeApply.X = maxThrust.X != 0 ? thrust.X * (m_dataByFuelType[typeIndex].MaxPositiveThrust.X + m_dataByFuelType[typeIndex].MaxNegativeThrust.X) / maxThrust.X : 0f;
                thrustBeforeApply.Y = maxThrust.Y != 0 ? thrust.Y * (m_dataByFuelType[typeIndex].MaxPositiveThrust.Y + m_dataByFuelType[typeIndex].MaxNegativeThrust.Y) / maxThrust.Y : 0f;
                thrustBeforeApply.Z = maxThrust.Z != 0 ? thrust.Z * (m_dataByFuelType[typeIndex].MaxPositiveThrust.Z + m_dataByFuelType[typeIndex].MaxNegativeThrust.Z) / maxThrust.Z : 0f;
                Vector3 finalThrust = ApplyThrustModifiers(fuelType, thrustBeforeApply);
                FinalThrust += finalThrust;
            }
        }

        private Vector3 ComputeBaseThrust(Vector3 controlThrust)
        {
	        if (Entity.Physics == null)
		        return Vector3.Zero;

            Matrix invWorldRot = Entity.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
            //Matrix invWorldRot = Matrix.Invert(m_grid.Physics.RigidBody.GetRigidBodyMatrix());// m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();

            Vector3 gravityVector = Entity.Physics.IsMoving ? Entity.Physics.Gravity / 2.0f : Vector3.Zero;
			Vector3 localVelocity = Vector3.TransformNormal(Entity.Physics.LinearVelocity + gravityVector, invWorldRot);
            Vector3 positiveControl = Vector3.Clamp(controlThrust, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(controlThrust, -Vector3.One, Vector3.Zero);
            Vector3 slowdownControl = Vector3.Zero;
			if (DampenersEnabled)
				slowdownControl = Vector3.IsZeroVector(controlThrust, 0.001f) * Vector3.IsZeroVector(m_totalThrustOverride);

            Vector3 thrust = negativeControl * m_maxNegativeThrust + positiveControl * m_maxPositiveThrust;
            thrust = Vector3.Clamp(thrust, -m_maxNegativeThrust, m_maxPositiveThrust);

            const float STOPPING_TIME = 0.5f;
            Vector3 slowdownAcceleration = -localVelocity / STOPPING_TIME;
	        Vector3 slowdownThrust = slowdownAcceleration * Entity.Physics.Mass * slowdownControl;

			thrust = Vector3.Clamp(thrust + slowdownThrust, -m_maxNegativeThrust * SlowdownFactor, m_maxPositiveThrust * SlowdownFactor);

            return thrust;
        }

        public Vector3 ComputeAiThrust(Vector3 direction)
        {
            Matrix invWorldRot = Entity.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();

            Vector3 positiveControl = Vector3.Clamp(direction, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(direction, -Vector3.One, Vector3.Zero);

            Vector3 positiveGravity = Vector3.Clamp(-Vector3.Transform(Entity.Physics.Gravity, ref invWorldRot) * Entity.Physics.Mass, Vector3.Zero, Vector3.PositiveInfinity);
            Vector3 negativeGravity = Vector3.Clamp(-Vector3.Transform(Entity.Physics.Gravity, ref invWorldRot) * Entity.Physics.Mass, Vector3.NegativeInfinity, Vector3.Zero);

            Vector3 maxPositiveThrustWithGravity = Vector3.Clamp((m_maxPositiveThrust - positiveGravity), Vector3.Zero, Vector3.PositiveInfinity);
            Vector3 maxNegativeThrustWithGravity = Vector3.Clamp((m_maxNegativeThrust + negativeGravity), Vector3.Zero, Vector3.PositiveInfinity);

            Vector3 maxPositiveControl = maxPositiveThrustWithGravity * positiveControl;
            Vector3 maxNegativeControl = maxNegativeThrustWithGravity * -negativeControl;
            
            float max = Math.Max(maxPositiveControl.Max(), maxNegativeControl.Max());

            Vector3 thrust = Vector3.Zero;
            if (max > 0.001f)
            {
                Vector3 optimalPositive = positiveControl * max;
                Vector3 optimalNegative = -negativeControl * max;

                Vector3 optimalPositiveRatio = maxPositiveThrustWithGravity / optimalPositive;
                Vector3 optimalNegativeRatio = maxNegativeThrustWithGravity / optimalNegative;

                FlipNegativeInfinity(ref optimalPositiveRatio);
                FlipNegativeInfinity(ref optimalNegativeRatio);

                float min = Math.Min(optimalPositiveRatio.Min(), optimalNegativeRatio.Min());

                if (min > 1.0f)
                    min = 1.0f;

                thrust = -optimalNegative * min + optimalPositive * min;
                thrust += positiveGravity + negativeGravity;
                thrust = Vector3.Clamp(thrust, -m_maxNegativeThrust, m_maxPositiveThrust);
            }

            const float STOPPING_TIME = 0.5f;
            Vector3 localVelocity = Vector3.Transform(Entity.Physics.LinearVelocity + Entity.Physics.Gravity / 2.0f, ref invWorldRot);

            Vector3D velocityToCancel;
            if (!Vector3.IsZero(direction))
            {
                Vector3D normalizedDir = Vector3.Normalize(direction);
                velocityToCancel = Vector3.Reject(localVelocity, normalizedDir);
            }
            else
            {
                velocityToCancel = localVelocity;
            }

            var slowdownAcceleration = -velocityToCancel / STOPPING_TIME;
            var slowdownThrust = slowdownAcceleration * Entity.Physics.Mass;
            thrust = Vector3.Clamp(thrust + slowdownThrust, -m_maxNegativeThrust * SlowdownFactor, m_maxPositiveThrust * SlowdownFactor);

            return thrust;
        }

        private void FlipNegativeInfinity(ref Vector3 v)
        {
            if (float.IsNegativeInfinity(v.X)) v.X = float.PositiveInfinity;
            if (float.IsNegativeInfinity(v.Y)) v.Y = float.PositiveInfinity;
            if (float.IsNegativeInfinity(v.Z)) v.Z = float.PositiveInfinity;
        }

        private Vector3 ApplyThrustModifiers(MyDefinitionId fuelType, Vector3 thrust)
        {
            thrust += m_totalThrustOverride;
            float suppliedRatio = 1.0f;
            if (MaxRequiredPowerInput != 0f)
                suppliedRatio = ResourceSink.SuppliedRatioByType(fuelType);

            thrust *= suppliedRatio;
            thrust *= MyFakes.THRUST_FORCE_RATIO;

            return thrust;
        }

        internal void UpdatePowerAndThrustStrength(Vector3 thrust, bool updateThrust)
        {
            //if ((Container.Entity.Physics as Sandbox.Engine.Physics.MyPhysicsBody).IsWelded)
            //    thrust = Vector3.TransformNormal(thrust, (Container.Entity.Physics as Sandbox.Engine.Physics.MyPhysicsBody).WeldInfo.Transform);
            // Calculate ratio of usage for different directions.
            Vector3 thrustPositive = thrust/(m_maxPositiveThrust + 0.0000001f);
            Vector3 thrustNegative = -thrust/(m_maxNegativeThrust + 0.0000001f);
            thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One);
            thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One);

            foreach (var fuelKey in m_fuelTypeToIndex.Keys)
            {
                float requiredPower = 0f;
                var fuelType = fuelKey;
                if (Enabled)
                {
                    int typeIndex = GetTypeIndex(ref fuelType);
                    requiredPower += (thrustPositive.X > 0) ? thrustPositive.X * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Left) : 0;
                    requiredPower += (thrustPositive.Y > 0) ? thrustPositive.Y * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Down) : 0;
                    requiredPower += (thrustPositive.Z > 0) ? thrustPositive.Z * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Forward) : 0;
                    requiredPower += (thrustNegative.X > 0) ? thrustNegative.X * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Right) : 0;
                    requiredPower += (thrustNegative.Y > 0) ? thrustNegative.Y * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Up) : 0;
                    requiredPower += (thrustNegative.Z > 0) ? thrustNegative.Z * GetMaxPowerRequirement(ref fuelType, ref Vector3I.Backward) : 0;
                    requiredPower += m_totalThrustOverridePower;
                    requiredPower = Math.Max(requiredPower, m_dataByFuelType[typeIndex].MinRequiredPowerInput);
                }
                SetRequiredFuelInput(ref fuelType, PowerAmountToFuel(ref fuelType, requiredPower));
            }

            ResourceSink.Update();

            ProfilerShort.Begin("Update thrust strengths");
            if (updateThrust)
            {
                foreach (var dataByType in m_dataByFuelType)
                {
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Left], thrustPositive.X);
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Down], thrustPositive.Y);
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Forward], thrustPositive.Z);
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Right], thrustNegative.X);
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Up], thrustNegative.Y);
                    UpdateThrustStrength(dataByType.ThrustsByDirection[Vector3I.Backward], thrustNegative.Z);
                }
            }
            ProfilerShort.End();
        }

        public Vector3 GetAutoPilotThrustForDirection(Vector3 direction)
        {
            Vector3 thrust = ComputeAiThrust(direction);
            UpdatePowerAndThrustStrength(thrust, false);

            Vector3 finalThrust = new Vector3();
            for (int typeIndex = 0; typeIndex < m_dataByFuelType.Count; ++typeIndex)
            {
                MyDefinitionId fuelType = m_fuelTypes[typeIndex];
                Vector3 finalTypeThrust = ApplyThrustModifiers(fuelType, thrust * (m_dataByFuelType[typeIndex].MaxPositiveThrust + m_dataByFuelType[typeIndex].MaxNegativeThrust) / (m_maxPositiveThrust + m_maxNegativeThrust));
                finalThrust += finalTypeThrust;
            }
            return finalThrust;
        }

        protected float GetMaxPowerRequirement(ref MyDefinitionId fuelType, ref Vector3I direction)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            return m_dataByFuelType[typeIndex].MaxRequirementsByDirection[direction];
        }

        public void MarkDirty()
        {
            m_thrustsChanged = true;
        }

        private float ConvertedPowerFromFuel(ref MyDefinitionId fuelType)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            if(m_dataByFuelType[typeIndex].FuelConversionDirty)
                RecalculateFuelConversion(ref fuelType);

            return m_dataByFuelType[typeIndex].CurrentPowerFromFuel;
        }

        private float RequiredFuelInput(ref MyDefinitionId fuelType)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            return m_dataByFuelType[typeIndex].CurrentRequiredFuelInput;
        }

        public void SetRequiredFuelInput(ref MyDefinitionId fuelType, float newFuelInput)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return;

            m_dataByFuelType[typeIndex].CurrentRequiredFuelInput = newFuelInput;
        }

        protected float PowerAmountToFuel(ref MyDefinitionId fuelType, float powerAmount)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            return powerAmount / (m_dataByFuelType[typeIndex].Efficiency * m_dataByFuelType[typeIndex].EnergyDensity);
        }

        protected float FuelAmountToPower(ref MyDefinitionId fuelType, float fuelAmount)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            return m_dataByFuelType[typeIndex].EnergyDensity * fuelAmount * m_dataByFuelType[typeIndex].Efficiency;
        }

        private void RecalculateFuelConversion(ref MyDefinitionId fuelType)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                return;

            m_dataByFuelType[typeIndex].CurrentPowerFromFuel = FuelAmountToPower(ref fuelType, ResourceSink.CurrentInputByType(fuelType));
            m_dataByFuelType[typeIndex].FuelConversionDirty = false;
        }

        protected bool TryGetTypeIndex(ref MyDefinitionId fuelId, out int typeIndex)
        {
            typeIndex = 0;
            if (m_fuelTypeToIndex.Count > 1)
            {
                if (!m_fuelTypeToIndex.TryGetValue(fuelId, out typeIndex))
                    return false;
            }
            return m_fuelTypeToIndex.Count > 0;
        }

        protected int GetTypeIndex(ref MyDefinitionId fuelId)
        {
            var typeIndex = 0;
            if(m_fuelTypeToIndex.Count > 1)
            {
                int fetchedValue;
                if (m_fuelTypeToIndex.TryGetValue(fuelId, out fetchedValue))
                    typeIndex = fetchedValue;
            }
            return typeIndex;
        }

        protected abstract void UpdateThrustStrength(HashSet<MyEntity> entities, float thrustForce);
        protected abstract bool RecomputeOverriddenParameters(MyEntity thrustEntity);
        protected abstract bool IsUsed(MyEntity thrustEntity);
        protected abstract float ForceMagnitude(MyEntity thrustEntity);
        protected abstract float CalculateForceMultiplier(MyEntity thrustEntity);
        protected abstract float MaxPowerConsumption(MyEntity thrustEntity);
        protected abstract float MinPowerConsumption(MyEntity thrustEntity);
        protected abstract MyDefinitionId FuelType(MyEntity thrustEntity);

		public override string ComponentTypeDebugString { get { return "Thrust Component"; } }
    }
}
