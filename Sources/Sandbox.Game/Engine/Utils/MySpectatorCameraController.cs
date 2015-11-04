using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    //  Player with movements like 6DOF camera
    public class MySpectatorCameraController : MySpectator, IMyCameraController
    {
        public static MySpectatorCameraController Static;

        private double m_yaw;
        private double m_pitch;
        private double m_roll;

        public MySpectatorCameraController()
        {
            Static = this;
        }

        //  Moves and rotates player by specified vector and angles
        public override void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            switch (SpectatorCameraMovement)
            {
                case MySpectatorCameraMovementEnum.None:
                    return;
                    break;

                case MySpectatorCameraMovementEnum.ConstantDelta:
                    {
                        if (!MyInput.Static.IsAnyAltKeyPressed() && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
                        {
                            if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                            {
                                ThirdPersonCameraDelta /= 1.1f;
                            }
                            else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                            {
                                ThirdPersonCameraDelta *= 1.1f;
                            }
                        }

                        if (MySession.ControlledEntity != null)
                        {
                            Position = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition() + ThirdPersonCameraDelta;
                            Target = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition();
                        }
                    }
                    break;

                case MySpectatorCameraMovementEnum.UserControlled:
                    {
                        if (MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                            {
                                SpeedModeAngular = Math.Min(SpeedModeAngular * 1.5f, MAX_SPECTATOR_ANGULAR_SPEED);
                            }
                            else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                            {
                                SpeedModeAngular = Math.Max(SpeedModeAngular / 1.5f, MIN_SPECTATOR_ANGULAR_SPEED);
                            }
                        }
                        else
                        {
                            if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                            {
                                SpeedModeLinear = Math.Min(SpeedModeLinear * 1.5f, MAX_SPECTATOR_LINEAR_SPEED);
                            }
                            else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                            {
                                SpeedModeLinear = Math.Max(SpeedModeLinear / 1.5f, MIN_SPECTATOR_LINEAR_SPEED);
                            }
                        }

                        //  Physical movement and rotation is based on constant time, therefore is indepedent of time delta
                        //  This formulas works even if FPS is low or high, or if step size is 1/10 or 1/10000
                        float amountOfMovement = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 100;
                        float amountOfRotation = 0.0025f * m_speedModeAngular;

                        if (MyFakes.ENABLE_DEVELOPER_SPECTATOR_CONTROLS)
                        {
                            rollIndicator = MyInput.Static.GetDeveloperRoll();
                        }

                        float rollAmount = 0;
                        if (rollIndicator != 0)
                        {
                            Vector3D r, u;
                            rollAmount = rollIndicator * m_speedModeAngular * 0.1f;
                            rollAmount = MathHelper.Clamp(rollAmount, -0.02f, 0.02f);
                            MyUtils.VectorPlaneRotation(m_orientation.Up, m_orientation.Right, out u, out r, rollAmount);
                            m_orientation.Right = r;
                            m_orientation.Up = u;
                        }

                        Vector3 moveVector;

                        if (MyPerGameSettings.RestrictSpectatorFlyMode && !MyFakes.ENABLE_DEVELOPER_SPECTATOR_CONTROLS)
                        {
                            // Spectator has constatnt speed (reset speed to default value)
                            SpeedModeLinear = 11 * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                            if (MyInput.Static.IsAnyShiftKeyPressed())
                                SpeedModeLinear *= 5;

                            Vector3D forward = m_orientation.Forward;
                            double sinX = forward.Dot(ref Vector3D.Up);
                            double angleX = Math.Asin(sinX);
                            double angleY;
                            if (MyUtils.IsZero(sinX - 1f))
                            {
                                // Looking up
                                var up = m_orientation.Up;
                                angleY = Math.Atan2(up.Dot(ref Vector3D.Right), up.Dot(ref Vector3D.Backward));
                            }
                            else if (MyUtils.IsZero(sinX + 1f))
                            {
                                // Looking down
                                var up = m_orientation.Up;
                                angleY = Math.Atan2(up.Dot(ref Vector3D.Left), up.Dot(ref Vector3D.Forward));
                            }
                            else
                            {
                                // non-degenerate case
                                forward.Y = 0.0;
                                forward.Normalize();
                                angleY = Math.Atan2(forward.Dot(ref Vector3D.Left), forward.Dot(ref Vector3D.Forward));
                            }

                            angleX = MathHelper.Clamp(angleX - rotationIndicator.X * amountOfRotation, -MathHelper.PiOver2, MathHelper.PiOver2);

                            angleY -= rotationIndicator.Y * amountOfRotation;
                            if (angleY > MathHelper.Pi) angleY -= MathHelper.TwoPi;
                            if (angleY < -MathHelper.Pi) angleY += MathHelper.TwoPi;

                            m_orientation = MatrixD.CreateRotationX(angleX) * MatrixD.CreateRotationY(angleY);

                            moveIndicator *= SpeedModeLinear;
                            moveVector = moveIndicator * amountOfMovement;
                        }
                        else
                        {
                            if (!MyFakes.ENABLE_SPECTATOR_ROLL_MOVEMENT)
                            {   
                                // TODO: compute from current orientation matrix yaw/pitch/roll

                                rotationIndicator.Rotate(m_roll);

                                m_yaw -= rotationIndicator.Y * amountOfRotation;
                                m_pitch -= rotationIndicator.X * amountOfRotation;
                                m_roll -= rollAmount;                                                               

                                MathHelper.LimitRadians2PI(ref m_yaw);
                                m_pitch = MathHelper.Clamp(m_pitch, -Math.PI * 0.5f, Math.PI * 0.5f);
                                MathHelper.LimitRadians2PI(ref m_roll);                               

                                m_orientation = MatrixD.CreateFromYawPitchRoll(m_yaw, m_pitch, m_roll);                                
                            }
                            else
                            {
                                if (rotationIndicator.Y != 0)
                                {
                                    Vector3D r, f;
                                    MyUtils.VectorPlaneRotation(m_orientation.Right, m_orientation.Forward, out r, out f, -rotationIndicator.Y * amountOfRotation);

                                    m_orientation.Right = r;
                                    m_orientation.Forward = f;
                                }

                                if (rotationIndicator.X != 0)
                                {
                                    Vector3D u, f;
                                    MyUtils.VectorPlaneRotation(m_orientation.Up, m_orientation.Forward, out u, out f, rotationIndicator.X * amountOfRotation);
                                    m_orientation.Up = u;
                                    m_orientation.Forward = f;
                                }
                            }                                                

                            float afterburner = (MyInput.Static.IsAnyShiftKeyPressed() ? 1.0f : 0.35f) * (MyInput.Static.IsAnyCtrlKeyPressed() ? 0.3f : 1);
                            moveIndicator *= afterburner * SpeedModeLinear;
                            moveVector = moveIndicator * amountOfMovement;
                        }


                        Position += Vector3.Transform(moveVector, m_orientation);
                    }
                    break;
            }

        }
        MatrixD IMyCameraController.GetViewMatrix()
        {
            return GetViewMatrix();
        }

        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            Rotate(rotationIndicator, rollIndicator);
        }

        void IMyCameraController.RotateStopped()
        {
            RotateStopped();
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {
            OnAssumeControl(previousCameraController);
        }

        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
        }

        bool IMyCameraController.IsInFirstPersonView
        {
            get
            {
                return IsInFirstPersonView;
            }
            set
            {
                IsInFirstPersonView = value;
            }
        }

        bool IMyCameraController.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.HandleUse()
        {
            return false;
        }

        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return true;
            }
        }
    }
}
