using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using VRageMath;
using System.Diagnostics;
using VRageRender;

using System.Reflection;
using Sandbox.Common;
using Sandbox.Game.GameSystems;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using VRage.ModAPI;
using Sandbox.ModAPI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ConveyorConnector))]
    public class MyConveyorConnector : MyCubeBlock, IMyConveyorSegmentBlock, IMyConveyorTube
    {
        private MyConveyorSegment m_segment = new MyConveyorSegment();
        private bool m_working;
        private bool m_emissivitySet;

        public override float MaxGlassDistSq
        {
            get
            {
                // Temporary hotfix, tube glass is not visible further than 150m
                return 150 * 150;
            }
        }

        public MyConveyorSegment ConveyorSegment
        {
            get { return m_segment; }
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);
            this.NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_emissivitySet = false;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorSegment(m_segment));
        }

        public void InitializeConveyorSegment()
        {
            MyConveyorLine.BlockLinePositionInformation[] positionInfo = MyConveyorLine.GetBlockLinePositions(this);

            Debug.Assert(positionInfo.Length == 2, "Dummies not correctly defined for conveyor frame");

            if (positionInfo.Length > 0)
            {
                ConveyorLinePosition position1 = PositionToGridCoords(positionInfo[0].Position).GetConnectingPosition();
                ConveyorLinePosition position2 = PositionToGridCoords(positionInfo[1].Position).GetConnectingPosition();
                Debug.Assert(positionInfo[0].LineType == positionInfo[1].LineType, "Inconsistent conveyor line type in conveyor segment block model");

                m_segment.Init(this, position1, position2, positionInfo[0].LineType);
            }
        }

        private ConveyorLinePosition PositionToGridCoords(ConveyorLinePosition position)
        {
            ConveyorLinePosition retval = new ConveyorLinePosition();

            Matrix matrix = new Matrix();
            this.Orientation.GetMatrix(out matrix);
            Vector3 transformedPosition = Vector3.Transform(new Vector3(position.LocalGridPosition), matrix);

            retval.LocalGridPosition = Vector3I.Round(transformedPosition) + this.Position;
            retval.Direction = this.Orientation.TransformDirection(position.Direction);

            return retval;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (m_segment.ConveyorLine == null)
                return;

            if (!m_emissivitySet || m_working != m_segment.ConveyorLine.IsWorking)
            {
                m_working = m_segment.ConveyorLine.IsWorking;
                UpdateEmissivity();
            }
        }

        public override void OnRemovedFromScene(object source)
        {
 	        base.OnRemovedFromScene(source);
            
            m_emissivitySet = false;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
        }

        public override void OnModelChange()
        {
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            Color newColor = m_working ? Color.GreenYellow : Color.DarkRed;

            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, newColor, Color.White);
            m_emissivitySet = true;
        }
    }
}
