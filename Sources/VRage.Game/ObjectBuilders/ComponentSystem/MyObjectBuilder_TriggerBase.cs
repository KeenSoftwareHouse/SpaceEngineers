using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerBase : MyObjectBuilder_ComponentBase
    {
        public int Type = 0;
        public BoundingBoxD AABB;
        public BoundingSphereD BoundingSphere;
        public Vector3D Offset = Vector3D.Zero;
    }
}
