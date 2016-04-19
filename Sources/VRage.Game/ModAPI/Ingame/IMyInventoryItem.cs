using VRage.Game;
using VRage.Game;
using VRage.ObjectBuilders;

namespace VRage.Game.ModAPI.Ingame
{
    public static class MyInventoryItemExtension
    {
        public static MyDefinitionId GetDefinitionId(this IMyInventoryItem self)
        {
            var physicalObject = self.Content as MyObjectBuilder_PhysicalObject;
            if (physicalObject != null)
            {
                return physicalObject.GetObjectId();
            }
            else
            {
                return new MyDefinitionId(self.Content.TypeId, self.Content.SubtypeId);
            }
        }
    }

    public interface IMyInventoryItem
    {
        VRage.MyFixedPoint Amount
        {
            get;
            set;
        }

        float Scale
        {
            get;
            set;
        }

        MyObjectBuilder_Base Content
        {
            get;
            set;
        }

        uint ItemId
        {
            get;
            set;
        }
    }
}