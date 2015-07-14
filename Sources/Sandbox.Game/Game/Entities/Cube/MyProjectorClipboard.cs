using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public class MyProjectorClipboard : MyGridClipboard
    {
        private MyProjector m_projector;


        public MyProjectorClipboard(MyProjector projector)
            : base(MyPerGameSettings.PastingSettings)
        {
            MyDebug.AssertDebug(projector != null);
            m_projector = projector;
            m_calculateVelocity = false;
        }

        private bool m_hasPreviewBBox = false;
        public override bool HasPreviewBBox
        {
            get
            {
                return m_hasPreviewBBox;
            }
            set
            {
                m_hasPreviewBBox = value;
            }
        }

        protected override float Transparency
        {
            get
            {
                return 0f;
            }
        }

        private bool m_projectionCanBePlaced;
        protected override bool CanBePlaced
        {
            get
            {
                return m_projectionCanBePlaced;
            }
        }

        public void Clear()
        {
            CopiedGrids.Clear();
            m_copiedGridOffsets.Clear();
        }

        protected override void TestBuildingMaterials()
        {
            m_characterHasEnoughMaterials = true;
        }

        public bool HasGridsLoaded()
        {
            return (CopiedGrids != null && CopiedGrids.Count > 0);
        }

        public void ProcessCubeGrid(MyObjectBuilder_CubeGrid gridBuilder)
        {
            gridBuilder.IsStatic = false;
            // To prevent exploits
            gridBuilder.DestructibleBlocks = false;
            foreach (var block in gridBuilder.CubeBlocks)
            {
                var functionalBlock = block as MyObjectBuilder_FunctionalBlock;
                if (functionalBlock != null)
                {
                    functionalBlock.Enabled = false;
                }
            }
        }

        protected override void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;

            // Current position of the placed entity is either simple translation or
            // it can be calculated by raycast, if we want to snap to surfaces
            m_pastePosition = m_projector.WorldMatrix.Translation;
        }

        protected override bool TestPlacement()
        {
            //Not needed for projector and causes performance problems
            return true;
        }

        //Called on demand, not every frame
        public bool ActuallyTestPlacement()
        {
            m_projectionCanBePlaced = base.TestPlacement();
            return m_projectionCanBePlaced;
        }

        protected override MyEntity GetClipboardBuilder()
        {
            return null;
        }

        public void ResetGridOrientation()
        {
            m_pasteDirForward = Vector3.Forward;
            m_pasteDirUp = Vector3.Up;
            m_pasteOrientationAngle = 0f;
        }

        protected override void UpdateGridTransformations()
        {
            MatrixD originalOrientation = Matrix.Multiply(base.GetFirstGridOrientationMatrix(), m_projector.WorldMatrix);
            var invRotation = Matrix.Invert(CopiedGrids[0].PositionAndOrientation.Value.GetMatrix()).GetOrientation();
            MatrixD orientationDelta = invRotation * originalOrientation; // matrix from original orientation to new orientation

            for (int i = 0; i < PreviewGrids.Count; i++)
            {
                MatrixD worldMatrix2 = CopiedGrids[i].PositionAndOrientation.Value.GetMatrix(); //get original rotation and position
                var offset = worldMatrix2.Translation - CopiedGrids[0].PositionAndOrientation.Value.Position;//calculate offset to first pasted grid
                m_copiedGridOffsets[i] = Vector3D.TransformNormal(offset, orientationDelta); // Transform the offset to new orientation
                if (!AnyCopiedGridIsStatic)
                    worldMatrix2 = worldMatrix2 * orientationDelta; //correct rotation
                Vector3D translation = m_pastePosition + m_copiedGridOffsets[i]; //correct position

                worldMatrix2.Translation = Vector3.Zero;
                worldMatrix2 = Matrix.Orthogonalize(worldMatrix2);
                worldMatrix2.Translation = translation + Vector3D.Transform(m_projector.GetProjectionTranslationOffset(), m_projector.WorldMatrix.GetOrientation());

                PreviewGrids[i].PositionComp.SetWorldMatrix(worldMatrix2);// Set the corrected position
            }
        }

        public float GridSize
        {
            get
            {
                if (CopiedGrids != null && CopiedGrids.Count > 0)
                {
                    return MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
                }

                return 0f;
            }
        }
    }
}
