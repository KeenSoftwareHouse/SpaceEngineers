using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.ModAPI
{
    public delegate void CharacterMovementStateDelegate(MyCharacterMovementEnum oldState, MyCharacterMovementEnum newState);

    public interface IMyCharacter
    {
        float BaseMass { get; }

        float CurrentMass { get; }

        event CharacterMovementStateDelegate OnMovementStateChanged;

        void Kill(object killData = null);

        /// <summary>
        /// Character's name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Character's current health.
        /// </summary>
        float Health { get; }

        /// <summary>
        /// Character's max reachable health (from the suit model definition).
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Sets the character's health, clamped between 0 and MaxHealth.
        /// NOTE: This does not kill the character if health is set to 0.
        /// </summary>
        /// <param name="health">the health to set, limited between 0 and MaxHealth.</param>
        /// <param name="sync">updates the server and other players with the change</param>
        void SetHealth(float health, bool sync = true);

        /// <summary>
        /// The total damage that this character received throughout their life.
        /// </summary>
        float AccumulatedDamage { get; }

        /// <summary>
        /// If the character is dead.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// Suit battery percent, 0 to 1.
        /// </summary>
        float BatteryLevel { get; }

        /// <summary>
        /// Suit's battery interface.
        /// </summary>
        IMySuitBattery Battery { get; }

        /// <summary>
        /// Suit oxygen percent, 0 to 1.
        /// </summary>
        float SuitOxygenLevel { get; }

        /// <summary>
        /// Suit oxygen amount.
        /// </summary>
        float SuitOxygen { get; }

        /// <summary>
        /// Suit total oxygen capacity.
        /// </summary>
        float SuitMaxOxygen { get; }

        /// <summary>
        /// Set the character's oxygen levels.
        /// </summary>
        /// <param name="level">from 0.0 to 1.0.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetOxygenLevel(float level, bool sync = true);

        /// <summary>
        /// Oxygen levels around the character
        /// </summary>
        float EnvironmentOxygenLevel { get; }

        /// <summary>
        /// If the current suit requires environment oxygen.
        /// Can also be used to determine if the character has his helmet on or not.
        /// </summary>
        bool SuitNeedsOxygen { get; }

        /// <summary>
        /// Character's current movement action.
        /// </summary>
        MyCharacterMovementEnum CurrentMovement { get; }

        bool CanJump { get; }
        bool CanFly { get; }
        bool CanDie { get; }
        bool IsSitting { get; }
        bool IsCrouching { get; }
        bool IsWalking { get; }
        bool IsSprinting { get; }
        bool IsJumping { get; }
        bool IsFalling { get; }
        bool IsFlying { get; }
        bool IsAimingDownSights { get; }
        float CurrentSpeed { get; }
        float CurrentJump { get; }
        
        /// <summary>
        /// If the character's lights are on or not.
        /// </summary>
        bool LightEnabled { get; }

        /// <summary>
        /// Toggle the character's lights
        /// </summary>
        /// <param name="enable">set the state.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetLights(bool enable, bool sync = true);

        /// <summary>
        /// If the character's jetpack is on or not.
        /// </summary>
        bool JetpackEnabled { get; }

        /// <summary>
        /// Toggle the character's jetpack.
        /// </summary>
        /// <param name="enable">set the state.</param>
        /// <param name="notify">show the change notification on the HUD.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetJetpack(bool enable, bool notify = true, bool sync = true);

        /// <summary>
        /// If the character's jetpack has dampeners on or not.
        /// </summary>
        bool DampenersEnabled { get; }

        /// <summary>
        /// Toggle the character's jetpack dampeners.
        /// </summary>
        /// <param name="enable">set the state.</param>
        /// <param name="notify">show the change notification on the HUD.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetDampeners(bool enable, bool notify = true, bool sync = true);

        /// <summary>
        /// If the character has radio broadcasting enabled.
        /// </summary>
        bool BroadcastingEnabled { get; }

        /// <summary>
        /// Character's broadcasting radius.
        /// </summary>
        float BroadcastingRadius { get; }

        /// <summary>
        /// Toggle the character's radio broadcasting.
        /// </summary>
        /// <param name="enable">set the state.</param>
        /// <param name="notify">show the change notification on the HUD.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetBroadcasting(bool enable, bool notify = true, bool sync = true);

        /// <summary>
        /// If the character has their HUD hidden.
        /// </summary>
        bool MinimalHud { get; }

        /// <summary>
        /// The artificial gravity that the player is in.
        /// </summary>
        Vector3 ArtificialGravity { get; }

        Sandbox.Common.ObjectBuilders.MyToolbarType ToolbarType { get; }

        /// <summary>
        /// Character's suit model name.
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Character's suit color, in HSV format.
        /// </summary>
        Vector3 SuitColorHSV { get; }

        /// <summary>
        /// Changes the model and color of the character.
        /// </summary>
        /// <param name="model">the model name.</param>
        /// <param name="colorMaskHSV">the model color mask.</param>
        /// <param name="sync">send changes over the network to the server and other players.</param>
        void SetModelAndColor(string model, Vector3 colorMaskHSV, bool sync = true);

        /// <summary>
        /// Get the character's faction or null if they have no faction.
        /// </summary>
        /// <returns>faction or null.</returns>
        IMyFaction GetFaction();

        /// <summary>
        /// Get the entity this character is occupying (cockpits, seats, cryo chambers, etc), null if none.
        /// </summary>
        IMyEntity UsingEntity { get; }

        /// <summary>
        /// The entity this character remotely controls right now, null otherwise.
        /// </summary>
        IMyControllableEntity RemoteControlledEntity { get; }
    }
}
