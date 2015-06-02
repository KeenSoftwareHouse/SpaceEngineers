using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;

using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Common;
using VRageRender;
using VRage.Utils;
using VRage.Components;

using Sandbox.Game.Gui;

namespace Sandbox.Game.GameSystems
{
    public class MyGridGyroSystem : IMyPowerConsumer
    {
        // Rotation limiter, larger number, more limited max rotation
        static readonly float INV_TENSOR_MAX_LIMIT = 125000;
        static readonly float MAX_SLOWDOWN = 0.93f;
        static readonly float MAX_ROLL = MathHelper.PiOver2;

        // Gyroscope PID controller values.
        const float P_COEFF = -0.05f / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        const float I_COEFF = 0.1f * P_COEFF;
        const float D_COEFF = 0.05f * P_COEFF;

        #region Fields
        public Vector3 ControlTorque;

        private MyCubeGrid m_grid;
        private HashSet<MyGyro> m_gyros;
        private bool m_gyrosChanged;

        // Combined torque of all overridden gyros
        private float m_maxOverrideForce;

        private float m_maxRequiredPowerInput;

        private Vector3 m_overrideTargetVelocity;

        private Vector3 m_gyroControlIntegral;
        private Vector3 m_prevAngularVelocity;
        private Vector3 m_AngularVelocityAtRelease;
        private bool    m_enableIntegral;
        private bool    m_resetIntegral;

        #endregion

        #region Properties
        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public int GyroCount
        {
            get { return m_gyros.Count; }
        }

        /// <summary>
        /// This should not be used to modify the gyros.
        /// Use Register/Unregister for that.
        /// </summary>
        public HashSet<MyGyro> Gyros
        {
            get { return m_gyros; }
        }
        /// <summary>
        /// Final torque (clamped by available power, added anti-gravity, slowdown).
        /// </summary>
        public Vector3 Torque { get; private set; }

        public bool IsGyroOverrideActive { get; private set; }
        // Combined torque of all controlled gyros
        public float   MaxGyroForce   { get; private set; }
        public Vector3 SlowdownTorque { get; private set; }

        public bool    IsAutopilotActive         { get; set; }
        public bool    IsCourseEstablished       { get; set; }
        public Vector3 AutopilotAngularDeviation { get; set; }

        #endregion

        public MyGridGyroSystem(MyCubeGrid grid)
        {
            m_grid = grid;
            m_gyros = new HashSet<MyGyro>();
            m_gyrosChanged = false;
            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Gyro,
                true,
                m_maxRequiredPowerInput,
                () => m_maxRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
        }

        public void Register(MyGyro gyro)
        {
            MyDebug.AssertDebug(gyro != null);
            MyDebug.AssertDebug(!m_gyros.Contains(gyro));
            m_gyros.Add(gyro);
            m_gyrosChanged = true;

            gyro.EnabledChanged += gyro_EnabledChanged;
            gyro.SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            gyro.PropertiesChanged += gyro_PropertiesChanged;
        }

        void gyro_PropertiesChanged(MyTerminalBlock sender)
        {
            MarkDirty();
        }

        public void Unregister(MyGyro gyro)
        {
            MyDebug.AssertDebug(gyro != null);
            MyDebug.AssertDebug(m_gyros.Contains(gyro));
            m_gyros.Remove(gyro);
            m_gyrosChanged = true;

            gyro.EnabledChanged -= gyro_EnabledChanged;
            gyro.SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;
        }

