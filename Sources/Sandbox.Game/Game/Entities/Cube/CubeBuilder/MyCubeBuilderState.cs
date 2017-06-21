using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube.CubeBuilder
{

    /// <summary>
    /// Class that handles cube builder state.
    /// </summary>
    public class MyCubeBuilderState
    {
        #region Data members

        /// <summary>
        /// Store last rotation for each block definition.
        /// </summary>
        public Dictionary<MyDefinitionId, Quaternion> RotationsByDefinitionHash = new Dictionary<MyDefinitionId, Quaternion>(MyDefinitionId.Comparer);

        /// <summary>
        /// No idea what this one is.
        /// </summary>
        public Dictionary<MyDefinitionId, int> StageIndexByDefinitionHash = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);

        /// <summary>
        /// Block definition stages.
        /// </summary>
        public List<MyCubeBlockDefinition> CurrentBlockDefinitionStages = new List<MyCubeBlockDefinition>();
        /// <summary>
        /// Block definitions with variants.
        /// </summary>
        private MyCubeBlockDefinitionWithVariants m_definitionWithVariants;

        /// <summary>
        /// Indicates what build mode is on (small or big grid)
        /// </summary>
        private MyCubeSize m_cubeSizeMode = MyCubeSize.Large;

        #endregion

        #region Properties

        public MyCubeBlockDefinition CurrentBlockDefinition
        {
            get
            {
                return m_definitionWithVariants;
            }
            set
            {
                if (value == null)
                {
                    m_definitionWithVariants = null;
                    CurrentBlockDefinitionStages.Clear();
                }
                else
                {
                    m_definitionWithVariants = new MyCubeBlockDefinitionWithVariants(value, -1);

                    if (MyFakes.ENABLE_BLOCK_STAGES)
                    {
                        if (!CurrentBlockDefinitionStages.Contains(value))
                        {
                            CurrentBlockDefinitionStages.Clear();

                            if (value.BlockStages != null)
                            {
                                // First add this stage (main block definition from GUI)
                                CurrentBlockDefinitionStages.Add(value);

                                foreach (var stage in value.BlockStages)
                                {
                                    MyCubeBlockDefinition stageDef;
                                    MyDefinitionManager.Static.TryGetCubeBlockDefinition(stage, out stageDef);
                                    if (stageDef != null)
                                        CurrentBlockDefinitionStages.Add(stageDef);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Block definition set on activation of cube builder.
        /// </summary>
        public MyCubeBlockDefinition StartBlockDefinition { get; private set; }

        /// <summary>
        /// Current cube size mode.
        /// </summary>
        public MyCubeSize CubeSizeMode { get { return m_cubeSizeMode; } }

        #endregion

        #region Operations

        public void UpdateCubeBlockDefinition(MyDefinitionId? id, MatrixD localMatrixAdd)
        {
            if (!id.HasValue) 
                return;

            if (CurrentBlockDefinition != null)
            {
                var cubeBlockDefGroup = MyDefinitionManager.Static.GetDefinitionGroup(CurrentBlockDefinition.BlockPairName);

                if (CurrentBlockDefinitionStages.Count > 1)
                {
                    cubeBlockDefGroup = MyDefinitionManager.Static.GetDefinitionGroup(CurrentBlockDefinitionStages[0].BlockPairName);
                    if (cubeBlockDefGroup.Small != null)
                        StageIndexByDefinitionHash[cubeBlockDefGroup.Small.Id] = CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);

                    if (cubeBlockDefGroup.Large != null)
                        StageIndexByDefinitionHash[cubeBlockDefGroup.Large.Id] = CurrentBlockDefinitionStages.IndexOf(CurrentBlockDefinition);
                }

                var rotation = Quaternion.CreateFromRotationMatrix(localMatrixAdd);
                if (cubeBlockDefGroup.Small != null) RotationsByDefinitionHash[cubeBlockDefGroup.Small.Id] = rotation;
                if (cubeBlockDefGroup.Large != null) RotationsByDefinitionHash[cubeBlockDefGroup.Large.Id] = rotation;

            }
            var tmpDef = MyDefinitionManager.Static.GetCubeBlockDefinition(id.Value);
            if (tmpDef.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                CurrentBlockDefinition = tmpDef;
            else
            {
                CurrentBlockDefinition = tmpDef.CubeSize == MyCubeSize.Large ?
                    MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Small :
                    MyDefinitionManager.Static.GetDefinitionGroup(tmpDef.BlockPairName).Large;
            }

            StartBlockDefinition = CurrentBlockDefinition;
        }

        public void UpdateBlockDefinitionStages(MyDefinitionId? id)
        {
            if (!id.HasValue || CurrentBlockDefinition == null)
                return;

            MyDefinitionId defBlockId = id.Value;
            if (CurrentBlockDefinitionStages.Count > 1)
                defBlockId = CurrentBlockDefinitionStages[0].Id;

            if (CurrentBlockDefinitionStages.Count <= 1)
                return;

            int lastStage;
            if (StageIndexByDefinitionHash.TryGetValue(defBlockId, out lastStage))
            {
                if (lastStage >= 0 && lastStage < CurrentBlockDefinitionStages.Count)
                    CurrentBlockDefinition = CurrentBlockDefinitionStages[lastStage];
            }
        }

        /// <summary>
        /// Chooses same cube but for different grid size
        /// </summary>
        public void ChooseComplementBlock()
        {
            var oldBlock = m_definitionWithVariants;

            if (oldBlock != null)
            {
                var group = MyDefinitionManager.Static.GetDefinitionGroup(oldBlock.Base.BlockPairName);
                if (oldBlock.Base.CubeSize == MyCubeSize.Small)
                {
                    if (group.Large != null && (group.Large.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                    {
                        CurrentBlockDefinition = group.Large;
                    }
                }
                else if (oldBlock.Base.CubeSize == MyCubeSize.Large)
                {
                    if (group.Small != null && (group.Small.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                    {
                        CurrentBlockDefinition = group.Small;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there is complementary block available
        /// </summary>
        public bool HasComplementBlock()
        {
            if (m_definitionWithVariants != null)
            {
                var group = MyDefinitionManager.Static.GetDefinitionGroup(m_definitionWithVariants.Base.BlockPairName);
                if (m_definitionWithVariants.Base.CubeSize == MyCubeSize.Small)
                {
                    if (group.Large != null && (group.Large.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                        return true;
                }
                else if (m_definitionWithVariants.Base.CubeSize == MyCubeSize.Large)
                {
                    if (group.Small != null && (group.Small.Public || MyFakes.ENABLE_NON_PUBLIC_BLOCKS))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets cube size mode.
        /// </summary>
        /// <param name="newCubeSize">New cube size mode.</param>
        public void SetCubeSize(MyCubeSize newCubeSize)
        {
            m_cubeSizeMode = newCubeSize;
            UpdateComplementBlock();
        }

        /// <summary>
        /// Updates Current block definition with current cube size mode.
        /// </summary>
        internal void UpdateComplementBlock()
        {
            //GK: UpdateComplementBlock is called twice upon CubeBuilder Activation resulting in invalid StartBlockDefintion / CurrentBlockDefinition pairs. Do this hotfix for now to ignore first call
            if (CurrentBlockDefinition == null || StartBlockDefinition == null)
                return;

            var blockDefGroup = MyDefinitionManager.Static.GetDefinitionGroup(StartBlockDefinition.BlockPairName);

            CurrentBlockDefinition = m_cubeSizeMode == MyCubeSize.Large ? blockDefGroup.Large : blockDefGroup.Small;
        }

        #endregion

    }
}
