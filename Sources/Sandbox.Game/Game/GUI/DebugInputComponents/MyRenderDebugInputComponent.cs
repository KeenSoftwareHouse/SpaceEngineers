using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        static public List<Tuple<VRageMath.Matrix,VRageMath.Color>> MatricesToDraw = new List<Tuple<VRageMath.Matrix,VRageMath.Color>>();
        
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
        }

        /// <summary>
        /// Clears the lists.
        /// </summary>
        static public void Clear()
        {
            AABBsToDraw.Clear();
            MatricesToDraw.Clear();
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

        public override string GetName()
        {
            return "Render";
        }
    }
}
