using System;
using System.Diagnostics;
using VRage.Game.Definitions;
using VRage.Utils;

namespace VRage.Game
{
    [MyDefinitionType(typeof(MyObjectBuilder_DefinitionBase))]
    public class MyDefinitionBase
    {
        public MyDefinitionId Id;

        /// <summary>
        /// Enum used for localization of display name. Null for player created definitions.
        /// </summary>
        public MyStringId? DisplayNameEnum;

        /// <summary>
        /// Enum used for localization of description. Null for player created definitions.
        /// </summary>
        public MyStringId? DescriptionEnum;

        /// <summary>
        /// String name used for user created definitions which do not have localization support.
        /// </summary>
        public String DisplayNameString;

        /// <summary>
        /// String used for user created description which do not have localization support.
        /// </summary>
        public String DescriptionString;

        /// <summary>
        /// Icons for the definition, they are used from top to bottom.
        /// </summary>
        public string[] Icons;

        /// <summary>
        /// Definition can be disabled by mod, then it will be removed from definition manager
        /// </summary>
        public bool Enabled = true; 

        /// <summary>
        /// Indicates if definition should be offered in Cube builder
        /// </summary>
        public bool Public = true;

		public bool AvailableInSurvival;

        public MyModContext Context;

        /// <summary>
        /// Use this property when showing name in GUI instead of DisplayName. This takes into
        /// account more complex name construction.
        /// </summary>
        public virtual String DisplayNameText
        {
            get
            {
                return (DisplayNameEnum.HasValue)
                    ? MyTexts.GetString(DisplayNameEnum.Value)
                    : DisplayNameString;
            }
        }

        /// <summary>
        /// Use this property when showing description in GUI, as it takes into account more
        /// complex description construction.
        /// </summary>
        public virtual String DescriptionText
        {
            get
            {
                return (DescriptionEnum.HasValue)
                    ? MyTexts.GetString(DescriptionEnum.Value)
                    : DescriptionString;
            }
        }

        public void Init(MyObjectBuilder_DefinitionBase builder, MyModContext modContext)
        {
            Context = modContext;
            Init(builder);
        }

        protected virtual void Init(MyObjectBuilder_DefinitionBase builder)
        {
            this.Id = builder.Id;
            this.Public = builder.Public;
            this.Enabled = builder.Enabled;
			this.AvailableInSurvival = builder.AvailableInSurvival;
            this.Icons = builder.Icons;

            if (builder.DisplayName != null && builder.DisplayName.StartsWith("DisplayName_"))
            {
                DisplayNameEnum = MyStringId.GetOrCompute(builder.DisplayName);
            }
            else
            {
                DisplayNameString = builder.DisplayName;
            }

            if (builder.Description != null && builder.Description.StartsWith("Description_"))
            {
                DescriptionEnum = MyStringId.GetOrCompute(builder.Description);
            }
            else
            {
                DescriptionString = builder.Description;
            }

            Debug.Assert(!Context.IsBaseGame || !Public || string.IsNullOrWhiteSpace(builder.DisplayName) || (DisplayNameEnum.HasValue && builder.DisplayName.StartsWith("DisplayName_")),
                string.Format("Bad display name '{0}' on definition '{1}'. It should either be empty, or it must start with 'DisplayName_' and have corresponding text enum defined.",
                    builder.DisplayName, Id));
            Debug.Assert(!Context.IsBaseGame || !Public || string.IsNullOrWhiteSpace(builder.Description) || (DescriptionEnum.HasValue && builder.Description.StartsWith("Description_")),
                string.Format("Bad description '{0}' on definition '{1}'. It should either be empty, or it must start with 'Description_' and have corresponding text enum defined.",
                    builder.Description, Id));
        }

        /// <summary>
        /// Override this in case you want to do some postprocessing of the definition before the game starts.
        /// 
        /// TODO: Obsolete me
        /// <para>Postprocess is useful if you want to process the definition before the game begins,</para>
        /// <para>but you only want to do it when all the definitions are loaded and merged.</para>
        /// </summary>
        [Obsolete("Prefer to use MyDefinitionPostprocessor instead.")]
        public virtual void Postprocess() { return; }

        public void Save(string filepath)
        {
            GetObjectBuilder().Save(filepath);
        }

        public virtual MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var builder = MyDefinitionManagerBase.GetObjectFactory().CreateObjectBuilder<MyObjectBuilder_DefinitionBase>(this);

            builder.Id          = Id;
            builder.Description = (DescriptionEnum.HasValue) ? DescriptionEnum.Value.ToString() : DescriptionString != null ? DescriptionString.ToString() : null;
            builder.DisplayName = (DisplayNameEnum.HasValue) ? DisplayNameEnum.Value.ToString() : DisplayNameString != null ? DisplayNameString.ToString() : null;
            builder.Icons        = Icons;
            builder.Public      = Public;
			builder.Enabled		= Enabled;

			builder.AvailableInSurvival = AvailableInSurvival;

            return builder;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
