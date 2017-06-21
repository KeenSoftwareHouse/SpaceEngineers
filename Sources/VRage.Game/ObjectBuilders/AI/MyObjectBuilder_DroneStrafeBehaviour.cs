using VRage.ObjectBuilders;
namespace VRage.Game.ObjectBuilders.AI
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DroneStrafeBehaviour : MyObjectBuilder_AutomaticBehaviour
    {
        public string CurrentPreset = "";
        public bool AlternativebehaviorSwitched = false;
        public SerializableVector3D ReturnPosition = new SerializableVector3D();
        public bool CanSkipWaypoint = true;
    }
}
