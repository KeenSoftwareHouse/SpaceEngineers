using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AmmoMagazineDefinition))]
    public class MyAmmoMagazineDefinition : MyPhysicalItemDefinition
    {
        public int Capacity;

        public MyAmmoCategoryEnum Category;

        public MyDefinitionId AmmoDefinitionId;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AmmoMagazineDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Capacity = ob.Capacity;
            this.Category = ob.Category;

            if (ob.AmmoDefinitionId != null)
            {
                this.AmmoDefinitionId = new MyDefinitionId(ob.AmmoDefinitionId.Type, ob.AmmoDefinitionId.Subtype);
            }
            else
            {
                this.AmmoDefinitionId = GetAmmoDefinitionIdFromCategory(Category);
            }
        }

        private MyDefinitionId GetAmmoDefinitionIdFromCategory(MyAmmoCategoryEnum category)
        {
            switch (category)
            {
                case MyAmmoCategoryEnum.LargeCaliber:
                    return new MyDefinitionId(typeof(MyObjectBuilder_AmmoDefinition), "LargeCaliber");
                    break;
                case MyAmmoCategoryEnum.SmallCaliber:
                    return new MyDefinitionId(typeof(MyObjectBuilder_AmmoDefinition), "SmallCaliber");
                    break;
                case MyAmmoCategoryEnum.Missile:
                    return new MyDefinitionId(typeof(MyObjectBuilder_AmmoDefinition), "Missile");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
