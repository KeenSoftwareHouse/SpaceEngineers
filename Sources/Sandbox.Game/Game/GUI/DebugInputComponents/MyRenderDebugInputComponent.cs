using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Input;
using VRageRender;

namespace Sandbox.Common
{

    /// <summary>
    /// This debug component can be used to remember debug draws methods, aabbs or matrices to be drawn, even if the event occurs just once
    /// and data can not be retrieved to render them in next frames.
    /// </summary>
    public class MyRenderDebugInputComponent : MyDebugComponent
    {
        /// <summary>
        /// This list can be used to track down specific objects during runtime debug step -> CheckedObjects.Add(this), and change then this later to -> if (CheckedObjects.Contain(this)) Debugger.Break();
        /// </summary>
        static public List<object> CheckedObjects = new List<object>();

        /// <summary>
        /// Subscribe to this event debug draw callbacks for specific objects to be draw independetly
        /// </summary>
        static public event Action OnDraw;

        /// <summary>
        /// Add your AABB and Color to draw it every update when this component is enabled
        /// </summary>
        static public List<Tuple<VRageMath.BoundingBoxD, VRageMath.Color>> AABBsToDraw = new List<Tuple<VRageMath.BoundingBoxD,VRageMath.Color>>();

        /// <summary>
        /// Add your matrix to be draw every update if this component is enabled
        /// </summary>
        static public List<Tuple<VRageMath.Matrix, VRageMath.Color>> MatricesToDraw = new List<Tuple<VRageMath.Matrix,VRageMath.Color>>();

        static public List<Tuple<VRageMath.CapsuleD, VRageMath.Color>> CapsulesToDraw = new List<Tuple<VRageMath.CapsuleD, VRageMath.Color>>();

        static public List<Tuple<VRageMath.Vector3, VRageMath.Vector3, VRageMath.Color>> LinesToDraw = new List<Tuple<VRageMath.Vector3, VRageMath.Vector3, VRageMath.Color>>();

        public MyRenderDebugInputComponent()
        {
            AddShortcut(MyKeys.C, true, true, false, false, () => "Clears the drawed objects", () => ClearObjects());
        }

        private bool ClearObjects()
        {
            Clear();
            return true;
        }

        public override void Draw()
        {
            base.Draw();
            if (OnDraw != null)
            {
                try
                {
                    OnDraw();
                }
                catch (Exception e)
                {
                    OnDraw = null;
                }
            }

            foreach (var entry in AABBsToDraw)
            {
                MyRenderProxy.DebugDrawAABB(entry.Item1, entry.Item2, 1f, 1f, false); 
            }

            foreach (var entry in MatricesToDraw)
            {
                MyRenderProxy.DebugDrawAxis(entry.Item1, 1f, false);
                MyRenderProxy.DebugDrawOBB(entry.Item1, entry.Item2, 1f, false, false);
            }

            foreach (var capsule in CapsulesToDraw)
            {
                MyRenderProxy.DebugDrawCapsule(capsule.Item1.P0, capsule.Item1.P1, capsule.Item1.Radius, capsule.Item2, false, true);
            }

            foreach (var line in LinesToDraw)
            {
                MyRenderProxy.DebugDrawLine3D(line.Item1, line.Item2, line.Item3, line.Item3, false);
            }
        }

        /// <summary>
        /// Clears the lists.
        /// </summary>
        static public void Clear()
        {
            AABBsToDraw.Clear();
            MatricesToDraw.Clear();
            CapsulesToDraw.Clear();
            LinesToDraw.Clear();
            OnDraw = null;
        }

        /// <summary>
        /// Add matrix to be drawn as axes with OBB
        /// </summary>
        /// <param name="mat">World matrix</param>
        /// <param name="col">Color</param>
        static public void AddMatrix(VRageMath.Matrix mat, VRageMath.Color col)
        {
            MatricesToDraw.Add(new Tuple<VRageMath.Matrix, VRageMath.Color>(mat, col));
        }

        /// <summary>
        /// Add AABB box to be drawn
        /// </summary>
        /// <param name="aabb">AABB box in world coords</param>
        /// <param name="col">Color</param>
        static public void AddAABB(VRageMath.BoundingBoxD aabb, VRageMath.Color col)
        {
            AABBsToDraw.Add(new Tuple<VRageMath.BoundingBoxD, VRageMath.Color>(aabb, col));
        }

        static public void AddCapsule(VRageMath.CapsuleD capsule, VRageMath.Color col)
        {
            CapsulesToDraw.Add(new Tuple<VRageMath.CapsuleD, VRageMath.Color>(capsule, col));
        }

        static public void AddLine(VRageMath.Vector3 from, VRageMath.Vector3 to, VRageMath.Color color)
        {
            LinesToDraw.Add(new Tuple<VRageMath.Vector3, VRageMath.Vector3, VRageMath.Color>(from, to, color));
        }

        public override string GetName()
        {
            return "Render";
        }

        /// <summary>
        /// This will break the debugger, if passed object was added to MyRenderDebugInputComponent.CheckedObjects. Use for breaking in the code when you need to break at specific object.
        /// </summary>        
        static public void BreakIfChecked(object objectToCheck)
        {
            if (CheckedObjects.Contains(objectToCheck))
            {
                System.Diagnostics.Debugger.Break();
            }
        }
        
    }
}
