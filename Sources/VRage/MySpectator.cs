using System;
using VRage.Utils;
using VRageMath;

namespace VRage
{
    public enum MySpectatorCameraMovementEnum
    {
        UserControlled,
        ConstantDelta,
        None,
        Orbit
    }

    //  Player with movements like 6DOF camera
    public class MySpectator
    {
        public static MySpectator Static;

        public const float DEFAULT_SPECTATOR_LINEAR_SPEED = 0.1f;
        public const float MIN_SPECTATOR_LINEAR_SPEED = 0.0001f;
        public const float MAX_SPECTATOR_LINEAR_SPEED = 8000.0f;

        public const float DEFAULT_SPECTATOR_ANGULAR_SPEED = 1f;
        public const float MIN_SPECTATOR_ANGULAR_SPEED = 0.0001f;
        public const float MAX_SPECTATOR_ANGULAR_SPEED = 6.0f;


        public Vector3D ThirdPersonCameraDelta = new Vector3D(-10, 10, -10);
        public MySpectatorCameraMovementEnum SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;

        public bool IsInFirstPersonView { get; set; }
        public bool ForceFirstPersonCamera { get; set; }


        public MySpectator()
        {
            Static = this;
        }

        Vector3D m_position;
        Vector3D m_target;

        public Vector3D Position
        {
            get { return m_position; }
            set
            {
                value.AssertIsValid();
                m_position = value;
            }
        }

        public float SpeedModeLinear
        {
            get { return m_speedModeLinear; }
            set { m_speedModeLinear = value; }
        }

        public float SpeedModeAngular
        {
            get { return m_speedModeAngular; }
            set { m_speedModeAngular = value; }
        }

        protected float m_speedModeLinear = DEFAULT_SPECTATOR_LINEAR_SPEED;
        protected float m_speedModeAngular = DEFAULT_SPECTATOR_ANGULAR_SPEED;
        protected MatrixD m_orientation = MatrixD.Identity;

        //  Gets or sets camera's target.
        //  You can set target as point where camera will be looking from it's current position. Angles are calculated automatically.
        public Vector3D Target
        {
            get
            {
                return m_target;
            }
            set
            {
                m_target = value;

                if (SpectatorCameraMovement != MySpectatorCameraMovementEnum.Orbit)
                {
                    UpdateOrientation();
                }
            }
        }

        public void UpdateOrientation()
        {
            var forward = MyUtils.Normalize(Target - Position);
            forward = forward.LengthSquared() > 0 ? forward : Vector3D.Forward;

            var unnormalizedRight = Vector3D.Cross(forward, Vector3D.Up);
            var right = unnormalizedRight.LengthSquared() > 0 ? MyUtils.Normalize(unnormalizedRight) : Vector3D.Right;

            var up = MyUtils.Normalize(Vector3D.Cross(right, forward));

            m_orientation = MatrixD.Identity;
            m_orientation.Forward = forward;
            m_orientation.Right = right;
            m_orientation.Up = up;
        }

        public MatrixD Orientation
        {
            get
            {
                return m_orientation;
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


        float m_orbitY = 0;
        float m_orbitX = 0;

        //  Moves and rotates player by specified vector and angles
        public virtual void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            // this method is overriden by MySpecatorCameraController, content of MySpectator::MoveAndRotate is not run

            var oldPosition = Position;

            moveIndicator *= m_speedModeLinear;

            float amountOfMovement = 0.1f;
            float amountOfRotation = 0.0025f * m_speedModeAngular;

            Vector3D moveVector = (Vector3D)moveIndicator * amountOfMovement;

            switch (SpectatorCameraMovement)
            {
                case MySpectatorCameraMovementEnum.UserControlled:
                    {
                        if (rollIndicator != 0)
                        {
                            Vector3D r, u;
                            float rollAmount = rollIndicator * m_speedModeLinear * 0.1f;
                            rollAmount = MathHelper.Clamp(rollAmount, -0.02f, 0.02f);
                            MyUtils.VectorPlaneRotation(m_orientation.Up, m_orientation.Right, out u, out r, rollAmount);
                            m_orientation.Right = r;
                            m_orientation.Up = u;
                        }

                        if (rotationIndicator.X != 0)
                        {
                            Vector3D u, f;
                            MyUtils.VectorPlaneRotation(m_orientation.Up, m_orientation.Forward, out u, out f, rotationIndicator.X * amountOfRotation);
                            m_orientation.Up = u;
                            m_orientation.Forward = f;
                        }

                        if (rotationIndicator.Y != 0)
                        {
                            Vector3D r, f;
                            MyUtils.VectorPlaneRotation(m_orientation.Right, m_orientation.Forward, out r, out f, -rotationIndicator.Y * amountOfRotation);

                            m_orientation.Right = r;
                            m_orientation.Forward = f;
                        }

                        Position += Vector3D.Transform(moveVector, m_orientation);
                    }
                    break;

                case MySpectatorCameraMovementEnum.Orbit:
                    {
                        m_orbitY += rotationIndicator.Y * 0.01f;
                        m_orbitX += rotationIndicator.X * 0.01f;

                        var delta = Position - Target;
                        MatrixD invRot = Matrix.Invert(m_orientation);
                        var deltaInv = Vector3D.Transform(delta, invRot);

                        rotationIndicator *= 0.01f;

                        MatrixD rotationMatrix = MatrixD.CreateRotationX(m_orbitX) * MatrixD.CreateRotationY(m_orbitY) * MatrixD.CreateRotationZ(rollIndicator);
                        delta = Vector3D.Transform(deltaInv, rotationMatrix);

                        Position = Target + delta;

                        var strafe = (m_orientation.Right * moveVector.X) + (m_orientation.Up * moveVector.Y);

                        Target += strafe;
                        Position += strafe;

                        var forwardDelta = m_orientation.Forward * -moveVector.Z;
                        Position += forwardDelta;

                        m_orientation = rotationMatrix;
                    }
                    break;
            }
        }

        public virtual void MoveAndRotateStopped()
        {
        }

        public MatrixD GetViewMatrix()
        {
            return MatrixD.Invert(MatrixD.CreateWorld(Position, m_orientation.Forward, m_orientation.Up));
        }

        public void SetViewMatrix(MatrixD viewMatrix)
        {
            MyUtils.AssertIsValid(viewMatrix);
            
            MatrixD inverted = MatrixD.Invert(viewMatrix);
            Position = inverted.Translation;
            m_orientation = MatrixD.Identity;
            m_orientation.Right = inverted.Right;
            m_orientation.Up = inverted.Up;
            m_orientation.Forward = inverted.Forward;
        }

        public void Reset()
        {
            m_position = Vector3.Zero;
            m_target = m_position + Vector3.Forward;
            ThirdPersonCameraDelta = new Vector3D(-10, 10, -10);
        }
    }
}
