using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace VRage.Game
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicsComponentDefinitionBase))]
    public class MyPhysicsComponentDefinitionBase : MyComponentDefinitionBase
    {
        public MyObjectBuilder_PhysicsComponentDefinitionBase.MyMassPropertiesComputationType MassPropertiesComputation;
        public RigidBodyFlag RigidBodyFlags;
        public string CollisionLayer;
        public float? LinearDamping;
        public float? AngularDamping;
        public bool ForceActivate;
        public MyObjectBuilder_PhysicsComponentDefinitionBase.MyUpdateFlags UpdateFlags;
        public bool Serialize;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicsComponentDefinitionBase;
            MassPropertiesComputation = ob.MassPropertiesComputation;
            RigidBodyFlags = ob.RigidBodyFlags;
            CollisionLayer = ob.CollisionLayer;
            LinearDamping = ob.LinearDamping;
            AngularDamping = ob.AngularDamping;
            ForceActivate = ob.ForceActivate;
            UpdateFlags = ob.UpdateFlags;
            Serialize = ob.Serialize;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_PhysicsComponentDefinitionBase;
            ob.MassPropertiesComputation = MassPropertiesComputation;
            ob.RigidBodyFlags = RigidBodyFlags;
            ob.CollisionLayer = CollisionLayer;
            ob.LinearDamping = LinearDamping;
            ob.AngularDamping = AngularDamping;
            ob.ForceActivate = ForceActivate;
            ob.UpdateFlags = UpdateFlags;
            ob.Serialize = Serialize;

            return ob;
        }
    }
}
