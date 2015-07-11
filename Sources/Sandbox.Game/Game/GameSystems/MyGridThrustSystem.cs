// NOTE: To adjust aggressiveness of thruster rotational damping, change DAMPING_CONSTANT in AdjustThrustForRotation() method.

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
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Common;
using VRage;
using VRage.Components;

using Sandbox.Game.Gui;

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
        
        // Only used when thruster torque is enabled (it's currently tied to thruster damage checkbox).
        const int   COM_UPDATE_TICKS      = 10;
        const float MAX_THRUST_CHANGE     = 0.2f;
        const float ANGULAR_STABILISATION = -0.01f;

        private float m_currentRequiredPowerInput;
        private MyCubeGrid m_grid;
        private Vector3 m_maxNegativeThrust;
        private Vector3 m_maxPositiveThrust;
        private Vector3 m_maxNegativeLinearThrust;
        private Vector3 m_maxPositiveLinearThrust;
        private Vector3 m_maxNegativeRotationalThrust;
        private Vector3 m_maxPositiveRotationalThrust;
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

        public  Vector3 ControlTorque;
        private Vector3 m_DesiredAngularVelocityStab;
        private Vector3 m_AngularVelocityAtRelease;
        private bool    m_enableIntegral;
        private bool    m_resetIntegral;
        private int     m_COMUpdateCounter;
        private float   m_lastSuppliedPowerRatio;   // Used by autopilot to estimate thrust. Calling PowerReceiver.Update() more than once
                                                    // per tick results in buggy power usage.
        private bool?   m_lastRCSMode;  // Type is nullable to ensure that CoT is properly initialised at the session start.
        private Vector3 m_prevSpeed;

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

        public Vector3 LocalAngularVelocity { get; private set; }

        public bool    FlyByWireEnabled          { get; set; }
        public bool    CourseEstablished         { get; set; }
        public Vector3 AutopilotAngularDeviation { get; set; }

        #endregion

        public MyGridThrustSystem(MyCubeGrid grid)
        {
            MyDebug.AssertDebug(grid != null);

            m_maxRequirementsByDirection = new Dictionary<Vector3I, float>(6, m_directionComparer);
            m_thrustsByDirection = new Dictionary<Vector3I, HashSet<MyThrust>>(6, m_directionComparer);
            m_thrustsByDirection[Vector3I.Backward] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Forward ] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Right   ] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Left    ] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Down    ] = new HashSet<MyThrust>();
            m_thrustsByDirection[Vector3I.Up      ] = new HashSet<MyThrust>();
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
            thrust.StaticMoment  = thrust.GridCenterPos * (m_grid.GridSize * thrust.ThrustForce.Length()); 
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

            //MyHud.Notifications.Add(new MyHudNotificationDebug("ZzzZzz", 20));

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
            Matrix  invWorldRot     = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
            Vector3 localVelocity   = Vector3.Transform(m_grid.Physics.LinearVelocity, ref invWorldRot);
            Vector3 positiveControl = Vector3.Clamp(direction,  Vector3.Zero, Vector3.One );
            Vector3 negativeControl = Vector3.Clamp(direction, -Vector3.One , Vector3.Zero);

            Vector3 thrust = negativeControl * m_maxNegativeLinearThrust + positiveControl * m_maxPositiveLinearThrust;
            thrust = Vector3.Clamp(thrust, -m_maxNegativeLinearThrust, m_maxPositiveLinearThrust);

            if (DampenersEnabled)
            {
                const float STOPPING_TIME = 0.5f;

                var slowdownControl      = Vector3.IsZeroVector(direction, 0.001f) * Vector3.IsZeroVector(m_totalThrustOverride);
                var slowdownAcceleration = -localVelocity / STOPPING_TIME;
                var localGravityForce    = Vector3.Transform(m_grid.Physics.Gravity, ref invWorldRot) * m_grid.Physics.Mass;
                var slowdownThrust       = slowdownAcceleration * m_grid.Physics.Mass;
                if (FlyByWireEnabled)
                    slowdownThrust -= localGravityForce;
                thrust = Vector3.Clamp(thrust + slowdownThrust * slowdownControl, -m_maxNegativeLinearThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER, m_maxPositiveLinearThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);
            }

            return thrust;
        }

        private Vector3 ComputeAiThrust(Vector3 direction, bool includeSlowdown, bool subtractGravity)
        {
            float thrustX = (direction.X >= 0.0f) ? m_maxPositiveLinearThrust.X : m_maxNegativeLinearThrust.X;
            float thrustY = (direction.Y >= 0.0f) ? m_maxPositiveLinearThrust.Y : m_maxNegativeLinearThrust.Y;
            float thrustZ = (direction.Z >= 0.0f) ? m_maxPositiveLinearThrust.Z : m_maxNegativeLinearThrust.Z;
            float minThrust = (thrustX < thrustY) ? thrustX : thrustY;
            if (minThrust > thrustZ)
                minThrust = thrustZ;
            
            Vector3 adjustedDirection;
            if (minThrust == 0.0f || direction.LengthSquared() < 0.0001f)
                adjustedDirection = direction;
            else
            {
                adjustedDirection  = new Vector3(direction.X * minThrust / thrustX, direction.Y * minThrust / thrustY, direction.Z * minThrust / thrustZ);

                adjustedDirection *= direction.Length() / adjustedDirection.Length();
                if (adjustedDirection.X < -1.0f || adjustedDirection.X > 1.0f)
                    adjustedDirection *= 1.0f / Math.Abs(adjustedDirection.X);
                if (adjustedDirection.Y < -1.0f || adjustedDirection.Y > 1.0f)
                    adjustedDirection *= 1.0f / Math.Abs(adjustedDirection.Y);
                if (adjustedDirection.Z < -1.0f || adjustedDirection.Z > 1.0f)
                    adjustedDirection *= 1.0f / Math.Abs(adjustedDirection.Z);
            }
            
            Matrix invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
            Vector3 positiveControl = Vector3.Clamp(adjustedDirection,  Vector3.Zero, Vector3.One );
            Vector3 negativeControl = Vector3.Clamp(adjustedDirection, -Vector3.One , Vector3.Zero);

            Vector3 localGravity = Vector3.Transform(m_grid.Physics.Gravity, ref invWorldRot);
            Vector3 localGravityForce = localGravity * m_grid.Physics.Mass;
            Vector3 thrust = negativeControl * m_maxNegativeLinearThrust + positiveControl * m_maxPositiveLinearThrust;

            if (adjustedDirection.LengthSquared() > 0.01f && localGravity.LengthSquared() >= 1.0f && Vector3.Dot(thrust, localGravity) > 0.0)
            {
                // Use gravity instead of thrusters to propel the ship downwards.
                var gravityDirection = localGravity;
                gravityDirection.Normalize();
                thrust -= gravityDirection * gravityDirection.Dot(thrust);
            }
            else
                thrust -= localGravityForce;
            thrust = Vector3.Clamp(thrust, -m_maxNegativeLinearThrust, m_maxPositiveLinearThrust);

            if (includeSlowdown)
            {
                const float STOPPING_TIME = 0.5f;

                var localVelocity        = Vector3.Transform(m_grid.Physics.LinearVelocity, ref invWorldRot);
                var localSpeed           = localVelocity.Length();
                var slowdownControl      = Vector3.IsZeroVector(adjustedDirection, 0.001f) * Vector3.IsZeroVector(m_totalThrustOverride);
                var slowdownAcceleration = -localVelocity / STOPPING_TIME;
                var slowdownThrust       = slowdownAcceleration * m_grid.Physics.Mass * slowdownControl;
                thrust = Vector3.Clamp(thrust + slowdownThrust, -m_maxNegativeLinearThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER, m_maxPositiveLinearThrust * MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER);
            }

            if (subtractGravity)
                thrust += localGravityForce;    // Used by remote control block to estimate acceleration in a gravity environemnt.
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

        private void UpdatePowerAndThrustStrength(ref Vector3 thrust)
        {
            const float ROTATION_LIMITER    = 5.0f;
            const float STATIC_MODE_MAX_ACC = 0.2f;   // A threshold linear acceleration value, above which RCS will switch 
                                                      // from stationary to accelerating mode.
                                                      // In stationary mode RCS employs 3 separate centres of thrust, one for each pair
                                                      // of opposite thruster sets in order to maximise turning rate.
                                                      // In accelerating mode centre of mass is used instead as a reference point.
            const float LINEAR_INEFFICIENCY = 0.1f;   // A maximum relative force magnitude at which opposing thrusters are allowed 
                                                      // to push against each other when accelerating. Increasing this value will prioritise
                                                      // turning over linear acceleration, thus improving rotational responsiveness on 
                                                      // unbalanced designs at cost of producing undesired linear movement. Use a value 
                                                      // greater than MyConstants.MAX_THRUST to give rotation a #1 priority at all times.
            
            Matrix  invWorldRot    = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
            Vector3 thrustPositive =  thrust / (m_maxPositiveLinearThrust + 0.0000001f);
            Vector3 thrustNegative = -thrust / (m_maxNegativeLinearThrust + 0.0000001f);
            thrustPositive         = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
            thrustNegative         = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);

            var gyros = m_grid.GridSystems.GyroSystem;
            Vector3 adjustedThrust = Vector3.Zero;
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Left    ], thrustPositive.X, ref adjustedThrust, ref invWorldRot);
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Down    ], thrustPositive.Y, ref adjustedThrust, ref invWorldRot);
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Forward ], thrustPositive.Z, ref adjustedThrust, ref invWorldRot);
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Right   ], thrustNegative.X, ref adjustedThrust, ref invWorldRot);
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Up      ], thrustNegative.Y, ref adjustedThrust, ref invWorldRot);
            InitializeLinearThrottleAndCOMOffset(m_thrustsByDirection[Vector3I.Backward], thrustNegative.Z, ref adjustedThrust, ref invWorldRot);
            if (m_COMUpdateCounter == COM_UPDATE_TICKS)
                m_lastRCSMode = null;
            if (m_COMUpdateCounter <= COM_UPDATE_TICKS)
                ++m_COMUpdateCounter;

            LocalAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);
            var linearVelocity   = m_grid.Physics.LinearVelocity;
            // A quick'n'dirty way of making torque optional. Activating gyroscope override or losing all remote control blocks also disables RCS.
            if (   MySession.Static.ThrusterDamage && !gyros.IsGyroOverrideActive && FlyByWireEnabled
                && (LocalAngularVelocity != Vector3.Zero || linearVelocity != Vector3.Zero || ControlTorque != Vector3.Zero || AutopilotEnabled))
            {
                if (MyFakes.SLOWDOWN_FACTOR_THRUST_MULTIPLIER > 1.0f)
                {
                    // Add a magic force that mimics TurboBrake of stock inertia dampeners. Not 100% accurate, but since it's magic, it's no huge deal.
                    adjustedThrust += thrust - thrustPositive * m_maxPositiveLinearThrust + thrustNegative * m_maxNegativeLinearThrust;
                }
                
                bool useAcceleratingMode = (linearVelocity - m_prevSpeed).Length() / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS > STATIC_MODE_MAX_ACC;
                if (useAcceleratingMode != m_lastRCSMode)
                {
                    if (useAcceleratingMode)
                        UpdateCenterOfThrustAccelerating();
                    else
                    {
                        UpdateCenterOfThrustStationary(m_thrustsByDirection[Vector3I.Left   ], m_thrustsByDirection[Vector3I.Right   ]);
                        UpdateCenterOfThrustStationary(m_thrustsByDirection[Vector3I.Up     ], m_thrustsByDirection[Vector3I.Down    ]);
                        UpdateCenterOfThrustStationary(m_thrustsByDirection[Vector3I.Forward], m_thrustsByDirection[Vector3I.Backward]);
                    }
                }
                m_lastRCSMode = useAcceleratingMode;
                m_prevSpeed   = linearVelocity;

                LocalAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);
                
                bool areGyroscopesSaturated = 0.75f * gyros.MaxGyroForce <= gyros.SlowdownTorque.Length();
                // Don't use thruster stabilisation unless gyroscopes are saturated or inoperative.
                if (ControlTorque.LengthSquared() >= 0.0001f || !areGyroscopesSaturated)  
                    m_enableIntegral = false;
                else
                {
                    if (!m_enableIntegral && !m_resetIntegral)
                    {
                        m_AngularVelocityAtRelease = LocalAngularVelocity;
                        m_resetIntegral = true;
                    }
                    m_enableIntegral = true;
                }

                if (!AutopilotEnabled)
                {
                    m_DesiredAngularVelocityStab = m_enableIntegral ? (m_DesiredAngularVelocityStab + LocalAngularVelocity * ANGULAR_STABILISATION) : Vector3.Zero;
                    CourseEstablished = false;    // Just in case.
                }
                else if (CourseEstablished)
                {
                    // Don't use thruster stabilisation unless gyroscopes are saturated or inoperative.
                    if (!areGyroscopesSaturated)
                        m_DesiredAngularVelocityStab = Vector3.Zero;
                    else
                        m_DesiredAngularVelocityStab = (-AutopilotAngularDeviation) * (ANGULAR_STABILISATION * MyEngineConstants.UPDATE_STEPS_PER_SECOND);
                    m_resetIntegral = false;
                }
                else 
                    m_DesiredAngularVelocityStab = Vector3.Zero;

                if (m_resetIntegral && Vector3.Dot(LocalAngularVelocity, m_AngularVelocityAtRelease) <= 0.0f)
                {
                    m_DesiredAngularVelocityStab = Vector3.Zero;
                    m_resetIntegral = false;
                }
                Vector3 desiredAngularVelocity = ControlTorque * ROTATION_LIMITER + m_DesiredAngularVelocityStab;

                // Do not adjust for rotation when thrusters on opposite side are overidden or in linear acceleration mode.
                if (thrustNegative.X <= LINEAR_INEFFICIENCY && m_totalThrustOverride.X >= -1.0f)
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Left    ], Vector3.Right   , ref adjustedThrust, m_maxPositiveRotationalThrust.X, LocalAngularVelocity, desiredAngularVelocity);
                if (thrustNegative.Y <= LINEAR_INEFFICIENCY && m_totalThrustOverride.Y >= -1.0f)
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Down    ], Vector3.Up      , ref adjustedThrust, m_maxPositiveRotationalThrust.Y, LocalAngularVelocity, desiredAngularVelocity);
                if (thrustNegative.Z <= LINEAR_INEFFICIENCY && m_totalThrustOverride.Z >= -1.0f)
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Forward ], Vector3.Backward, ref adjustedThrust, m_maxPositiveRotationalThrust.Z, LocalAngularVelocity, desiredAngularVelocity);
                if (thrustPositive.X <= LINEAR_INEFFICIENCY && m_totalThrustOverride.X <= 1.0f )
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Right   ], Vector3.Right   , ref adjustedThrust, m_maxNegativeRotationalThrust.X, LocalAngularVelocity, desiredAngularVelocity);
                if (thrustPositive.Y <= LINEAR_INEFFICIENCY && m_totalThrustOverride.Y <= 1.0f )
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Up      ], Vector3.Up      , ref adjustedThrust, m_maxNegativeRotationalThrust.Y, LocalAngularVelocity, desiredAngularVelocity);
                if (thrustPositive.Z <= LINEAR_INEFFICIENCY && m_totalThrustOverride.Z <= 1.0f)
                    AdjustThrustForRotation(m_thrustsByDirection[Vector3I.Backward], Vector3.Backward, ref adjustedThrust, m_maxNegativeRotationalThrust.Z, LocalAngularVelocity, desiredAngularVelocity);
                ControlTorque = Vector3.Zero;
                thrust = adjustedThrust;

                // Recalculate power usage after rotational ajustments.
                thrustPositive = thrustNegative = Vector3.Zero;
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Left    ], ref thrustPositive);
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Down    ], ref thrustPositive);
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Forward ], ref thrustPositive);
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Right   ], ref thrustNegative);
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Up      ], ref thrustNegative);
                SumTotalThrustForPowerUsage(m_thrustsByDirection[Vector3I.Backward], ref thrustNegative);
                thrustPositive =  thrustPositive / (m_maxPositiveThrust + 0.0000001f);
                thrustNegative = -thrustNegative / (m_maxNegativeThrust + 0.0000001f);
                thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
                thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One * MyConstants.MAX_THRUST);
            }
            FlyByWireEnabled = false;  // Needs to be done here and not in the remote control code, as there may be more than 1 RC block.

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

            m_currentTorque = Vector3.Zero;
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Left    ], PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Down    ], PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Forward ], PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Right   ], PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Up      ], PowerReceiver.SuppliedRatio);
            UpdateThrustStrength(m_thrustsByDirection[Vector3I.Backward], PowerReceiver.SuppliedRatio);
            m_lastSuppliedPowerRatio = PowerReceiver.SuppliedRatio;
        }
        
        public Vector3 GetThrustForDirection(Vector3 direction)
        {
            Vector3 thrust = ComputeBaseThrust(direction);
            thrust = (thrust + m_totalThrustOverride) * (m_lastSuppliedPowerRatio * MyFakes.THRUST_FORCE_RATIO);
            return thrust;
        }

        public HashSet<MyThrust> GetThrustersForDirection(Vector3I direction)
        {
            HashSet<MyThrust> thrustersForDirection;
            m_thrustsByDirection.TryGetValue(direction, out thrustersForDirection);
            return thrustersForDirection;
        }

        public Vector3 GetAutoPilotThrustForDirection(Vector3 direction, bool includeSlowdown)
        {
            Vector3 thrust = ComputeAiThrust(direction, includeSlowdown, subtractGravity: true);
            thrust = (thrust + m_totalThrustOverride) * (m_lastSuppliedPowerRatio * MyFakes.THRUST_FORCE_RATIO);
            return thrust;
        }

        private void UpdateThrusts()
        {
            Vector3 thrust;

            if (AutopilotEnabled)
            {
                thrust = ComputeAiThrust(AutoPilotThrust, includeSlowdown: true, subtractGravity: false);
            }
            else
            {
                thrust = ComputeBaseThrust(ControlThrust);
            }
            
            UpdatePowerAndThrustStrength(ref thrust);
            thrust = ApplyThrustModifiers(thrust);

            Thrust = thrust;

            if (m_grid.GridSystems.ControlSystem.IsLocallyControlled || (!m_grid.GridSystems.ControlSystem.IsControlled && Sync.IsServer) || (false && Sync.IsServer))
            {
                if (Thrust.LengthSquared() > 0.001f || m_currentTorque.LengthSquared() > 0.001f)
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

        private void UpdateCenterOfThrustAccelerating()
        {
            foreach (var dir in m_thrustsByDirection)
            {
                foreach (var curThrust in dir.Value)
                    curThrust.COTOffsetVector = curThrust.COMOffsetVector;
            }
        }

        private void UpdateCenterOfThrustStationary(HashSet<MyThrust> thrusters1, HashSet<MyThrust> thrusters2)
        {
            Vector3 totalThrustStaticMoment = Vector3.Zero, totalThrust1 = Vector3.Zero, totalThrust2 = Vector3.Zero;
            bool useCOM = false;

            foreach (var curThrust in thrusters1)
            {
                if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                {
                    totalThrustStaticMoment += curThrust.StaticMoment;
                    totalThrust1            += curThrust.ThrustForce;
                }
                if (totalThrust1.Length() == 0.0f)
                    useCOM = true;
            }
            foreach (var curThrust in thrusters2)
            {
                if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                {
                    totalThrustStaticMoment += curThrust.StaticMoment;
                    totalThrust2            += curThrust.ThrustForce;
                }
                if (totalThrust2.Length() == 0.0f)
                    useCOM = true;
            }

            if (useCOM)
            {
                // No rotational thrusters on one or both sides.
                foreach (var curThrust in thrusters1)
                {
                    if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                        curThrust.COTOffsetVector = curThrust.COMOffsetVector;
                }
                foreach (var curThrust in thrusters2)
                {
                    if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                        curThrust.COTOffsetVector = curThrust.COMOffsetVector;
                }
            }
            else
            {
                // totalThrust1 and totalThrust2 are opposite, so subtraction (instead of addition) is needed to obtain grand total.
                Vector3 COTLocation = totalThrustStaticMoment / (totalThrust1 - totalThrust2).Length();
                foreach (var curThrust in thrusters1)
                {
                    if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                        curThrust.COTOffsetVector = curThrust.GridCenterPos * m_grid.GridSize - COTLocation;
                }
                foreach (var curThrust in thrusters2)
                {
                    if (curThrust.RotationalModeOn && (IsOverridden(curThrust) || IsUsed(curThrust)))
                        curThrust.COTOffsetVector = curThrust.GridCenterPos * m_grid.GridSize - COTLocation;
                }
            }
        }

        private void ScheduleCOMOffsetUpdate(MySlimBlock dummy)
        {
            // Do not reset the counter if the update has already been scheduled.
            if (m_COMUpdateCounter > COM_UPDATE_TICKS)
                m_COMUpdateCounter = 0;
        }

        private void InitializeLinearThrottleAndCOMOffset(HashSet<MyThrust> thrusters,  float throttle, ref Vector3 thrustTotalForce, ref Matrix invWorldRot)
        {
            float linearThrottle;
            
            if (!MySession.Static.ThrusterDamage)
            {
                foreach (var curThrust in thrusters)
                {
                    if (IsUsed(curThrust))
                    {
                        curThrust.CurrentStrength = throttle;
                    }
                }
            }
            else
            {
                if (m_COMUpdateCounter == COM_UPDATE_TICKS)
                {
                    foreach (var curThrust in thrusters)
                    {
                        curThrust.COMOffsetVector = Vector3.Transform(m_grid.GridIntegerToWorld(curThrust.GridCenterPos) - m_grid.Physics.CenterOfMassWorld, ref invWorldRot);
                        curThrust.ThrustTorque    = Vector3.Cross(curThrust.COMOffsetVector, curThrust.ThrustForce);
                    }
                }
                foreach (var curThrust in thrusters)
                {
                    if (IsUsed(curThrust))
                    {
                        // To suppress an annoying flicker, change thruster output gradually.
                        curThrust.PrevStrength    = curThrust.CurrentStrength;
                        linearThrottle            = curThrust.LinearModeOn ? throttle : 0.0f;
                        curThrust.CurrentStrength = MathHelper.Clamp(linearThrottle, curThrust.PrevStrength - MAX_THRUST_CHANGE, curThrust.PrevStrength + MAX_THRUST_CHANGE);
                        thrustTotalForce         += curThrust.ThrustForce * curThrust.CurrentStrength;
                    }
                }
            }
        }

        private void SumTotalThrustForPowerUsage(HashSet<MyThrust> thrusters, ref Vector3 totalThrust)
        {
            foreach (var curThrust in thrusters)
            {
                totalThrust += curThrust.ThrustForce * curThrust.CurrentStrength;
            }
        }

        private void AdjustThrustForRotation(HashSet<MyThrust> thrusters, Vector3 primaryAxisDirection, ref Vector3 thrustTotalForce, float thrustMaxForce, Vector3 localAngularVelocity, Vector3 desiredAngularVelocity)
        {
            const float DAMPING_CONSTANT             = 10.0f;
            const float MAX_THRUST_TO_ALLOW_SKIPPING = 0.1f;
            Vector3 desiredAcceleration, currentThrust, newThrust, rotForceDir, maxThrust;
            float   thrustMagnitude;

            foreach (var curThrust in thrusters)
            {
                if (!IsUsed(curThrust) || !curThrust.RotationalModeOn)      // Skip thrusters on override and linear-only
                    continue;
                
                // Skip thrusters with relative output less than MAX_THRUST_TO_ALLOW_SKIPPING, 
                // whose force direction is opposite to one required for turning.
                // Remember that ThrustForwardVector is opposite to actual force direction.
                rotForceDir =  Vector3.Cross((desiredAngularVelocity - localAngularVelocity), curThrust.COTOffsetVector) * primaryAxisDirection;
                if (curThrust.CurrentStrength <= MAX_THRUST_TO_ALLOW_SKIPPING && Vector3.Dot(rotForceDir, curThrust.ThrustForwardVector) >= 0.0f)
                    continue;

                maxThrust           = curThrust.ThrustForce;
                thrustMagnitude     = maxThrust.Length(); 
                currentThrust       = maxThrust * curThrust.CurrentStrength;
                desiredAcceleration = DAMPING_CONSTANT * (rotForceDir * thrustMagnitude / thrustMaxForce);
                newThrust           = currentThrust + m_grid.Physics.Mass * desiredAcceleration;
                if (Vector3.Dot(newThrust, curThrust.ThrustForwardVector) >= 0.0f)  // desired force is opposite to thruster's force?
                    newThrust = Vector3.Zero;
                else if (newThrust.LengthSquared() > thrustMagnitude * thrustMagnitude)
                    newThrust = maxThrust;

                // To suppress an annoying flicker, change thruster output gradually.
                curThrust.CurrentStrength = MathHelper.Clamp(newThrust.Length() / thrustMagnitude, curThrust.PrevStrength - MAX_THRUST_CHANGE, curThrust.PrevStrength + MAX_THRUST_CHANGE);
                thrustTotalForce += maxThrust * curThrust.CurrentStrength - currentThrust;
            }
        }

        private /*static*/ void UpdateThrustStrength(HashSet<MyThrust> thrusters, float suppliedPowerRatio)
        {
            if (MySession.Static.ThrusterDamage)
            {
                foreach (var thrust in thrusters)
                {
                    if (IsOverridden(thrust))
                    {
                        thrust.CurrentStrength = thrust.ThrustOverride * suppliedPowerRatio / thrust.ThrustForce.Length();
                        m_currentTorque       += thrust.ThrustTorque * thrust.CurrentStrength;
                    }
                    else if (IsUsed(thrust))
                    {
                        thrust.CurrentStrength *= suppliedPowerRatio;   // thrust.CurrentStrength is initialized in InitializeThrottleAndCOMOffset()
                                                                        // and optionally adjusted in AdjustThrustForRotation().
                        m_currentTorque        += thrust.ThrustTorque * thrust.CurrentStrength;
                    }
                    else
                        thrust.CurrentStrength = 0;
                }
            }
            else
            {
                foreach (var thrust in thrusters)
                {
                    if (IsOverridden(thrust))
                    {
                        thrust.CurrentStrength = thrust.ThrustOverride * suppliedPowerRatio / thrust.ThrustForce.Length();
                    }
                    else if (IsUsed(thrust))
                    {
                        thrust.CurrentStrength *= suppliedPowerRatio;   // thrust.CurrentStrength is initialized in InitializeThrottleAndCOMOffset()
                    }
                    else
                        thrust.CurrentStrength = 0;
                }
            }
        }

        private void RecomputeThrustParameters()
        {
            Vector3 positiveThrust, negativeThrust, thrustForce;
            
            //MyHud.Notifications.Add(new MyHudNotificationDebug("ZzzZzz"));

            m_grid.OnBlockAdded   -= ScheduleCOMOffsetUpdate;
            m_grid.OnBlockRemoved -= ScheduleCOMOffsetUpdate;
            if (ThrustCount > 0)
            {
                m_grid.OnBlockAdded   += ScheduleCOMOffsetUpdate;
                m_grid.OnBlockRemoved += ScheduleCOMOffsetUpdate;
            }
            ScheduleCOMOffsetUpdate(null);
            
            m_totalThrustOverride = Vector3.Zero;
            m_totalThrustOverridePower = 0;

            m_maxPositiveThrust           = new Vector3();
            m_maxNegativeThrust           = new Vector3();
            m_maxPositiveLinearThrust     = new Vector3();
            m_maxNegativeLinearThrust     = new Vector3();
            m_maxPositiveRotationalThrust = new Vector3();
            m_maxNegativeRotationalThrust = new Vector3();
            m_maxRequirementsByDirection.Clear();
            MaxRequiredPowerInput = 0.0f;
            float minRequiredPower = 0;
            foreach (var dir in m_thrustsByDirection)
            {
                float maxRequiredPower = 0;
                foreach (var thrust in dir.Value)
                {
                    thrustForce = thrust.ThrustForce;
                    
                    if (IsOverridden(thrust))
                    {
                        m_totalThrustOverride += thrust.ThrustOverride * -thrust.ThrustForwardVector;
                        minRequiredPower += thrust.MinPowerConsumption;
                        m_totalThrustOverridePower += thrust.ThrustOverride / thrustForce.Length() * thrust.MaxPowerConsumption;
                        continue;
                    }

                    if (!IsUsed(thrust))
                        continue;

                    positiveThrust =  Vector3.Clamp(thrustForce, Vector3.Zero            , Vector3.PositiveInfinity);
                    negativeThrust = -Vector3.Clamp(thrustForce, Vector3.NegativeInfinity, Vector3.Zero            );
                    m_maxPositiveThrust += positiveThrust;
                    m_maxNegativeThrust += negativeThrust;
                    if (!MySession.Static.ThrusterDamage || thrust.LinearModeOn)
                    {
                        m_maxPositiveLinearThrust += positiveThrust;
                        m_maxNegativeLinearThrust += negativeThrust;
                    }
                    if (MySession.Static.ThrusterDamage && thrust.RotationalModeOn)
                    {
                        m_maxPositiveRotationalThrust += positiveThrust;
                        m_maxNegativeRotationalThrust += negativeThrust;
                    }
                    minRequiredPower += thrust.MinPowerConsumption;
                    maxRequiredPower += thrust.MaxPowerConsumption;
                }
                m_maxRequirementsByDirection[dir.Key] = maxRequiredPower;
            }
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Forward), GetMaxRequirement(Vector3I.Backward));
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Left   ), GetMaxRequirement(Vector3I.Right   ));
            MaxRequiredPowerInput += Math.Max(GetMaxRequirement(Vector3I.Up     ), GetMaxRequirement(Vector3I.Down    ));

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
