using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;

namespace VRageMath.Spatial
{
    //Leave it for final because of edit and continue
    //public class MyClusterTree<T> where T: class
    public class MyClusterTree
    {
        #region Events

        public Func<int, BoundingBoxD, object> OnClusterCreated;
        public Action<object> OnClusterRemoved;
        public Action<object> OnFinishBatch;
        public Action OnClustersReordered;
        public Func<long, bool> GetEntityReplicableExistsById; //Debug function. Remove once crash with inconsistent clusters is resolved

        #endregion

        #region Classes

        public interface IMyActivationHandler
        {
            /// <summary>
            /// Called when standalone object is added to cluster
            /// </summary>
            /// <param name="userData"></param>
            /// <param name="clusterObjectID"></param>
            void Activate(object userData, ulong clusterObjectID);

            /// <summary>
            /// Called when standalone object is removed from cluster
            /// </summary>
            /// <param name="userData"></param>
            void Deactivate(object userData);

            /// <summary>
            /// Called when multiple objects are added to cluster.
            /// </summary>
            /// <param name="userData"></param>
            /// <param name="clusterObjectID"></param>
            void ActivateBatch(object userData, ulong clusterObjectID);

            /// <summary>
            /// Called when multiple objects are removed from cluster.
            /// </summary>
            /// <param name="userData"></param>
            void DeactivateBatch(object userData);

            /// <summary>
            /// Called when adding multiple objects is finished.
            /// </summary>
            void FinishAddBatch();

            /// <summary>
            /// Called when removing multiple objects is finished.
            /// </summary>
            void FinishRemoveBatch(object userData);

            /// <summary>
            /// If true, than this object is not calculated into cluster division algorithm. It is just added or removed if dynamic object is in range.
            /// </summary>
            bool IsStaticForCluster { get; }
        }

        #endregion

        #region Fields

        public const ulong CLUSTERED_OBJECT_ID_UNITIALIZED = ulong.MaxValue;

        public static Vector3 IdealClusterSize = new Vector3(20000); //This size will have cluster for ideal performance
        public static Vector3 MinimumDistanceFromBorder = IdealClusterSize / 10.0f; //Closer objects will force reorder of clusters
        public static Vector3 MaximumForSplit = IdealClusterSize * 2; //If bigger than this, cluster tries to split

        public readonly BoundingBoxD? SingleCluster;
        public readonly bool ForcedClusters;


        class MyCluster
        {
            public int ClusterId;
            public BoundingBoxD AABB;
            public HashSet<ulong> Objects;
            public object UserData;

            public override string ToString()
            {
                return "MyCluster" + ClusterId + ": " + AABB.Center;
            }
        }

        class MyObjectData
        {
            public ulong Id;
            public MyCluster Cluster;
            public IMyActivationHandler ActivationHandler;
            public BoundingBoxD AABB;
            public int StaticId;
            public string Tag;
            public long EntityId;
        }

        public struct MyClusterQueryResult
        {
            public BoundingBoxD AABB;
            public object UserData;
        }

        MyDynamicAABBTreeD m_clusterTree = new MyDynamicAABBTreeD(Vector3D.Zero);
        MyDynamicAABBTreeD m_staticTree = new MyDynamicAABBTreeD(Vector3D.Zero);
        Dictionary<ulong, MyObjectData> m_objectsData = new Dictionary<ulong, MyObjectData>();
        List<MyCluster> m_clusters = new List<MyCluster>();


        ulong m_clusterObjectCounter = 0;

        List<MyCluster> m_returnedClusters = new List<MyCluster>(1);
        List<object> m_userObjects = new List<object>();
        #endregion

        #region Cluster management

        public MyClusterTree(BoundingBoxD? singleCluster, bool forcedClusters)
        {
            SingleCluster = singleCluster;
            ForcedClusters = forcedClusters;
        }

