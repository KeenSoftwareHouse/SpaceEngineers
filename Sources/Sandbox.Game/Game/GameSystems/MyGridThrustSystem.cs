using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;

using VRage.Utils;
using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics;
using Sandbox.Common;
using VRage;
using VRage.Components;

namespace Sandbox.Game.GameSystems
{
    public class MyGridThrustSystem : IMyPowerConsumer
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

        private static DirectionComparer m_directionComparer = new DirectionComparer();

        #region Fields
        private float m_currentRequiredPowerInput;
        private MyCubeGrid m_grid;
        private Vector3 m_maxNegativeThrust;
        private Vector3 m_maxPositiveThrust;
        private Dictionary<Vector3I, float> m_maxRequirementsByDirection;
        private float m_minPowerInputTotal;
        private Dictionary<Vector3I, HashSet<MyThrust>> m_thrustsByDirection;

        private Vector3 m_totalThrustOverride;
        private float m_totalThrustOverridePower;

        // Levitation period length in seconds
        private float m_levitationPeriodLength = 1.3f;
        private float m_levitationTorqueCoeficient = 0.25f;

        /// <summary>
        /// True whenever thrust was added or removed.
        /// </summary>
        private bool m_thrustsChanged;
        private bool m_enabled;

        #endregion

        #region Properties
        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        /// <summary>
        /// Torque and thrust wanted by player (from input).
        /// </summary>
        public Vector3 ControlThrust;

        /// <summary>
        /// Thrust wanted by AutoPilot
        /// </summary>
        public Vector3 AutoPilotThrust;

        public bool AutopilotEnabled;

        public bool IsPowered
        {
            get { return PowerReceiver.IsPowered; }
        }

