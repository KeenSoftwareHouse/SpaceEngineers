using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_FloraElementDefinition))]
    public class MyFloraElementDefinition : MyDefinitionBase
    {
        public struct GrowthStep
        {
            public int GroupInsId;
            public float Percent;

            public GrowthStep(int groupIndsId, float percent)
            {
                GroupInsId = groupIndsId;
                Percent = percent;
            }
        }

        public Dictionary<string, MyGroupedIds> AppliedGroups;
        public List<GrowthStep> GrowthSteps;
        public MyDefinitionId GatheredItemDefinition;
        public float GatheredAmount;
        public bool IsGatherable;
        public bool Regrowable;
        public float GrowTime; // h
        public int PostGatherStep;
        public int GatherableStep;
        public float SpawnProbability;
        public MyAreaTransformType AreaTransformType;
        public float DecayTime;


        public byte TransformTypeByte { get { return (byte)AreaTransformType; } }
        public bool HasGrowthSteps { get { return GrowthSteps.Count > 0; } }
        public int StartingId { get { return HasGrowthSteps ? GrowthSteps[GrowthSteps.Count - 1].GroupInsId : 0; } }
        public bool ShouldDecay { get { return DecayTime != 0; } }

        private static List<string> m_tmpGroupHelper = new List<string>();

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_FloraElementDefinition;

            AppliedGroups = new Dictionary<string, MyGroupedIds>();
            if (ob.AppliedGroups != null)
            {
                var groups = MyDefinitionManager.Static.GetGroupedIds("EnvGroups");
                MyGroupedIds groupedIds = null;
                foreach (var group in ob.AppliedGroups)
                {
                    if (groups.TryGetValue(group, out groupedIds))
                        AppliedGroups.Add(group, groupedIds);
                }
            }

            GrowthSteps = new List<GrowthStep>();
            if (ob.GrowthSteps != null)
            {
                foreach (var element in ob.GrowthSteps)
                    GrowthSteps.Add(new GrowthStep(element.GroupInsId, element.Percent));
            }

            if (ob.GatheredItem != null)
            {
                GatheredItemDefinition = ob.GatheredItem.Id;
                GatheredAmount = ob.GatheredItem.Amount;
                IsGatherable = true;
            }
            else
            {
                GatheredItemDefinition = new MyDefinitionId();
                GatheredAmount = -1;
                IsGatherable = false;
            }

            Regrowable = ob.Regrowable;
            GrowTime = ob.GrowTime;
            GatherableStep = ob.GatherableStep;
            PostGatherStep = ob.PostGatherStep;
            SpawnProbability = ob.SpawnProbability;
            AreaTransformType = ob.AreaTransformType;
            DecayTime = ob.DecayTime;
        }

        public MyStringHash GetRandomItem()
        {
            var chosenGroup = AppliedGroups.Keys.ToList()[MyRandom.Instance.Next() % AppliedGroups.Count];
            return AppliedGroups[chosenGroup].Entries.Last().SubtypeId;
        }

        public bool BelongsToGroups(MyStringHash subtypeId)
        {
            foreach (var group in AppliedGroups.Values)
            {
                foreach (var value in group.Entries)
                {
                    if (value.SubtypeId == subtypeId)
                        return true;
                }
            }

            return false;
        }

        public bool IsFirst(MyStringHash subtypeId)
        {
            foreach (var group in AppliedGroups)
            {
                if (group.Value.Entries[0].SubtypeId == subtypeId)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsFirst(string groupName, MyStringHash subtypeId)
        {
            return AppliedGroups[groupName].Entries[0].SubtypeId == subtypeId;
        }

        public int GetGroupIndex(string groupName, MyStringHash subtypeId)
        {
            var entries = AppliedGroups[groupName].Entries;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].SubtypeId == subtypeId)
                    return i;
            return 0;        
        }

        public bool IsGatherableSubtype(string group, MyStringHash subtypeId)
        {
            if (!IsGatherable)
                return false;

            if (GatherableStep != -1)
            {
                var step = GrowthSteps[GatherableStep];
                var groupInsIndex = GetGroupIndex(group, subtypeId);
                return step.GroupInsId == groupInsIndex;
            }

            return IsFirst(group, subtypeId);
        }

        public string GetRandomGroup(MyStringHash subtypeId)
        {
            m_tmpGroupHelper.Clear();
            foreach (var group in AppliedGroups)
            {
                foreach (var entry in group.Value.Entries)
                {
                    if (entry.SubtypeId == subtypeId)
                    {
                        m_tmpGroupHelper.Add(group.Key);
                    }
                }
            }

            if (m_tmpGroupHelper.Count == 0)
                return null;
            var randomIdx = MyRandom.Instance.Next() % m_tmpGroupHelper.Count;
            return m_tmpGroupHelper[randomIdx];
        }

        public MyStringHash GetFinalSubtype(string group)
        {
            return AppliedGroups[group].Entries[0].SubtypeId;
        }

        public MyStringHash GetSubtypeForGrowthStep(string group, int growthStep)
        {
            var step = GrowthSteps[growthStep];
            var chosenGroup = AppliedGroups[group];
            return chosenGroup.Entries[step.GroupInsId].SubtypeId;
        }

        public int GetGrowthStepForSubtype(string group, MyStringHash subtype)
        {
            var groupIndex = GetGroupIndex(group, subtype);
            for (int i = GrowthSteps.Count - 1; i >= 0; --i)
            {
                if (GrowthSteps[i].GroupInsId == groupIndex)
                    return i;
            }

            return -1;
        }
    }
}
