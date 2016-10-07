using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyHighLevelGroup : MyPathFindingSystem<MyNavigationPrimitive>, IMyNavigationGroup
    {
        private IMyNavigationGroup m_lowLevel;
        private Dictionary<int, MyHighLevelPrimitive> m_primitives;
        // So far, only smart paths need to be notified about change in the primitives. If that changes, we can store lists of some interface
        private Dictionary<int, List<IMyHighLevelPrimitiveObserver>> m_primitiveObservers;
        private MyNavgroupLinks m_links;

        private int m_removingPrimitive = -1;

        private static List<int> m_tmpNeighbors = new List<int>();

        public IMyNavigationGroup LowLevelGroup
        {
            get
            {
                return m_lowLevel;
            }
        }

        public MyHighLevelGroup(IMyNavigationGroup lowLevelPathfinding, MyNavgroupLinks links, Func<long> timestampFunction)
            : base(128, timestampFunction)
        {
            m_lowLevel = lowLevelPathfinding;
            m_primitives = new Dictionary<int, MyHighLevelPrimitive>();
            m_primitiveObservers = new Dictionary<int, List<IMyHighLevelPrimitiveObserver>>();
            m_links = links;
        }

        public override string ToString()
        {
            if (m_lowLevel == null)
                return "Invalid HLPFG";
            else
                return "HLPFG of " + m_lowLevel.ToString();
        }

        public void AddPrimitive(int index, Vector3 localPosition)
        {
            Debug.Assert(!m_primitives.ContainsKey(index), "High-level primitive with the given index already exists!");
            m_primitives.Add(index, new MyHighLevelPrimitive(this, index, localPosition));
        }

        public MyHighLevelPrimitive TryGetPrimitive(int index)
        {
            MyHighLevelPrimitive retval = null;
            m_primitives.TryGetValue(index, out retval);
            return retval;
        }

        public MyHighLevelPrimitive GetPrimitive(int index)
        {
            MyHighLevelPrimitive retval = null;
            m_primitives.TryGetValue(index, out retval);

            Debug.Assert(retval != null, "High-level primitive not found!");
            return retval;
        }

        public void RemovePrimitive(int index)
        {
            m_removingPrimitive = index;
            MyHighLevelPrimitive primitive = null;
            if (!m_primitives.TryGetValue(index, out primitive))
            {
                Debug.Assert(false, "Could not find the primitive to remove!");
                m_removingPrimitive = -1;
                return;
            }

            List<IMyHighLevelPrimitiveObserver> observers = null;
            if (m_primitiveObservers.TryGetValue(index, out observers))
            {
                foreach (var path in observers)
                {
                    path.Invalidate();
                }
            }
            m_primitiveObservers.Remove(index);

            m_links.RemoveAllLinks(primitive);

            m_tmpNeighbors.Clear();
            primitive.GetNeighbours(m_tmpNeighbors);

            foreach (var neighborIndex in m_tmpNeighbors)
            {
                MyHighLevelPrimitive neighbor = null;
                m_primitives.TryGetValue(neighborIndex, out neighbor);

                Debug.Assert(neighbor != null, "Could not find the neighbor of a high-level primitive!");
                if (neighbor == null) continue;

                neighbor.Disconnect(index);
            }

            m_primitives.Remove(index);
            m_removingPrimitive = -1;
        }

        public void ConnectPrimitives(int a, int b)
        {
            Debug.Assert(m_primitives.ContainsKey(a), "Connecting non-existent navigation primitives!");
            Debug.Assert(m_primitives.ContainsKey(b), "Connecting non-existent navigation primitives!");
            Connect(a, b);
        }

        public void DisconnectPrimitives(int a, int b)
        {
            Debug.Assert(m_primitives.ContainsKey(a), "Disconnecting non-existent navigation primitives!");
            Debug.Assert(m_primitives.ContainsKey(b), "Disconnecting non-existent navigation primitives!");
            Disconnect(a, b);
        }

        private void Connect(int a, int b)
        {
            var primA = GetPrimitive(a);
            var primB = GetPrimitive(b);
            if (primA == null || primB == null)
            {
                return;
            }
            primA.Connect(b);
            primB.Connect(a);
        }

        private void Disconnect(int a, int b)
        {
            var primA = GetPrimitive(a);
            var primB = GetPrimitive(b);
            if (primA == null || primB == null)
            {
                return;
            }
            primA.Disconnect(b);
            primB.Disconnect(a);
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq)
        {
 	        throw new NotImplementedException();
        }

        public int GetExternalNeighborCount(MyNavigationPrimitive primitive)
        {
            return m_links.GetLinkCount(primitive);
        }

        public MyNavigationPrimitive GetExternalNeighbor(MyNavigationPrimitive primitive, int index)
        {
            return m_links.GetLinkedNeighbor(primitive, index);
        }

        public IMyPathEdge<MyNavigationPrimitive> GetExternalEdge(MyNavigationPrimitive primitive, int index)
        {
            return m_links.GetEdge(primitive, index);
        }

        public void RefinePath(MyPath<MyNavigationPrimitive> path, List<Vector4D> output, ref Vector3 startPoint, ref Vector3 endPoint, int begin, int end)
        {
 	        throw new NotImplementedException();
        }

        public Vector3 GlobalToLocal(Vector3D globalPos)
        {
 	        return m_lowLevel.GlobalToLocal(globalPos);
        }

        public Vector3D LocalToGlobal(Vector3 localPos)
        {
 	        return m_lowLevel.LocalToGlobal(localPos);
        }

        public void ObservePrimitive(MyHighLevelPrimitive primitive, IMyHighLevelPrimitiveObserver observer)
        {
            Debug.Assert(primitive.Parent == this);
            if (primitive.Parent != this) return;

            List<IMyHighLevelPrimitiveObserver> observers = null;
            int index = primitive.Index;
            if (!m_primitiveObservers.TryGetValue(index, out observers))
            {
                observers = new List<IMyHighLevelPrimitiveObserver>(4);
                m_primitiveObservers.Add(index, observers);
            }

            Debug.Assert(!observers.Contains(observer), "The given path is already observing the primitive!");
            observers.Add(observer);
        }

        public void StopObservingPrimitive(MyHighLevelPrimitive primitive, IMyHighLevelPrimitiveObserver observer)
        {
            Debug.Assert(primitive.Parent == this);
            if (primitive.Parent != this) return;

            List<IMyHighLevelPrimitiveObserver> observers = null;
            int index = primitive.Index;

            // This primitive is being removed currently, so all observers will be removed from it anyway.
            if (index == m_removingPrimitive)
            {
                return;
            }

            if (m_primitiveObservers.TryGetValue(index, out observers))
            {
                observers.Remove(observer);
                if (observers.Count == 0)
                {
                    m_primitiveObservers.Remove(index);
                }
            }
            else
            {
                Debug.Assert(false, "The path is not observing this primitive anymore!");
            }
        }

        public void DebugDraw(bool lite)
        {
            var lastTimestamp = MyCestmirPathfindingShorts.Pathfinding.LastHighLevelTimestamp;

            foreach (var entry in m_primitives)
            {
                if (lite)
                {
                    MyRenderProxy.DebugDrawPoint(entry.Value.WorldPosition, Color.CadetBlue, false);
                }
                else
                {
                    var primitive = entry.Value;

                    Vector3D offset = MySector.MainCamera.WorldMatrix.Down * 0.3f;

                    float dist = (float)Vector3D.Distance(primitive.WorldPosition, MySector.MainCamera.Position);
                    float textSize = 7.0f / dist;
                    if (textSize > 30.0f)
                        textSize = 30.0f;
                    if (textSize < 0.5f)
                        textSize = 0.5f;

                    if (dist < 100)
                    {
                        List<IMyHighLevelPrimitiveObserver> observingPaths = null;
                        if (m_primitiveObservers.TryGetValue(entry.Key, out observingPaths))
                        {
                            MyRenderProxy.DebugDrawText3D(primitive.WorldPosition + offset, observingPaths.Count.ToString(), Color.Red, textSize * 3.0f, false);
                        }
                        //MyRenderProxy.DebugDrawSphere(primitive.WorldPosition, 0.2f, Color.CadetBlue, 1.0f, false);
                        MyRenderProxy.DebugDrawText3D(primitive.WorldPosition + offset, entry.Key.ToString(), Color.CadetBlue, textSize, false);
                    }

                    for (int i = 0; i < primitive.GetOwnNeighborCount(); ++i)
                    {
                        var primitive2 = primitive.GetOwnNeighbor(i) as MyHighLevelPrimitive;
                        MyRenderProxy.DebugDrawLine3D(primitive.WorldPosition, primitive2.WorldPosition, Color.CadetBlue, Color.CadetBlue, false);
                    }

                    if (primitive.PathfindingData.GetTimestamp() == lastTimestamp)
                    {
                        MyRenderProxy.DebugDrawSphere(primitive.WorldPosition, 0.5f, Color.DarkRed, 1.0f, false);
                    }
                }
            }
        }

        public MyHighLevelGroup HighLevelGroup
        {
            get { return null; }
        }

        public MyHighLevelPrimitive GetHighLevelPrimitive(MyNavigationPrimitive myNavigationTriangle)
        {
            return null;
        }

        public IMyHighLevelComponent GetComponent(MyHighLevelPrimitive highLevelPrimitive)
        {
            return null;
        }

        public void GetPrimitivesOnPath(ref List<MyHighLevelPrimitive> primitives)
        {
            // primitives on a path are observed...
            foreach(var v in m_primitiveObservers)
            {
                // get high level primitive
                MyHighLevelPrimitive primitive = TryGetPrimitive(v.Key);
                Debug.Assert(primitive != null); // observer primitive should be in primitives
                primitives.Add(primitive);
            }
        }
    }
}
