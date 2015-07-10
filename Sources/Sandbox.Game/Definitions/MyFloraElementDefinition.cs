using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_FloraElementDefinition))]
    public class MyFloraElementDefinition : MyDefinitionBase
    {
        public struct GrowthStep
        {
            public int SubModelId;
            public float Percent;

            public GrowthStep(int subModelId, float percent)
            {
                SubModelId = subModelId;
                Percent = percent;
            }
        }

        public Dictionary<MyStringHash, List<MyStringHash>> AppliedItems;
        public List<GrowthStep> GrowthSteps;
        public MyDefinitionId GatheredItemDefinition;
        public float GatheredAmount;
        public bool Regrowable;
        public float GrowTime; // h
        public int PostGatherStep;
        public int GatherableStep;
        public float SpawnProbability;
        public MyAreaTransformType AreaTransformType;

        public bool HasGrowthSteps { get { return GrowthSteps.Count > 0; } }
        public int StartingModel { get { return HasGrowthSteps ? GrowthSteps[GrowthSteps.Count - 1].SubModelId : 0; } }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_FloraElementDefinition;

            AppliedItems = new Dictionary<MyStringHash, List<MyStringHash>>(MyStringHash.Comparer);
            foreach (var element in ob.EnvironmentItems)
            {
                var groupId = MyStringHash.GetOrCompute(element.Group);
                if (!AppliedItems.ContainsKey(groupId))
                    AppliedItems[groupId] = new List<MyStringHash>();
                AppliedItems[groupId].Add(MyStringHash.GetOrCompute(element.Subtype));
            }

            GrowthSteps = new List<GrowthStep>();
            if (ob.GrowthSteps != null)
            {
                foreach (var element in ob.GrowthSteps)
                    GrowthSteps.Add(new GrowthStep(element.SubModelId, element.Percent));
            }

            if (ob.GatheredItem != null)
            {
                GatheredItemDefinition = ob.GatheredItem.Id;
                GatheredAmount = ob.GatheredItem.Amount;
            }
            else
            {
                GatheredItemDefinition = new MyDefinitionId();
                GatheredAmount = -1;
            }

            Regrowable = ob.Regrowable;
            GrowTime = ob.GrowTime;
            GatherableStep = ob.GatherableStep;
            PostGatherStep = ob.PostGatherStep;
            SpawnProbability = ob.SpawnProbability;
            AreaTransformType = ob.AreaTransformType;
        }

        public MyStringHash GetRandomItem(MyStringHash groupSubtype)
        {
            var idx = MyRandom.Instance.Next() % AppliedItems[groupSubtype].Count;
            return AppliedItems[groupSubtype][idx];
        }
    }
}
