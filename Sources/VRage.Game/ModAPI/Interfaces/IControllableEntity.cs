using VRage.ModAPI;
using VRageMath;

namespace VRage.Game.ModAPI.Interfaces
{
    public struct MoveInformation
    {
        public Vector3 MoveIndicator;
        public Vector2 RotationIndicator;
        public float RollIndicator;
    }

    public interface IMyControllableEntity
    {
        IMyEntity Entity { get; }

        bool ForceFirstPersonCamera { get; set; }

        MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false);

        void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator);
        void MoveAndRotateStopped();

        void Use();
        void UseContinues();
        void PickUp();
        void PickUpContinues();
        void Jump();
        void SwitchWalk();
        void Up();
        void Crouch();
        void Down();
        void ShowInventory();
        void ShowTerminal();
        void SwitchThrusts();
        void SwitchDamping();
        void SwitchLights();
        void SwitchLeadingGears();
        void SwitchReactors();
        void SwitchHelmet();

        bool EnabledThrusts { get; }
        bool EnabledDamping { get; }
        bool EnabledLights { get; }
        bool EnabledLeadingGears { get; }
        bool EnabledReactors { get; }
        bool EnabledHelmet { get; }

        void DrawHud(IMyCameraController camera, long playerId);

        void Die();

        bool PrimaryLookaround { get; }
    }
}
