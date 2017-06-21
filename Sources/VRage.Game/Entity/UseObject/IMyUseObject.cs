using System;
using VRage.Import;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;

namespace VRage.Game.Entity.UseObject
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
        PickUp = 1 << 5,
    }

    public enum UseActionResult
    {
        OK,
        UsedBySomeoneElse,
        AccessDenied,
        Closed,
        Unpowered,
        CockpitDamaged
    }

    public struct MyActionDescription
    {
        public MyStringId Text;
        public object[] FormatParams;
        public bool IsTextControlHint;
        public MyStringId? JoystickText;
        public object[] JoystickFormatParams;
    }

    public interface IMyUseObject
    {
        IMyEntity Owner { get; }

        MyModelDummy Dummy { get; }

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
        /// Instance ID of objects (this should mostly be unused
        /// </summary>
        int InstanceID { get; }

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
        void Use(UseActionEnum actionEnum, IMyEntity user);

        /// <summary>
        /// Gets action text
        /// Caller calls this method only on supported actions
        /// </summary>
        MyActionDescription GetActionInfo(UseActionEnum actionEnum);

        bool HandleInput();

        void OnSelectionLost();

        void SetRenderID(uint id);

        void SetInstanceID(int id);

        bool PlayIndicatorSound { get; }
    }

    public static class UseObjectExtensions
    {
        public static bool IsActionSupported(this IMyUseObject useObject, UseActionEnum action)
        {
            return (useObject.SupportedActions & action) == action;
        }
    }
}
