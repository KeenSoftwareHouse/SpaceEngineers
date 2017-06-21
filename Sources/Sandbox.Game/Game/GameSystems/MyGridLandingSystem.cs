using Sandbox.Game.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using Sandbox.Game.Entities.Cube;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Localization;
using VRage.ModAPI;

namespace Sandbox.Game.GameSystems
{
    public class MyGridLandingSystem
    {
        static readonly int GEAR_MODE_COUNT = MyUtils.GetMaxValueFromEnum<LandingGearMode>() + 1;
        static readonly List<IMyLandingGear> m_gearTmpList = new List<IMyLandingGear>();

        HashSet<IMyLandingGear>[] m_gearStates;
        LockModeChangedHandler m_onStateChanged;

        public MyStringId HudMessage = MyStringId.NullOrEmpty;

        public MyMultipleEnabledEnum Locked
        {
            get
            {
                var count = TotalGearCount;
                if (count == 0)
                    return MyMultipleEnabledEnum.NoObjects;
                else if (count == this[LandingGearMode.Locked])
                    return MyMultipleEnabledEnum.AllEnabled;
                else if (count == this[LandingGearMode.ReadyToLock] + this[LandingGearMode.Unlocked])
                    return MyMultipleEnabledEnum.AllDisabled;
                else
                    return MyMultipleEnabledEnum.Mixed;
            }
        }

        public int TotalGearCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < GEAR_MODE_COUNT; i++)
                {
                    count += m_gearStates[i].Count;
                }
                return count;
            }
        }

        public int this[LandingGearMode mode]
        {
            get
            {
                return m_gearStates[(int)mode].Count;
            }
        }

        public MyGridLandingSystem()
        {
            m_gearStates = new HashSet<IMyLandingGear>[GEAR_MODE_COUNT];
            for (int i = 0; i < GEAR_MODE_COUNT; i++)
            {
                m_gearStates[i] = new HashSet<IMyLandingGear>();
            }
            m_onStateChanged = new LockModeChangedHandler(StateChanged);
        }

        void StateChanged(IMyLandingGear gear, LandingGearMode oldMode)
        {
            if (oldMode == LandingGearMode.ReadyToLock && gear.LockMode == LandingGearMode.Locked)
                HudMessage = MySpaceTexts.NotificationLandingGearSwitchLocked;
            else if (oldMode == LandingGearMode.Locked && gear.LockMode == LandingGearMode.Unlocked)
                HudMessage = MySpaceTexts.NotificationLandingGearSwitchUnlocked;
            else //if (oldMode == LandingGearMode.ReadyToLock && gear.LockMode == LandingGearMode.Unlocked)
                HudMessage = MyStringId.NullOrEmpty;

            m_gearStates[(int)oldMode].Remove(gear);
            m_gearStates[(int)gear.LockMode].Add(gear);
        }

        public void Switch()
        {
            if (Locked == MyMultipleEnabledEnum.AllEnabled || Locked == MyMultipleEnabledEnum.Mixed)
                Switch(false);
            else if (Locked == MyMultipleEnabledEnum.AllDisabled)
                Switch(true);
        }

        public List<IMyEntity> GetAttachedEntities()
        {
            List<IMyEntity> entities = new List<IMyEntity>();
            foreach (var g in m_gearStates[(int)LandingGearMode.Locked])
            {
                var entity = g.GetAttachedEntity();
                if (entity != null)
                    entities.Add(entity);
            }
            return entities;
        }

        public void Switch(bool enabled)
        {
            int index = enabled ? (int)LandingGearMode.ReadyToLock : (int)LandingGearMode.Locked;
            var resetAutolock =  !enabled && m_gearStates[(int)LandingGearMode.Locked].Count > 0;

            foreach (var g in m_gearStates[index])
            {
                m_gearTmpList.Add(g);
            }
            if (enabled)
            {
                foreach (var g in m_gearStates[(int)LandingGearMode.Unlocked])
                {
                    m_gearTmpList.Add(g);
                }
            }
            foreach (var g in m_gearTmpList)
            {
                g.RequestLock(enabled);
            }

            m_gearTmpList.Clear();

            //Reset Autolock on Deataching (user input)
            if(resetAutolock)
                foreach (var lst in m_gearStates)
                    foreach (var gear in lst)
                        if(gear.AutoLock)
                            gear.ResetAutolock();
        }

        public void Register(IMyLandingGear gear)
        {
            gear.LockModeChanged += m_onStateChanged;
            m_gearStates[(int)gear.LockMode].Add(gear);
        }

        public void Unregister(IMyLandingGear gear)
        {
            m_gearStates[(int)gear.LockMode].Remove(gear);
            gear.LockModeChanged -= m_onStateChanged;
        }
    }
}
