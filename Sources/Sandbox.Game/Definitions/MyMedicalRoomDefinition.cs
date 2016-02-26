using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MedicalRoomDefinition))]
    public class MyMedicalRoomDefinition : MyCubeBlockDefinition
    {
	    public string ResourceSinkGroup;
        public string IdleSound;
        public string ProgressSound;
        public string RespawnSuitName;
        public HashSet<string> CustomWardrobeNames;

        public bool RespawnAllowed;
        public bool HealingAllowed;
        public bool RefuelAllowed;
        public bool SuitChangeAllowed;
        public bool CustomWardrobesEnabled;
        public bool ForceSuitChangeOnRespawn;
        public bool SpawnWithoutOxygenEnabled;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var medicalRoomBuilder = builder as MyObjectBuilder_MedicalRoomDefinition;
            MyDebug.AssertDebug(medicalRoomBuilder != null);
	        ResourceSinkGroup = medicalRoomBuilder.ResourceSinkGroup;
            IdleSound = medicalRoomBuilder.IdleSound;
            ProgressSound = medicalRoomBuilder.ProgressSound;

            RespawnSuitName             = medicalRoomBuilder.RespawnSuitName;
            RespawnAllowed              = medicalRoomBuilder.RespawnAllowed;
            HealingAllowed              = medicalRoomBuilder.HealingAllowed;
            RefuelAllowed               = medicalRoomBuilder.RefuelAllowed;
            SuitChangeAllowed           = medicalRoomBuilder.SuitChangeAllowed;
            CustomWardrobesEnabled      = medicalRoomBuilder.CustomWardrobesEnabled;
            ForceSuitChangeOnRespawn    = medicalRoomBuilder.ForceSuitChangeOnRespawn;
            SpawnWithoutOxygenEnabled   = medicalRoomBuilder.SpawnWithoutOxygenEnabled;

            if(medicalRoomBuilder.CustomWardRobeNames == null)
                CustomWardrobeNames = new HashSet<string>();
            else
                CustomWardrobeNames = new HashSet<string>(medicalRoomBuilder.CustomWardRobeNames);
        }
    }
}
