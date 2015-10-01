using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

using Sandbox.ModAPI;

namespace Sandbox.Game.GameSystems.Electricity
{
    partial class MyBattery : IMySuitBattery
    {
        public IMyCharacter Owner
        {
            get { return m_owner; }
        }

        void IMySuitBattery.SetRemainingCapacity(float level)
        {
            m_remainingCapacity = MathHelper.Clamp(level * MyEnergyConstants.BATTERY_MAX_CAPACITY, 0.0f, MyEnergyConstants.BATTERY_MAX_CAPACITY);
            PowerReceiver.Update();
        }

        float IMySuitBattery.CurrentInput
        {
            get { return PowerReceiver.CurrentInput; }
        }

        float IMySuitBattery.RequiredInput
        {
            get { return PowerReceiver.RequiredInput; }
        }

        float IMySuitBattery.MaxRequiredInput
        {
            get { return PowerReceiver.MaxRequiredInput; }
        }

        float IMySuitBattery.InputSuppliedRatio
        {
            get { return PowerReceiver.SuppliedRatio; }
        }

        bool IMySuitBattery.InputIsPowered
        {
            get { return PowerReceiver.IsPowered; }
        }

        bool IMySuitBattery.InputIsAdaptible
        {
            get { return PowerReceiver.IsAdaptible; }
        }
    }
}
