using ParallelTasks;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MySolarGameLogicComponent : MyGameLogicComponent
    {
        private const int NUMBER_OF_PIVOTS = 8;

        private float m_maxOutput = 0f;
        public float MaxOutput
        {
            get
            {
                return m_maxOutput;
            }
        }

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
        private MyFunctionalBlock m_solarBlock;

        private bool m_initialized = false;

        // Values accessed by debugger
        private byte m_debugCurrentPivot;
        public byte DebugCurrentPivot { get { return m_debugCurrentPivot; } }

        private bool[] m_debugIsPivotInSun = new bool[NUMBER_OF_PIVOTS];
        public bool[] DebugIsPivotInSun { get { return m_debugIsPivotInSun; } }

        // Threaded computation values
        private bool m_isBackgroundProcessing = false;
        private byte m_currentPivot = 0;
        private float m_angleToSun = 0;
        private int m_pivotsInSun = 0;
        private bool[] m_isPivotInSun = new bool[NUMBER_OF_PIVOTS];
        private List<MyPhysics.HitInfo> m_hitList = new List<MyPhysics.HitInfo>();

        public void Initialize(Vector3 panelOrientation, bool isTwoSided, float panelOffset, MyFunctionalBlock solarBlock)
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

            // If the block is not functional, stop processing here.
            if (!m_solarBlock.Enabled)
            {
                m_maxOutput = 0;
                return;
            }

            // Only recompute the sunlight if the background thread finished processing
            if (!m_isBackgroundProcessing)
            {
                m_isBackgroundProcessing = true;

                // Copy data ready for thread
                m_currentPivot = m_debugCurrentPivot;
                for (int i = 0; i < NUMBER_OF_PIVOTS; i++)
                    m_isPivotInSun[i] = m_debugIsPivotInSun[i];

                Parallel.Start(ComputeSunAngle, OnSunAngleComputed);
            }
        }

        private void ComputeSunAngle()
        {
            m_angleToSun = Vector3.Dot(Vector3.Transform(m_panelOrientation, m_solarBlock.WorldMatrix.GetOrientation()), MySector.DirectionToSunNormalized);

            // If the sun is on the backside of the panel, and we're not two-sided, OR the block not functional, just stop processing here
            if ((m_angleToSun < 0 && !m_isTwoSided) || !m_solarBlock.IsFunctional)
            {
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

            MyPhysics.CastRay(l.To, l.From, m_hitList, MyPhysics.CollisionLayers.DefaultCollisionLayer);
            m_isPivotInSun[m_currentPivot] = true;
            foreach (var hit in m_hitList)
            {
                var ent = hit.HkHitInfo.GetHitEntity();
                if (ent != m_solarBlock.CubeGrid)
                {
                    m_isPivotInSun[m_currentPivot] = false;
                    break;
                }
                else
                {
                    var grid = ent as MyCubeGrid;
                    var pos = grid.RayCastBlocks(l.From, l.To);
                    if (pos.HasValue && grid.GetCubeBlock(pos.Value) != m_solarBlock.SlimBlock)
                    {
                        m_isPivotInSun[m_currentPivot] = false;
                        break;
                    }
                }
            }

            m_pivotsInSun = 0;
            foreach (bool p in m_isPivotInSun)
            {
                if (p)
                    m_pivotsInSun++;
            }
        }

        private void OnSunAngleComputed()
        {
            // If the sun is on the backside of the panel, and we're not two-sided, OR the block is toggled off return zero (continue background checking though)
            if ((m_angleToSun < 0 && !m_isTwoSided) || !m_solarBlock.Enabled)
            {
                m_maxOutput = 0;
                m_isBackgroundProcessing = false;
                return;
            }

            m_maxOutput = m_angleToSun;
            if (m_maxOutput < 0)
            {
                if (m_isTwoSided)
                    m_maxOutput = Math.Abs(m_maxOutput);
                else
                    m_maxOutput = 0;
            }

            m_maxOutput *= m_pivotsInSun / 8f;

            m_debugCurrentPivot = m_currentPivot;
            m_debugCurrentPivot++;

            for (int i = 0; i < NUMBER_OF_PIVOTS; i++)
                m_debugIsPivotInSun[i] = m_isPivotInSun[i];

            m_isBackgroundProcessing = false;
        }
    }
}
