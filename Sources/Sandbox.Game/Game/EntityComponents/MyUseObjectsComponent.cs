﻿using System;
using System.Collections.Generic;
using Havok;
using Sandbox.Common.Components;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Components;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRageMath;
using System.Diagnostics;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_UseObjectsComponent))]
    public class MyUseObjectsComponent : MyUseObjectsComponentBase
    {
        private struct DetectorData
        {
            public IMyUseObject UseObject;
            public Matrix Matrix;
            public string DetectorName;

            public DetectorData(IMyUseObject useObject, Matrix mat, string name)
            {
                UseObject = useObject;
                Matrix = mat;
                DetectorName = name;
            }
        }

        static readonly Vector3[] m_detectorVertices = new Vector3[BoundingBox.CornerCount];
        static readonly List<HkShape> m_shapes = new List<HkShape>();
        
        private Dictionary<uint, DetectorData> m_detectorInteractiveObjects = new Dictionary<uint, DetectorData>();
        private List<uint> m_customAddedDetectors = new List<uint>();

        private MyPhysicsBody m_detectorPhysics;
        private MyObjectBuilder_UseObjectsComponent m_objectBuilder = null;
        public override MyPhysicsComponentBase DetectorPhysics
        {
            get { return m_detectorPhysics; }
            protected set { m_detectorPhysics = value as MyPhysicsBody; }
        }

        public override void LoadDetectorsFromModel()
        {
            m_detectors.Clear();
            m_detectorInteractiveObjects.Clear();

            if (m_detectorPhysics != null)
            {
                m_detectorPhysics.Close();
            }

            var renderComponent = Container.Get<MyRenderComponentBase>();

            if (renderComponent.GetModel() != null)
            {
                foreach (var dummy in renderComponent.GetModel().Dummies)
                {
                    var dummyLowerCaseKey = dummy.Key.ToLower();
                    const string DETECTOR_PREFIX = "detector_";
                    if (dummyLowerCaseKey.StartsWith(DETECTOR_PREFIX) && dummyLowerCaseKey.Length > DETECTOR_PREFIX.Length)
                    {
                        String[] parts = dummyLowerCaseKey.Split('_');
                        if (parts.Length < 2)
                            continue;

                        var dummyData = dummy.Value;
                        AddDetector(parts[1], dummyLowerCaseKey, dummyData);
                    }
                }
            }

            if (m_detectorInteractiveObjects.Count > 0)
                RecreatePhysics();
            /*
            var inventoryBlock = this as IMyInventoryOwner;
            if (refreshNetworks && inventoryBlock != null)
            {
                CubeGrid.ConveyorSystem.Remove(inventoryBlock);
                CubeGrid.ConveyorSystem.Add(inventoryBlock);
            }*/
        }

        private IMyUseObject CreateInteractiveObject(string detectorName, string dummyName, MyModelDummy dummyData, uint shapeKey)
        {
            // temporary hack until dummy for door terminal is renamed
            if (Container.Entity is MyDoor && detectorName == "terminal")
                return new MyUseObjectDoorTerminal(Container.Entity, dummyName, dummyData, shapeKey);

            return MyUseObjectFactory.CreateUseObject(detectorName, Container.Entity, dummyName, dummyData, shapeKey);
        }

        private uint AddDetector(string detectorName, string dummyName, MyModelDummy dummyData)
        {
            List<Matrix> matrices;
            if (!m_detectors.TryGetValue(detectorName, out matrices))
            {
                matrices = new List<Matrix>();
                m_detectors[detectorName] = matrices;
            }
            matrices.Add(Matrix.Invert(dummyData.Matrix));

            var shapeKey = (uint)m_detectorInteractiveObjects.Count;
            var interactiveObject = CreateInteractiveObject(detectorName, dummyName, dummyData, shapeKey);
            if (interactiveObject != null)
            {
                m_detectorInteractiveObjects.Add(shapeKey, new DetectorData(interactiveObject, dummyData.Matrix, detectorName));
            }

            return shapeKey;
        }

        public override void RemoveDetector(uint id)
        {
            if (!m_detectorInteractiveObjects.ContainsKey(id))
            {
                Debug.Assert(false, "Use object key not found");
            }
            else
            {
                m_detectorInteractiveObjects.Remove(id);
            }
        }

        public override uint AddDetector(string name, Matrix dummyMatrix)
        {
            var detectorName = name.ToLower();
            var dummyName = "detector_" + detectorName;
            MyModelDummy modelDummy = new MyModelDummy() { CustomData = null, Matrix = dummyMatrix };
            var detector = AddDetector(detectorName, dummyName, modelDummy);
            m_customAddedDetectors.Add(detector);
            return detector;
        }

        public override void RecreatePhysics()
        {
            if (m_detectorPhysics != null)
            {
                m_detectorPhysics.Close();
                m_detectorPhysics = null;
            }

            m_shapes.Clear();

            BoundingBox aabb = new BoundingBox(-Vector3.One / 2, Vector3.One / 2);
            var positionComponent = Container.Get<MyPositionComponentBase>();

            foreach (var pair in m_detectorInteractiveObjects)
            {
                unsafe
                {
                    fixed (Vector3* corner = m_detectorVertices)
                        aabb.GetCornersUnsafe(corner);
                }
                for (int i = 0; i < BoundingBox.CornerCount; i++)
                {
                    m_detectorVertices[i] = Vector3.Transform(m_detectorVertices[i], pair.Value.Matrix);
                }

                m_shapes.Add(new HkConvexVerticesShape(m_detectorVertices, BoundingBox.CornerCount, false, 0));
            }

            if (m_shapes.Count > 0)
            { 
                var listShape = new HkListShape(m_shapes.GetInternalArray(), m_shapes.Count, HkReferencePolicy.TakeOwnership);
                m_detectorPhysics = new MyPhysicsBody(Container.Entity, RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE);
                m_detectorPhysics.CreateFromCollisionObject((HkShape)listShape, Vector3.Zero, positionComponent.WorldMatrix);
                //m_detectorPhysics.Enabled = true;
                listShape.Base.RemoveReference();

                //positionComponent.OnPositionChanged += positionComponent_OnPositionChanged;
            }

        }

        public override void PositionChanged(MyPositionComponentBase obj)
        {
            if (m_detectorPhysics != null)
                m_detectorPhysics.OnWorldPositionChanged(obj);
        }

        void positionComponent_OnPositionChanged(MyPositionComponentBase obj)
        {
            m_detectorPhysics.OnWorldPositionChanged(obj);
        }

        public override IMyUseObject GetInteractiveObject(uint shapeKey)
        {
            DetectorData result;
            if (!m_detectorInteractiveObjects.TryGetValue(shapeKey, out result))
            {
                return null;
            }
            return result.UseObject;
        }

        public override void GetInteractiveObjects<T>(List<T> objects)
        {
            foreach (var obj in m_detectorInteractiveObjects)
            {
                T typeObj = obj.Value.UseObject as T;
                if (typeObj != null)
                    objects.Add(typeObj);
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            
            var positionComponent = Container.Get<MyPositionComponentBase>();
            if (positionComponent != null)
            {
                //positionComponent.OnPositionChanged -= positionComponent_OnPositionChanged;
            }
        }

        public override bool IsSerialized()
        {
            return m_customAddedDetectors.Count > 0;
        }

        public override Common.ObjectBuilders.ComponentSystem.MyObjectBuilder_ComponentBase Serialize()
        {
            var builder = MyComponentFactory.CreateObjectBuilder(this) as MyObjectBuilder_UseObjectsComponent;
            builder.CustomDetectorsCount = (uint)m_customAddedDetectors.Count;

            int i = 0;
            if (builder.CustomDetectorsCount > 0)
            {
                builder.CustomDetectorsMatrices = new Matrix[builder.CustomDetectorsCount];
                builder.CustomDetectorsNames = new string[builder.CustomDetectorsCount];
                foreach (var detector in m_customAddedDetectors)
                {
                    builder.CustomDetectorsNames[i] = m_detectorInteractiveObjects[detector].DetectorName;
                    builder.CustomDetectorsMatrices[i] = m_detectorInteractiveObjects[detector].Matrix;
                    i++;
                }
            }

            return builder;
        }

        public override void Deserialize(Common.ObjectBuilders.ComponentSystem.MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            m_objectBuilder = builder as MyObjectBuilder_UseObjectsComponent;            
        }
        
        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            if (m_objectBuilder != null)
            {
                for (int i = 0; i < m_objectBuilder.CustomDetectorsCount; ++i)
                {
                    if (!m_detectors.ContainsKey(m_objectBuilder.CustomDetectorsNames[i]))
                    {
                        AddDetector(m_objectBuilder.CustomDetectorsNames[i], m_objectBuilder.CustomDetectorsMatrices[i]);
                    }
                }
                RecreatePhysics();
            }
        }
    }
}
