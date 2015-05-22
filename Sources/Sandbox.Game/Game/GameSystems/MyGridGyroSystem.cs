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

namespace Sandbox.Game.GameSystems
{
    class MyGridGyroSystem : IMyPowerConsumer
    {
        // Rotation limiter, larger number, more limited max rotation
        static readonly float INV_TENSOR_MAX_LIMIT = 125000;
        static readonly float MAX_SLOWDOWN = 0.93f;
        static readonly float MAX_ROLL = MathHelper.PiOver2;

        #region Fields
        public Vector3 ControlTorque;

        private MyCubeGrid m_grid;
        private HashSet<MyGyro> m_gyros;
        private bool m_gyrosChanged;

        // Combined torque of all controlled gyros
        private float m_maxGyroForce;
        // Combined torque of all overridden gyros
        private float m_maxOverrideForce;

        private float m_maxRequiredPowerInput;

        private Vector3 m_overrideTargetVelocity;

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
                    Matrix invWorldRot = m_grid.PositionComp.GetWorldMatrixNormalizedInv().GetOrientation();
                    Matrix worldRot = m_grid.WorldMatrix.GetOrientation();
                    Vector3 localAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);

                    float slowdown = (1 - MAX_SLOWDOWN) * (1 - PowerReceiver.SuppliedRatio) + MAX_SLOWDOWN;
                    var slowdownAngularAcceleration = -localAngularVelocity / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    var invTensor = m_grid.Physics.RigidBody.InverseInertiaTensor;
                    invTensor.M44 = 1;
                    var minInvTensor = Math.Min(Math.Min(invTensor.M11, invTensor.M22), invTensor.M33);
                    var slowdownTorque = slowdownAngularAcceleration / new Vector3(invTensor.M11, invTensor.M22, invTensor.M33);

                    float torqueSlowdownMultiplier = m_grid.GridSizeEnum == MyCubeSize.Large ? MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER_LARGE_SHIP : MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER;
                    Vector3 slowdownClamp = new Vector3(m_maxGyroForce * torqueSlowdownMultiplier);
                    slowdownTorque = Vector3.Clamp(slowdownTorque, -slowdownClamp, slowdownClamp) * Vector3.IsZeroVector(ControlTorque);

                    if (slowdownTorque.LengthSquared() > 0.0001f)
                    {
                        m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, slowdownTorque);
                        var newVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);
                        var maxDelta = Vector3.Abs(localAngularVelocity) * (1 - slowdown);
                        m_grid.Physics.AngularVelocity = Vector3.Transform(Vector3.Clamp(newVelocity, localAngularVelocity - maxDelta, localAngularVelocity + maxDelta), ref worldRot);
                    }

                    // Max rotation limiter
                    float divider = Math.Max(1, minInvTensor * INV_TENSOR_MAX_LIMIT);

                    Torque = Vector3.Clamp(ControlTorque, -Vector3.One, Vector3.One) * m_maxGyroForce / divider;
                    Torque *= PowerReceiver.SuppliedRatio;
                    if (Torque.LengthSquared() > 0.0001f)
                    {
                        // Manually apply torque and use minimal component of inverted inertia tensor to make rotate same in all axes
                        var delta = Torque * new Vector3(minInvTensor) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                        var newAngularVelocity = localAngularVelocity + delta;
                        m_grid.Physics.AngularVelocity = Vector3.Transform(newAngularVelocity, ref worldRot);
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
                float correctionForce = m_maxOverrideForce + m_maxGyroForce * (1.0f - control.Length());

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

                Torque = (control * m_maxGyroForce + correctionTorque) / divider;

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
                if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                    MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Old gyros", Color.White, 1.0f);
                UpdateBeforeSimulationOld();
                return;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "New gyros", Color.White, 1.0f);

            if (m_grid.Physics != null)
            {
                m_grid.Physics.AngularVelocity = GetAngularVelocity(ControlTorque);
            }
        }

        private void RecomputeGyroParameters()
        {
            m_gyrosChanged = false;

            float oldRequirement = m_maxRequiredPowerInput;
            m_maxGyroForce = 0.0f;
            m_maxOverrideForce = 0.0f;
            m_maxRequiredPowerInput = 0.0f;
            m_overrideTargetVelocity = Vector3.Zero;
            foreach (var gyro in m_gyros)
            {
                if (IsUsed(gyro))
                {
                    if (!gyro.GyroOverride)
                        m_maxGyroForce += gyro.MaxGyroForce;
                    else
                    {
                        m_overrideTargetVelocity += gyro.GyroOverrideVelocityGrid * gyro.MaxGyroForce;
                        m_maxOverrideForce += gyro.MaxGyroForce;
                    }
                    m_maxRequiredPowerInput += gyro.RequiredPowerInput;
                }
            }
            if ((m_maxOverrideForce + m_maxGyroForce) != 0.0f)
                m_overrideTargetVelocity /= (m_maxOverrideForce + m_maxGyroForce);

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
