using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Generics
{
    /// <summary>
    /// Interface of totally generic condition.
    /// </summary>
    public interface IMyCondition
    {
        /// <summary>
        /// Evaluate the condition, it can be true/false.
        /// </summary>
        bool Evaluate();
    }
}
