using ProtoBuf;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    // Used in space as projectors but also using this in medieval for projectors as fundation block for blueprint building.
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedievalProjector : MyObjectBuilder_ProjectorBase
    {
        
    }
}
