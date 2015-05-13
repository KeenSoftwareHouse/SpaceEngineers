using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Utils
{
    /// <summary>
    /// This class allows for uniform generation of points from a set of bounding boxes.
    /// 
    /// You start by constructing a bounding box from where the points will be sampled.
    /// Then you can incrementally subtract bounding boxes and the resulting structure will allow you
    /// to generate uniformly distributed points using the Sample() function.
    /// </summary>
    public class MyBBSetSampler
    {
        private class IntervalSampler
        {
            private struct SamplingEntry
            {
                public double UpperLimit;
                public double CumulativeWeight;
                public bool Full; // Whether the entry is filled (i.e. should not be sampled) or not. Valid only for entries with no Sampler
                public IntervalSampler Sampler;

                public SamplingEntry(double limit, IntervalSampler sampler, double weight)
                {
                    UpperLimit = limit;
                    Sampler = sampler;
                    CumulativeWeight = weight;
                    Full = false;
                }

                public SamplingEntry(SamplingEntry other)
                {
                    UpperLimit = other.UpperLimit;
                    CumulativeWeight = other.CumulativeWeight;
                    Full = other.Full;
                    if (other.Sampler == null)
                    {
                        Sampler = null;
                    }
                    else
                    {
                        Sampler = new IntervalSampler(other.Sampler, 1.0, clone: true);
                    }
                }

                public static SamplingEntry Divide(ref SamplingEntry oldEntry, double prevUpperLimit, double prevCumulativeWeight, double weightMult, double newUpperLimit)
                {
                    SamplingEntry newEntry = new SamplingEntry();
                    newEntry.UpperLimit = newUpperLimit;

                    double newWidth = newUpperLimit - prevUpperLimit;
                    double oldWidth = oldEntry.UpperLimit - newUpperLimit;
                    double t = newWidth / (newWidth + oldWidth);

                    newEntry.Full = oldEntry.Full;

                    if (oldEntry.Sampler != null)
                    {
                        newEntry.Sampler = new IntervalSampler(oldEntry.Sampler, t, clone: false); // Will fix weights of the old sampler as well
                        newEntry.CumulativeWeight = prevCumulativeWeight + newEntry.Sampler.TotalWeight;
                        oldEntry.CumulativeWeight = newEntry.CumulativeWeight + oldEntry.Sampler.TotalWeight;
                    }
                    else
                    {
                        newEntry.Sampler = null;
                        if (oldEntry.Full)
                        {
                            newEntry.CumulativeWeight = oldEntry.CumulativeWeight = prevCumulativeWeight;
                        }
                        else
                        {
                            newEntry.CumulativeWeight = prevCumulativeWeight + weightMult * newWidth;
                            oldEntry.CumulativeWeight = newEntry.CumulativeWeight + weightMult * oldWidth;
                        }
                    }

                    return newEntry;
                }
            }

            private Base6Directions.Axis m_axis;
            private double m_min;
            private double m_max;
            private double m_weightMult;

            private List<SamplingEntry> m_entries;
            private double m_totalWeight;

            public double TotalWeight
            {
                get
                {
                    return m_totalWeight;
                }
            }

            public IntervalSampler(double min, double max, double weightMultiplier, Base6Directions.Axis axis)
            {
                m_min = min;
                m_max = max;
                m_axis = axis;
                m_weightMult = weightMultiplier;

                m_totalWeight = weightMultiplier * (m_max - m_min);

                m_entries = new List<SamplingEntry>();
                m_entries.Add(new SamplingEntry(m_max, null, m_totalWeight));
            }

            private IntervalSampler(IntervalSampler other, double t, bool clone)
            {
                m_min = other.m_min;
                m_max = other.m_max;
                m_axis = other.m_axis;
                m_weightMult = other.m_weightMult;
                m_totalWeight = other.m_totalWeight;

                m_entries = new List<SamplingEntry>(other.m_entries);
                for (int i = 0; i < other.m_entries.Count; ++i)
                {
                    m_entries[i] = new SamplingEntry(other.m_entries[i]);
                }

                Multiply(t);

                // If we are not cloning, we are splitting, so we have to multiply the remnant as well
                if (!clone)
                {
                    other.Multiply(1.0 - t);
                }
            }

            private void Multiply(double t)
            {
                m_weightMult *= t;
                m_totalWeight *= t;

                for (int i = 0; i < m_entries.Count; ++i)
                {
                    SamplingEntry entry = m_entries[i];
                    entry.CumulativeWeight *= t;
                    m_entries[i] = entry;
                    if (entry.Sampler != null)
                        entry.Sampler.Multiply(t);
                }
            }

            public void Subtract(ref BoundingBoxD originalBox, ref BoundingBoxD bb)
            {
                double min, max;
                SelectMinMax(ref bb, m_axis, out min, out max);

                bool minInserted = false;

                double prevLimit = m_min;
                double cumul = 0.0;
                for (int i = 0; i < m_entries.Count; ++i)
                {
                    SamplingEntry entry = m_entries[i];

                    if (!minInserted)
                    {
                        if (entry.UpperLimit >= min)
                        {
                            if (entry.UpperLimit == min)
                            {
                                minInserted = true;
                            }
                            else // (entry.UpperLimit > min)
                            {
                                if (prevLimit == min)
                                {
                                    minInserted = true;
                                    i--;
                                    continue;
                                }

                                minInserted = true;
                                SamplingEntry insertedEntry = SamplingEntry.Divide(ref entry, prevLimit, cumul, m_weightMult, min);
                                m_entries[i] = entry;
                                m_entries.Insert(i, insertedEntry);

                                entry = insertedEntry;
                            }
                        }
                    }
                    else
                    {
                        if (prevLimit < max)
                        {
                            if (entry.UpperLimit > max)
                            {
                                SamplingEntry insertedEntry = SamplingEntry.Divide(ref entry, prevLimit, cumul, m_weightMult, max);
                                m_entries[i] = entry;
                                m_entries.Insert(i, insertedEntry);
                                entry = insertedEntry;
                            }

                            if (entry.UpperLimit <= max)
                            {
                                if (entry.Sampler == null)
                                {
                                    if (m_axis == Base6Directions.Axis.ForwardBackward)
                                    {
                                        entry.Full = true;
                                        entry.CumulativeWeight = cumul;
                                    }
                                    else
                                    {
                                        if (entry.Full == false) // Full entries can be kept as they are
                                        {
                                            Base6Directions.Axis nextAxis = m_axis == Base6Directions.Axis.LeftRight ? Base6Directions.Axis.UpDown : Base6Directions.Axis.ForwardBackward;

                                            double min2, max2;
                                            SelectMinMax(ref originalBox, nextAxis, out min2, out max2);

                                            double range = m_max - m_min;
                                            double volume = m_weightMult * range;
                                            double relativeWidth = (entry.UpperLimit - prevLimit) / range;
                                            double newRange = max2 - min2;

                                            entry.Sampler = new IntervalSampler(min2, max2, (volume * relativeWidth) / newRange, nextAxis);
                                        }
                                    }
                                }
                                if (entry.Sampler != null)
                                {
                                    entry.Sampler.Subtract(ref originalBox, ref bb);
                                    entry.CumulativeWeight = cumul + entry.Sampler.TotalWeight;
                                }
                                m_entries[i] = entry;
                            }
                        }
                        else
                        {
                            if (entry.Sampler == null)
                            {
                                if (entry.Full)
                                {
                                    entry.CumulativeWeight = cumul;
                                }
                                else
                                {
                                    entry.CumulativeWeight = cumul + (entry.UpperLimit - prevLimit) * m_weightMult;
                                }
                            }
                            else
                            {
                                entry.CumulativeWeight = cumul + entry.Sampler.TotalWeight;
                            }
                            m_entries[i] = entry;
                        }
                    }

                    prevLimit = entry.UpperLimit;
                    cumul = entry.CumulativeWeight;
                }

                m_totalWeight = cumul;
            }

            private void SelectMinMax(ref BoundingBoxD bb, Base6Directions.Axis axis, out double min, out double max)
            {
                if (axis == Base6Directions.Axis.UpDown)
                {
                    min = bb.Min.Y;
                    max = bb.Max.Y;
                }
                else if (axis == Base6Directions.Axis.ForwardBackward)
                {
                    min = bb.Min.Z;
                    max = bb.Max.Z;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(axis == Base6Directions.Axis.LeftRight, "Invalid Axis value");
                    min = bb.Min.X;
                    max = bb.Max.X;
                }
                return;
            }

            public double Sample(out IntervalSampler childSampler)
            {
                // TODO: Implement divide & conquer for speedup
                double sample = MyUtils.GetRandomDouble(0.0, TotalWeight);
                double lastLimit = m_min;
                double lastWeight = 0.0;
                for (int i = 0; i < m_entries.Count; ++i)
                {
                    if (m_entries[i].CumulativeWeight >= sample)
                    {
                        childSampler = m_entries[i].Sampler;
                        double weightRange = m_entries[i].CumulativeWeight - lastWeight;
                        double t = (sample - lastWeight) / weightRange;
                        return t * m_entries[i].UpperLimit + (1.0 - t) * lastLimit;
                    }

                    lastLimit = m_entries[i].UpperLimit;
                    lastWeight = m_entries[i].CumulativeWeight;
                }

                System.Diagnostics.Debug.Assert(false, "Shouldn't get here!");
                childSampler = null;
                return m_max;
            }
        }

        private IntervalSampler m_sampler;
        private BoundingBoxD m_bBox;

        public bool Valid
        {
            get
            {
                if (m_sampler == null)
                {
                    return m_bBox.Volume > 0;
                }
                else
                {
                    return m_sampler.TotalWeight > 0;
                }
            }
        }

        public MyBBSetSampler(Vector3D min, Vector3D max)
        {
            Vector3D newMax = Vector3D.Max(min, max);
            Vector3D newMin = Vector3D.Min(min, max);

            m_bBox = new BoundingBoxD(newMin, newMax);

            m_sampler = new IntervalSampler(newMin.X, newMax.X, (newMax.Y - newMin.Y) * (newMax.Z - newMin.Z), Base6Directions.Axis.LeftRight);
        }

        public void SubtractBB(ref BoundingBoxD bb)
        {
            if (m_bBox.Intersects(ref bb) == false) return;

            BoundingBoxD intersected = m_bBox.Intersect(bb);

            m_sampler.Subtract(ref m_bBox, ref intersected);
        }

        public Vector3D Sample()
        {
            Vector3D output;

            IntervalSampler sampler = m_sampler;
            output.X = sampler.Sample(out sampler);

            if (sampler != null)
                output.Y = sampler.Sample(out sampler);
            else
                output.Y = MyUtils.GetRandomDouble(m_bBox.Min.Y, m_bBox.Max.Y);

            if (sampler != null)
                output.Z = sampler.Sample(out sampler);
            else
                output.Z = MyUtils.GetRandomDouble(m_bBox.Min.Z, m_bBox.Max.Z);

            System.Diagnostics.Debug.Assert(sampler == null, "Inconsistency in MyBBSetSampler");

            return output;
        }
    }
}
