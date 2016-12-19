using System.Collections.Generic;
using VRage.Collections;
using VRage.Game.Entity.UseObject;
using VRageMath;

namespace VRage.Game.Components
{
    [MyComponentType(typeof(MyUseObjectsComponentBase))]
    public abstract class MyUseObjectsComponentBase : MyEntityComponentBase
    {
        protected Dictionary<string, List<Matrix>> m_detectors = new Dictionary<string, List<Matrix>>();

        public abstract MyPhysicsComponentBase DetectorPhysics { get; protected set; }

        public abstract uint AddDetector(string name, Matrix matrix);
        public abstract void RemoveDetector(uint id);
        public abstract void RecreatePhysics();
        public abstract void LoadDetectorsFromModel();

        public abstract IMyUseObject GetInteractiveObject(uint shapeKey);
        public abstract IMyUseObject GetInteractiveObject(string detectorName);
        public abstract void GetInteractiveObjects<T>(List<T> objects)
            where T : class, IMyUseObject;

        public string RaycastDetectors(Vector3 worldFrom, Vector3 worldTo)
        {
            var positionComp = Container.Get<MyPositionComponentBase>();
            var invWorld = positionComp.WorldMatrixNormalizedInv;
            var from = Vector3.Transform(worldFrom, invWorld);
            var to = Vector3.Transform(worldTo, invWorld);

            BoundingBox unit = new BoundingBox(-Vector3.One, Vector3.One);

            Vector3 localFrom, localTo;

            string result = null;
            float distance = float.MaxValue;

            foreach (var group in m_detectors)
            {
                foreach (var det in group.Value)
                {
                    localFrom = Vector3.Transform(from, det);
                    localTo = Vector3.Transform(to, det);

                    float? dist = unit.Intersects(new Ray(localFrom, localTo));
                    if (dist.HasValue && dist.Value < distance)
                    {
                        distance = dist.Value;
                        result = group.Key;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determine whether the given ray intersects any detector. If so, returns
        /// the parametric value of the point of first intersection.
        /// PARAMATER IS NOT DISTANCE!
        /// </summary>
        public abstract IMyUseObject RaycastDetectors(Vector3D worldFrom, Vector3D worldTo, out float parameter);

        public ListReader<Matrix> GetDetectors(string detectorName)
        {
            List<Matrix> detectorList = null;
            m_detectors.TryGetValue(detectorName, out detectorList);

            if (detectorList == null || detectorList.Count == 0)
            {
                return ListReader<Matrix>.Empty;
            }

            return new ListReader<Matrix>(detectorList);
        }

        public virtual void ClearPhysics()
        {
            if (DetectorPhysics != null)
            {
                DetectorPhysics.Close();
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            ClearPhysics();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            if (DetectorPhysics != null)
            {
                DetectorPhysics.Activate();
            }
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            if (DetectorPhysics != null)
            {
                DetectorPhysics.Deactivate();
            }
        }
        public abstract void PositionChanged(MyPositionComponentBase obj);

        public override string ComponentTypeDebugString
        {
            get { return "Use Objects"; }
        }
    }
}
