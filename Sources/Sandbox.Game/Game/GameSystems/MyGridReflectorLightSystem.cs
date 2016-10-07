using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Interfaces;
using VRage;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics;
using VRage.Profiler;

namespace Sandbox.Game.GameSystems
{
    public class MyGridReflectorLightSystem
    {
        HashSet<MyReflectorLight> m_reflectors;

        public int ReflectorCount { get { return m_reflectors.Count; } }

        public bool IsClosing = false;

        public MyMultipleEnabledEnum ReflectorsEnabled
        {
            get
            {
                if (m_reflectorsEnabledNeedsRefresh)
                    RefreshReflectorsEnabled();

                return m_reflectorsEnabled;
            }
            set
            {
                Debug.Assert(value != MyMultipleEnabledEnum.Mixed, "You must NOT use this property to set mixed state.");
                Debug.Assert(value != MyMultipleEnabledEnum.NoObjects, "You must NOT use this property to set state without any objects.");
                if (m_reflectorsEnabled != value && m_reflectorsEnabled != MyMultipleEnabledEnum.NoObjects && !IsClosing)
                {
                    m_grid.SendReflectorState(value);
                }
            }
        }
        private MyMultipleEnabledEnum m_reflectorsEnabled;
        private bool m_reflectorsEnabledNeedsRefresh;
        private MyCubeGrid m_grid;

        public MyGridReflectorLightSystem(MyCubeGrid grid)
        {
            m_reflectors = new HashSet<MyReflectorLight>();
            m_reflectorsEnabled = MyMultipleEnabledEnum.NoObjects;
            m_grid = grid;
        }

        public void ReflectorStateChanged(MyMultipleEnabledEnum enabledState)
        {
            m_reflectorsEnabled = enabledState;
            bool enabled = (enabledState == MyMultipleEnabledEnum.AllEnabled);
            foreach (var reflector in m_reflectors)
            {
                reflector.EnabledChanged -= reflector_EnabledChanged;
                reflector.Enabled = enabled;
                reflector.EnabledChanged += reflector_EnabledChanged;
            }

            m_reflectorsEnabledNeedsRefresh = false;
        }

        public void Register(MyReflectorLight reflector)
        {
            Debug.Assert(!m_reflectors.Contains(reflector), "Reflector is already registered in the grid.");
            m_reflectors.Add(reflector);
            reflector.EnabledChanged += reflector_EnabledChanged;
            if (m_reflectors.Count == 1)
            {
                m_reflectorsEnabled = (reflector.Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            }
            else if ((ReflectorsEnabled == MyMultipleEnabledEnum.AllEnabled && !reflector.Enabled) ||
                     (ReflectorsEnabled == MyMultipleEnabledEnum.AllDisabled && reflector.Enabled))
            {
                m_reflectorsEnabled = MyMultipleEnabledEnum.Mixed;
            }
        }

        public void Unregister(MyReflectorLight reflector)
        {
            Debug.Assert(m_reflectors.Contains(reflector), "Removing reflector which was not registered.");
            m_reflectors.Remove(reflector);
            reflector.EnabledChanged -= reflector_EnabledChanged;
            if (m_reflectors.Count == 0)
            {
                m_reflectorsEnabled = MyMultipleEnabledEnum.NoObjects;
            }
            else if (m_reflectors.Count == 1)
            {
                ReflectorsEnabled = (m_reflectors.First().Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            }
            else if (ReflectorsEnabled == MyMultipleEnabledEnum.Mixed)
            {
                // We were in mixed state and need to check whether we still are.
                m_reflectorsEnabledNeedsRefresh = true;
            }
        }

        private void RefreshReflectorsEnabled()
        {
            ProfilerShort.Begin("MyGridReflectorLightSystem.RefreshReflectorsEnabled");
            m_reflectorsEnabledNeedsRefresh = false;
            // Simplest method for now. If it takes too long at some point, we can change it.

            bool allOn = true;
            bool allOff = true;
            foreach (var tmp in m_reflectors)
            {
                allOn = allOn && tmp.Enabled;
                allOff = allOff && !tmp.Enabled;
                if (!allOn && !allOff)
                {
                    m_reflectorsEnabled = MyMultipleEnabledEnum.Mixed;
                    ProfilerShort.End();
                    return;
                }
            }
            ReflectorsEnabled = (allOn) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            ProfilerShort.End();
        }

        private void reflector_EnabledChanged(MyTerminalBlock obj)
        {
            Debug.Assert(obj is MyReflectorLight);
            m_reflectorsEnabledNeedsRefresh = true;
        }

    }
}
