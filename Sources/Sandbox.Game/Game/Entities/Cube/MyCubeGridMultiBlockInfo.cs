using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Helper data for multiblock in a grid.
    /// </summary>
    public class MyCubeGridMultiBlockInfo
    {
        private static List<MyMultiBlockDefinition.MyMultiBlockPartDefinition> m_tmpPartDefinitions = new List<MyMultiBlockDefinition.MyMultiBlockPartDefinition>();

        public int MultiBlockId;
        public MyMultiBlockDefinition MultiBlockDefinition;
        // Block definition which defines whole multiblock.
        public MyCubeBlockDefinition MainBlockDefinition;
        public HashSet<MySlimBlock> Blocks = new HashSet<MySlimBlock>();

        public bool GetTransform(out MatrixI transform)
        {
            transform = default(MatrixI);

            if (Blocks.Count != 0)
            {
                var refBlock = Blocks.First();
                Debug.Assert(refBlock.MultiBlockIndex < MultiBlockDefinition.BlockDefinitions.Length);
                if (refBlock.MultiBlockIndex < MultiBlockDefinition.BlockDefinitions.Length)
                {
                    var refBlockDefInfo = MultiBlockDefinition.BlockDefinitions[refBlock.MultiBlockIndex];
                    transform = MatrixI.CreateRotation(refBlockDefInfo.Forward, refBlockDefInfo.Up, refBlock.Orientation.Forward, refBlock.Orientation.Up);
                    transform.Translation = refBlock.Position - Vector3I.TransformNormal(refBlockDefInfo.Min, ref transform);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns multiblok min and max grid coordinates.
        /// </summary>
        public bool GetBoundingBox(out Vector3I min, out Vector3I max)
        {
            min = default(Vector3I);
            max = default(Vector3I);

            MatrixI transform;
            if (!GetTransform(out transform))
                return false;

            Vector3I minTmp = Vector3I.Transform(MultiBlockDefinition.Min, transform);
            Vector3I maxTmp = Vector3I.Transform(MultiBlockDefinition.Max, transform);
            min = Vector3I.Min(minTmp, maxTmp);
            max = Vector3I.Max(minTmp, maxTmp);
            return true;
        }

        public bool GetMissingBlocks(out MatrixI transform, List<int> multiBlockIndices)
        {
            // Fill missing indices.
            Debug.Assert(multiBlockIndices.Count == 0);
            for (int i = 0; i < MultiBlockDefinition.BlockDefinitions.Length; ++i)
            {
                if (!Blocks.Any(b => b.MultiBlockIndex == i))
                    multiBlockIndices.Add(i);
            }

            // ...and return transform
            return GetTransform(out transform);
        }

        /// <summary>
        /// Check if other block can be added to area of multiblock.
        /// </summary>
        public bool CanAddBlock(ref Vector3I otherGridPositionMin, ref Vector3I otherGridPositionMax, MyBlockOrientation otherOrientation, MyCubeBlockDefinition otherDefinition)
        {
            MatrixI transform;
            if (!GetTransform(out transform))
                return true;

            try 
            {
                // Calculate other block position in multiblock space.
                MatrixI invTransform;
                MatrixI.Invert(ref transform, out invTransform);

                Vector3I otherPositionInMultiBlockMinTmp = Vector3I.Transform(otherGridPositionMin, ref invTransform);
                Vector3I otherPositionInMultiBlockMaxTmp = Vector3I.Transform(otherGridPositionMax, ref invTransform);
                Vector3I otherPositionInMultiBlockMin = Vector3I.Min(otherPositionInMultiBlockMinTmp, otherPositionInMultiBlockMaxTmp);
                Vector3I otherPositionInMultiBlockMax = Vector3I.Max(otherPositionInMultiBlockMinTmp, otherPositionInMultiBlockMaxTmp);

                // Check intersection with AABB of whole multiblock
                if (!Vector3I.BoxIntersects(ref MultiBlockDefinition.Min, ref MultiBlockDefinition.Max, ref otherPositionInMultiBlockMin, ref otherPositionInMultiBlockMax))
                    return true;

                // Other block rotation in multiblock space.
                MatrixI otherRotation = new MatrixI(otherOrientation);
                MatrixI otherRotationInMultiBlock;
                MatrixI.Multiply(ref otherRotation, ref invTransform, out otherRotationInMultiBlock);
                MyBlockOrientation otherOrientationInMultiBlock = new MyBlockOrientation(otherRotationInMultiBlock.Forward, otherRotationInMultiBlock.Up);

                // Multiblock part (block) definitions in the same position.
                m_tmpPartDefinitions.Clear();
                foreach (var partDefinition in MultiBlockDefinition.BlockDefinitions)
                {
                    if (Vector3I.BoxIntersects(ref partDefinition.Min, ref partDefinition.Max, ref otherPositionInMultiBlockMin, ref otherPositionInMultiBlockMax))
                    {
                        if (otherPositionInMultiBlockMin == otherPositionInMultiBlockMax && partDefinition.Min == partDefinition.Max) // Size = 1
                            m_tmpPartDefinitions.Add(partDefinition);
                        else
                            return false;
                    }
                }

                if (m_tmpPartDefinitions.Count == 0)
                    return true;

                // Check if multiblock part blocks and other block can be added together
                bool canAdd = true;
                foreach (var partDefinition in m_tmpPartDefinitions) 
                {
                    MyCubeBlockDefinition blockDefinition;
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(partDefinition.Id, out blockDefinition) && blockDefinition != null) 
                    {
                        canAdd &= MyCompoundCubeBlock.CanAddBlocks(blockDefinition, new MyBlockOrientation(partDefinition.Forward, partDefinition.Up), 
                            otherDefinition, otherOrientationInMultiBlock);
                        if (!canAdd)
                            break;
                    }
                }

                return canAdd;
            }
            finally 
            {
                m_tmpPartDefinitions.Clear();
            }
        }

        public bool IsFractured()
        {
            foreach (var multiBlockPart in Blocks)
            {
                if (multiBlockPart.GetFractureComponent() != null)
                    return true;
            }

            return false;
        }

        public float GetTotalMaxIntegrity()
        {
            float integrity = 0;
            foreach (var multiBlockPart in Blocks)
                integrity += multiBlockPart.MaxIntegrity;

            return integrity;
        }
    }
}
