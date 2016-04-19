using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    class MyRenderComponentRope : MyRenderComponent
    {
        public Vector3D WorldPivotA = -Vector3D.One;
        public Vector3D WorldPivotB = Vector3D.One;

        public override object ModelStorage
        {
            get { return null; }
            set { }
        }

        public override void AddRenderObjects()
        {
            MyRopeData data;
            MyRopeComponent.GetRopeData(Container.Entity.EntityId, out data);
            Debug.Assert(data.Definition.ColorMetalTexture != null);
            Debug.Assert(data.Definition.NormalGlossTexture != null);
            Debug.Assert(data.Definition.AddMapsTexture != null);
            SetRenderObjectID(0, MyRenderProxy.CreateLineBasedObject(
                data.Definition.ColorMetalTexture,
                data.Definition.NormalGlossTexture,
                data.Definition.AddMapsTexture));
        }

        public override void SetRenderObjectID(int index, uint ID)
        {
            Debug.Assert(index == 0);
            m_renderObjectIDs[0] = ID;
        }

        public override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
            MyRenderProxy.UpdateLineBasedObject(m_renderObjectIDs[0], WorldPivotA, WorldPivotB);
        }

        public override void ReleaseRenderObjectID(int index)
        {
            MyRenderProxy.RemoveRenderObject(m_renderObjectIDs[0]);
        }

        public override bool IsVisible()
        {
            Debug.Fail("Not implemented.");
            return false;
        }

        public override void Draw() { }
    }

    [MyEntityType(typeof(MyObjectBuilder_Rope))]
    public class MyRope : MyEntity, IMyEventProxy
    {
        public MyRope()
        {
            Render = new MyRenderComponentRope();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            var ob = (MyObjectBuilder_Rope)objectBuilder;
            var ropeDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_RopeDefinition), ob.SubtypeName ?? "BasicRope");
            var ropeDefinition = MyDefinitionManager.Static.GetRopeDefinition(ropeDefinitionId);
            MyRopeComponent.AddRopeData(new MyRopeData
            {
                HookEntityIdA     = ob.EntityIdHookA,
                HookEntityIdB     = ob.EntityIdHookB,
                MaxRopeLength     = ob.MaxRopeLength,
                CurrentRopeLength = ob.CurrentRopeLength,
                Definition        = ropeDefinition,
            }, ob.EntityId);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = (MyObjectBuilder_Rope)base.GetObjectBuilder(copy);
            MyRopeData data;
            MyRopeComponent.GetRopeData(EntityId, out data);
            ob.MaxRopeLength     = data.MaxRopeLength;
            ob.CurrentRopeLength = data.CurrentRopeLength;
            ob.EntityIdHookA     = data.HookEntityIdA;
            ob.EntityIdHookB     = data.HookEntityIdB;
            ob.SubtypeName       = data.Definition.Id.SubtypeName;
            return ob;
        }
    }
}
