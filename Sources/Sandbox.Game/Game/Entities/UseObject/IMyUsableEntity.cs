using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity.UseObject;

namespace Sandbox.Game.Entities.UseObject
{
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
}
