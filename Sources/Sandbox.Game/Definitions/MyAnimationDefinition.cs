using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AnimationDefinition))]
    public class MyAnimationDefinition : MyDefinitionBase
    {        
        public enum AnimationStatus
        {
            Unchecked,
            OK,
            Failed
        }

        public string AnimationModel;
        public string AnimationModelFPS;

        public int ClipIndex;
        public string InfluenceArea;
        public bool AllowInCockpit;
        public bool AllowWithWeapon;
        public bool Loop;
        public string[] SupportedSkeletons;
        public AnimationStatus Status = AnimationStatus.Unchecked;
        public MyDefinitionId LeftHandItem;

        public AnimationSet[] AnimationSets = null;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AnimationDefinition;
            MyDebug.AssertDebug(ob != null);

            this.AnimationModel = ob.AnimationModel;
            this.AnimationModelFPS = ob.AnimationModelFPS;

            this.ClipIndex = ob.ClipIndex;
            this.InfluenceArea = ob.InfluenceArea;
            this.AllowInCockpit = ob.AllowInCockpit;
            this.AllowWithWeapon = ob.AllowWithWeapon;
            if (!string.IsNullOrEmpty(ob.SupportedSkeletons))
                SupportedSkeletons = ob.SupportedSkeletons.Split(' ');
            this.Loop = ob.Loop;
            if (!ob.LeftHandItem.TypeId.IsNull)
            {
                this.LeftHandItem = ob.LeftHandItem;
            }

            this.AnimationSets = ob.AnimationSets;
        }
    }
}
