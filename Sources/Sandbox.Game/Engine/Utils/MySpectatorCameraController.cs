using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Lights;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    // ----------------------------------------------------------------------------------------------------
    //  Player with movements like 6DOF camera
    public class MySpectatorCameraController : MySpectator, IMyCameraController
    {
        // ------------------------------------------------------------------------------------------------
        // Increases how far the light can reflect
        private const int REFLECTOR_RANGE_MULTIPLIER = 5;

        public static MySpectatorCameraController Static;

        private double m_yaw;
        private double m_pitch;
        private double m_roll;

        private Vector3D m_lastRightVec = Vector3D.Right;
        private Vector3D m_lastUpVec = Vector3D.Up;
        private MatrixD m_lastOrientation = MatrixD.Identity;
        private float m_lastOrientationWeight = 1;

        private MyLight m_light;
        Vector3 m_lightLocalPosition;
        Matrix m_reflectorAngleMatrix;

        // ------------------------------------------------------------------------------------------------
        public bool IsLightOn { get { return m_light != null && m_light.LightOn; } }
        public bool AlignSpectatorToGravity { get; set; }

        // ------------------------------------------------------------------------------------------------
        public MySpectatorCameraController()
        {
            Static = this;
        }

        // ------------------------------------------------------------------------------------------------
        //  Moves and rotates player by specified vector and angles
        public override void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            switch (SpectatorCameraMovement)
            {
                case MySpectatorCameraMovementEnum.None:
                    break;
                case MySpectatorCameraMovementEnum.ConstantDelta:
                    MoveAndRotate_ConstantDelta();
                    if (IsLightOn)
                        UpdateLightPosition();
                    break;
                case MySpectatorCameraMovementEnum.UserControlled:
                    MoveAndRotate_UserControlled(moveIndicator, rotationIndicator, rollIndicator);
                    if (IsLightOn)
                        UpdateLightPosition();
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------
        private void MoveAndRotate_UserControlled(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
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
            float amountOfMovement = VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 100;
            float amountOfRotation = 0.0025f * m_speedModeAngular;

            rollIndicator = MyInput.Static.GetDeveloperRoll();
            
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

            {
                if (AlignSpectatorToGravity)
                {
                    rotationIndicator.Rotate(m_roll); // TODO: ROLL

                    m_yaw -= rotationIndicator.Y * amountOfRotation;
                    m_pitch -= rotationIndicator.X * amountOfRotation;
                    m_roll -= rollAmount;

                    MathHelper.LimitRadians2PI(ref m_yaw);
                    m_pitch = MathHelper.Clamp(m_pitch, -Math.PI * 0.5f, Math.PI * 0.5f);
                    MathHelper.LimitRadians2PI(ref m_roll);

                    ComputeGravityAlignedOrientation(out m_orientation);
                }
                else
                {
                    if (rotationIndicator.Y != 0)
                    {
                        Vector3D r, f;
                        MyUtils.VectorPlaneRotation(m_orientation.Right, m_orientation.Forward, out r, out f,
                            -rotationIndicator.Y * amountOfRotation);

                        m_orientation.Right = r;
                        m_orientation.Forward = f;
                    }

                    if (rotationIndicator.X != 0)
                    {
                        Vector3D u, f;
                        MyUtils.VectorPlaneRotation(m_orientation.Up, m_orientation.Forward, out u, out f,
                            rotationIndicator.X * amountOfRotation);
                        m_orientation.Up = u;
                        m_orientation.Forward = f;
                    }

                    m_lastOrientation = m_orientation;
                    m_lastOrientationWeight = 1;
                    m_roll = 0;
                    m_pitch = 0;
                }

                float afterburner = (MyInput.Static.IsAnyShiftKeyPressed() ? 1.0f : 0.35f) *
                                    (MyInput.Static.IsAnyCtrlKeyPressed() ? 0.3f : 1);
                moveIndicator *= afterburner * SpeedModeLinear;
                moveVector = moveIndicator * amountOfMovement;
            }


            Position += Vector3.Transform(moveVector, m_orientation);
        }

        private void ComputeGravityAlignedOrientation(out MatrixD resultOrientationStorage)
        {
            // Y axis
            bool inGravityField = true;
            Vector3D upVector = -MyGravityProviderSystem.CalculateTotalGravityInPoint(Position);
            if (upVector.LengthSquared() < MyMathConstants.EPSILON)
            {
                upVector = m_lastUpVec;
                m_lastOrientationWeight = 1;
                inGravityField = false;
            }
            else
            {
                m_lastUpVec = upVector;
            }
            upVector.Normalize();
            // X axis
            Vector3D rightVector = m_lastRightVec - Vector3D.Dot(m_lastRightVec, upVector) * upVector;
            if (rightVector.LengthSquared() < MyMathConstants.EPSILON)
            {
                rightVector = m_orientation.Right - Vector3D.Dot(m_orientation.Right, upVector) * upVector;
                    // backup behavior if singularity happens
                if (rightVector.LengthSquared() < MyMathConstants.EPSILON)
                    rightVector = m_orientation.Forward - Vector3D.Dot(m_orientation.Forward, upVector) * upVector;
                        // backup behavior if singularity happens
            }
            rightVector.Normalize();
            m_lastRightVec = rightVector;
            // Z axis
            Vector3D forwardVector;
            Vector3D.Cross(ref upVector, ref rightVector, out forwardVector);

            resultOrientationStorage = MatrixD.Identity;
            resultOrientationStorage.Right = rightVector;
            resultOrientationStorage.Up = upVector;
            resultOrientationStorage.Forward = forwardVector;
            resultOrientationStorage = MatrixD.CreateFromAxisAngle(Vector3D.Right, m_pitch) * resultOrientationStorage *
                            MatrixD.CreateFromAxisAngle(upVector, m_yaw);
            upVector = resultOrientationStorage.Up;
            rightVector = resultOrientationStorage.Right;
            resultOrientationStorage.Right = Math.Cos(m_roll) * rightVector + Math.Sin(m_roll) * upVector;
            resultOrientationStorage.Up = -Math.Sin(m_roll) * rightVector + Math.Cos(m_roll) * upVector;

            if (inGravityField && m_lastOrientationWeight > 0)
            {
                m_lastOrientationWeight = Math.Max(0, m_lastOrientationWeight - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

                resultOrientationStorage = MatrixD.Slerp(resultOrientationStorage, m_lastOrientation,
                    MathHelper.SmoothStepStable(m_lastOrientationWeight));
            }
            if (!inGravityField)
            {
                m_lastOrientation = resultOrientationStorage;
            }
        }

        // ------------------------------------------------------------------------------------------------
        private void MoveAndRotate_ConstantDelta()
        {
            if (!MyInput.Static.IsAnyAltKeyPressed() && !MyInput.Static.IsAnyCtrlKeyPressed() &&
                !MyInput.Static.IsAnyShiftKeyPressed())
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

            if (MySession.Static.ControlledEntity != null)
            {
                MatrixD gravityOrientationMatrix;
                m_roll = 0;
                m_yaw = 0;
                m_pitch = 0;
                ComputeGravityAlignedOrientation(out gravityOrientationMatrix);
                var target = (Vector3D)MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition();
                Position = target + Vector3D.Transform(ThirdPersonCameraDelta, gravityOrientationMatrix);
                Target = target;
                m_orientation.Up = gravityOrientationMatrix.Up;
            }
        }

        // ------------------------------------------------------------------------------------------------
        void IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            currentCamera.SetViewMatrix(GetViewMatrix());
        }

        // ------------------------------------------------------------------------------------------------
        #region Light
        public void InitLight(bool isLightOn)
        {
            m_light = MyLights.AddLight();
            m_light.Start(MyLight.LightTypeEnum.Spotlight, 1.5f);
            m_light.ShadowDistance = 20;
            m_light.ReflectorFalloff = 5;
            m_light.LightOwner = MyLight.LightOwnerEnum.SmallShip;
            m_light.UseInForwardRender = true;
            m_light.ReflectorTexture = "Textures\\Lights\\dual_reflector_2.dds";
            m_light.Range = 2;

            m_light.ReflectorRange = MyCharacter.REFLECTOR_RANGE;
            m_light.ReflectorColor = MyCharacter.REFLECTOR_COLOR;
            m_light.ReflectorIntensity = MyCharacter.REFLECTOR_INTENSITY;
            m_light.Color = MyCharacter.POINT_COLOR;
            m_light.SpecularColor = new Vector3(MyCharacter.POINT_COLOR_SPECULAR);
            m_light.Intensity = MyCharacter.POINT_LIGHT_INTENSITY;
            // Reflector Range now very far
            m_light.UpdateReflectorRangeAndAngle(MyCharacter.REFLECTOR_CONE_ANGLE, MyCharacter.REFLECTOR_RANGE * REFLECTOR_RANGE_MULTIPLIER);

            m_light.LightOn = isLightOn;
            m_light.ReflectorOn = isLightOn;
        }

        
        public void UpdateLightPosition()
        {
            if (m_light != null)
            {
                MatrixD specMatrix = MatrixD.CreateWorld(Position, m_orientation.Forward, m_orientation.Up);
                m_reflectorAngleMatrix = MatrixD.CreateFromAxisAngle(specMatrix.Backward, MathHelper.ToRadians(MyCharacter.REFLECTOR_DIRECTION));
                m_light.ReflectorDirection = Vector3.Transform(specMatrix.Forward, m_reflectorAngleMatrix);
                m_light.ReflectorUp = specMatrix.Up;
                m_light.Position = Position;
                m_light.UpdateLight();
            }
        }

        /// <summary>
        /// Switch the light of the spectator - especially relevant during night time or dark zone
        /// </summary>
        public void SwitchLight()
        {
            if (m_light != null)
            {
                m_light.LightOn = !m_light.LightOn;
                m_light.ReflectorOn = !m_light.ReflectorOn;
                m_light.UpdateLight();
            }
        }

        public void TurnLightOff()
        {
            if (m_light != null)
            {
                m_light.LightOn = false;
                m_light.ReflectorOn = false;
                m_light.UpdateLight();
            }
        }

        public void CleanLight()
        {
            if (m_light != null)
            {
                MyLights.RemoveLight(m_light);
                m_light = null;
            }
        }
        #endregion

        // ------------------------------------------------------------------------------------------------
        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            Rotate(rotationIndicator, rollIndicator);
        }

        // ------------------------------------------------------------------------------------------------
        void IMyCameraController.RotateStopped()
        {
            RotateStopped();
        }

        // ------------------------------------------------------------------------------------------------
        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }

        // ------------------------------------------------------------------------------------------------
        public void OnReleaseControl(IMyCameraController newCameraController)
        {
            TurnLightOff();
        }

        // ------------------------------------------------------------------------------------------------
        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {
            OnAssumeControl(previousCameraController);
        }

        // ------------------------------------------------------------------------------------------------
        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
        }

        // ------------------------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------------------------------
        bool IMyCameraController.HandleUse()
        {
            return false;
        }

        // ------------------------------------------------------------------------------------------------
        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------
        bool IMyCameraController.HandlePickUp()
        {
            return false;
        }
    }
}
