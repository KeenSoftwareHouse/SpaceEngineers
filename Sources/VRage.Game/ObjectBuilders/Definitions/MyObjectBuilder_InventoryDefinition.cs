using ProtoBuf;

namespace VRage.Game
{
    // OM: TODO: remove when inventories are set through entity containers
    // CH: TODO: This is really ugly, but the reason it's here is that we cannot define the aggregate inventory in any other way now.
    [ProtoContract]    
    public class MyObjectBuilder_InventoryDefinition 
    {
        [ProtoMember]
        public float InventoryVolume = float.MaxValue;

        [ProtoMember]
        public float InventoryMass = float.MaxValue;

        [ProtoMember]
        public float InventorySizeX = 1.2f;

        [ProtoMember]
        public float InventorySizeY = 0.7f;

        [ProtoMember]
        public float InventorySizeZ = 0.4f;

        [ProtoMember]
        public int MaxItemCount = int.MaxValue;
    }
}
