using VRage.Game.Definitions;

namespace VRage.Game
{
    [MyDefinitionType(typeof(MyObjectBuilder_ComponentDefinitionBase))]
    public class MyComponentDefinitionBase : MyDefinitionBase
    {
        #region Definition init and serialization

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            return base.GetObjectBuilder();
        }

        public override string ToString()
        {
            return string.Format("ComponentDefinitionId={0}", Id.TypeId);
        }

        #endregion
    }
}
