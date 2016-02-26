using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Physics;
using VRageMath;
using VRage.Generics;
using Sandbox.Game.World;
using System.Diagnostics;
using Sandbox.Graphics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Components;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Sensor, detects objects in area defined by physics body.
    /// Using triangle mesh is not recommended, because entities inside mesh (without any colliding triangles) will be considered "not in sensor".
    /// </summary>
    internal class MySensor : MySensorBase
    {
        public void InitPhysics()
        {
            //this.Physics.ContactAddedCallback = ContactAdded;
            //this.Physics.RigidBody.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse | BulletSharp.CollisionFlags.CustomMaterialCallback | CollisionFlags.KinematicObject;
            //this.Physics.RigidBody.ActivationState = ActivationState.DisableDeactivation; // This may not be required
            //this.Physics.RigidBody.Activate(true); // This may not be required
           // this.Physics.Enabled = true;

            //NeedsDraw = true;
        }

       

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
          
            base.Init(objectBuilder);
            }
       

        /// <summary>
        /// Return false when ManifoldPoint was not changed, return true when ManifoldPoint was changed
        /// </summary>
        //void ContactAdded(ManifoldPoint cp, CollisionObjectWrapper colObj0Wrap, int partId0, int index0, CollisionObjectWrapper colObj1Wrap, int partId1, int index1)
        //{
        //    var otherBody = colObj0Wrap.CollisionObject.UserObject == this.Physics ? colObj1Wrap.CollisionObject.UserObject as MyPhysicsBody : colObj0Wrap.CollisionObject.UserObject as MyPhysicsBody;
        //    if (otherBody == null || otherBody.Entity == null)
        //        return;
            
        //    TrackEntity(otherBody.Entity);
        //}

        public MySensor()
        {
            Render = new MyRenderComponentSensor();
    }
    }
}
