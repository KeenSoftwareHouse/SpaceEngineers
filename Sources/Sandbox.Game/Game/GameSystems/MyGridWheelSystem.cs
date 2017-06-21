using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using System;

namespace Sandbox.Game.GameSystems
{
    public class MyGridWheelSystem
    {
        #region Fields
        public Vector3 AngularVelocity;

        private bool m_wheelsChanged;
        private float m_maxRequiredPowerInput;
        private MyCubeGrid m_grid;
        public HashSet<MyMotorSuspension> Wheels { get { return m_wheels; } }
        private HashSet<MyMotorSuspension> m_wheels;

        #endregion

        #region Properties

	    public  MyResourceSinkComponent SinkComp;

        public int WheelCount { get { return m_wheels.Count; } }

        public Matrix CockpitMatrix;
        private bool m_handbrake;
        private bool m_brake;
        public bool HandBrake
        {
            get { return m_handbrake; }
            set
            {
                if (m_handbrake != value)
                {
                    m_handbrake = value;
                    UpdateBrake();
                }
            }
        }

        public bool Brake
        {
            set
            {
                if (m_brake != value)
                {
                    m_brake = value;
                     UpdateBrake();
                }
            }
        }

        private void UpdateBrake()
        {
            foreach (var motor in m_wheels)
                motor.Brake = m_brake | m_handbrake;
        }


        #endregion

        public MyGridWheelSystem(MyCubeGrid grid)
        {
            m_wheels = new HashSet<MyMotorSuspension>();
            m_wheelsChanged = false;
            m_grid = grid;

			SinkComp = new MyResourceSinkComponent();
			SinkComp.Init(MyStringHash.GetOrCompute("Utility"), m_maxRequiredPowerInput, () => m_maxRequiredPowerInput);
            SinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;

            grid.OnPhysicsChanged += grid_OnPhysicsChanged;
        }

        void grid_OnPhysicsChanged(MyEntity obj)
        {
            if (m_grid.GridSystems != null && m_grid.GridSystems.ControlSystem != null)
            {
                MyShipController controller = m_grid.GridSystems.ControlSystem.GetShipController();
                if (controller != null)
                {
                    InitControl(controller);
                }
            }
        }

        public void Register(MyMotorSuspension motor)
        {
            Debug.Assert(!m_wheels.Contains(motor), "Wheel is already registered in the grid.");
            m_wheels.Add(motor);
            m_wheelsChanged = true;
            motor.EnabledChanged += motor_EnabledChanged;
            motor.Brake = m_handbrake;
        }

        public event Action<MyCubeGrid> OnMotorUnregister;

        public void Unregister(MyMotorSuspension motor)
        {
            Debug.Assert(m_wheels.Contains(motor), "Removing wheel which was not registered.");
            if (motor != null && motor.RotorGrid != null && OnMotorUnregister != null)
                OnMotorUnregister(motor.RotorGrid);
            m_wheels.Remove(motor);
            m_wheelsChanged = true;
            motor.EnabledChanged -= motor_EnabledChanged;
        }

        public void UpdateBeforeSimulation()
        {
            if (m_wheelsChanged)
                RecomputeWheelParameters();

            if (m_grid.Physics != null)
            {
                foreach (var motor in m_wheels)
                {
                    if (!motor.IsWorking)
                        continue;
                    if (motor.Steering)
                    {
                        if (AngularVelocity.X > 0)
                            motor.Right();
                        else if (AngularVelocity.X < 0)
                            motor.Left();
                    }
                    if (motor.Propulsion)
                    {
                        if (AngularVelocity.Z < 0)
                            motor.Forward();
                        else if (AngularVelocity.Z > 0)
                            motor.Backward();
                    }
                }
            }
        }

        public bool HasWorkingWheels(bool propulsion)
        {
            foreach (var motor in m_wheels)
            {
                if (motor.IsWorking)
                {
                    if (propulsion)
                    {
                        if (motor.RotorGrid != null && motor.RotorAngularVelocity.LengthSquared() > 2f)
                            return true;
                    }
                    else
                        return true;
                }
            }
            return false;
        }

        private void RecomputeWheelParameters()
        {
            m_wheelsChanged = false;

            float oldRequirement = m_maxRequiredPowerInput;
            m_maxRequiredPowerInput = 0.0f;

            foreach (var motor in m_wheels)
            {
                if (!IsUsed(motor))
                    continue;

                m_maxRequiredPowerInput += motor.RequiredPowerInput;
            }

            SinkComp.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, m_maxRequiredPowerInput);
			SinkComp.Update();
        }

        private bool IsUsed(MyMotorSuspension motor)
        {
            return motor.Enabled && motor.IsFunctional;
        }

        private void motor_EnabledChanged(MyTerminalBlock obj)
        {
            Debug.Assert(obj is MyMotorBase);
            m_wheelsChanged = true;
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            m_wheelsChanged = true;
        }

        private void Receiver_IsPoweredChanged()
        {
            foreach (var motor in m_wheels)
                motor.UpdateIsWorking();
        }

        internal void InitControl(MyEntity controller)
        {
            foreach (var motor in m_wheels)
                motor.InitControl(controller);
        }

    }
}
