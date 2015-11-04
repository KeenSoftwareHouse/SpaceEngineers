using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using VRage.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    /// <summary>
    /// Fracture component adds fractures to entities. The component replaces renderer so the entity is responsible to restore it to original when this component is removed and original state is needed (repaired blocks).
    /// </summary>
    public abstract class MyFractureComponentBase : MyEntityComponentBase
    {
        protected static readonly List<HkdShapeInstanceInfo> m_tmpChildren = new List<HkdShapeInstanceInfo>();
        protected static readonly List<HkdShapeInstanceInfo> m_tmpShapeInfos = new List<HkdShapeInstanceInfo>();
        protected static readonly List<MyObjectBuilder_FractureComponentBase.FracturedShape> m_tmpShapeList = new List<MyObjectBuilder_FractureComponentBase.FracturedShape>();

        public HkdBreakableShape Shape;

        public abstract MyPhysicalModelDefinition PhysicalModelDefinition { get; }

        public struct Info
        {
            public MyEntity Entity;
            public HkdBreakableShape Shape;
            public bool Compound;
        }


        public override bool IsSerialized()
        {
            return true;
        }

        public override string ComponentTypeDebugString
        {
            get { return "Fracture"; }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            var newRender = new MyRenderComponentFracturedPiece();
            if (Entity.Render.ModelStorage != null)
                newRender.ModelStorage = Entity.Render.ModelStorage;

            Entity.Render.UpdateRenderObject(false);
            var persistentFlags = Entity.Render.PersistentFlags;
            var colorMaskHsv = Entity.Render.ColorMaskHsv;

            Entity.Render = newRender;
            Entity.Render.NeedsDraw = true;
            Entity.Render.PersistentFlags |= persistentFlags | MyPersistentEntityFlags2.CastShadows;
            Entity.Render.ColorMaskHsv = colorMaskHsv;
            Entity.Render.EnableColorMaskHsv = false;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            // Note that entity has the render MyRenderComponentFracturedPiece!

            if (Shape.IsValid())
                Shape.RemoveReference();
        }

        public void RemoveChildShapes(List<string> shapeNames)
        {
            RemoveChildShapes(shapeNames.GetInternalArray());
        }

        public virtual void RemoveChildShapes(string[] shapeNames)
        {
            Debug.Assert(m_tmpShapeList.Count == 0);
            m_tmpShapeList.Clear();

            GetCurrentFracturedShapeList(m_tmpShapeList, shapeNames);
            RecreateShape(m_tmpShapeList);

            m_tmpShapeList.Clear();
        }

        protected void GetCurrentFracturedShapeList(List<MyObjectBuilder_FractureComponentBase.FracturedShape> shapeList, string[] excludeShapeNames = null)
        {
            Debug.Assert(shapeList.Count == 0);
            GetCurrentFracturedShapeList(Shape, shapeList, excludeShapeNames: excludeShapeNames);
        }

        private bool GetCurrentFracturedShapeList(HkdBreakableShape breakableShape, List<MyObjectBuilder_FractureComponentBase.FracturedShape> shapeList, string[] excludeShapeNames = null)
        {
            var shapeName = breakableShape.Name;
            bool shapeNameEmpty = string.IsNullOrEmpty(shapeName);

            if (excludeShapeNames != null && !shapeNameEmpty)
            {
                foreach (var shapeNameToRemove in excludeShapeNames)
                {
                    if (shapeName == shapeNameToRemove)
                        return false;
                }
            }

            if (breakableShape.GetChildrenCount() > 0)
            {
                List<HkdShapeInstanceInfo> shapeInst = new List<HkdShapeInstanceInfo>();
                breakableShape.GetChildren(shapeInst);

                bool allChildrenAdded = true;
                foreach (var inst in shapeInst)
                {
                    allChildrenAdded &= GetCurrentFracturedShapeList(inst.Shape, shapeList, excludeShapeNames: excludeShapeNames);
                }

                if (!shapeNameEmpty && allChildrenAdded)
                {
                    foreach (var inst in shapeInst)
                    {
                        shapeList.RemoveAll(s => s.Name == inst.ShapeName);
                    }

                    shapeList.Add(new MyObjectBuilder_FractureComponentBase.FracturedShape() { Name = shapeName, Fixed = breakableShape.IsFixed() });
                }

                return allChildrenAdded;
            }
            else
            {
                if (!shapeNameEmpty)
                {
                    shapeList.Add(new MyObjectBuilder_FractureComponentBase.FracturedShape() { Name = shapeName, Fixed = breakableShape.IsFixed() });
                    return true;
                }
            }

            return false;
        }

        protected abstract void RecreateShape(List<MyObjectBuilder_FractureComponentBase.FracturedShape> shapeList);

        protected void SerializeInternal(MyObjectBuilder_FractureComponentBase ob)
        {
            Debug.Assert(m_tmpChildren.Count == 0);

            if (string.IsNullOrEmpty(Shape.Name) || Shape.IsCompound() || Shape.GetChildrenCount() > 0)
            {
                Shape.GetChildren(m_tmpChildren);
                foreach (var child in m_tmpChildren)
                {
                    var shape = new MyObjectBuilder_FractureComponentCubeBlock.FracturedShape()
                    {
                        Name = child.ShapeName,
                        Fixed = MyDestructionHelper.IsFixed(child.Shape)
                    };
                    ob.Shapes.Add(shape);
                }
                m_tmpChildren.Clear();
            }
            else
            {
                ob.Shapes.Add(new MyObjectBuilder_FractureComponentCubeBlock.FracturedShape() { Name = Shape.Name });
            }
        }

        public virtual void SetShape(HkdBreakableShape shape, bool compound)
        {
            Debug.Assert(shape.IsValid());

            if (Shape.IsValid())
                Shape.RemoveReference();

            Shape = shape;

            var render = Entity.Render as MyRenderComponentFracturedPiece;
            Debug.Assert(render != null);

            if (render != null)
            {
                render.ClearModels();

                if (compound)
                {
                    Debug.Assert(m_tmpChildren.Count == 0);

                    shape.GetChildren(m_tmpChildren);

                    foreach (var shapeInstanceInfo in m_tmpChildren)
                    {
                        System.Diagnostics.Debug.Assert(shapeInstanceInfo.IsValid(), "Invalid shapeInstanceInfo!");
                        if (shapeInstanceInfo.IsValid())
                        {
                            render.AddPiece(shapeInstanceInfo.ShapeName, Matrix.Identity);
                        }
                    }

                    m_tmpChildren.Clear();
                }
                else
                {
                    render.AddPiece(shape.Name, Matrix.Identity);
                }

                render.UpdateRenderObject(true);
            }
        }
    }
}
