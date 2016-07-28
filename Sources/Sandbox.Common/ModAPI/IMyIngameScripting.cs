using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.ModAPI
{
    public interface IMyIngameScripting
    {
        /// <summary>
        /// Provides the ability for mods to add and remove items from a type and member blacklist,
        /// giving the ability to remove even more API for scripts. Intended for server admins to
        /// restrict what people are able to do with scripts to keep their simspeed up.
        /// </summary>
        IMyScriptBlacklist ScriptBlacklist { get; }

        void Clean();
    }
}