        public ulong AddObject(BoundingBoxD bbox, IMyActivationHandler activationHandler, ulong? customId, string tag, long entityId)
        {
            if (SingleCluster.HasValue && m_clusters.Count == 0)
            {
                BoundingBoxD bb = SingleCluster.Value;
                bb.Inflate(200); //inflate 200m so objects near world border have AABB inside => physics created
                CreateCluster(ref bb);
            }

            BoundingBoxD inflatedBBox;
            if (SingleCluster.HasValue || ForcedClusters)
                inflatedBBox = bbox;
            else
                inflatedBBox = bbox.GetInflated(MinimumDistanceFromBorder);

            m_clusterTree.OverlapAllBoundingBox(ref inflatedBBox, m_returnedClusters);

            MyCluster cluster = null;
            bool needReorder = false;

            if (m_returnedClusters.Count == 1)
            {
                if (m_returnedClusters[0].AABB.Contains(inflatedBBox) == ContainmentType.Contains)
                    cluster = m_returnedClusters[0];
                else
                    if (m_returnedClusters[0].AABB.Contains(inflatedBBox) == ContainmentType.Intersects && activationHandler.IsStaticForCluster)
                    {
                        if (m_returnedClusters[0].AABB.Contains(bbox) == ContainmentType.Disjoint)
                        {   //completely out
                        }
                        else
                            cluster = m_returnedClusters[0];
                    }
                    else
                        needReorder = true;
            }
            else
                if (m_returnedClusters.Count > 1)
                {
                    if (!activationHandler.IsStaticForCluster)
                        needReorder = true;
                }
                else
                    if (m_returnedClusters.Count == 0)
                    {
                        if (SingleCluster.HasValue)
                            return VRageMath.Spatial.MyClusterTree.CLUSTERED_OBJECT_ID_UNITIALIZED;

                        if (!activationHandler.IsStaticForCluster)
                        {
                            var clusterBB = new BoundingBoxD(bbox.Center - IdealClusterSize / 2, bbox.Center + IdealClusterSize / 2);
                            m_clusterTree.OverlapAllBoundingBox(ref clusterBB, m_returnedClusters);

                            if (m_returnedClusters.Count == 0)
                            { //Space is empty, create new cluster

                                m_staticTree.OverlapAllBoundingBox(ref clusterBB, m_objectDataResultList);
                                cluster = CreateCluster(ref clusterBB);

                                foreach (var ob in m_objectDataResultList)
                                {
                                    System.Diagnostics.Debug.Assert(m_objectsData[ob].Cluster == null, "Found object must not be in cluster!");
                                    if (m_objectsData[ob].Cluster == null)
                                    {
                                        AddObjectToCluster(cluster, ob, false);
                                    }
                                }
                            }
                            else  //There is still some blocking cluster
                                needReorder = true;
                        }
                    }

            ulong objectId = customId.HasValue ? customId.Value : m_clusterObjectCounter++;
            int staticObjectId = MyDynamicAABBTreeD.NullNode;

            m_objectsData[objectId] = new MyObjectData()
            {
                Id = objectId,
                Cluster = cluster,
                ActivationHandler = activationHandler,
                AABB = bbox,
                StaticId = staticObjectId,
                Tag = tag,
                EntityId = entityId
            };

            System.Diagnostics.Debug.Assert(!needReorder || (!SingleCluster.HasValue && needReorder), "Object cannot be added outside borders of a single cluster");

            System.Diagnostics.Debug.Assert(!needReorder || (!ForcedClusters && needReorder), "Error in cluster data, they dont correspond to provided objects");

            if (needReorder && !SingleCluster.HasValue && !ForcedClusters)
            {
                System.Diagnostics.Debug.Assert(cluster == null, "Error in cluster logic");

                ReorderClusters(bbox, objectId);
                if (!m_objectsData[objectId].ActivationHandler.IsStaticForCluster)
                {
                    System.Diagnostics.Debug.Assert(m_objectsData[objectId].Cluster != null, "Object not added");
                }
#if DEBUG
                m_clusterTree.OverlapAllBoundingBox(ref bbox, m_returnedClusters);

                System.Diagnostics.Debug.Assert(m_returnedClusters.Count <= 1, "Clusters overlap!");
                if (m_returnedClusters.Count != 0)
                {
                    System.Diagnostics.Debug.Assert(activationHandler.IsStaticForCluster ? m_returnedClusters[0].AABB.Contains(inflatedBBox) != ContainmentType.Disjoint : m_returnedClusters[0].AABB.Contains(inflatedBBox) == ContainmentType.Contains, "Clusters reorder failure!");
                }
#endif
            }

            if (activationHandler.IsStaticForCluster)
            {
                staticObjectId = m_staticTree.AddProxy(ref bbox, objectId, 0);

                m_objectsData[objectId].StaticId = staticObjectId;
            }

            if (cluster != null)
            {
                return AddObjectToCluster(cluster, objectId, false);
            }
            else
                return objectId;
        }

        ulong AddObjectToCluster(MyCluster cluster, ulong objectId, bool batch)
        {
            System.Diagnostics.Debug.Assert(cluster.AABB.Contains(m_objectsData[objectId].AABB) != ContainmentType.Disjoint, "Adding object which is completely out");

            cluster.Objects.Add(objectId);

            var objectData = m_objectsData[objectId];

            m_objectsData[objectId].Id = objectId;
            m_objectsData[objectId].Cluster = cluster;                

            if (batch)
            {
                if (objectData.ActivationHandler != null)
                    objectData.ActivationHandler.ActivateBatch(cluster.UserData, objectId);
            }
            else
            {
                if (objectData.ActivationHandler != null)
                    objectData.ActivationHandler.Activate(cluster.UserData, objectId);
            }

            return objectId;
        }

