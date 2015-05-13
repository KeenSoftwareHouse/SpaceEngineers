using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using System;
using VRageMath;

using VRage.Utils;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.Entities
{
    [Flags]
    public enum UseActionEnum
    {
        None = 0,
        Manipulate   = 1 << 0,
        OpenTerminal = 1 << 1,
        OpenInventory = 1 << 2,
        UseFinished = 1 << 3,           // Finished of using "USE" key (key released)
        Close = 1 << 4,               // Use object is closing (called before). Ie. character just got out of sight of interactive object
    }

    public enum UseActionResult
    {
        OK,
        UsedBySomeoneElse,
        AccessDenied,
        Closed,
        Unpowered
    }

    public struct MyActionDescription
    {
        public MyStringId Text;
        public object[] FormatParams;
        public bool IsTextControlHint;
        public MyStringId? JoystickText;
        public object[] JoystickFormatParams;
    }

    /// <summary>
    /// Simple interface for entities so they don't have to implement IMyUseObject.
    /// </summary>
    public interface IMyUsableEntity
    {
        /// <summary>
        /// Test use on server and based on results sends success or failure
        /// </summary>
        UseActionResult CanUse(UseActionEnum actionEnum, IMyControllableEntity user);

        void RemoveUsers(bool local);
    }

    public interface IMyUseObject
    {
        /// <summary>
        /// Consider object as being in interactive range only if distance from character is smaller or equal to this value
        /// </summary>
        float InteractiveDistance { get; }

        /// <summary>
        /// Matrix of object, scale represents size of object
        /// </summary>
        MatrixD ActivationMatrix { get; }

        /// <summary>
        /// Matrix of object, scale represents size of object
        /// </summary>
        MatrixD WorldMatrix { get; }

        /// <summary>
        /// Render ID of objects 
        /// </summary>
        int RenderObjectID { get; }

        /// <summary>
        /// Show overlay (semitransparent bounding box)
        /// </summary>
        bool ShowOverlay { get; }

        /// <summary>
        /// Returns supported actions
        /// </summary>
        UseActionEnum SupportedActions { get; }

        /// <summary>
        /// When true, use will be called every frame
        /// </summary>
        bool ContinuousUsage { get; }

        /// <summary>
        /// Uses object by specified action
        /// Caller calls this method only on supported actions
        /// </summary>
        void Use(UseActionEnum actionEnum, MyCharacter user);

        /// <summary>
        /// Gets action text
        /// Caller calls this method only on supported actions
        /// </summary>
        MyActionDescription GetActionInfo(UseActionEnum actionEnum);

        bool HandleInput();

        void OnSelectionLost();
    }

    static class UseObjectExtensions
    {
        public static bool IsActionSupported(this IMyUseObject useObject, UseActionEnum action)
        {
            return (useObject.SupportedActions & action) == action;
        }
    }
}
