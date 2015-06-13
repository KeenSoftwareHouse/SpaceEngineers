using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyControllableEntity
    {
        IMyEntity Entity { get; }

        bool ForceFirstPersonCamera { get; set; }

        MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false);

        void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator);
        void MoveAndRotateStopped();

        void Use();
        void UseContinues();
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