        private MyCluster CreateCluster(ref BoundingBoxD clusterBB)
        {
            MyCluster cluster = new MyCluster() 
            {
                AABB = clusterBB,
                Objects = new HashSet<ulong>()
            };
            cluster.ClusterId = m_clusterTree.AddProxy(ref cluster.AABB, cluster, 0);

            if (OnClusterCreated != null)
                cluster.UserData = OnClusterCreated(cluster.ClusterId, cluster.AABB);

            m_clusters.Add(cluster);
            m_userObjects.Add(cluster.UserData);

            return cluster;
        }

        public static BoundingBoxD AdjustAABBByVelocity(BoundingBoxD aabb, Vector3 velocity)
        {
            if (velocity.LengthSquared() > 0.001f)
            {
                velocity.Normalize();
            }

            aabb.Include(aabb.Center + velocity * 2000.0f);

            return aabb;
        }

        public void MoveObject(ulong id, BoundingBoxD aabb, Vector3 velocity)
        {
            System.Diagnostics.Debug.Assert(id != CLUSTERED_OBJECT_ID_UNITIALIZED, "Unitialized object in cluster!");

            MyObjectData objectData;
            if (m_objectsData.TryGetValue(id, out objectData))
            {
                System.Diagnostics.Debug.Assert(!objectData.ActivationHandler.IsStaticForCluster, "Cannot move static object!");

                var oldAABB = objectData.AABB;
                objectData.AABB = aabb;

                aabb = AdjustAABBByVelocity(aabb, velocity);
                
                System.Diagnostics.Debug.Assert(m_clusters.Contains(objectData.Cluster));

                var newContainmentType = objectData.Cluster.AABB.Contains(aabb);
                if (newContainmentType != ContainmentType.Contains && !SingleCluster.HasValue && !ForcedClusters)
                {
                    if (newContainmentType == ContainmentType.Disjoint)
                    {
                        //Probably caused by teleport 
                        m_clusterTree.OverlapAllBoundingBox(ref aabb, m_returnedClusters);
                        if ((m_returnedClusters.Count == 1) &&
                            (m_returnedClusters[0].AABB.Contains(aabb) == ContainmentType.Contains))
                        {
                            //Just move object from one cluster to another
                            var oldCluster = objectData.Cluster;
                            RemoveObjectFromCluster(objectData, false);
                            if (oldCluster.Objects.Count == 0)
                                RemoveCluster(oldCluster);

                            AddObjectToCluster(m_returnedClusters[0], objectData.Id, false);
                        }
                        else
                        {
                            aabb.InflateToMinimum(IdealClusterSize);
                            ReorderClusters(aabb.Include(oldAABB), id);
                        }
                    }
                    else
                    {
                        aabb.InflateToMinimum(IdealClusterSize);
                        ReorderClusters(aabb.Include(oldAABB), id);
                    }
                }

                System.Diagnostics.Debug.Assert(m_objectsData[id].Cluster.AABB.Contains(objectData.AABB) == ContainmentType.Contains || SingleCluster.HasValue, "Inconsistency in clusters");
            }

            //foreach (var ob in m_objectsData)
            //{
            //    if (ob.Value.ActivationHandler.IsStatic && ob.Value.Cluster != null)
            //        System.Diagnostics.Debug.Assert(ob.Value.Cluster.AABB.Contains(ob.Value.AABB) != ContainmentType.Disjoint, "Inconsistency in clusters");
            //    else
            //      if (!ob.Value.ActivationHandler.IsStatic)
            //        System.Diagnostics.Debug.Assert(ob.Value.Cluster.AABB.Contains(ob.Value.AABB) == ContainmentType.Contains, "Inconsistency in clusters");
            //}
        }

        public void EnsureClusterSpace(BoundingBoxD aabb)
        {
            if (SingleCluster.HasValue)
                return;
            if (ForcedClusters)
                return;

            m_clusterTree.OverlapAllBoundingBox(ref aabb, m_returnedClusters);

            bool needReorder = true;

            if (m_returnedClusters.Count == 1)
            {
                if (m_returnedClusters[0].AABB.Contains(aabb) == ContainmentType.Contains)
                    needReorder = false;
            }

            if (needReorder)
            {
                ulong objectId = m_clusterObjectCounter++;
                int staticObjectId = MyDynamicAABBTreeD.NullNode;

                m_objectsData[objectId] = new MyObjectData()
                {
                    Id = objectId,
                    Cluster = null,
                    ActivationHandler = null,
                    AABB = aabb,
                    StaticId = staticObjectId
                };

                ReorderClusters(aabb, objectId);

                RemoveObjectFromCluster(m_objectsData[objectId], false);

                m_objectsData.Remove(objectId);
            }
        }

