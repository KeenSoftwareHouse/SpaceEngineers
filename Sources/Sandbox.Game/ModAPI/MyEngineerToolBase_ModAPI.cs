using VRage.Game.ModAPI;
using Sandbox.ModAPI.Weapons;

namespace Sandbox.Game.Weapons
{
    public abstract partial class MyEngineerToolBase : IMyEngineerToolBase
    {
      IMyCharacter IMyEngineerToolBase.Owner
      {
         get
         {
            return Owner;
         }
      }
   }
}
