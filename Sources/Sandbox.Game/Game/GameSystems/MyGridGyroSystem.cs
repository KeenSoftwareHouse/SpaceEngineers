using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;

using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using VRageRender;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game;

namespace Sandbox.Game.GameSystems
{
    public class MyGridGyroSystem
    {
        // Rotation limiter, larger number, more limited max rotation
        static readonly float INV_TENSOR_MAX_LIMIT = 125000;
        static readonly float MAX_SLOWDOWN = MyFakes.WELD_LANDING_GEARS ? 0.8f : 0.93f;
        static readonly float MAX_ROLL = MathHelper.PiOver2;

        #region Fields
        public Vector3 ControlTorque;
        public bool AutopilotEnabled;

        private MyCubeGrid m_grid;
        private HashSet<MyGyro> m_gyros;
        private bool m_gyrosChanged;

        // Combined torque of all controlled gyros
        private float m_maxGyroForce;
        // Combined torque of all overridden gyros
        private float m_maxOverrideForce;

        private float m_maxRequiredPowerInput;

        private Vector3 m_overrideTargetVelocity;
        private int? m_overrideAccelerationRampFrames;

        public Vector3 SlowdownTorque;

        #endregion

        #region Properties
        public MyResourceSinkComponent ResourceSink
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
			ResourceSink = new MyResourceSinkComponent();
            ResourceSink.Init(
                MyStringHash.GetOrCompute("Gyro"),
                m_maxRequiredPowerInput,
                () => m_maxRequiredPowerInput);
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
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

        private void UpdateGyros()
        {
            SlowdownTorque = Vector3.Zero;

            if (m_grid.Physics == null || m_grid.Physics.IsKinematic)
                return;
            if (!ControlTorque.IsValid()) 
                ControlTorque = Vector3.Zero;
            if (Vector3.IsZero(m_grid.Physics.AngularVelocity, 0.001f) && Vector3.IsZero(ControlTorque, 0.001f))
                return;

            // Not checking whether engines are running, since ControlTorque should be 0 when
            // engines are stopped (set by cockpit).
            if (ResourceSink.SuppliedRatio > 0f && m_grid.Physics != null && (m_grid.Physics.Enabled || m_grid.Physics.IsWelded) && !m_grid.Physics.RigidBody.IsFixed)
            {
                var invTensor = m_grid.Physics.RigidBody.InverseInertiaTensor;
                invTensor.M44 = 1;

                Matrix invWorldRot = m_grid.PositionComp.WorldMatrixNormalizedInv.GetOrientation();
                Vector3 localAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);

                float slowdown = (1 - MAX_SLOWDOWN) * (1 - ResourceSink.SuppliedRatio) + MAX_SLOWDOWN;

                SlowdownTorque = -localAngularVelocity;

                float torqueSlowdownMultiplier = m_grid.GridSizeEnum == MyCubeSize.Large ? MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER_LARGE_SHIP : MyFakes.SLOWDOWN_FACTOR_TORQUE_MULTIPLIER;
                Vector3 slowdownClamp = new Vector3(m_maxGyroForce * torqueSlowdownMultiplier);

                if (m_grid.Physics.IsWelded)
                {
                    //slowdownTorque = Vector3.TransformNormal(slowdownTorque, Matrix.Invert(m_grid.GetPhysicsBody().WeldInfo.Transform));
                    //only reliable variant
                    SlowdownTorque = Vector3.TransformNormal(SlowdownTorque, m_grid.WorldMatrix);
                    SlowdownTorque = Vector3.TransformNormal(SlowdownTorque, Matrix.Invert(m_grid.Physics.RigidBody.GetRigidBodyMatrix()));
                }                 
                // Only multiply the slowdown by the multiplier if we want to move in a different direction in the given axis
                if (!localAngularVelocity.IsValid()) localAngularVelocity = Vector3.Zero;
                Vector3 selector = Vector3.One - Vector3.IsZeroVector(Vector3.Sign(localAngularVelocity) - Vector3.Sign(ControlTorque));
                SlowdownTorque *= torqueSlowdownMultiplier;

                SlowdownTorque /= invTensor.Scale;
                SlowdownTorque = Vector3.Clamp(SlowdownTorque, -slowdownClamp, slowdownClamp) * selector;

                if (SlowdownTorque.LengthSquared() > 0.0001f)
                {
                          
                    //if(Sandbox.Game.World.MySession.Static.ControlledEntity.Entity.GetTopMostParent() == m_grid)
                    //    MyRenderProxy.DebugDrawText2D(new Vector2(300,320), (slowdownTorque * slowdown).ToString(), Color.White, 0.8f);
                    m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, SlowdownTorque * slowdown);
                }