        public void RemoveObject(ulong id)
        {
            System.Diagnostics.Debug.Assert(id != CLUSTERED_OBJECT_ID_UNITIALIZED, "Unitialized object in cluster!");

            MyObjectData objectData;
            if (m_objectsData.TryGetValue(id, out objectData))
            {
                MyCluster cluster = objectData.Cluster;

                if (cluster != null)
                {
                    RemoveObjectFromCluster(objectData, false);

                    if (cluster.Objects.Count == 0)
                    {
                        RemoveCluster(cluster);
                    }
                }

                if (objectData.StaticId != MyDynamicAABBTreeD.NullNode)
                {
                    m_staticTree.RemoveProxy(objectData.StaticId);
                    objectData.StaticId = MyDynamicAABBTreeD.NullNode;
                }

                m_objectsData.Remove(id);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Removed object is not in cluster");
            }
        }

        private void RemoveObjectFromCluster(MyObjectData objectData, bool batch)
        {
            objectData.Cluster.Objects.Remove(objectData.Id);

            if (batch)
            {
                if (objectData.ActivationHandler != null)
                    objectData.ActivationHandler.DeactivateBatch(objectData.Cluster.UserData);
            }
            else
            {
                //during batch process cluster is nulled in finish batch
                if (objectData.ActivationHandler != null)
                {
                    objectData.ActivationHandler.Deactivate(objectData.Cluster.UserData);
                }
                m_objectsData[objectData.Id].Cluster = null;
            }
        }

        private void RemoveCluster(MyCluster cluster)
        {
            m_clusterTree.RemoveProxy(cluster.ClusterId);
            m_clusters.Remove(cluster);
            m_userObjects.Remove(cluster.UserData);

            if (OnClusterRemoved != null)
                OnClusterRemoved(cluster.UserData);
        }

        #endregion

        public Vector3D GetObjectOffset(ulong id)
        {
            MyObjectData objectData;
            if (m_objectsData.TryGetValue(id, out objectData))
            {
                if (objectData.Cluster == null)
                    return Vector3D.Zero;
                return objectData.Cluster.AABB.Center;
            }

            return Vector3D.Zero;
        }

        public object GetClusterForPosition(Vector3D pos)
        {
            var bs = new BoundingSphereD(pos, 1);
            m_clusterTree.OverlapAllBoundingSphere(ref bs, m_returnedClusters);
            return m_returnedClusters.Count > 0 ? m_returnedClusters.Single().UserData : null;
        }

        public void Dispose()
        {
            foreach (var cluster in m_clusters)
            {
                if (OnClusterRemoved != null)
                    OnClusterRemoved(cluster.UserData);
            }

            m_clusters.Clear();
            m_userObjects.Clear();
            m_clusterTree.Clear();
            m_objectsData.Clear();
            m_staticTree.Clear();
            m_clusterObjectCounter = 0;
        }


        public ListReader<object> GetList()
        {
            return new ListReader<object>(m_userObjects);
        }

        [ThreadStatic]
        static List<MyLineSegmentOverlapResult<MyCluster>> m_lineResultListPerThread;
        static List<MyLineSegmentOverlapResult<MyCluster>> m_lineResultList
        {
            get
            {
                if (m_lineResultListPerThread == null)
                    m_lineResultListPerThread = new List<MyLineSegmentOverlapResult<MyCluster>>();
                return m_lineResultListPerThread;
            }
        }

        [ThreadStatic]
        static List<MyCluster> m_resultListPerThread;
        static List<MyCluster> m_resultList
        {
            get
            {
                if (m_resultListPerThread == null)
                    m_resultListPerThread = new List<MyCluster>();
                return m_resultListPerThread;
            }
        }

        [ThreadStatic]
        static List<ulong> m_objectDataResultListPerThread;
        static List<ulong> m_objectDataResultList
        {
            get
            {
                if (m_objectDataResultListPerThread == null)
                    m_objectDataResultListPerThread = new List<ulong>();
                return m_objectDataResultListPerThread;
            }
        }

        public void CastRay(Vector3D from, Vector3D to, List<MyClusterQueryResult> results)
        {
            // If m_clusterTree doesn't exist, or the results array is null, don't perform function
            if (m_clusterTree == null || results == null)
                return;

            LineD line = new LineD(from, to);
            m_clusterTree.OverlapAllLineSegment(ref line, m_lineResultList);

            foreach (var res in m_lineResultList)
            {
                // Skip results without an element
                if (res.Element == null)
                    continue;

                results.Add(new MyClusterQueryResult()
                {
                    AABB = res.Element.AABB,
                    UserData = res.Element.UserData
                });
            }
        }

