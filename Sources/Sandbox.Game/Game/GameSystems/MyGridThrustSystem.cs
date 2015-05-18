// NOTE: To adjust aggressiveness of thruster rotational damping, change STOPPING_TIME constant in AdjustThrustForRotation() method.

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

//using Sandbox.Game.Gui;

namespace Sandbox.Game.GameSystems
{
    class MyGridThrustSystem : IMyPowerConsumer
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

        private Vector3 m_currentTorque;

        public Vector3 ControlTorque;

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
            thrust.GridCenterPos = (thrust.Min + thrust.Max) / 2.0f;
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

            //MyHud.Notifications.Add(new MyHudNotificationDebug("ZzzZzz"));

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

        private void UpdateThrusts()
        {
            Matrix invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();

            Vector3 localVelocity = Vector3.Transform(m_grid.Physics.LinearVelocity, ref invWorldRot);
            Vector3 positiveControl = Vector3.Clamp(ControlThrust, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(ControlThrust, -Vector3.One, Vector3.Zero);
            Vector3 slowdownControl = Vector3.Zero;

            if (DampenersEnabled)
                slowdownControl = Vector3.IsZeroVector(ControlThrust, 0.001f) * Vector3.IsZeroVector(m_totalThrustOverride);

            Thrust = negativeControl * m_maxNegativeThrust + positiveControl * m_maxPositiveThrust;
            Thrust = Vector3.Clamp(Thrust, -m_maxNegativeThrust, m_maxPositiveThrust);

            const float STOPPING_TIME = 0.5f;
            var slowdownAcceleration = -localVelocity / STOPPING_TIME;
            var slowdownThrust = slowdownAcceleration * m_grid.Physics.Mass * slowdownControl;
            Thrust = Vector3.Clamp(Thrust + slowdownThrust, -m_maxNegativeThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER, m_maxPositiveThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);
            
            // Calculate ratio of usage for different directions.
            Vector3 thrustPositive = Thrust / (m_maxPositiveThrust + 0.0000001f);
            Vector3 thrustNegative = -Thrust / (m_maxNegativeThrust + 0.0000001f);
            thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
            thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);

            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Left    ], thrustPositive.X, ref invWorldRot);
            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Down    ], thrustPositive.Y, ref invWorldRot);
            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Forward ], thrustPositive.Z, ref invWorldRot);
            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Right   ], thrustNegative.X, ref invWorldRot);
            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Up      ], thrustNegative.Y, ref invWorldRot);
            InitializeThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Backward], thrustNegative.Z, ref invWorldRot);

            Vector3 localAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);
            Vector3 desiredAngularVelocity = ControlTorque * 10;
            Vector3 adjustedThrust = Thrust;
            // Do not adjust for rotation when thrusters on opposite side are overidden or in linear acceleration mode.
            if (thrustNegative.X <= 0.001f && m_totalThrustOverride.X >= -1.0f) 
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Left    ], Vector3.Right   , ref adjustedThrust, m_maxPositiveThrust.X, localAngularVelocity, desiredAngularVelocity);
            if (thrustNegative.Y <= 0.001f && m_totalThrustOverride.Y >= -1.0f)
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Down    ], Vector3.Up      , ref adjustedThrust, m_maxPositiveThrust.Y, localAngularVelocity, desiredAngularVelocity);
            if (thrustNegative.Z <= 0.001f && m_totalThrustOverride.Z >= -1.0f)
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Forward ], Vector3.Backward, ref adjustedThrust, m_maxPositiveThrust.Z, localAngularVelocity, desiredAngularVelocity);
            if (thrustPositive.X <= 0.001f && m_totalThrustOverride.X <= 1.0f)
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Right   ], Vector3.Right   , ref adjustedThrust, m_maxNegativeThrust.X, localAngularVelocity, desiredAngularVelocity);
            if (thrustPositive.Y <= 0.001f && m_totalThrustOverride.Y <= 1.0f)
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Up      ], Vector3.Up      , ref adjustedThrust, m_maxNegativeThrust.Y, localAngularVelocity, desiredAngularVelocity);
            if (thrustPositive.Z <= 0.001f && m_totalThrustOverride.Z <= 1.0f)
                AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Backward], Vector3.Backward, ref adjustedThrust, m_maxNegativeThrust.Z, localAngularVelocity, desiredAngularVelocity);
            ControlTorque = Vector3.Zero;

            // Recalculate ratio of usage after rotational ajustments.
            Thrust         = adjustedThrust;
            thrustPositive = Thrust / (m_maxPositiveThrust + 0.0000001f);
            thrustNegative = -Thrust / (m_maxNegativeThrust + 0.0000001f);
            thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
            thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);

            // When using joystick, there may be fractional values, not just 0 and 1.
            float requiredPower = 0;
            requiredPower += (thrustPositive.X > 0) ? thrustPositive.X * GetMaxRequirement(Vector3I.Left    ) : 0;
            requiredPower += (thrustPositive.Y > 0) ? thrustPositive.Y * GetMaxRequirement(Vector3I.Down    ) : 0;
            requiredPower += (thrustPositive.Z > 0) ? thrustPositive.Z * GetMaxRequirement(Vector3I.Forward ) : 0;
            requiredPower += (thrustNegative.X > 0) ? thrustNegative.X * GetMaxRequirement(Vector3I.Right   ) : 0;
            requiredPower += (thrustNegative.Y > 0) ? thrustNegative.Y * GetMaxRequirement(Vector3I.Up      ) : 0;
            requiredPower += (thrustNegative.Z > 0) ? thrustNegative.Z * GetMaxRequirement(Vector3I.Backward) : 0;
            requiredPower += m_totalThrustOverridePower;
            if (requiredPower < m_minPowerInputTotal)
                requiredPower = m_minPowerInputTotal;

            // Setting this notifies power distributor who updates power input and thus changes SuppliedPowerRatio.
            RequiredPowerInput = requiredPower;
            PowerReceiver.Update();

            Thrust += m_totalThrustOverride;
            Thrust *= PowerReceiver.SuppliedRatio;
            Thrust *= MyFakes.THRUST_FORCE_RATIO;

            m_currentTorque = Vector3.Zero;
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Left    ], thrustPositive.X, PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Down    ], thrustPositive.Y, PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Forward ], thrustPositive.Z, PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Right   ], thrustNegative.X, PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Up      ], thrustNegative.Y, PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Backward], thrustNegative.Z, PowerReceiver.SuppliedRatio);

            if (m_grid.GridSystems.ControlSystem.IsLocallyControlled || (!m_grid.GridSystems.ControlSystem.IsControlled && Sync.IsServer) || (false && Sync.IsServer))
            {
                if (Thrust.LengthSquared() > 0.001f)
                {
                    if (m_grid.Physics.Enabled)
                        m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, Thrust, null, m_currentTorque);
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

        private void InitializeThrottleAndCOMOffset(HashSet<MyThrust> thrusters, float throttle, ref Matrix invWorldRot)
        {
            foreach (var curThrust in thrusters)
            {
                if (IsOverridden(curThrust) || IsUsed(curThrust))
                {
                    curThrust.COMOffsetVector = Vector3.Transform(m_grid.GridIntegerToWorld(curThrust.GridCenterPos) - m_grid.Physics.CenterOfMassWorld, ref invWorldRot);
                    curThrust.CurrentStrength = throttle;   // Safe to do on overriden thrusters, as UpdateThrustStrength() will replace it with a correct value.
                }
            }
        }

        private void AdjustThrustForRotation(HashSet<MyThrust> thrusters, Vector3 thrustDirection, ref Vector3 thrustTotalForce, float thrustMaxForce, Vector3 localAngularVelocity, Vector3 desiredAngularVelocity)
        {
            const float STOPPING_TIME = 0.2f;
            Vector3 localLinearVelocity, desiredLinearVelocity, desiredAcceleration, currentThrust, extraThrust, newThrust;

            foreach (var curThrust in thrusters)
            {
                if (IsUsed(curThrust))      // Skip thrusters on override
                {
                    currentThrust         = curThrust.ThrustForce * curThrust.CurrentStrength;
                    localLinearVelocity   = Vector3.Cross(  localAngularVelocity, curThrust.COMOffsetVector);
                    desiredLinearVelocity = Vector3.Cross(desiredAngularVelocity, curThrust.COMOffsetVector);
                    desiredAcceleration   = (desiredLinearVelocity - localLinearVelocity) / (STOPPING_TIME * thrustMaxForce / curThrust.ThrustForce.Length());
                    extraThrust           = m_grid.Physics.Mass * desiredAcceleration * thrustDirection;
                    newThrust             = currentThrust + extraThrust;
                    if (Vector3.Dot(newThrust, curThrust.ThrustForce) <= 0.0f)  // desired force is opposite to thruster's force?
                        newThrust = Vector3.Zero;
                    else if (newThrust.LengthSquared() > curThrust.ThrustForce.LengthSquared())
                        newThrust = curThrust.ThrustForce;
                    curThrust.CurrentStrength = newThrust.Length() / curThrust.ThrustForce.Length();
                    thrustTotalForce         += newThrust - currentThrust;
                }
            }
        }

        private /*static*/ void UpdateThrustStrength(HashSet<MyThrust> thrusters, float thrustForce, float suppliedPowerRatio)
        {
            foreach (var thrust in thrusters)
            {
                if (IsOverridden(thrust))
                {
                    thrust.CurrentStrength = thrust.ThrustOverride * suppliedPowerRatio / thrust.ThrustForce.Length();
                    m_currentTorque += Vector3.Cross(thrust.COMOffsetVector, thrust.ThrustForce * thrust.CurrentStrength * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);
                }
                else if (IsUsed(thrust))
                {
                    thrust.CurrentStrength *= suppliedPowerRatio;   // thrust.CurrentStrength is initialized in InitializeThrottleAndCOMOffset()
                                                                    // and optionally adjusted in AdjustThrustForRotation().
                    m_currentTorque += Vector3.Cross(thrust.COMOffsetVector, thrust.ThrustForce * thrust.CurrentStrength * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);
                }
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
            return thrust.Enabled && thrust.IsFunctional && thrust.ThrustOverride > 0;
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
