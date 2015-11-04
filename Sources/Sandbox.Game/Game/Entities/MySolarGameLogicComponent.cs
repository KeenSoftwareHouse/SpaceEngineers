using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MySolarGameLogicComponent : MyGameLogicComponent
    {
        private float m_maxOutput = 0f;
        public float MaxOutput
        {
            get
            {
                return m_maxOutput;
            }
        }

        private bool[] m_pivotInSun = new bool[8];
        public bool[] PivotInSun { get { return m_pivotInSun; } }
        private byte m_currentPivot;
        public byte CurrentPivot { get { return m_currentPivot; } }
        private List<MyPhysics.HitInfo> m_hitList = new List<MyPhysics.HitInfo>();

        private Vector3 m_panelOrientation;
        public Vector3 PanelOrientation
        {
            get
            {
                return m_panelOrientation;
            }
        }
        private float m_panelOffset;
        public float PanelOffset
        {
            get
            {
                return m_panelOffset;
            }
        }
        private bool m_isTwoSided;
        private MyTerminalBlock m_solarBlock;

        private bool m_initialized = false;

        public void Initialize(Vector3 panelOrientation, bool isTwoSided, float panelOffset, MyTerminalBlock solarBlock)
        {
            m_initialized = true;

            m_panelOrientation = panelOrientation;
            m_isTwoSided = isTwoSided;
            m_panelOffset = panelOffset;
            m_solarBlock = solarBlock;

            //Warning: this will change the NeedsUpdate variable on the entity
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return null;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            Debug.Assert(m_initialized, "SolarGameLogic was not initialized before use!");

            if (m_solarBlock.CubeGrid.Physics == null)
                return;

            float angleToSun = Vector3.Dot(Vector3.Transform(m_panelOrientation, m_solarBlock.WorldMatrix.GetOrientation()), MySector.DirectionToSunNormalized);
            if ((angleToSun < 0 && !m_isTwoSided) || !m_solarBlock.IsFunctional)
            {
                m_maxOutput = 0;
                return;
            }

            m_currentPivot %= 8;

            MatrixD rot = m_solarBlock.WorldMatrix.GetOrientation();
            float scale = (float)m_solarBlock.WorldMatrix.Forward.Dot(Vector3.Transform(m_panelOrientation, rot));
            float unit = m_solarBlock.BlockDefinition.CubeSize == MyCubeSize.Large ? 2.5f : 0.5f;

            Vector3D pivot = m_solarBlock.WorldMatrix.Translation;
            pivot += ((m_currentPivot % 4 - 1.5f) * unit * scale * (m_solarBlock.BlockDefinition.Size.X / 4f)) * m_solarBlock.WorldMatrix.Left;
            pivot += ((m_currentPivot / 4 - 0.5f) * unit * scale * (m_solarBlock.BlockDefinition.Size.Y / 2f)) * m_solarBlock.WorldMatrix.Up;
            pivot += unit * scale * (m_solarBlock.BlockDefinition.Size.Z / 2f) * Vector3.Transform(m_panelOrientation, rot) * m_panelOffset;

            LineD l = new LineD(pivot + MySector.DirectionToSunNormalized * 100, pivot + MySector.DirectionToSunNormalized * m_solarBlock.CubeGrid.GridSize / 4); //shadows are drawn only 1000m

            MyPhysics.CastRay(l.From, l.To, m_hitList);
            m_pivotInSun[m_currentPivot] = true;
            foreach (var hit in m_hitList)
            {
                var ent = hit.HkHitInfo.GetHitEntity();
                if (ent != m_solarBlock.CubeGrid)
                {
                    m_pivotInSun[m_currentPivot] = false;
                    break;
                }
                else
                {
                    var grid = ent as MyCubeGrid;
                    var pos = grid.RayCastBlocks(l.From, l.To);
                    if (pos.HasValue && grid.GetCubeBlock(pos.Value) != m_solarBlock.SlimBlock)
                    {
                        m_pivotInSun[m_currentPivot] = false;
                        break;
                    }
                }
            }
            int pivotsInSun = 0;
            foreach (bool p in m_pivotInSun)
                if (p)
                    pivotsInSun++;
            
            m_maxOutput = angleToSun;
            if (m_maxOutput < 0)
                if (m_isTwoSided)
                    m_maxOutput = Math.Abs(m_maxOutput);
                else
                    m_maxOutput = 0;
            m_maxOutput *= pivotsInSun / 8f;
            m_currentPivot++;
        }
    }
}