        public void Intersects(Vector3D translation, List<MyClusterQueryResult> results)
        {
            BoundingBoxD box = new BoundingBoxD(translation - new Vector3D(1), translation + new Vector3D(1));
            m_clusterTree.OverlapAllBoundingBox(ref box, m_resultList);

            foreach (var res in m_resultList)
            {
                results.Add(new MyClusterQueryResult()
                {
                    AABB = res.AABB,
                    UserData = res.UserData
                });
            }
        }

        public void GetAll(List<MyClusterQueryResult> results)
        {
            m_clusterTree.GetAll(m_resultList, true);

            foreach (var res in m_resultList)
            {
                results.Add(new MyClusterQueryResult()
                {
                    AABB = res.AABB,
                    UserData = res.UserData
                });
            }
        }


        #region AABB Comparers

        private class AABBComparerX : IComparer<MyObjectData>
        {
            public static AABBComparerX Static = new AABBComparerX();

            public int Compare(MyObjectData x, MyObjectData y)
            {
                return x.AABB.Min.X.CompareTo(y.AABB.Min.X);
            }
        }

        private class AABBComparerY : IComparer<MyObjectData>
        {
            public static AABBComparerY Static = new AABBComparerY();

            public int Compare(MyObjectData x, MyObjectData y)
            {
                return x.AABB.Min.Y.CompareTo(y.AABB.Min.Y);
            }
        }

        private class AABBComparerZ : IComparer<MyObjectData>
        {
            public static AABBComparerZ Static = new AABBComparerZ();

            public int Compare(MyObjectData x, MyObjectData y)
            {
                return x.AABB.Min.Z.CompareTo(y.AABB.Min.Z);
            }
        }

        #endregion

        struct MyClusterDescription
        {
            public BoundingBoxD AABB;
            public List<MyObjectData> DynamicObjects;
            public List<MyObjectData> StaticObjects;
        }

