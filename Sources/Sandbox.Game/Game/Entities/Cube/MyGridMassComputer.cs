using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Havok;
using VRage.Generics;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    struct MassCellData
    {
        public HkMassElement MassElement;
        public float LastMass;
    }

    class MyGridMassComputer : MySparseGrid<HkMassElement,MassCellData>
    {
        [ThreadStatic]
        private static HkInertiaTensorComputer s_inertiaComputer;
        private static HkInertiaTensorComputer InertiaComputer { get { return MyUtils.Init(ref s_inertiaComputer); } }

        [ThreadStatic]
        private static List<HkMassElement> s_tmpElements;
        private static List<HkMassElement> TmpElements { get { return MyUtils.Init(ref s_tmpElements); } }

        const float DefaultUpdateThreshold = 0.1f;

        private float m_updateThreshold;
        private HkMassProperties m_massProperties;
        public MyGridMassComputer(int cellSize, float updateThreshold = DefaultUpdateThreshold) : base(cellSize)
        {
            m_updateThreshold = updateThreshold;
        }

        public HkMassProperties UpdateMass()
        {
            HkMassElement element = new HkMassElement { Tranform = Matrix.Identity };
            bool massChanged = false;
            ProfilerShort.Begin("UpdateDirty", DirtyCells.Count);
            //TODO:we can limit # of recalculated dirty cells per update for perf++
            foreach (var dirtyCell in DirtyCells)
            {
                MySparseGrid<HkMassElement, MassCellData>.Cell cell;
                if (TryGetCell(dirtyCell, out cell))
                {
                    float cellMass = 0;
                    foreach (var item in cell.Items)
                    {
                        TmpElements.Add(item.Value);
                        cellMass += item.Value.Properties.Mass;
                    }

                    //Ignore "unsignificant" changes
                    if (Math.Abs(1 - cell.CellData.LastMass / cellMass) > m_updateThreshold)
                    {
                        element.Properties = InertiaComputer.CombineMassPropertiesInstance(TmpElements);
                        cell.CellData.MassElement = element;
                        cell.CellData.LastMass = cellMass;
                        massChanged = true;
                    }
                    TmpElements.Clear();
                }
                else
                {
                    //we have lost a cell & we dont know how much it contributed so update mass
                    massChanged = true; 
                }
            }
            ProfilerShort.End();
            UnmarkDirtyAll();

            if (!massChanged)
                return m_massProperties;

            foreach (var kv in this)
            {
                TmpElements.Add(kv.Value.CellData.MassElement);
            }

            //Debug.Assert(Count > 0, "Mass can't be zero, in that case, grid should not be created");

            // HACK: this prevents crash, but it's generally on wrong place, but we don't know how to handle it higher on call stack
            if (TmpElements.Count > 0)
            {
                m_massProperties = InertiaComputer.CombineMassPropertiesInstance(TmpElements);
            }
            else
            {
                m_massProperties = default(HkMassProperties);
            }
            TmpElements.Clear();
            return m_massProperties;
        }

        public HkMassProperties CombineMassProperties(List<HkMassElement> elements)
        {
            return InertiaComputer.CombineMassPropertiesInstance(elements);
        }
    }
}
