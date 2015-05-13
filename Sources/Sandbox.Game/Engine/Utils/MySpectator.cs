using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.Input;
using Sandbox.ModAPI.Interfaces;
using System;
using VRage.Common.Utils;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    //  Player with movements like 6DOF camera
    public class MySpectator : IMyCameraController
    {
        public static MySpectator Static;

        public Vector3 ThirdPersonCameraDelta = new Vector3(-10, 10, -10);
        public MySpectatorCameraMovementEnum SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;

        public bool IsInFirstPersonView { get; set; }
        public bool ForceFirstPersonCamera { get; set; }

        public MySpectator()
        {
            Static = this;
        }


        Vector3 m_position;
        public Vector3 Position
        {
            get { return m_position; }
            set
            {
                MyUtils.AssertIsValid(value);
                m_position = value;
            }
        }

        public bool ReflectorOn = true;
        public float SpeedMode
        {
            get { return m_speedMode; }
            set { m_speedMode = value; }
        }

        private float m_speedMode = MyConstants.DEFAULT_SPECTATOR_SPEED;
        Matrix m_baseRotation = Matrix.Identity;
        bool m_rotationEnabled = true;


        //  Direction to which player is looking. Normalized vector.
        public Vector3 Orientation
        {
            get
            {
                return Vector3.Transform(Vector3.Forward, m_baseRotation);
            }
        }

        //  Gets or sets camera's target.
        //  You can set target as point where camera will be looking from it's current position. Angles are calculated automatically.
        public Vector3 Target
        {
            get
            {
                return Position + Vector3.Transform(Vector3.Forward, m_baseRotation);
            }
            set
            {
                Vector3 forward = MyVRageUtils.Normalize(value - Position);
                forward = forward.LengthSquared() > 0 ? forward : Vector3.Forward;

                Vector3 unnormalizedRight = Vector3.Cross(forward, Vector3.Up);
                Vector3 right = unnormalizedRight.LengthSquared() > 0 ? MyVRageUtils.Normalize(unnormalizedRight) : Vector3.Right;

                Vector3 up = MyVRageUtils.Normalize(Vector3.Cross(right, forward));
                
                m_baseRotation = Matrix.Identity;
                m_baseRotation.Forward = forward;
                m_baseRotation.Right = right;
                m_baseRotation.Up = up;                
            }
        }

        public void Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(Vector3.Zero, rotationIndicator, rollIndicator);
        }

        public void RotateStopped()
        {
            MoveAndRotateStopped();
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        //  Moves and rotates player by specified vector and angles
        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
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
                            Position = MySession.ControlledEntity.Entity.GetPosition() + ThirdPersonCameraDelta;
                            Target = MySession.ControlledEntity.Entity.GetPosition();
                        }
                    }
                    break;

                case MySpectatorCameraMovementEnum.UserControlled:
                    {
                        if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                        {
                            SpeedMode = Math.Min(MySpectator.Static.SpeedMode * 1.5f, MyConstants.MAX_SPECTATOR_SPEED);
                        }
                        else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                        {
                            SpeedMode = Math.Max(MySpectator.Static.SpeedMode / 1.5f, MyConstants.MIN_SPECTATOR_SPEED);
                        }

                        Vector3 oldPosition = Position;

                        float afterburner = (MyInput.Static.IsAnyShiftKeyPressed() ? 1.0f : 0.35f) * (MyInput.Static.IsAnyCtrlKeyPressed() ? 0.3f : 1);

                        moveIndicator *= afterburner * m_speedMode;

                        //  Physical movement and rotation is based on constant time, therefore is indepedent of time delta
                        //  This formulas works even if FPS is low or high, or if step size is 1/10 or 1/10000
                        float amountOfMovement = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 100;
                        float amountOfRotation = 0.0025f;

                        if (m_rotationEnabled)
                        {

                            if (rollIndicator != 0)
                            {
                                Vector3 r, u;
                                float rollAmount = rollIndicator * m_speedMode * 0.1f;
                                rollAmount = MathHelper.Clamp(rollAmount, -0.02f, 0.02f);
                                MyUtils.VectorPlaneRotation(m_baseRotation.Up, m_baseRotation.Right, out u, out r, rollAmount);
                                m_baseRotation.Right = r;
                                m_baseRotation.Up = u;
                            }

                            if (rotationIndicator.X != 0)
                            {
                                Vector3 u, f;
                                MyUtils.VectorPlaneRotation(m_baseRotation.Up, m_baseRotation.Forward, out u, out f, rotationIndicator.X * amountOfRotation);
                                m_baseRotation.Up = u;
                                m_baseRotation.Forward = f;
                            }

                            if (rotationIndicator.Y != 0)
                            {
                                Vector3 r, f;
                                MyUtils.VectorPlaneRotation(m_baseRotation.Right, m_baseRotation.Forward, out r, out f, -rotationIndicator.Y * amountOfRotation);

                                m_baseRotation.Right = r;
                                m_baseRotation.Forward = f;
                            }

                            // m_baseRotation = Matrix.Identity;
                        }

                        Vector3 moveVector = moveIndicator * amountOfMovement;


                        Position += Vector3.Transform(moveVector, m_baseRotation);
                        
                        //Position = Vector3.Clamp(Position, -MySectorConstants.SECTOR_SIZE_VECTOR3 / 2, MySectorConstants.SECTOR_SIZE_VECTOR3 / 2);                        
                    }
                    break;
            }

        }

        public virtual void MoveAndRotateStopped()
        {
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateLookAt(Position, Position + m_baseRotation.Forward, m_baseRotation.Up);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            MyUtils.AssertIsValid(viewMatrix);
            
            Matrix inverted = Matrix.Invert(viewMatrix);
            Position = inverted.Translation;
            m_baseRotation = Matrix.Identity;
            m_baseRotation.Right = inverted.Right;
            m_baseRotation.Up = inverted.Up;
            m_baseRotation.Forward = inverted.Forward;
        }

        public void EnableRotation()
        {
            m_rotationEnabled = true;
        }

        public void DisableRotation()
        {
            m_rotationEnabled = false;
        }

        /// <summary>
        /// Reset position and orientation of spectator view matrix
        /// </summary>
        public void ResetSpectatorView()
        {
            Position = Vector3.Zero;
            m_baseRotation = Matrix.Identity;

        }

        Matrix IMyCameraController.GetViewMatrix()
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
    }
}
