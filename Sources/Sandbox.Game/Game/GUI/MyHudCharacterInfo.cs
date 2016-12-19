#region Using

using Sandbox.Common;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character.Components;
using VRage;
using VRage;
using VRage.Game;

#endregion

namespace Sandbox.Game.Gui
{
    #region Character Info
    public enum MyHudCharacterStateEnum
    {
        Crouching,
        Standing,
        Falling,
        Flying,
        PilotingSmallShip,
        PilotingLargeShip,
        ControllingStation,
        Magnetic
    }

    public class MyHudCharacterInfo
    {
        private enum LineEnum
        {
			CharacterState,
            HelmetState,
            Health,
            Jetpack,
            Dampeners,
            Lights,
            Mass,
            Speed,
            Energy,
            Oxygen,
            Hydrogen,
            Inventory,
            BroadcastRange,
            BroadcastOn,
        }

        private MyHudCharacterStateEnum m_state;
        public MyHudCharacterStateEnum State
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_dampenersEnabled;
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

        private bool m_jetpackEnabled;
        public bool JetpackEnabled
        {
            get { return m_jetpackEnabled; }
            set
            {
                if (m_jetpackEnabled != value)
                {
                    m_jetpackEnabled = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_broadcastEnabled;
        public bool BroadcastEnabled
        {
            get { return m_broadcastEnabled; }
            set
            {
                if (m_broadcastEnabled != value)
                {
                    m_broadcastEnabled = value;
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

        private int m_highestSpeedIndex = 0;
		private int m_currentSpeedIndex = 0;
		private const int m_speedHistoryLength = 8;
		private float[] m_speedHistory = new float[m_speedHistoryLength];
        public float Speed
        {
            get { return m_speedHistory[m_highestSpeedIndex]; }
			set
			{
				if (m_highestSpeedIndex == (m_currentSpeedIndex + 1) % m_speedHistoryLength)
				{
					var oldHighestValue = Speed;
					float highestValue = float.MinValue;
					int newHighestIndex = m_currentSpeedIndex;
					for (int index = (m_highestSpeedIndex + 1) % m_speedHistoryLength; index != m_highestSpeedIndex; index = (++index) % m_speedHistoryLength)
					{
						if (m_speedHistory[index] > highestValue)
						{
							newHighestIndex = index;
							highestValue = m_speedHistory[newHighestIndex];
						}
					}
					m_highestSpeedIndex = newHighestIndex;
					if(Speed != oldHighestValue)
						m_needsRefresh = true;
				}
				m_speedHistory[m_currentSpeedIndex = ((m_currentSpeedIndex+1) % m_speedHistoryLength)] = value;
				if (Speed <= value)
				{
					m_highestSpeedIndex = m_currentSpeedIndex;
					m_needsRefresh = true;
				}
			}
        }

        /// <summary>
        /// Battery energy in %.
        /// </summary>
        public float BatteryEnergy
        {
            get { return m_batteryEnergy; }
            set
            {
                if (m_batteryEnergy != value)
                {
                    m_batteryEnergy = value;
                    m_needsRefresh = true;
                }
            }
        }
        private float m_batteryEnergy;

        private bool m_isBatteryEnergyLow;
        public bool IsBatteryEnergyLow
        {
            get { return m_isBatteryEnergyLow; }
            set
            {
                if (m_isBatteryEnergyLow != value)
                {
                    m_isBatteryEnergyLow = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_lightEnabled;
        public bool LightEnabled
        {
            get { return m_lightEnabled; }
            set
            {
                if (m_lightEnabled != value)
                {
                    m_lightEnabled = value;
                    m_needsRefresh = true;
                }
            }
        }

        private float m_oxygenLevel;
        public float OxygenLevel
        {
            get { return m_oxygenLevel; }
            set
            {
                if (m_oxygenLevel != value)
                {
                    m_oxygenLevel = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_isOxygenLevelLow;
        public bool IsOxygenLevelLow
        {
            get { return m_isOxygenLevelLow; }
            set
            {
                if (m_isOxygenLevelLow != value)
                {
                    m_isOxygenLevelLow = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_isHelmetOn;
        public bool IsHelmetOn
        {
            get { return m_isHelmetOn; }
            set
            {
                if (m_isHelmetOn != value)
                {
                    m_isHelmetOn = value;
                    m_needsRefresh = true;
                }
            }
        }

        private float m_hydrogenRatio;
        public float HydrogenRatio
        {
            get { return m_hydrogenRatio; }
            set
            {
                if (m_hydrogenRatio != value)
                {
                    m_hydrogenRatio = value;
                    m_needsRefresh = true;
                }
            }
        }

        private bool m_needsRefresh = true;

        public MyHudNameValueData Data
        {
            get { if (m_needsRefresh) Refresh(); return m_data; }
        }
        private MyHudNameValueData m_data;

        public MyHudCharacterInfo()
        {
            m_data = new MyHudNameValueData(typeof(LineEnum).GetEnumValues().Length);
            Reload();
        }


        /// <summary>
        /// Health in %.
        /// </summary>
        public float HealthRatio
        {
            get { return m_healthRatio; }
            set
            {
                if (m_healthRatio != value)
                {
                    m_healthRatio = value;
                    m_needsRefresh = true;
                }
            }
        }


        public float BroadcastRange { get; set; }

        public MyFixedPoint InventoryVolume { get; set; }

        public bool IsInventoryFull { get; set; }

        //private float m_inventoryVolume;

        private float m_healthRatio;

        private bool m_isHealthLow;
        public bool IsHealthLow
        {
            get { return m_isHealthLow; }
            set
            {
                if (m_isHealthLow != value)
                {
                    m_isHealthLow = value;
                    m_needsRefresh = true;
                }
            }
        }

        public void Reload()
        {
            var items = m_data;
            items[(int)LineEnum.Dampeners].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameDampeners));
            items[(int)LineEnum.Jetpack].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameJetpack));
            items[(int)LineEnum.Energy].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameEnergy));
            items[(int)LineEnum.Mass].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameMass));
            items[(int)LineEnum.Speed].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameSpeed));
            items[(int)LineEnum.Lights].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameLights));
            items[(int)LineEnum.Health].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameHealth));
            items[(int)LineEnum.Inventory].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoInventoryVolume));
            items[(int)LineEnum.BroadcastRange].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoNameBroadcastRange));
            items[(int)LineEnum.BroadcastOn].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoBroadcasting));
            items[(int)LineEnum.Oxygen].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.DisplayName_Item_Oxygen));
            items[(int)LineEnum.Hydrogen].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.DisplayName_Item_Hydrogen));
            m_needsRefresh = true;
        }

        private void Refresh()
        {
            m_needsRefresh = false;
            var items = Data;

            StringBuilder stateText = null;
            switch (State)
            {
                case MyHudCharacterStateEnum.Crouching: stateText = MyTexts.Get(MySpaceTexts.HudInfoCrouching); break;
                case MyHudCharacterStateEnum.Standing: stateText = MyTexts.Get(MySpaceTexts.HudInfoStanding); break;
                case MyHudCharacterStateEnum.Falling: stateText = MyTexts.Get(MySpaceTexts.HudInfoFalling); break;
                case MyHudCharacterStateEnum.Flying: stateText = MyTexts.Get(MySpaceTexts.HudInfoFlying); break;
                case MyHudCharacterStateEnum.PilotingLargeShip: stateText = MyTexts.Get(MySpaceTexts.HudInfoPilotingLargeShip); break;
                case MyHudCharacterStateEnum.PilotingSmallShip: stateText = MyTexts.Get(MySpaceTexts.HudInfoPilotingSmallShip); break;
                case MyHudCharacterStateEnum.ControllingStation: stateText = MyTexts.Get(MySpaceTexts.HudInfoControllingStation); break;
                case MyHudCharacterStateEnum.Magnetic: stateText = MyTexts.Get(MySpaceTexts.HudInfoMagnetic); break;
                default:
                    Debug.Fail("Missing character state.");
                    break;
            }

            if (MySession.Static.Settings.EnableOxygen)
            {
				items[(int)LineEnum.HelmetState].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoHelmet));
				items[(int)LineEnum.HelmetState].Value.Clear().AppendStringBuilder(GetOnOffText(IsHelmetOn));
            }

			items[(int)LineEnum.CharacterState].Name.Clear();
			items[(int)LineEnum.CharacterState].Value.Clear();
			if (!FitsInSmallBackground())
			{
				items[(int)LineEnum.CharacterState].Name.AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudInfoState));
				items[(int)LineEnum.CharacterState].Value.AppendStringBuilder(stateText);
			}
			else
				items[(int)LineEnum.CharacterState].Name.AppendStringBuilder(stateText);
            items[(int)LineEnum.Lights].Value.Clear().AppendStringBuilder(GetOnOffText(LightEnabled));
            items[(int)LineEnum.Mass].Value.Clear().AppendInt32(Mass).Append(" kg");
            items[(int)LineEnum.Speed].Value.Clear().AppendDecimal(Speed, 1).Append(" m/s");
            items[(int)LineEnum.Jetpack].Value.Clear().AppendStringBuilder(GetOnOffText(JetpackEnabled));
            items[(int)LineEnum.Dampeners].Value.Clear().AppendStringBuilder(GetOnOffText(DampenersEnabled));
            items[(int)LineEnum.BroadcastOn].Value.Clear().AppendStringBuilder(GetOnOffText(BroadcastEnabled));
            items[(int)LineEnum.Oxygen].Value.Clear().AppendDecimal(OxygenLevel * 100f, 1).Append(" %");

            items[(int)LineEnum.Hydrogen].Value.Clear().AppendDecimal(HydrogenRatio * 100f, 1).Append(" %");

            items[(int)LineEnum.Lights].Visible    = true;
            items[(int)LineEnum.Mass].Visible      = true;
            items[(int)LineEnum.Speed].Visible     = true;
            items[(int)LineEnum.Dampeners].Visible = true;


            var energyItem = items[(int)LineEnum.Energy];
            var healthItem = items[(int)LineEnum.Health];
            var inventoryItem = items[(int)LineEnum.Inventory];
            var oxygenItem = items[(int)LineEnum.Oxygen];
            var hydrogenItem = items[(int) LineEnum.Hydrogen];
            energyItem.Value.Clear().AppendDecimal((float)Math.Round(BatteryEnergy, 1), 1).Append(" %");
            healthItem.Value.Clear().AppendDecimal(HealthRatio * 100f, 0).Append(" %");
            inventoryItem.Value.Clear().AppendDecimal((double)InventoryVolume * 1000, 0).Append(" l");
            energyItem.NameFont = energyItem.ValueFont = IsBatteryEnergyLow ? MyFontEnum.Red : null;
            healthItem.NameFont = healthItem.ValueFont = IsHealthLow ? MyFontEnum.Red : null;
            if (!MySession.Static.CreativeMode)
                inventoryItem.NameFont = inventoryItem.ValueFont = IsInventoryFull ? MyFontEnum.Red : null;

            items[(int)LineEnum.BroadcastRange].Value.Clear().AppendDecimal(BroadcastRange, 0).Append(" m");

            oxygenItem.NameFont = oxygenItem.ValueFont = IsOxygenLevelLow ? MyFontEnum.Red : null;
            oxygenItem.Visible = MySession.Static.Settings.EnableOxygen;

            var character = MySession.Static.LocalCharacter;
            bool showHydrogen = MyFakes.ENABLE_HYDROGEN_FUEL;
            if (showHydrogen && character != null)
            {
                showHydrogen = character.OxygenComponent != null && showHydrogen && character.OxygenComponent.ContainsGasStorage(MyCharacterOxygenComponent.HydrogenId);
            }
            hydrogenItem.Visible = showHydrogen;
        }

        private StringBuilder GetOnOffText(bool value)
        {
            return (value) ? MyTexts.Get(MySpaceTexts.HudInfoOn)
                           : MyTexts.Get(MySpaceTexts.HudInfoOff);
        }

        public bool Visible { get; private set; }

        public void Show(Action<MyHudCharacterInfo> propertiesInit)
        {
            Visible = true;
            if (propertiesInit != null)
                propertiesInit(this);
        }

        public void Hide()
        {
            Visible = false;
        }

        /// <summary>
        /// Indicates that ship info requires only small background on HUD.
        /// </summary>
        /// <returns></returns>
        internal bool FitsInSmallBackground()
        {
            return (State == MyHudCharacterStateEnum.PilotingSmallShip ||
                    State == MyHudCharacterStateEnum.PilotingLargeShip ||
                    State == MyHudCharacterStateEnum.ControllingStation);
        }

    }
    #endregion
}
