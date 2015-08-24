using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;

namespace Sandbox.Game.Entities
{
    public interface IMyHandheldGunObject<out T> : IMyGunObject<T> where T : MyDeviceBase
    {
        MyObjectBuilder_PhysicalGunObject PhysicalObject { get; }
        MyPhysicalItemDefinition PhysicalItemDefinition { get; }

        bool IsBlocking { get; }
    }
}