        //1. Object A reaches borders
        //2. Make ideal cluster box B around A
        //3. Detect all cluster intersections C of B
        //4. Make union of all C and B to D
        //5. Find best division of D
        //6. Foreach dominant axis
        //6A  split until only allowed size      
        //6B  leave lowest larger size
        //repeat 6 until dominant axis is allowed size or not splittable
        public void ReorderClusters(BoundingBoxD aabb, ulong objectId = ulong.MaxValue)
        {
            //1+2
            aabb.InflateToMinimum(IdealClusterSize);

            bool isBoxSafe = false;
            BoundingBoxD unionCluster = aabb;

            //3
            m_clusterTree.OverlapAllBoundingBox(ref unionCluster, m_resultList);
            HashSet<MyObjectData> objectsInUnion = new HashSet<MyObjectData>();

            while (!isBoxSafe)
            {
                //4
                objectsInUnion.Clear();

                if (objectId != ulong.MaxValue)
                    objectsInUnion.Add(m_objectsData[objectId]);

                foreach (MyCluster collidedCluster in m_resultList)
                {
                    unionCluster.Include(collidedCluster.AABB);
                    foreach (var ob in m_objectsData.Where(x => collidedCluster.Objects.Contains(x.Key)).Select(x => x.Value))
                    {
                        objectsInUnion.Add(ob);
                    }
                }

                int oldClustersCount = m_resultList.Count;
                //3
                m_clusterTree.OverlapAllBoundingBox(ref unionCluster, m_resultList);

                isBoxSafe = oldClustersCount == m_resultList.Count; //Box is safe only if no new clusters were added

                m_staticTree.OverlapAllBoundingBox(ref unionCluster, m_objectDataResultList);
                foreach (var ob in m_objectDataResultList)
                {
                    if (m_objectsData[ob].Cluster != null)
                    {
                        if (!m_resultList.Contains(m_objectsData[ob].Cluster))
                        {
                            unionCluster.Include(m_objectsData[ob].Cluster.AABB);
                            isBoxSafe = false;
                        }
                    }
                }
            }

            m_staticTree.OverlapAllBoundingBox(ref unionCluster, m_objectDataResultList);
            foreach (var ob in m_objectDataResultList)
            {
                //var c = m_objectsData[ob].Cluster.AABB.Contains(unionCluster);
                System.Diagnostics.Debug.Assert(m_objectsData[ob].Cluster == null || m_objectsData[ob].Cluster.AABB.Contains(m_objectsData[ob].AABB) != ContainmentType.Disjoint, "Clusters failure");
                System.Diagnostics.Debug.Assert(m_objectsData[ob].Cluster == null || (m_clusters.Contains(m_objectsData[ob].Cluster) && m_resultList.Contains(m_objectsData[ob].Cluster)), "Static object is not inside found clusters");
                objectsInUnion.Add(m_objectsData[ob]);
            }

#if DEBUG
            foreach (var ob in objectsInUnion)
            {
                System.Diagnostics.Debug.Assert(ob.Cluster == null || (m_clusters.Contains(ob.Cluster) && m_resultList.Contains(ob.Cluster)), "There is object not belonging to found cluster!");
            }
#endif

            //5
            Stack<MyClusterDescription> clustersToDivide = new Stack<MyClusterDescription>();
            List<MyClusterDescription> finalClusters = new List<MyClusterDescription>();

            var unionClusterDesc = new MyClusterDescription()
            {
                AABB = unionCluster,
                DynamicObjects = objectsInUnion.Where(x => x.ActivationHandler == null || !x.ActivationHandler.IsStaticForCluster).ToList(),
                StaticObjects = objectsInUnion.Where(x => (x.ActivationHandler != null) && x.ActivationHandler.IsStaticForCluster).ToList(),
            };
            clustersToDivide.Push(unionClusterDesc);

            var staticObjectsToRemove = unionClusterDesc.StaticObjects.Where(x => x.Cluster != null).ToList();
            var staticObjectsTotal = unionClusterDesc.StaticObjects.Count;


            while (clustersToDivide.Count > 0)
            {
                MyClusterDescription desc = clustersToDivide.Pop();

                if (desc.DynamicObjects.Count == 0)
                {
                    continue;
                }

                //minimal valid aabb usable for this cluster
                BoundingBoxD minimumCluster = BoundingBoxD.CreateInvalid();
                for (int i = 0; i < desc.DynamicObjects.Count; i++)
                {
                    MyObjectData objectData0 = desc.DynamicObjects[i];
                    BoundingBoxD aabb0 = objectData0.AABB.GetInflated(IdealClusterSize / 2);
                    minimumCluster.Include(aabb0);
                }

                //Divide along longest axis
                BoundingBoxD dividedCluster = minimumCluster;

                Vector3D currentMax = minimumCluster.Max;
                int longestAxis = minimumCluster.Size.AbsMaxComponent();

                switch (longestAxis)
                {
                    case 0:
                        desc.DynamicObjects.Sort(AABBComparerX.Static);
                        break;
                    case 1:
                        desc.DynamicObjects.Sort(AABBComparerY.Static);
                        break;
                    case 2:
                        desc.DynamicObjects.Sort(AABBComparerZ.Static);
                        break;
                }

                bool isClusterSplittable = false;

                if (minimumCluster.Size.AbsMax() >= MaximumForSplit.AbsMax())
                {
                    for (int i = 1; i < desc.DynamicObjects.Count; i++)
                    {
                        MyObjectData objectData0 = desc.DynamicObjects[i - 1];
                        MyObjectData objectData1 = desc.DynamicObjects[i];

                        BoundingBoxD aabb0 = objectData0.AABB.GetInflated(IdealClusterSize / 2);
                        BoundingBoxD aabb1 = objectData1.AABB.GetInflated(IdealClusterSize / 2);

                        //two neigbour object have distance between them bigger than minimum
                        if ((aabb1.Min.GetDim(longestAxis) - aabb0.Max.GetDim(longestAxis)) > 0)
                        {

                            System.Diagnostics.Debug.Assert(aabb0.Max.GetDim(longestAxis) - minimumCluster.Min.GetDim(longestAxis) > 0, "Invalid minimal cluster");
                            isClusterSplittable = true;

                            currentMax.SetDim(longestAxis, aabb0.Max.GetDim(longestAxis));

                            break;
                        }
                    }


                }

                dividedCluster.Max = currentMax;

                dividedCluster.InflateToMinimum(IdealClusterSize);

                MyClusterDescription dividedClusterDesc = new MyClusterDescription()
                {
                    AABB = dividedCluster,
                    DynamicObjects = new List<MyObjectData>(),
                    StaticObjects = new List<MyObjectData>(),
                };

                foreach (var dynObj in desc.DynamicObjects.ToList())
                {
                    var cont = dividedCluster.Contains(dynObj.AABB);
                    if (cont == ContainmentType.Contains)
                    {
                        dividedClusterDesc.DynamicObjects.Add(dynObj);
                        desc.DynamicObjects.Remove(dynObj);
                    }

                    System.Diagnostics.Debug.Assert(cont != ContainmentType.Intersects, "Cannot split clusters in the middle of objects");
                }
                foreach (var statObj in desc.StaticObjects.ToList())
                {
                    var cont = dividedCluster.Contains(statObj.AABB);
                    if ((cont == ContainmentType.Contains) || (cont == ContainmentType.Intersects))
                    {
                        dividedClusterDesc.StaticObjects.Add(statObj);
                        desc.StaticObjects.Remove(statObj);
                    }
                }

                dividedClusterDesc.AABB = dividedCluster;

                if (desc.DynamicObjects.Count > 0)
                {
                    BoundingBoxD restCluster = BoundingBoxD.CreateInvalid();

                    foreach (var restbb in desc.DynamicObjects)
                    {
                        restCluster.Include(restbb.AABB.GetInflated(MinimumDistanceFromBorder));
                    }

                    restCluster.InflateToMinimum(IdealClusterSize);

                    MyClusterDescription restClusterDesc = new MyClusterDescription()
                    {
                        AABB = restCluster,
                        DynamicObjects = desc.DynamicObjects.ToList(),
                        StaticObjects = desc.StaticObjects.ToList(),
                    };

                    if (restClusterDesc.AABB.Size.AbsMax() > 2 * IdealClusterSize.AbsMax())
                        clustersToDivide.Push(restClusterDesc);
                    else
                        finalClusters.Add(restClusterDesc);
                }

                if (dividedClusterDesc.AABB.Size.AbsMax() > 2 * IdealClusterSize.AbsMax() && isClusterSplittable)
                    clustersToDivide.Push(dividedClusterDesc);
                else
                    finalClusters.Add(dividedClusterDesc);
            }

#if DEBUG
            //Check consistency
            for (int i = 0; i < finalClusters.Count; i++)
                for (int j = 0; j < finalClusters.Count; j++)
                    if (i != j)
                    {
                        var cont = finalClusters[i].AABB.Contains(finalClusters[j].AABB);
                        System.Diagnostics.Debug.Assert(cont == ContainmentType.Disjoint, "Overlapped clusters!");
                        if (cont != ContainmentType.Disjoint)
                        {
                        }
                    }

#endif

#if DEBUG
            Dictionary<MyCluster, List<ulong>> objectsPerRemovedCluster = new Dictionary<MyCluster, List<ulong>>();
            Dictionary<MyCluster, List<ulong>> dynamicObjectsInCluster = new Dictionary<MyCluster, List<ulong>>();
            foreach (var finalCluster in finalClusters)
            {
                foreach (var ob in finalCluster.DynamicObjects)
                {
                    if (ob.Cluster != null)
                    {
                        if (!objectsPerRemovedCluster.ContainsKey(ob.Cluster))
                            objectsPerRemovedCluster[ob.Cluster] = new List<ulong>();

                        objectsPerRemovedCluster[ob.Cluster].Add(ob.Id);
                    }
                    else
                    {
                    }
                }
            }

            foreach (var removingCluster in objectsPerRemovedCluster)
            {
                dynamicObjectsInCluster[removingCluster.Key] = new List<ulong>();
                foreach (var ob in removingCluster.Key.Objects)
                {
                    if (!m_objectsData[ob].ActivationHandler.IsStaticForCluster)
                    {
                        dynamicObjectsInCluster[removingCluster.Key].Add(ob);
                    }
                }

                System.Diagnostics.Debug.Assert(removingCluster.Value.Count == dynamicObjectsInCluster[removingCluster.Key].Count, "Not all objects from removing cluster are going to new clusters!");
            }

            Dictionary<MyCluster, List<ulong>> staticObjectsInCluster = new Dictionary<MyCluster, List<ulong>>();
            foreach (var staticObj in staticObjectsToRemove)
            {
                System.Diagnostics.Debug.Assert(staticObj.Cluster != null, "Where to remove?");
                if (!staticObjectsInCluster.ContainsKey(staticObj.Cluster))
                    staticObjectsInCluster[staticObj.Cluster] = new List<ulong>();

                staticObjectsInCluster[staticObj.Cluster].Add(staticObj.Id);
            }
#endif

            HashSet<MyCluster> oldClusters = new HashSet<MyCluster>();
            HashSet<MyCluster> newClusters = new HashSet<MyCluster>();

            foreach (var staticObj in staticObjectsToRemove)
            {
                if (staticObj.Cluster != null)
                {
                    oldClusters.Add(staticObj.Cluster);
                    RemoveObjectFromCluster(staticObj, true);
                }
                else
                {
                }
            }

            foreach (var staticObj in staticObjectsToRemove)
            {
                if (staticObj.Cluster != null)
                {
                    staticObj.ActivationHandler.FinishRemoveBatch(staticObj.Cluster.UserData);
                    staticObj.Cluster = null;
                }
            }

            int staticObjectsAdded = 0;

            //Move objects from old clusters to new clusters, use batching
            foreach (var finalCluster in finalClusters)
            {
                BoundingBoxD clusterAABB = finalCluster.AABB;
                MyCluster newCluster = CreateCluster(ref clusterAABB);

#if DEBUG
                for (int i = 0; i < finalCluster.DynamicObjects.Count; i++)
                    for (int j = 0; j < finalCluster.DynamicObjects.Count; j++)
                    {
                        if (i != j)
                            System.Diagnostics.Debug.Assert(finalCluster.DynamicObjects[i].Id != finalCluster.DynamicObjects[j].Id);
                    }
#endif

                foreach (var obj in finalCluster.DynamicObjects)
                {
                    if (obj.Cluster != null)
                    {
                        oldClusters.Add(obj.Cluster);
                        RemoveObjectFromCluster(obj, true);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(objectId == obj.Id || obj.ActivationHandler.IsStaticForCluster, "Dynamic object must have cluster");
                    }
                }

                foreach (var obj in finalCluster.DynamicObjects)
                {
                    if (obj.Cluster != null)
                    {
                        obj.ActivationHandler.FinishRemoveBatch(obj.Cluster.UserData);
                        obj.Cluster = null;
                    }
                }

                //Finish batches on old worlds and remove old worlds
                foreach (MyCluster oldCluster in oldClusters)
                {
                    if (OnFinishBatch != null)
                        OnFinishBatch(oldCluster.UserData);
                }


                foreach (var obj in finalCluster.DynamicObjects)
                {
                    AddObjectToCluster(newCluster, obj.Id, true);
                }

                foreach (var obj in finalCluster.StaticObjects)
                {
                    if (newCluster.AABB.Contains(obj.AABB) != ContainmentType.Disjoint)
                    {
                        AddObjectToCluster(newCluster, obj.Id, true);
                        staticObjectsAdded++;
                    }
                }

                newClusters.Add(newCluster);
            }

            System.Diagnostics.Debug.Assert(staticObjectsTotal >= staticObjectsAdded, "Static objects appeared out of union");

            foreach (MyCluster oldCluster in oldClusters)
            {
                RemoveCluster(oldCluster);
            }
           
            //Finish batches on new world and their objects
            foreach (MyCluster newCluster in newClusters)
            {
                if (OnFinishBatch != null)
                    OnFinishBatch(newCluster.UserData);

                foreach (var ob in newCluster.Objects)
                {
                    if (m_objectsData[ob].ActivationHandler != null)
                        m_objectsData[ob].ActivationHandler.FinishAddBatch();
                }
            }

            if (OnClustersReordered != null)
                OnClustersReordered();
        }

