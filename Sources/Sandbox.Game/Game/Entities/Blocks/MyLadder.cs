#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using VRageMath;

using Sandbox.Game.Entities.Character;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Definitions;
using Sandbox.Game.Components;
using VRage.Game;
using VRage.Game.Components;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [Obsolete]
    class MyLadder : MyCubeBlock/*, IMyUseObject*/
    {
        #region Fields

        List<MyCharacter> m_charactersOnLadder = new List<MyCharacter>();

        #endregion

        #region Init
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
          
            base.Init(objectBuilder, cubeGrid);

            //var boxshape = new HkBoxShape((LocalAABB.Max - LocalAABB.Min) / 2);
            //var boxshape = new HkBoxShape(new Vector3(0.43f, MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large) / 2.0f, 0.43f));
            //Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE);
            //Physics.CreateFromCollisionObject((HkShape)boxshape, Vector3.Zero, WorldMatrix);
            //Physics.Enabled = true;
            //boxshape.Base.RemoveReference();
            AddDebugRenderComponent(new MyDebugRenderComponentLadder(this));
         
        }

        #endregion

        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public override bool GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            return false;

            //Matrix invWorld = Matrix.Invert(WorldMatrix);
            //Vector3 spherePos = Vector3.Transform(sphere.Center, invWorld);

            //var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(MyObjectBuilderTypeEnum.Ladder));
            //if (definition.ExcludedAreaForCamera != null)
            //    foreach (var b in definition.ExcludedAreaForCamera)
            //    {
            //        if (b.Contains(new BoundingSphere(spherePos, sphere.Radius)) != ContainmentType.Disjoint)
            //            return false;
            //    }

            //return base.GetIntersectionWithSphere(ref sphere);
        }

        //  Calculates intersection of line with any triangleVertexes in this model instance. Closest intersection and intersected triangleVertexes will be returned.
        public override bool GetIntersectionWithLine(ref LineD line, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            t = null;
            return false;

            //Matrix invWorld = Matrix.Invert(WorldMatrix);
            //Vector3 from = Vector3.Transform(line.From, invWorld);
            //Vector3 to = Vector3.Transform(line.To, invWorld);

            //Line lineLocal = new Line(from, to);

            //bool res = base.GetIntersectionWithLine(ref line, out t, flags);

            //if (res)
            //{
            //    var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(MyObjectBuilderTypeEnum.Ladder));
            //    if (definition.ExcludedAreaForCamera != null)
            //    {
            //        foreach (var b in definition.ExcludedAreaForCamera)
            //        {
            //            if (b.Contains(t.Value.IntersectionPointInObjectSpace) == ContainmentType.Contains)
            //            {
            //                t = null;
            //                return false;
            //            }
            //        }
            //    }
            //}

            //return res;
        }

      

        #region UseObject
        //float IMyUseObject.InteractiveDistance
        //{
        //    get { return 3.0f; }
        //}

        //MatrixD IMyUseObject.ActivationMatrix
        //{
        //    get { return DummyActivationMatrix * WorldMatrix; }
        //}

        //MatrixD IMyUseObject.WorldMatrix
        //{
        //    get { return WorldMatrix; }
        //}

        //int IMyUseObject.RenderObjectID
        //{
        //    get
        //    {
        //        if (Render.RenderObjectIDs.Length > 0)
        //            return (int)Render.RenderObjectIDs[0];
        //        return -1;
        //    }
        //}

        //bool IMyUseObject.ShowOverlay
        //{
        //    get { return true; }
        //}

        //UseActionEnum IMyUseObject.SupportedActions
        //{
        //    get { return UseActionEnum.Manipulate; }
        //}

        //void IMyUseObject.Use(UseActionEnum actionEnum, MyCharacter user)
        //{
        //    user.GetOnLadder(this);
        //}

        //MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        //{
        //    return new MyActionDescription()
        //    {
        //        //Text = "Press " + MyInput.Static.GetGameControl(MyGameControlEnums.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard) + " to enter ladder",
        //        Text =  MySpaceTexts.Blank,
        //        IsTextControlHint = true,
        //    };
        //}

        //bool IMyUseObject.ContinuousUsage
        //{
        //    get { return false; }
        //}

        #endregion
    }
}
