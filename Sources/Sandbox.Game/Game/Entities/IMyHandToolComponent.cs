#region Using

using Sandbox.Game.Entities.Character;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

#endregion


namespace Sandbox.Game.Entities
{
    public interface IMyHandToolComponent
    {
        bool Hit(MyEntity entity, MyHitInfo hitInfo, uint shapeKey, float efficiency);
        void Update();
        void DrawHud();
        void OnControlAcquired(MyCharacter owner);
        void OnControlReleased();
        void Shoot();
        string GetStateForTarget(MyEntity targetEntity, uint shapeKey);
    }
}