        public void GetAllStaticObjects(List<BoundingBoxD> staticObjects)
        {
            m_staticTree.GetAll(m_objectDataResultList, true);
            staticObjects.Clear();
            foreach (var ob in m_objectDataResultList)
            {
                staticObjects.Add(m_objectsData[ob].AABB);
            }
        }

        public void Serialize(List<BoundingBoxD> list)
        {
            foreach (var cluster in m_clusters)
            {
                list.Add(cluster.AABB);
            }
        }

        public void Deserialize(List<BoundingBoxD> list)
        { 
            //Remove all
            foreach (var obj in m_objectsData.Values)
            {
                if (obj.Cluster != null)
                {
                    RemoveObjectFromCluster(obj, true);
                }
                else
                {
                }
            }
            foreach (var obj in m_objectsData.Values)
            {
                if (obj.Cluster != null)
                {
                    obj.ActivationHandler.FinishRemoveBatch(obj.Cluster.UserData);
                    obj.Cluster = null;
                }
            }
            foreach (MyCluster oldCluster in m_clusters)
            {
                if (OnFinishBatch != null)
                    OnFinishBatch(oldCluster.UserData);
            }

            while (m_clusters.Count > 0)
            {
                RemoveCluster(m_clusters[0]);
            }


            for (int i = 0; i < list.Count; i++)
            {
                BoundingBoxD aabb = list[i];
                CreateCluster(ref aabb);
            }

            foreach (var obj in m_objectsData)
            {
                m_clusterTree.OverlapAllBoundingBox(ref obj.Value.AABB, m_returnedClusters);

                if (m_returnedClusters.Count != 1 && !(obj.Value.ActivationHandler.IsStaticForCluster))
                {
                    throw new Exception(String.Format("Inconsistent objects and deserialized clusters. Entity name: {0}, Found clusters: {1}, Replicable exists: {2}", obj.Value.Tag, m_returnedClusters.Count, GetEntityReplicableExistsById(obj.Value.EntityId)));
                }

                if (m_returnedClusters.Count == 1)
                {
                    AddObjectToCluster(m_returnedClusters[0], obj.Key, true);
                }
            }

            foreach (var cluster in m_clusters)
            {
                if (OnFinishBatch != null)
                    OnFinishBatch(cluster.UserData);

                foreach (var ob in cluster.Objects)
                {
                    if (m_objectsData[ob].ActivationHandler != null)
                        m_objectsData[ob].ActivationHandler.FinishAddBatch();
                }
            }

        }
    }
}
