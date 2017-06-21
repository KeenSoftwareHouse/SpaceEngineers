using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage;
using VRage.Algorithms;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GrowableEnvironmentItemDefinition))]
    public class MyGrowableEnvironmentItemDefinition : MyDefinitionBase
    {
        public class ItemGrowthStep
        {
            public string Name;
            public int NextStep;

            /// <summary>
            /// Time before transitioning to next growth state in seconds.
            /// </summary>
            public int TransitionTime;

            /// <summary>
            /// Supported actions.
            /// </summary>
            public Dictionary<string, EnvironmentItemAction> Actions;

            /// <summary>
            /// SubtypeId for the relevant model collection.
            /// </summary>
            public MyStringHash ModelCollectionSubtypeId;

            // Subtree information

            // Subtree this node belongs to
            public GrowthSubtree Subtree;

            // Key is compound, high 16 bits are: lineage index, the later is the step index inside the lineage
            // Lineage 0 is always the cycle.
            // -1 Marks unassigned, we use this to skip the nodes in the cycle for the lineages
            public int SubtreeId = -1;
        }

        public struct EnvironmentItemAction
        {
            public int NextStep;
            public MyDefinitionId? Id;
            public int Min;
            public int Max;
        }

        public Dictionary<string, int> StepNameIndex;
        public ItemGrowthStep[] GrowthSteps;
        public MyDiscreteSampler<int> StartingSteps;
        
        // Just for debug purposes
        private List<GrowthSubtree> m_subtrees = new List<GrowthSubtree>();

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_GrowableEnvironmentItemDefinition;

            StepNameIndex = new Dictionary<string, int>();
            List<float> stepProbabilities = new List<float>();

            for (int i = 0; i < ob.GrowthSteps.Length; i++)
            {
                var step = ob.GrowthSteps[i];
                Debug.Assert(step.Name != null && step.Name != "", String.Format("Growth step {0} in {1} is null!", i, ob.Id.SubtypeId));
                StepNameIndex.Add(step.Name, i);
                stepProbabilities.Add(step.StartingProbability);
            }

            GrowthSteps = new ItemGrowthStep[ob.GrowthSteps.Length];

            for (int i = 0; i < ob.GrowthSteps.Length; i++)
            {
                var step = ob.GrowthSteps[i];
                int nextStep = -1;
                if (step.NextStep != null)
                    StepNameIndex.TryGetValue(step.NextStep, out nextStep);

                Debug.Assert(step.TimeToNextStepInHours >= 0, "Time to next step for step {0} in {1} is negative!", step.Name, ob.Id.SubtypeId);
                var growthStep = new ItemGrowthStep()
                {
                    Name = step.Name,
                    NextStep = nextStep == i ? -1 : nextStep, // Self cycles are useless, protect the code from those.
                    TransitionTime = (int)(step.TimeToNextStepInHours * 3600),
                    ModelCollectionSubtypeId = MyStringHash.GetOrCompute(step.ModelCollectionSubtypeId),
                    Actions = new Dictionary<string, EnvironmentItemAction>()
                };

                if (step.Actions != null && step.Actions.Length > 0)
                {
                    for (int j = 0; j < step.Actions.Length; j++)
                    {
                        var actionDef = step.Actions[j];
                        var action = new EnvironmentItemAction();
                        if (!actionDef.Id.IsNull())
                        {
                            if (actionDef.NextStep != null && actionDef.NextStep != "" && StepNameIndex.ContainsKey(actionDef.NextStep))
                                action.NextStep = StepNameIndex[actionDef.NextStep];
                            else
                                action.NextStep = 0;
                            action.Id = actionDef.Id;
                            action.Min = actionDef.Min;
                            action.Max = actionDef.Max;
                        }

                        growthStep.Actions.Add(actionDef.Name, action);
                    }
                }

                GrowthSteps[i] = growthStep;
            }

            CalculateSubtrees();

            //DebugPrint();

            StartingSteps = new MyDiscreteSampler<int>(StepNameIndex.Values, stepProbabilities);
        }

        private void DebugPrint()
        {
            for (int index = 0; index < m_subtrees.Count; index++)
            {
                m_subtrees[index].DebugPrint(string.Format("{0}: Subtree {1}:", Id, index));
            }
        }

        #region Calculate subtrees and lineages

        // Temporary array used to reverse-index members of a connected component of the growth graph.
        private int[] m_componentIndex;

        private void CalculateSubtrees()
        {
            MyUnionFind uf = new MyUnionFind(GrowthSteps.Length);

            for (int i = 0; i < GrowthSteps.Length; ++i)
            {
                var next = GrowthSteps[i].NextStep;
                if(next != -1)
                    uf.Union(i, next);
            }

            /**
             * To calculate lineages:
             *  - Find the zero steps
             *  - Calculate a subtree for each.
             *  - There is at most one cycle per connected component
             *  - We follow any node and determine the cycle.
             *  - Wach sequential lineage ends where it meets the cycle, we store that index
             *  
             */

            // Lists of growth subtrees
            Dictionary<int, MyTuple<List<int>, GrowthSubtree>> growthLists = new Dictionary<int, MyTuple<List<int>, GrowthSubtree>>();

            for (int i = 0; i < GrowthSteps.Length; ++i)
            {
                var representant = uf.Find(i);

                MyTuple<List<int>, GrowthSubtree> subtree;
                if (!growthLists.TryGetValue(representant, out subtree))
                {
                    subtree = new MyTuple<List<int>, GrowthSubtree>(new List<int>(), new GrowthSubtree());
                    growthLists.Add(representant, subtree);
;
                    m_subtrees.Add(subtree.Item2);

                    GrowthSteps[i].Subtree = subtree.Item2;
                }

                subtree.Item1.Add(i);
                GrowthSteps[i].Subtree = subtree.Item2;
            }

            m_componentIndex = new int[GrowthSteps.Length];

            foreach (var subtree in growthLists.Values)
            {
                subtree.Item2.Init(this, subtree.Item1);
            }
        }

        /**
         * We know that (with respect to time) each connected component of the subgraph is a quas-tree with at most one cycle.
         * 
         * We calculate how many references each node has, the nodes with zero refernces are leaves
         * We calculate if a cycle exists and what nodes are in it.
         */
        public class GrowthSubtree
        {
            // Step in the graph.
            public struct Step : IComparable<Step>
            {
                public int Index;
                public long CumulativeTime;

                public Step(int index, long cumulativeTime)
                {
                    CumulativeTime = cumulativeTime;
                    Index = index;
                }

                public int CompareTo(Step other)
                {
                    return (int)(CumulativeTime - other.CumulativeTime);
                }
            }

            // Cumulative time to end of the lineage.
            public Step[][] Lineages;

            // Total length of the lineage.
            public long CycleTime;

            // Weather this subtree has a cycle.
            public bool Cycles;

            // Quick reference to the definition, so we can get our steps.
            private MyGrowableEnvironmentItemDefinition m_parent;

            private ItemGrowthStep[] m_steps;

            // What do you think?
            public void DebugPrint(string name)
            {
                StringBuilder sb = new StringBuilder();
                Debug.Print(name);
                for (int i = 0; i < Lineages.Length; i++)
                {
                    sb.Clear();

                    sb.AppendFormat("  Lineage {0}:", i);
                    for (int j = 0; j < Lineages[i].Length; j++)
                    {
                        var step = m_parent.GrowthSteps[Lineages[i][j].Index];
                        var id = step.SubtreeId;

                        sb.AppendFormat(" -> {0}({2}:{3}):{1}", step.Name, Lineages[i][j].CumulativeTime, id>> 16, id & 0xFFFF);
                    }
                    Debug.Print(sb.ToString());
                }
            }

            /**
             * You'd be surprised how hard it actually is to set up some accelerated lookup for these graphs of evil.
             */
            public unsafe void Init(MyGrowableEnvironmentItemDefinition parent, List<int> elements)
            {
                const int LINEAGE_MASK = 0x7FFF0000;

                // shortcuts
                m_parent = parent;
                var index = m_parent.m_componentIndex;
                m_steps = m_parent.GrowthSteps;

                // Lineage, a lineage is a path to the root or the first member of a cycle in the growth graph.
                List<Step> lineage = new List<Step>(elements.Count);

                // List of lineages
                List<Step[]> lineages = new List<Step[]>();

                // Indexing pass
                // We need to be able to find the last index of steps based on their next step index
                for (int i = 0; i < elements.Count; i++)
                {
                    index[elements[i]] = i;
                }

                // --------------
                // Step 1: find the cycle:
                HashSet<int> visited = new HashSet<int>();

                // Iterate over the elements and mark them untill we go around the cycle or reach the root.
                var current = elements[0];
                visited.Add(current);
                while ((current = m_steps[current].NextStep) != -1)
                {
                    if (visited.Contains(current)) break;
                    visited.Add(current);
                }

                if (current != -1)
                {
                    // start recording the cycle.
                    lineage.Add(new Step(current, m_steps[current].TransitionTime));
                    m_steps[current].SubtreeId = 0;

                    // Follow it arround a second time, this time we will only have the elemnts that are a part of it.
                    int next = current;
                    for (int i = 1; (next = m_steps[next].NextStep) != current; ++i)
                    {
                        lineage.Add(new Step(next, m_steps[next].TransitionTime));
                        m_steps[next].SubtreeId = i;
                    }

                    Cycles = true;

                    lineages.Add(lineage.ToArray());
                    lineage.Clear();
                }

                // --------------
                // Step 2: Determine the start and record each other lineage:

                int[] references = new int[elements.Count];

                // Reference counting pass
                for (int i = 0; i < elements.Count; i++)
                {
                    var next = m_steps[elements[i]].NextStep;
                    if (next != -1)
                        references[index[next]]++;
                }

                // Lineage calculation pass, each lineage root is prepared here
                for (int i = 0; i < elements.Count; i++)
                {
                    if (references[i] == 0) // these are the roots
                    {
                        current = elements[i];
                        lineage.Add(new Step(current, m_steps[current].TransitionTime));
                        m_steps[current].SubtreeId = lineages.Count << 16;

                        // we go untill we hit a node that is in the cycle or the root of the tree
                        int next = current;
                        for (int j = 1; ; ++j)
                        {
                            next = m_steps[next].NextStep;

                            // We check if we reched the end or a node in the cycle.
                            if (next == -1
                                || (Cycles && (m_steps[next].SubtreeId & LINEAGE_MASK) == 0))
                                break;

                            lineage.Add(new Step(next, m_steps[next].TransitionTime));
                            m_steps[next].SubtreeId = (lineages.Count << 16) | j;
                        }

                        // Add to the list and move on
                        lineages.Add(lineage.ToArray());
                        lineage.Clear();
                    }
                }

                // Steal the array
                lineages.Capacity = lineages.Count;
                Lineages = lineages.GetInternalArray();

                // --------------
                // Step 3: Calculate the cumulative times:
                for (int i = 0; i < Lineages.Length; i++)
                {
                    fixed (Step* cumulativeSteps = Lineages[i])
                        for (int j = 1; j < Lineages[i].Length; j++)
                        {
                            cumulativeSteps[j].CumulativeTime += cumulativeSteps[j - 1].CumulativeTime;
                        }
                }

                if (Cycles)
                    CycleTime = Lineages[0].Last().CumulativeTime;
            }

            public int CalculateStep(int globalStepIndex, long enlapsedTime)
            {
                UpdateStep(ref globalStepIndex, ref enlapsedTime);
                return globalStepIndex;
            }

            public void UpdateStep(ref int globalStepIndex, ref long enlapsedTime)
            {
                Step key;
                int stepIndex;

                var originalTime = enlapsedTime;

                int subtreeId = m_parent.GrowthSteps[globalStepIndex].SubtreeId;

                int lineageIndex = (subtreeId >> 16) & 0xFFFF;
                var lineage = Lineages[lineageIndex];

                int step = subtreeId & 0xFFFF;

                // do it as if from the start
                if (step != 0)
                    enlapsedTime += lineage[step - 1].CumulativeTime;

                // Calculate position in the peripheric lineage
                if (lineageIndex != 0 && Cycles)
                {
                    key = new Step(-1, enlapsedTime);
                    stepIndex = lineage.BinaryIntervalSearch(key);

                    if (stepIndex < lineage.Length)
                    {
                        // If we do not leave this lineage we are done.
                        globalStepIndex = lineage[stepIndex].Index;
                        goto calcNextTime;
                    }
                    else
                    {
                        var last = lineage[lineage.Length - 1];
                        // Remove any calculated time and move on to the cycle.
                        enlapsedTime -= last.CumulativeTime;

                        // As if we always were in that lineage.
                        subtreeId = m_steps[m_steps[last.Index].NextStep].SubtreeId;

                        lineage = Lineages[0];

                        step = subtreeId & 0xFFFF;
                        if (step != 0)
                            enlapsedTime += lineage[step - 1].CumulativeTime;
                    }
                }

                // Calculate position in the cycle.
                if (Cycles)
                    enlapsedTime %= CycleTime;

                key = new Step(-1, enlapsedTime);
                stepIndex = Math.Min(lineage.BinaryIntervalSearch(key), lineage.Length - 1);
                globalStepIndex = lineage[stepIndex].Index;

            calcNextTime:
                if (m_steps[globalStepIndex].NextStep != -1)
                {
                    var diff = stepIndex > 0 ? enlapsedTime - lineage[stepIndex - 1].CumulativeTime : enlapsedTime;
                    enlapsedTime = originalTime - diff + m_steps[globalStepIndex].TransitionTime;
                }
                else enlapsedTime = -1;
            }
        }

        #endregion

        /// <summary>
        /// Get the current state of an item based on it's last known state and the ammount of time since.
        /// </summary>
        /// <param name="last">Last known state of the item.</param>
        /// <param name="enlapsedTime">Ammount of time enlapsed since the last known state transition (in seconds).</param>
        /// <returns></returns>
        public void UpdateState(ref int last, ref long enlapsedTime)
        {
            var step = GrowthSteps[last];
            step.Subtree.UpdateStep(ref last, ref enlapsedTime);
        }
    }
}
