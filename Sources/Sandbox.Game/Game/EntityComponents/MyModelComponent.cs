using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using ModelChangedParams = VRage.Game.Components.MyEntityContainerEventExtensions.ModelChangedParams;
using Sandbox.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRageMath;
using VRage.Game.Models;

namespace Sandbox.Game.EntityComponents
{
    [MyComponentType(typeof(MyModelComponent))]
    [MyComponentBuilder(typeof(MyObjectBuilder_ModelComponent))]
    public class MyModelComponent : MyEntityComponentBase
    {
        public static MyStringHash ModelChanged = MyStringHash.GetOrCompute("ModelChanged");

        public MyModelComponentDefinition Definition { get; private set; }

        public MyModel Model
        {
            // TODO: model from entity is referenced now, will be instanced here when entity will be refactored as components container 
            get { return Entity != null ? (Entity as MyEntity).Model : null; }
        }

        public MyModel ModelCollision
        {
            // TODO: model from entity is referenced now, will be instanced here when entity will be refactored as components container 
            get { return Entity != null ? (Entity as MyEntity).ModelCollision : null; }
        }

        public override string ComponentTypeDebugString
        {
            get { return String.Format("Model Component {0}", Definition != null ? Definition.Model : "invalid"); }
        }


        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            Definition = definition as MyModelComponentDefinition;
            Debug.Assert(Definition != null, "Passed null definition or wrong type!");
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            
            // TODO : Later all entity properties should be contained only in containers. For compatibility we must init entity.. 
            InitEntity();

            Debug.Assert(Definition != null);
            if (Definition != null)
                this.RaiseEntityEvent(ModelChanged, new ModelChangedParams(Definition.Model, Definition.Size, Definition.Mass, Definition.Volume, Definition.DisplayNameText,
                    Definition.Icons));
        }

        /// <summary>
        /// This calls Refresh Models on Entity, this should be later handled by Render Component and Physics Component after receiving the "ModelChanged" entity event
        /// </summary>
        public void InitEntity()
        {
            Debug.Assert(Definition != null);
            if (Definition != null)
            {
                var entity = Entity as MyEntity;
                entity.Init(new StringBuilder(Definition.DisplayNameText), Definition.Model, null, null, null);
                entity.DisplayNameText = Definition.DisplayNameText;
            }
        }        
    }
}
