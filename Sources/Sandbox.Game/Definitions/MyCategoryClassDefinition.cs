using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GuiBlockCategoryDefinition))]
    public class MyGuiBlockCategoryDefinition : MyDefinitionBase
    {
        public string Name;
        public HashSet<string> ItemIds;
        public bool IsShipCategory = false;
        public bool IsBlockCategory = true;
        public bool SearchBlocks = true;
        public bool ShowAnimations = false;
        public bool ShowInCreative = true;
        public bool IsAnimationCategory = false;
        public bool IsToolCategory = false;
        public int ValidItems = 0;
        public bool Public = true;

        private class SubtypeComparer : IComparer<MyGuiBlockCategoryDefinition>
        {
            public static SubtypeComparer Static = new SubtypeComparer();

            public int Compare(MyGuiBlockCategoryDefinition x, MyGuiBlockCategoryDefinition y)
            {
                return x.Id.SubtypeName.CompareTo(y.Id.SubtypeName);
            }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            MyObjectBuilder_GuiBlockCategoryDefinition builder = (ob as MyObjectBuilder_GuiBlockCategoryDefinition);
            this.Name = builder.Name;
            this.ItemIds = new HashSet<string>(builder.ItemIds.ToList());
            this.IsBlockCategory = builder.IsBlockCategory;
            this.IsShipCategory = builder.IsShipCategory;
            this.SearchBlocks = builder.SearchBlocks;
            this.ShowAnimations = builder.ShowAnimations;
            this.ShowInCreative = builder.ShowInCreative;
            this.Public = builder.Public;
            this.IsAnimationCategory = builder.IsAnimationCategory;
            this.IsToolCategory = builder.IsToolCategory;
        }

        public bool HasItem(string itemId)
        {
            foreach (var category in ItemIds)
            {
                if (itemId.EndsWith(category))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
