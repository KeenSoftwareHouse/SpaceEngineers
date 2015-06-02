using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.Game.Entity.UseObject;
using VRageMath;

namespace VRage.Components
{
    public abstract class MyUseObjectsComponentBase : MyComponentBase
    {
        protected Dictionary<string, List<Matrix>> m_detectors = new Dictionary<string, List<Matrix>>();

        public abstract MyPhysicsComponentBase DetectorPhysics { get; }

        public abstract void AddDetector(string name, Matrix matrix);
        public abstract void RecreatePhysics();
        public abstract void LoadDetectorsFromModel();

        public abstract IMyUseObject GetInteractiveObject(int shapeKey);
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

        public virtual void ClearPhysics()
        {
            if (DetectorPhysics != null)
            {
                DetectorPhysics.Close();
            }
        }

        public override void OnRemovedFromContainer(MyComponentContainer container)
        {
            base.OnRemovedFromContainer(container);

            ClearPhysics();
        }
    }
}
