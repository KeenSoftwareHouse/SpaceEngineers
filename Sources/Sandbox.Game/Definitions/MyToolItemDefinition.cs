using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRage.Game.Gui;

namespace Sandbox.Definitions
{
    public struct MyVoxelMiningDefinition
    {
        public string MinedOre;
        // Mine counts needed to spawn floating objectMyToolHitCondition
        public int HitCount;
        public MyDefinitionId PhysicalItemId;
        public float RemovedRadius;
        public bool OnlyApplyMaterial;
    }

    public struct MyToolHitCondition
    {
		public string[] EntityType;
		public string Animation;
        public float AnimationTimeScale;
        public string StatsAction;
        public string StatsActionIfHit;
        public string StatsModifier;
        public string StatsModifierIfHit;
        public string Component;
    }

    public struct MyToolActionDefinition
    {
        public MyStringId Name;
        public float StartTime;
        public float EndTime;
        public float Efficiency;
        public string StatsEfficiency;

        public string SwingSound;
        public float SwingSoundStart;
        public float HitStart;
        public float HitDuration;
        public string HitSound;
        public float CustomShapeRadius;

        public MyHudTexturesEnum Crosshair;

        public MyToolHitCondition[] HitConditions;

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    [MyDefinitionType(typeof(MyObjectBuilder_ToolItemDefinition))]
    public class MyToolItemDefinition : MyPhysicalItemDefinition
    {
        public MyVoxelMiningDefinition[] VoxelMinings;

        public List<MyToolActionDefinition> PrimaryActions = new List<MyToolActionDefinition>();
        public List<MyToolActionDefinition> SecondaryActions = new List<MyToolActionDefinition>();

        public float HitDistance;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ToolItemDefinition;
            MyDebug.AssertDebug(ob != null);

            if (ob.VoxelMinings != null && ob.VoxelMinings.Length > 0)
            {
                VoxelMinings = new MyVoxelMiningDefinition[ob.VoxelMinings.Length];

                for (int i = 0; i < ob.VoxelMinings.Length; ++i)
                {
                    VoxelMinings[i].MinedOre = ob.VoxelMinings[i].MinedOre;
                    VoxelMinings[i].HitCount = ob.VoxelMinings[i].HitCount;
                    VoxelMinings[i].PhysicalItemId = ob.VoxelMinings[i].PhysicalItemId;
                    VoxelMinings[i].RemovedRadius = ob.VoxelMinings[i].RemovedRadius;
                    VoxelMinings[i].OnlyApplyMaterial = ob.VoxelMinings[i].OnlyApplyMaterial;
                }
            }

            CopyActions(ob.PrimaryActions, PrimaryActions);
            CopyActions(ob.SecondaryActions, SecondaryActions);

            HitDistance = ob.HitDistance;

        }

        void CopyActions(MyObjectBuilder_ToolItemDefinition.MyToolActionDefinition[] sourceActions, List<MyToolActionDefinition> targetList)
        {
            if (sourceActions != null && sourceActions.Length > 0)
            {
                for (int i = 0; i < sourceActions.Length; ++i)
                {
                    MyToolActionDefinition actionDef = new MyToolActionDefinition();

                    actionDef.Name = MyStringId.GetOrCompute(sourceActions[i].Name);
                    actionDef.StartTime = sourceActions[i].StartTime;
                    actionDef.EndTime = sourceActions[i].EndTime;
                    actionDef.Efficiency = sourceActions[i].Efficiency;
                    actionDef.StatsEfficiency = sourceActions[i].StatsEfficiency;

                    actionDef.SwingSound = sourceActions[i].SwingSound;
                    actionDef.SwingSoundStart = sourceActions[i].SwingSoundStart;
                    actionDef.HitStart = sourceActions[i].HitStart;
                    actionDef.HitDuration = sourceActions[i].HitDuration;
                    actionDef.HitSound = sourceActions[i].HitSound;
                    actionDef.CustomShapeRadius = sourceActions[i].CustomShapeRadius;

                    actionDef.Crosshair = sourceActions[i].Crosshair;

                    if (sourceActions[i].HitConditions != null)
                    {
                        actionDef.HitConditions = new MyToolHitCondition[sourceActions[i].HitConditions.Length];
                        for (int j = 0; j < actionDef.HitConditions.Length; j++)
                        {
                            actionDef.HitConditions[j].EntityType = sourceActions[i].HitConditions[j].EntityType;
                            actionDef.HitConditions[j].Animation = sourceActions[i].HitConditions[j].Animation;
                            actionDef.HitConditions[j].AnimationTimeScale = sourceActions[i].HitConditions[j].AnimationTimeScale;
                            actionDef.HitConditions[j].StatsAction = sourceActions[i].HitConditions[j].StatsAction;
                            actionDef.HitConditions[j].StatsActionIfHit = sourceActions[i].HitConditions[j].StatsActionIfHit;
                            actionDef.HitConditions[j].StatsModifier = sourceActions[i].HitConditions[j].StatsModifier;
                            actionDef.HitConditions[j].StatsModifierIfHit = sourceActions[i].HitConditions[j].StatsModifierIfHit;
                            actionDef.HitConditions[j].Component = sourceActions[i].HitConditions[j].Component;
                        }
                    }

                    targetList.Add(actionDef);
                }

               // targetList.Sort((x, y) => y.Efficiency.CompareTo(x.Efficiency));
            }
        }
    }
}