                var minInvTensor = Math.Min(Math.Min(invTensor.M11, invTensor.M22), invTensor.M33);
                // Max rotation limiter

                float divider = Math.Max(1, minInvTensor * INV_TENSOR_MAX_LIMIT);

                Torque = Vector3.Clamp(ControlTorque, -Vector3.One, Vector3.One) * m_maxGyroForce / divider;
                Torque *= ResourceSink.SuppliedRatio;

                var scale = m_grid.Physics.RigidBody.InertiaTensor.Scale;
                scale = Vector3.Abs(scale / scale.AbsMax());
                if (Torque.LengthSquared() > 0.0001f)
                {
                    var torque = Torque;
                    if(m_grid.Physics.IsWelded)
                    {
                        torque = Vector3.TransformNormal(torque, m_grid.WorldMatrix);
                        torque = Vector3.TransformNormal(torque, Matrix.Invert(m_grid.Physics.RigidBody.GetRigidBodyMatrix()));
                        //torque *= new Vector3(-1, 1, -1);//jn: some weird transformation for welded ship
                    }
  
                    m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, torque * scale);
                    //if (Sandbox.Game.World.MySession.Static.ControlledEntity.Entity.GetTopMostParent() == m_grid)
                    //    MyRenderProxy.DebugDrawText2D(new Vector2(300,300), (torque * scale).ToString(), Color.Green, 0.8f);
                }

