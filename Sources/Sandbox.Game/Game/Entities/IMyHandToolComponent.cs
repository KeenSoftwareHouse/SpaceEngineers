#region Using

using Sandbox.Game.Entities.Character;
using VRage.Game.Entity;
using VRageMath;

#endregion


namespace Sandbox.Game.Entities
{
    public interface IMyHandToolComponent
    {
        bool Hit(MyEntity entity, Vector3D position, Vector3 normal, uint shapeKey, float efficiency);
        void Update();
        void DrawHud();
        void OnControlAcquired(MyCharacter owner);
        void OnControlReleased();
        void Shoot();
        string GetStateForTarget(MyEntity targetEntity);
    }
}
