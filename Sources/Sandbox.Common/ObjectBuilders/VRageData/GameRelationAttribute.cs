using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    public enum Game
    {
        Shared,
        SpaceEngineers,
        MedievalEngineers
    }

    /// <summary>
    /// This class is here only to distinguish fields in SessionSettings
    /// Will be removed after correct hierarchy and usage of session settings is made
    /// </summary>
    public sealed class GameRelationAttribute : Attribute
    {
        public GameRelationAttribute(Game relatedTo)
        {
            RelatedTo = relatedTo;
        }

        public Game RelatedTo;
    }
}