        public void UpdateBeforeSimulationOld()
        {
            //if (m_grid.GridControllers.IsControlledByLocalPlayer || (!m_grid.GridControllers.IsControlledByAnyPlayer && Sync.IsServer) || (false && Sync.IsServer))
            {
                // Not checking whether engines are running, since ControlTorque should be 0 when
                // engines are stopped (set by cockpit).
                if (PowerReceiver.SuppliedRatio > 0f && m_grid.Physics != null && m_grid.Physics.Enabled && !m_grid.Physics.RigidBody.IsFixed)
                {
                    Matrix  invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
                    Matrix  worldRot    = m_grid.WorldMatrix.GetOrientation();
                    // For some reason, calculating angular velocity inside MyGridGyroSystem.cs yields an incorrect value, causing gyroscopes to overcompensate.
                    Vector3 localAngularVelocity = m_grid.GridSystems.ThrustSystem.LocalAngularVelocity; 

                    if (!IsAutopilotActive)
                    {
                        m_gyroControlIntegral = m_enableIntegral ? (m_gyroControlIntegral + localAngularVelocity * I_COEFF) : Vector3.Zero;
                        IsCourseEstablished   = false;  // Just in case.
                    }
                    else if (IsCourseEstablished)
                    {
                        m_gyroControlIntegral = (-AutopilotAngularDeviation) * (I_COEFF * MyEngineConstants.UPDATE_STEPS_PER_SECOND);
                        m_resetIntegral       = false;
                    }
                    else
                        m_gyroControlIntegral = Vector3.Zero;
                    IsAutopilotActive = false;  // Needs to be done here and not in the remote control code, as there may be more than 1 RC block.

                    // To prevent integral part from fighting the player input, force reset of integral portion 
                    // when ship starts to spring back after releasing the controls.
                    if (m_resetIntegral && Vector3.Dot(localAngularVelocity, m_AngularVelocityAtRelease) <= 0.0f)
                    {
                        m_resetIntegral       = false;
                        m_gyroControlIntegral = Vector3.Zero;
                    }
                    var angularAcceleration = (localAngularVelocity - m_prevAngularVelocity) / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    //float slowdown = (1 - MAX_SLOWDOWN) * (1 - PowerReceiver.SuppliedRatio) + MAX_SLOWDOWN;
                    var slowdownAngularAcceleration = P_COEFF * localAngularVelocity + m_gyroControlIntegral + D_COEFF * angularAcceleration;
                    var invTensor = m_grid.Physics.RigidBody.InverseInertiaTensor;
                    invTensor.M44 = 1;
                    var minInvTensor = Math.Min(Math.Min(invTensor.M11, invTensor.M22), invTensor.M33);
                    SlowdownTorque = slowdownAngularAcceleration / new Vector3(invTensor.M11, invTensor.M22, invTensor.M33);

                    m_prevAngularVelocity = localAngularVelocity;

                    float torqueSlowdownMultiplier = m_grid.GridSizeEnum == MyCubeSize.Large ? MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER_LARGE_SHIP : MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER;
                    Vector3 slowdownClamp = new Vector3(MaxGyroForce * torqueSlowdownMultiplier);
                    SlowdownTorque = Vector3.Clamp(SlowdownTorque, -slowdownClamp, slowdownClamp) * Vector3.IsZeroVector(ControlTorque);

                    if (SlowdownTorque.LengthSquared() > 0.0001f)
                    {
                        m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, SlowdownTorque);

                        // The following code severely interferes with PID logic and has been disabled.
                        //var newVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);
                        //var maxDelta = Vector3.Abs(LocalAngularVelocity) * (1 - slowdown);
                        //m_grid.Physics.AngularVelocity = Vector3.Transform(Vector3.Clamp(newVelocity, LocalAngularVelocity - maxDelta, LocalAngularVelocity + maxDelta), ref worldRot);
                    }

                    // Max rotation limiter
                    float divider = Math.Max(1, minInvTensor * INV_TENSOR_MAX_LIMIT);

                    Torque = Vector3.Clamp(ControlTorque, -Vector3.One, Vector3.One) * MaxGyroForce / divider;
                    Torque *= PowerReceiver.SuppliedRatio;
                    if (Torque.LengthSquared() <= 0.0001f)
                    {
                        if (!m_enableIntegral && !m_resetIntegral)
                        {
                            m_AngularVelocityAtRelease = localAngularVelocity;
                            m_resetIntegral            = true;
                        }
                        m_enableIntegral = true;
                    }
                    else
                    {
                        m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, Torque);

                        // Manually apply torque and use minimal component of inverted inertia tensor to make rotate same in all axes.
                        // This code is not compatible with thruster torque and has been disabled as well.
                        //var delta = Torque * new Vector3(minInvTensor) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                        //var newAngularVelocity = LocalAngularVelocity + delta;
                        //m_grid.Physics.AngularVelocity = Vector3.Transform(newAngularVelocity, ref worldRot);

                        // Disable integral part when player activates the rotation controls.
                        m_enableIntegral = false;
                    }

                    const float stoppingVelocitySq = 0.0003f * 0.0003f;
                    if (ControlTorque == Vector3.Zero && m_grid.Physics.AngularVelocity != Vector3.Zero && m_grid.Physics.AngularVelocity.LengthSquared() < stoppingVelocitySq && m_grid.Physics.RigidBody.IsActive)
                    {
                        m_grid.Physics.AngularVelocity = Vector3.Zero;
                    }
                }
            }
        }

        public Vector3 GetAngularVelocity(Vector3 control)
        {
            /*if (m_grid.GridControllers.IsControlledByLocalPlayer || (!m_grid.GridControllers.IsControlledByAnyPlayer && Sync.IsServer) || (false && Sync.IsServer))
            {*/
            // Not checking whether engines are running, since ControlTorque should be 0 when
            // engines are stopped (set by cockpit).
            if (PowerReceiver.SuppliedRatio > 0f && m_grid.Physics != null && m_grid.Physics.Enabled && !m_grid.Physics.RigidBody.IsFixed)
            {
                Matrix invWorldRot = m_grid.PositionComp.WorldMatrixInvScaled.GetOrientation();
                Matrix worldRot = m_grid.WorldMatrix.GetOrientation();
                Vector3 localAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);

                // CH: CAUTION: Don't try to use InertiaTensor, although it might be more intuitive in some cases.
                // I tried it and it's not an inverse of the InverseInertiaTensor! Only the InverseInertiaTensor seems to be correct!
                var invTensor = m_grid.Physics.RigidBody.InverseInertiaTensor;
                Vector3 invTensorVector = new Vector3(invTensor.M11, invTensor.M22, invTensor.M33);
                var minInvTensor = invTensorVector.Min();

                // Max rotation limiter
                float divider = Math.Max(1, minInvTensor * INV_TENSOR_MAX_LIMIT);

                // Calculate the velocity correction torque
                Vector3 correctionTorque = Vector3.Zero;
                Vector3 desiredAcceleration = desiredAcceleration = (m_overrideTargetVelocity - localAngularVelocity) * MyEngineConstants.UPDATE_STEPS_PER_SECOND;

                // The correction is done by overridden gyros and by the remaining power of the controlled gyros
                // This is not entirely physically correct, but it feels good
                float correctionForce = m_maxOverrideForce + MaxGyroForce * (1.0f - control.Length());

                // This is to ensure that the correction is done uniformly in all axes
                desiredAcceleration = desiredAcceleration * Vector3.Normalize(invTensorVector);

                Vector3 desiredTorque = desiredAcceleration / invTensorVector;
                float framesToDesiredVelocity = desiredTorque.Length() / correctionForce;

                // If we are very close to the target velocity, just set it without applying the torque
                const float minimalBypassVelocity = 0.005f * 0.005f;
                if (framesToDesiredVelocity < 0.5f && m_overrideTargetVelocity.LengthSquared() < minimalBypassVelocity)
                {
                    return m_overrideTargetVelocity;
                }

                if (!Vector3.IsZero(desiredAcceleration, 0.0001f))
                {
                    // The smoothing coefficient is here to avoid the slowdown stopping the ship abruptly, which doesn't look good
                    float smoothingCoeff = 1.0f - 0.8f / (float)Math.Exp(0.5f * framesToDesiredVelocity);
                    correctionTorque = Vector3.ClampToSphere(desiredTorque, correctionForce) * 0.95f * smoothingCoeff + desiredTorque * 0.05f * (1.0f - smoothingCoeff);

                    // A little black magic to make slowdown on large ships bigger
                    if (m_grid.GridSizeEnum == MyCubeSize.Large)
                        correctionTorque *= 2.0f;
                }

                Torque = (control * MaxGyroForce + correctionTorque) / divider;

                Torque *= PowerReceiver.SuppliedRatio;
                if (Torque.LengthSquared() > 0.0001f)
                {
                    // Manually apply torque and use minimal component of inverted inertia tensor to make rotate same in all axes
                    var delta = Torque * new Vector3(minInvTensor) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    var newAngularVelocity = localAngularVelocity + delta;
                    return Vector3.Transform(newAngularVelocity, ref worldRot);
                }

                const float stoppingVelocitySq = 0.0003f * 0.0003f;
                if (control == Vector3.Zero && m_overrideTargetVelocity == Vector3.Zero && m_grid.Physics.AngularVelocity != Vector3.Zero && m_grid.Physics.AngularVelocity.LengthSquared() < stoppingVelocitySq && m_grid.Physics.RigidBody.IsActive)
                {
                    return Vector3.Zero;
                }
            }
            //}

            if (m_grid.Physics != null)
            {
                return m_grid.Physics.AngularVelocity;
            }
            else
            {
                return Vector3.Zero;
            }
        }

        public void UpdateBeforeSimulation()
        {
            if (m_gyrosChanged)
                RecomputeGyroParameters();

            if (m_maxOverrideForce == 0.0f)
            {
                IsGyroOverrideActive = false;
                if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                    MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Old gyros", Color.White, 1.0f);
                UpdateBeforeSimulationOld();
                return;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "New gyros", Color.White, 1.0f);

            m_gyroControlIntegral = Vector3.Zero;
            m_enableIntegral      = m_resetIntegral = false;
            IsGyroOverrideActive  = true;
            if (m_grid.Physics != null)
            {
                m_grid.Physics.AngularVelocity = GetAngularVelocity(ControlTorque);
            }
        }

        private void RecomputeGyroParameters()
        {
            m_gyrosChanged = false;

            float oldRequirement = m_maxRequiredPowerInput;
            MaxGyroForce = 0.0f;
            m_maxOverrideForce = 0.0f;
            m_maxRequiredPowerInput = 0.0f;
            m_overrideTargetVelocity = Vector3.Zero;
            foreach (var gyro in m_gyros)
            {
                if (IsUsed(gyro))
                {
                    if (!gyro.GyroOverride)
                        MaxGyroForce += gyro.MaxGyroForce;
                    else
                    {
                        m_overrideTargetVelocity += gyro.GyroOverrideVelocityGrid * gyro.MaxGyroForce;
                        m_maxOverrideForce += gyro.MaxGyroForce;
                    }
                    m_maxRequiredPowerInput += gyro.RequiredPowerInput;
                }
            }
            if ((m_maxOverrideForce + MaxGyroForce) != 0.0f)
                m_overrideTargetVelocity /= (m_maxOverrideForce + MaxGyroForce);

            PowerReceiver.MaxRequiredInput = m_maxRequiredPowerInput;
            PowerReceiver.Update();

            UpdateAutomaticDeactivation();
        }

        private bool IsUsed(MyGyro gyro)
        {
            return gyro.Enabled && gyro.IsFunctional;
        }

        private void gyro_EnabledChanged(MyTerminalBlock obj)
        {
            MarkDirty();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            MarkDirty();
        }

        public void MarkDirty()
        {
            m_gyrosChanged = true;
        }

        private void Receiver_IsPoweredChanged()
        {
            foreach (var gyro in m_gyros)
                gyro.UpdateIsWorking();
        }

        private void UpdateAutomaticDeactivation()
        {
            if (m_grid.Physics == null || m_grid.Physics.RigidBody.IsFixed) return;

            if (!Vector3.IsZero(m_overrideTargetVelocity) && PowerReceiver.IsPowered)
                m_grid.Physics.RigidBody.EnableDeactivation = false;
            else
                m_grid.Physics.RigidBody.EnableDeactivation = true;
        }
    }
}