                const float stoppingVelocitySq = 0.0003f * 0.0003f;
                if (ControlTorque == Vector3.Zero && m_grid.Physics.AngularVelocity != Vector3.Zero && m_grid.Physics.AngularVelocity.LengthSquared() < stoppingVelocitySq && m_grid.Physics.RigidBody.IsActive)
                {
                    m_grid.Physics.AngularVelocity = Vector3.Zero;
                }
            }
        }

        private void UpdateOverriddenGyros()
        {
            // Not checking whether engines are running, since ControlTorque should be 0 when
            // engines are stopped (set by cockpit).
            if (ResourceSink.SuppliedRatio > 0f && m_grid.Physics.Enabled && !m_grid.Physics.RigidBody.IsFixed)
            {
                Matrix invWorldRot = m_grid.PositionComp.WorldMatrixInvScaled.GetOrientation();
                Matrix worldRot = m_grid.WorldMatrix.GetOrientation();
                Vector3 localAngularVelocity = Vector3.Transform(m_grid.Physics.AngularVelocity, ref invWorldRot);

                Vector3 velocityDiff = m_overrideTargetVelocity - localAngularVelocity;
                if (velocityDiff == Vector3.Zero)
                    return;

                UpdateOverrideAccelerationRampFrames(velocityDiff);

                // acceleration = m/s * (1/s)
                Vector3 desiredAcceleration = velocityDiff * (MyEngineConstants.UPDATE_STEPS_PER_SECOND / m_overrideAccelerationRampFrames.Value);

                // CH: CAUTION: Don't try to use InertiaTensor, although it might be more intuitive in some cases.
                // I tried it and it's not an inverse of the InverseInertiaTensor! Only the InverseInertiaTensor seems to be correct!
                var invTensor = m_grid.Physics.RigidBody.InverseInertiaTensor;
                Vector3 invTensorVector = new Vector3(invTensor.M11, invTensor.M22, invTensor.M33);

                // Calculate the desired velocity correction torque
                Vector3 desiredTorque = desiredAcceleration / invTensorVector;

                // Calculate the available force for the correction by arbitrarily sum overridden gyros
                // and the remaining force force of the controlled gyros
                float correctionForce = m_maxOverrideForce + m_maxGyroForce * (1.0f - ControlTorque.Length());

                // Reduce the desired torque to the available force
                Vector3 availableTorque = Vector3.ClampToSphere(desiredTorque, correctionForce);

                Torque = ControlTorque * m_maxGyroForce + availableTorque;
                Torque *= ResourceSink.SuppliedRatio;

                const float TORQUE_SQ_LEN_TH = 0.0001f;
                if (Torque.LengthSquared() < TORQUE_SQ_LEN_TH)
                    return;

                m_grid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, null, null, Torque);
            }
        }

        // Update frames count to obtain a smooth acceleration ramp for overriden gyros
        private void UpdateOverrideAccelerationRampFrames(Vector3 velocityDiff)
        {
            if (m_overrideAccelerationRampFrames == null)
            {
                float squaredSpeed = velocityDiff.LengthSquared();
                const float MIN_ROTATION_SPEED_SQ_TH = (float)((Math.PI / 2) * (Math.PI / 2));
                const int ACCELARION_RAMP_FRAMES = (int)MyEngineConstants.UPDATE_STEPS_PER_SECOND * 2;
                if (squaredSpeed > MIN_ROTATION_SPEED_SQ_TH)
                {
                    m_overrideAccelerationRampFrames = ACCELARION_RAMP_FRAMES;
                }
                else
                {
                    const float K_PROP_ACCEL = (ACCELARION_RAMP_FRAMES - 1) / MIN_ROTATION_SPEED_SQ_TH;
                    m_overrideAccelerationRampFrames = (int)(squaredSpeed * K_PROP_ACCEL) + 1;
                }
            }
            else if (m_overrideAccelerationRampFrames > 1)
            {
                m_overrideAccelerationRampFrames--;
            }
        }

        // NOTE: This method had problems with overridden gyros, so it's not used anymore in the normal
        //  code path. It is still used in the autopilot code path, though. For reference on the new code
        // look at UpdateOverriddenGyros()
        public Vector3 GetAngularVelocity(Vector3 control)
        {
            // Not checking whether engines are running, since ControlTorque should be 0 when
            // engines are stopped (set by cockpit).
            if (ResourceSink.SuppliedRatio > 0f && m_grid.Physics != null && m_grid.Physics.Enabled && !m_grid.Physics.RigidBody.IsFixed)
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
                Vector3 desiredAcceleration = (m_overrideTargetVelocity - localAngularVelocity) * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND;

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

                Torque *= ResourceSink.SuppliedRatio;
                if (Torque.LengthSquared() > 0.0001f)
                {
                    // Manually apply torque and use minimal component of inverted inertia tensor to make rotate same in all axes
                    var delta = Torque * new Vector3(minInvTensor) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
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
            VRage.MySimpleProfiler.Begin("Gyro");
            if (m_gyrosChanged)
                RecomputeGyroParameters();

            if (m_maxOverrideForce == 0.0f)
            {
                if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                    MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "Old gyros", Color.White, 1.0f);
                UpdateGyros();
                return;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_GYROS)
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "New gyros", Color.White, 1.0f);

            if (m_grid.Physics != null)
                UpdateOverriddenGyros();
            VRage.MySimpleProfiler.End("Gyro");
        }

        private void RecomputeGyroParameters()
        {
            m_gyrosChanged = false;

            float oldRequirement = m_maxRequiredPowerInput;
            m_maxGyroForce = 0.0f;
            m_maxOverrideForce = 0.0f;
            m_maxRequiredPowerInput = 0.0f;
            m_overrideTargetVelocity = Vector3.Zero;
            m_overrideAccelerationRampFrames = null;
            foreach (var gyro in m_gyros)
            {
                if (IsUsed(gyro))
                {
                    if (!gyro.GyroOverride || AutopilotEnabled)
                        m_maxGyroForce += gyro.MaxGyroForce;
                    else
                    {
                        m_overrideTargetVelocity += gyro.GyroOverrideVelocityGrid * gyro.MaxGyroForce;
                        m_maxOverrideForce += gyro.MaxGyroForce;
                    }
                    m_maxRequiredPowerInput += gyro.RequiredPowerInput;
                }
            }
            if (m_maxOverrideForce != 0.0f)
                m_overrideTargetVelocity /= m_maxOverrideForce;

            ResourceSink.MaxRequiredInput = m_maxRequiredPowerInput;
            ResourceSink.Update();

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

            if (!Vector3.IsZero(m_overrideTargetVelocity) && ResourceSink.IsPowered)
                m_grid.Physics.RigidBody.EnableDeactivation = false;
            else
                m_grid.Physics.RigidBody.EnableDeactivation = true;
        }
    }
}
