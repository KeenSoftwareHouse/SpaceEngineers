using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems.CoordinateSystem;

namespace Sandbox.Game.Entities.Cube
{
    public class MyProjectorClipboard : MyGridClipboard
    {
        private MyProjectorBase m_projector;
        Vector3I m_oldProjectorRotation;
        Vector3I m_oldProjectorOffset;
        MatrixD m_oldProjectorMatrix;
        bool m_firstUpdateAfterNewBlueprint = false;

        public MyProjectorClipboard(MyProjectorBase projector, MyPlacementSettings settings)
            : base(settings) //Pasting Settings here ?
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
                block.Owner = 0;
                block.ShareMode = MyOwnershipShareModeEnum.None;
                block.EntityId = 0;
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
            MyCoordinateSystem.Static.Visible = false;
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
            MatrixD worldMatrix = m_projector.WorldMatrix;

            if (m_firstUpdateAfterNewBlueprint || m_oldProjectorRotation != m_projector.ProjectionRotation || m_oldProjectorOffset != m_projector.ProjectionOffset || !m_oldProjectorMatrix.EqualsFast(ref worldMatrix))
            {
                m_firstUpdateAfterNewBlueprint = false;
                m_oldProjectorRotation = m_projector.ProjectionRotation;
                m_oldProjectorMatrix = worldMatrix;
                m_oldProjectorOffset = m_projector.ProjectionOffset;

                // Update rotation based on projector settings
                Quaternion rotation = m_projector.ProjectionRotationQuaternion;
                Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
                worldMatrix = Matrix.Multiply(rotationMatrix, worldMatrix);

                // Update PreviewGrids
                for (int i = 0; i < PreviewGrids.Count; i++)
                {
                    // ensure the first block touches the projector base at (0,0,0) projector offset config
                    MySlimBlock firstBlock = PreviewGrids[i].CubeBlocks.First();
                    Vector3D firstBlockPos = MyCubeGrid.GridIntegerToWorld(PreviewGrids[i].GridSize, firstBlock.Position, worldMatrix);

                    Vector3D delta = firstBlockPos - m_projector.WorldMatrix.Translation;

                    // Re-adjust position
                    Vector3D projectionOffset = m_projector.GetProjectionTranslationOffset();
                    projectionOffset = Vector3D.Transform(projectionOffset, m_projector.WorldMatrix.GetOrientation());
                    worldMatrix.Translation -= delta + projectionOffset;

                    PreviewGrids[i].PositionComp.SetWorldMatrix(worldMatrix);
                }
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

        public override void Activate(Action callback = null)
        {
            ActivateNoAlign(callback);
            m_firstUpdateAfterNewBlueprint = true;
        }
    }
}