        /// <summary>
        /// Final thrust (clamped by available power, added anti-gravity, slowdown).
        /// </summary>
        public Vector3 Thrust
        {
            get;
            private set;
        }

        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                {
                    m_enabled = value;
                    if (!m_enabled)
                    {
                        RequiredPowerInput = 0.0f;
                        PowerReceiver.Update();
                    }
                }
            }
        }

        public int ThrustCount
        {
            get;
            private set;
        }

        public bool DampenersEnabled
        {
            get;
            set;
        }

        #endregion

        public MyGridThrustSystem(MyCubeGrid grid)
        {
            MyDebug.AssertDebug(grid != null);

            m_maxRequirementsByDirection = new Dictionary<Vector3I, float>(6, m_directionComparer);
            m_thrustsByDirection = new Dictionary<Vector3I, HashSet<MyThrust>>(6, m_directionComparer);
            m_thrustsByDirection[Vector3I.Backward] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Forward] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Right] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Left] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Down] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Up] = new HashSet<MyThrust>();
            Enabled = true;
            m_thrustsChanged = true;
            m_grid = grid;
            ThrustCount = 0;
            DampenersEnabled = true;
            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Thrust,
                true,
                0f,
                () => RequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
        }

        public void Register(MyThrust thrust)
        {
            Debug.Assert(thrust != null);
            Debug.Assert(!IsRegistered(thrust));
            m_thrustsByDirection[thrust.ThrustForwardVector].Add(thrust);
            m_thrustsChanged = true;
            thrust.EnabledChanged += thrust_EnabledChanged;
            thrust.SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            ++ThrustCount;
        }

        public bool IsRegistered(MyThrust thrust)
        {
            Debug.Assert(thrust != null);
            return m_thrustsByDirection[thrust.ThrustForwardVector].Contains(thrust);
        }

        public void Unregister(MyThrust thrust)
        {
            Debug.Assert(thrust != null);
            Debug.Assert(IsRegistered(thrust));
            m_thrustsByDirection[thrust.ThrustForwardVector].Remove(thrust);
            m_thrustsChanged = true;
            thrust.EnabledChanged -= thrust_EnabledChanged;
            thrust.SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;
            --ThrustCount;
        }

        void StopLevitation()
        {
            if (!MyFakes.SMALL_SHIP_LEVITATION)
                return;

            var max = m_levitationTorqueCoeficient * m_grid.Physics.Mass;
            var maxTorque = new Vector3(max, 0, max);
            var maxVelocity = maxTorque / m_grid.Physics.RigidBody.InertiaTensor.Scale * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            maxVelocity *= 1 / (MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * m_levitationPeriodLength);

            if (m_grid.Physics.RigidBody.AngularVelocity.LengthSquared() < maxVelocity.LengthSquared())
            {
                m_grid.Physics.AngularVelocity = Vector3.Zero;
            }
        }

        void UpdateLevitation()
        {
            if (!MyFakes.SMALL_SHIP_LEVITATION)
                return;

            if (ControlThrust.LengthSquared() <= float.Epsilon * float.Epsilon)
            {
                if (PowerReceiver.SuppliedRatio > 0)
                {
                    float globalOffset = MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000.0f;
                    float x = (float)Math.Sin(globalOffset * m_levitationPeriodLength);
                    float y = 0;// (float)Math.Sin(globalOffset * periodLength);
                    float z = (float)Math.Sin(globalOffset * m_levitationPeriodLength + m_levitationPeriodLength / 2);

                    var torque = new Vector3(x, y, z) * m_levitationTorqueCoeficient * m_grid.Physics.Mass * PowerReceiver.SuppliedRatio;
                    m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Vector3.Zero, null, torque);
                }
            }
        }

        public void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin();
            if (m_thrustsChanged)
                RecomputeThrustParameters();

            if (Enabled && m_grid.Physics != null)
            {
                if (m_grid.GridSizeEnum == Common.ObjectBuilders.MyCubeSize.Small)
                {
                    UpdateLevitation();
                }

                UpdateThrusts();
            }

            ProfilerShort.End();
        }

        private Vector3 ComputeBaseThrust(Vector3 direction)
        {
            Matrix invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();

            Vector3 localVelocity = Vector3.Transform(m_grid.Physics.LinearVelocity, ref invWorldRot);
            Vector3 positiveControl = Vector3.Clamp(direction, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(direction, -Vector3.One, Vector3.Zero);
            Vector3 slowdownControl = Vector3.Zero;
            if (DampenersEnabled)
                slowdownControl = Vector3.IsZeroVector(direction, 0.001f) * Vector3.IsZeroVector(m_totalThrustOverride);

            Vector3 thrust = negativeControl * m_maxNegativeThrust + positiveControl * m_maxPositiveThrust;
            thrust = Vector3.Clamp(thrust, -m_maxNegativeThrust, m_maxPositiveThrust);

            const float STOPPING_TIME = 0.5f;
            var slowdownAcceleration = -localVelocity / STOPPING_TIME;
            var slowdownThrust = slowdownAcceleration * m_grid.Physics.Mass * slowdownControl;
            thrust = Vector3.Clamp(thrust + slowdownThrust, -m_maxNegativeThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER, m_maxPositiveThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);

            return thrust;
        }

        public Vector3 ComputeAiThrust(Vector3 direction)
        {
            Matrix invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();

            Vector3 positiveControl = Vector3.Clamp(direction, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(direction, -Vector3.One, Vector3.Zero);

            Vector3 positiveGravity = Vector3.Clamp(-Vector3.Transform(m_grid.Physics.Gravity, ref invWorldRot) * m_grid.Physics.Mass, Vector3.Zero, Vector3.PositiveInfinity);
            Vector3 negativeGravity = Vector3.Clamp(-Vector3.Transform(m_grid.Physics.Gravity, ref invWorldRot) * m_grid.Physics.Mass, Vector3.NegativeInfinity, Vector3.Zero);

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
            Vector3 localVelocity = Vector3.Transform(m_grid.Physics.LinearVelocity + m_grid.Physics.Gravity / 2.0f, ref invWorldRot);
            Vector3 slowdownControl = Vector3.IsZeroVector(direction, 0.001f);
            var slowdownAcceleration = -localVelocity / STOPPING_TIME;
            var slowdownThrust = slowdownAcceleration * m_grid.Physics.Mass * slowdownControl;
            thrust = Vector3.Clamp(thrust + slowdownThrust, -m_maxNegativeThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER, m_maxPositiveThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);

            return thrust;
        }

        private void FlipNegativeInfinity(ref Vector3 v)
        {
            if (v.X == float.NegativeInfinity) v.X = float.PositiveInfinity;
            if (v.Y == float.NegativeInfinity) v.Y = float.PositiveInfinity;
            if (v.Z == float.NegativeInfinity) v.Z = float.PositiveInfinity;
        }

        private Vector3 ApplyThrustModifiers(Vector3 thrust)
        {
            thrust += m_totalThrustOverride;
            thrust *= PowerReceiver.SuppliedRatio;
            thrust *= MyFakes.THRUST_FORCE_RATIO;

            return thrust;
        }

        private void UpdatePowerAndThrustStrength(Vector3 thrust, bool updateThrust)
        {
            // Calculate ratio of usage for different directions.
            Vector3 thrustPositive = thrust / (m_maxPositiveThrust + 0.0000001f);
            Vector3 thrustNegative = -thrust / (m_maxNegativeThrust + 0.0000001f);
            thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
            thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);

            // When using joystick, there may be fractional values, not just 0 and 1.
            float requiredPower = 0;
            requiredPower += (thrustPositive.X > 0) ? thrustPositive.X * GetMaxRequirement(Vector3I.Left) : 0;
            requiredPower += (thrustPositive.Y > 0) ? thrustPositive.Y * GetMaxRequirement(Vector3I.Down) : 0;
            requiredPower += (thrustPositive.Z > 0) ? thrustPositive.Z * GetMaxRequirement(Vector3I.Forward) : 0;
            requiredPower += (thrustNegative.X > 0) ? thrustNegative.X * GetMaxRequirement(Vector3I.Right) : 0;
            requiredPower += (thrustNegative.Y > 0) ? thrustNegative.Y * GetMaxRequirement(Vector3I.Up) : 0;
            requiredPower += (thrustNegative.Z > 0) ? thrustNegative.Z * GetMaxRequirement(Vector3I.Backward) : 0;
            requiredPower += m_totalThrustOverridePower;
            if (requiredPower < m_minPowerInputTotal)
                requiredPower = m_minPowerInputTotal;

            // Setting this notifies power distributor who updates power input and thus changes SuppliedPowerRatio.
            RequiredPowerInput = requiredPower;
            PowerReceiver.Update();

            if (updateThrust)
            {
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Left], thrustPositive.X, PowerReceiver.SuppliedRatio);
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Down], thrustPositive.Y, PowerReceiver.SuppliedRatio);
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Forward], thrustPositive.Z, PowerReceiver.SuppliedRatio);
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Right], thrustNegative.X, PowerReceiver.SuppliedRatio);
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Up], thrustNegative.Y, PowerReceiver.SuppliedRatio);
                UpdateThrustStrength(m_thrustsByDirection[Vector3I.Backward], thrustNegative.Z, PowerReceiver.SuppliedRatio);
            }
        }
        
        public Vector3 GetThrustForDirection(Vector3 direction)
        {
            Vector3 thrust = ComputeBaseThrust(direction);
            UpdatePowerAndThrustStrength(thrust, false);
            thrust = ApplyThrustModifiers(thrust);
            return thrust;
        }

        public HashSet<MyThrust> GetThrustersForDirection(Vector3I direction)
        {
            HashSet<MyThrust> thrustersForDirection;
            m_thrustsByDirection.TryGetValue(direction, out thrustersForDirection);
            return thrustersForDirection;
        }

        public Vector3 GetAutoPilotThrustForDirection(Vector3 direction)
        {
            Vector3 thrust = ComputeAiThrust(direction);
            UpdatePowerAndThrustStrength(thrust, false);
            thrust = ApplyThrustModifiers(thrust);
            return thrust;
        }

        private void UpdateThrusts()
        {
            Vector3 thrust;
            if (AutopilotEnabled)
            {
                thrust = ComputeAiThrust(AutoPilotThrust);
            }
            else
            {
                thrust = ComputeBaseThrust(ControlThrust);
            }
            UpdatePowerAndThrustStrength(thrust, true);
            thrust = ApplyThrustModifiers(thrust);

            Thrust = thrust;

            if (m_grid.GridSystems.ControlSystem.IsLocallyControlled || (!m_grid.GridSystems.ControlSystem.IsControlled && Sync.IsServer) || (false && Sync.IsServer))
            {
                if (Thrust.LengthSquared() > 0.001f)
                {
                    if (m_grid.Physics.Enabled)
                        m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Thrust, null, null);
                }

                const float stoppingVelocitySq = 0.001f * 0.001f;
                if (m_grid.Physics.Enabled)
                {
                    if (m_grid.Physics.LinearVelocity != Vector3.Zero && m_grid.Physics.LinearVelocity.LengthSquared() < stoppingVelocitySq && m_grid.Physics.RigidBody.IsActive)
                    {
                        m_grid.Physics.LinearVelocity = Vector3.Zero;
                    }
                }
            }
        }

        private static void UpdateThrustStrength(HashSet<MyThrust> thrusters, float thrustForce, float suppliedPowerRatio)
        {
            foreach (var thrust in thrusters)
            {
                if (IsOverridden(thrust))
                    thrust.CurrentStrength = thrust.ThrustOverride * suppliedPowerRatio / thrust.ThrustForce.Length();
                else if (IsUsed(thrust))
                    thrust.CurrentStrength = thrustForce * suppliedPowerRatio;
                else
                    thrust.CurrentStrength = 0;
            }
        }

        private void RecomputeThrustParameters()
        {
            m_totalThrustOverride = Vector3.Zero;
            m_totalThrustOverridePower = 0;

            m_maxPositiveThrust = new Vector3();
            m_maxNegativeThrust = new Vector3();
            m_maxRequirementsByDirection.Clear();
            MaxRequiredPowerInput = 0.0f;
            float minRequiredPower = 0;
            foreach (var dir in m_thrustsByDirection)
            {
                float maxRequiredPower = 0;
                foreach (var thrust in dir.Value)
                {
                    if (IsOverridden(thrust))
                    {
                        m_totalThrustOverride += thrust.ThrustOverride * -thrust.ThrustForwardVector;
                        minRequiredPower += thrust.MinPowerConsumption;
                        m_totalThrustOverridePower += thrust.ThrustOverride / thrust.ThrustForce.Length() * thrust.MaxPowerConsumption;
                        continue;
                    }

                    if (!IsUsed(thrust))
                        continue;

                    m_maxPositiveThrust += Vector3.Clamp(thrust.ThrustForce, Vector3.Zero, Vector3.PositiveInfinity);
                    m_maxNegativeThrust += -Vector3.Clamp(thrust.ThrustForce, Vector3.NegativeInfinity, Vector3.Zero);
                    minRequiredPower += thrust.MinPowerConsumption;
                    maxRequiredPower += thrust.MaxPowerConsumption;
                }
                m_maxRequirementsByDirection[dir.Key] = maxRequiredPower;
            }
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Forward), GetMaxRequirement(Vector3I.Backward));
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Left), GetMaxRequirement(Vector3I.Right));
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Up), GetMaxRequirement(Vector3I.Down));

            m_thrustsChanged = false;
            m_minPowerInputTotal = minRequiredPower;

            PowerReceiver.MaxRequiredInput = MaxRequiredPowerInput + m_totalThrustOverridePower;
            PowerReceiver.Update();
        }

        private float GetMaxRequirement(Vector3I direction)
        {
            return m_maxRequirementsByDirection[direction];
        }

        public float MaxRequiredPowerInput
        {
            get;
            private set;
        }

        public float RequiredPowerInput
        {
            get { return m_currentRequiredPowerInput; }
            private set
            {
                var delta = value - m_currentRequiredPowerInput;
                var threshold = Math.Max(m_currentRequiredPowerInput * 0.01f, MyEnergyConstants.MIN_REQUIRED_POWER_THRUST_CHANGE_THRESHOLD);
                if (!MyUtils.IsZero(delta, threshold))
                    m_currentRequiredPowerInput = value;
            }
        }

        public void MarkDirty()
        {
            m_thrustsChanged = true;
        }

        private void thrust_EnabledChanged(MyTerminalBlock obj)
        {
            m_thrustsChanged = true;
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            m_thrustsChanged = true;
        }

        private static bool IsOverridden(MyThrust thrust)
        {
            return thrust.Enabled && thrust.IsFunctional && thrust.ThrustOverride > 0 && !thrust.CubeGrid.GridSystems.ThrustSystem.AutopilotEnabled;
        }

        private static bool IsUsed(MyThrust thrust)
        {
            // Overridden thrusts are calculated separately
            return thrust.Enabled && thrust.IsFunctional && thrust.ThrustOverride == 0;
        }

        private void Receiver_IsPoweredChanged()
        {
            foreach (var entry in m_thrustsByDirection)
                foreach (var thrust in entry.Value)
                    thrust.UpdateIsWorking();
        }


    }
}
