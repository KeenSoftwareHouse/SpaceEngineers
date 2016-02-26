using System;

namespace VRage.Game.Entity
{
    /// <summary>
    /// Callbacks to various MyEntities methods.
    /// </summary>
    public class MyEntitiesInterface
    {
        /// <summary>
        /// Register entity for updating.
        /// </summary>
        public static Action<MyEntity> RegisterUpdate;
        /// <summary>
        /// Unregister entity from updating.
        /// </summary>
        public static Action<MyEntity, bool> UnregisterUpdate;

        /// <summary>
        /// Register entity for drawing.
        /// </summary>
        public static Action<MyEntity> RegisterDraw;
        /// <summary>
        /// Unregister entity from drawing.
        /// </summary>
        public static Action<MyEntity> UnregisterDraw;

        /// <summary>
        /// Callback to public static void MyEntities.SetEntityName(MyEntity myEntity, bool possibleRename = true).
        /// </summary>
        public static Action<MyEntity, bool> SetEntityName;

        /// <summary>
        /// Is update of all entities in progress?
        /// </summary>
        public static Func<bool> IsUpdateInProgress;

        /// <summary>
        /// Is closing of objects allowed?
        /// </summary>
        public static Func<bool> IsCloseAllowed;

        public static Action<MyEntity> RemoveName;
        public static Action<MyEntity> RemoveFromClosedEntities;
        public static Action<MyEntity> Remove;
        public static Action<MyEntity> RaiseEntityRemove;
        public static Action<MyEntity> Close;
    }
}
