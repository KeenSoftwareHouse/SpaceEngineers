#region Using

using Sandbox.Common;
using Sandbox.Game.Localization;
using System;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Game;
using VRage.Utils;


#endregion

namespace Sandbox.Game.Gui
{
    #region Ship Info
    public class MyHudShipInfo
    {
        private enum LineEnum
        {
            ReflectorLights,
            Mass,
            Speed,
            PowerUsage,
            ReactorsMaxOutput,
            ThrustCount,
            DampenersState,
            GyroCount,
            FuelTime,
            NumberOfBatteries,
            PowerState,
            LandingGearState,
            LandingGearStateSecondLine,
        }

        private static StringBuilder m_formattingCache = new StringBuilder();

        private MyMultipleEnabledEnum m_reflectorLights;
        public MyMultipleEnabledEnum ReflectorLights
        {
            get { return m_reflectorLights; }
            set
            {
                if (m_reflectorLights != value)
                {
                    m_reflectorLights = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_mass;
        public int Mass
        {
            get { return m_mass; }
            set
            {
                if (m_mass != value)
                {
                    m_mass = value;
                    m_needsRefresh = true;
                }
            }
        }

        public bool SpeedInKmH;
        private float m_speed;
        public float Speed
        {
            get { return m_speed; }
            set
            {
                if (m_speed != value)
                {
                    m_speed = value;
                    m_needsRefresh = true;
                }
            }
        }

        private float m_powerUsage;
        public float PowerUsage
        {
            get { return m_powerUsage; }
            set
            {
                if (m_powerUsage != value)
                {
                    m_powerUsage = value;
                    m_needsRefresh = true;
                }
            }
        }

        private float m_reactors;
        public float Reactors
        {
            get { return m_reactors; }
            set
            {
                if (m_reactors != value)
                {
                    m_reactors = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_landingGearsInProximity = 0;
        public int LandingGearsInProximity
        {
            get { return m_landingGearsInProximity; }
            set
            {
                if (m_landingGearsInProximity != value)
                {
                    m_landingGearsInProximity = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_landingGearsLocked = 0;
        public int LandingGearsLocked
        {
            get { return m_landingGearsLocked; }
            set
            {
                if (m_landingGearsLocked != value)
                {
                    m_landingGearsLocked = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_landingGearsTotal = 0;
        public int LandingGearsTotal
        {
            get { return m_landingGearsTotal; }
            set
            {
                if (m_landingGearsTotal != value)
                {
                    m_landingGearsTotal = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_thrustCount;
        public int ThrustCount
        {
            get { return m_thrustCount; }
            set
            {
                if (m_thrustCount != value)
                {
                    m_thrustCount = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_gyroCount;
        public int GyroCount
        {
            get { return m_gyroCount; }
            set
            {
                if (m_gyroCount != value)
                {
                    m_gyroCount = value;
                    m_needsRefresh = true;
                }
            }
        }

        private int m_numberOfBatteries;
        public int NumberOfBatteries
        {
            get { return m_numberOfBatteries; }
            set
            {
                if (m_numberOfBatteries != value)
                {
                    m_numberOfBatteries = value;
                    m_needsRefresh = true;
                }
            }
        }

        /// <summary>
        /// This time is in hours, as it is computed from MWh (MegaWatt-hours).
        /// </summary>
        public float FuelRemainingTime
        {
            get { return m_fuelRemainingTime; }
            set
            {
                if (m_fuelRemainingTime != value)
                {
                    m_fuelRemainingTime = value;
                    m_needsRefresh = true;
                }
            }
        }
        private float m_fuelRemainingTime;

        public MyResourceStateEnum ResourceState
        {
            get { return m_resourceState; }
            set
            {
                if (m_resourceState != value)
                {
                    m_resourceState = value;
                    m_needsRefresh = true;
                }
            }
        }
        private MyResourceStateEnum m_resourceState;

        public bool DampenersEnabled
        {
            get { return m_dampenersEnabled; }
            set
            {
                if (m_dampenersEnabled != value)
                {
                    m_dampenersEnabled = value;
                    m_needsRefresh = true;
                }
            }
        }
        private bool m_dampenersEnabled;

        private bool m_needsRefresh = true;

        public MyHudNameValueData Data
        {
            get { if (m_needsRefresh) Refresh(); return m_data; }
        }
        private MyHudNameValueData m_data;

        public MyHudShipInfo()
        {
            m_data = new MyHudNameValueData(typeof(LineEnum).GetEnumValues().Length);
            Reload();
        }

        public void Reload()
        {
            var data = Data;
            data[(int)LineEnum.Mass].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameMass));
            data[(int)LineEnum.Speed].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameSpeed));
            data[(int)LineEnum.PowerUsage].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNamePowerUsage));
            data[(int)LineEnum.ReactorsMaxOutput].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameReactors));
            data[(int)LineEnum.FuelTime].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameFuelTime));
            data[(int)LineEnum.NumberOfBatteries].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameNumberOfBatteries));
            data[(int)LineEnum.GyroCount].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameGyroscopes));
            data[(int)LineEnum.ThrustCount].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameThrusts));
            data[(int)LineEnum.DampenersState].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameDampeners));
            data[(int)LineEnum.LandingGearState].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameLandingGear));
            m_needsRefresh = true;
        }

        public bool Visible { get; private set; }

        public void Show(Action<MyHudShipInfo> propertiesInit)
        {
            Visible = true;
            if (propertiesInit != null)
                propertiesInit(this);
        }

        public void Hide()
        {
            Visible = false;
        }

        private void Refresh()
        {
            m_needsRefresh = false;
            var items = Data;

            items[(int)LineEnum.ReflectorLights].Name.Clear()
                .AppendStringBuilder((ReflectorLights == MyMultipleEnabledEnum.AllDisabled)
                        ? MyTexts.Get(MySpaceTexts.HudInfoReflectorsOff)
                        : (ReflectorLights == MyMultipleEnabledEnum.NoObjects) ? MyTexts.Get(MySpaceTexts.HudInfoNoReflectors)
                                                                               : MyTexts.Get(MySpaceTexts.HudInfoReflectorsOn));
            if (Mass == 0)
                items[(int)LineEnum.Mass].Value.Clear().Append("-").Append(" kg");
            else
                items[(int)LineEnum.Mass].Value.Clear().AppendInt32(Mass).Append(" kg");
            if(SpeedInKmH)
                items[(int)LineEnum.Speed].Value.Clear().AppendDecimal(Speed * 3.6f, 1).Append(" km/h");
            else
                items[(int)LineEnum.Speed].Value.Clear().AppendDecimal(Speed, 1).Append(" m/s");

            var powerState = items[(int)LineEnum.PowerState];
            if (ResourceState == MyResourceStateEnum.NoPower)
            {
                powerState.Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNoPower));
                powerState.Visible = true;
            }
            else
                powerState.Visible = false;

            var powerUsage = items[(int)LineEnum.PowerUsage];
            if (ResourceState == MyResourceStateEnum.OverloadBlackout || ResourceState == MyResourceStateEnum.OverloadAdaptible)
                powerUsage.NameFont = powerUsage.ValueFont = MyFontEnum.Red;
            else
                powerUsage.NameFont = powerUsage.ValueFont = null;

            powerUsage.Value.Clear();
            if (ResourceState == MyResourceStateEnum.OverloadBlackout)
                powerUsage.Value.AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoPowerOverload));
            else
                powerUsage.Value.AppendDecimal(PowerUsage * 100, 2).Append(" %");

            {
                var text = items[(int)LineEnum.ReactorsMaxOutput].Value;
                text.Clear();
                MyValueFormatter.AppendWorkInBestUnit(Reactors, text);
            }

            var fuelTime = items[(int)LineEnum.FuelTime];
            fuelTime.Value.Clear();
            if (ResourceState != MyResourceStateEnum.NoPower)
            {
                MyValueFormatter.AppendTimeInBestUnit(FuelRemainingTime * 3600, fuelTime.Value);
                fuelTime.Visible = true;
            }
            else
                fuelTime.Visible = false;

            var numberOfBatteries = items[(int)LineEnum.NumberOfBatteries];
            numberOfBatteries.Value.Clear().AppendInt32(NumberOfBatteries);

            var gyroCount = items[(int)LineEnum.GyroCount];
            gyroCount.Value.Clear().AppendInt32(GyroCount);
            if (GyroCount == 0)
                gyroCount.NameFont = gyroCount.ValueFont = MyFontEnum.Red;
            else
                gyroCount.NameFont = gyroCount.ValueFont = null;

            var thrustCount = items[(int)LineEnum.ThrustCount];
            thrustCount.Value.Clear().AppendInt32(ThrustCount);
            if (ThrustCount == 0)
                thrustCount.NameFont = thrustCount.ValueFont = MyFontEnum.Red;
            else
                thrustCount.NameFont = thrustCount.ValueFont = null;

            var dampenersState = items[(int)LineEnum.DampenersState];
            dampenersState.Value.Clear().AppendStringBuilder(MyTexts.Get((DampenersEnabled) ? MySpaceTexts.HudInfoOn : MySpaceTexts.HudInfoOff));

            var landingGearState = items[(int)LineEnum.LandingGearState];
            var landingGearStateLine2 = items[(int)LineEnum.LandingGearStateSecondLine];
            if (LandingGearsLocked > 0)
            {
                items[(int)LineEnum.LandingGearStateSecondLine].Name.Clear().Append("  ").AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameLocked));
                landingGearState.Value.Clear().Append(LandingGearsTotal);
                landingGearStateLine2.Value.Clear().AppendInt32(LandingGearsLocked);
            }
            else
            {
                items[(int)LineEnum.LandingGearStateSecondLine].Name.Clear().Append("  ").AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameInProximity));
                landingGearState.Value.Clear().Append(LandingGearsTotal);
                landingGearStateLine2.Value.Clear().AppendInt32(LandingGearsInProximity);
            }
        }
    }
    #endregion
}
