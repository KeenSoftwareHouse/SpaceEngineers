using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.EntityComponents
{

    /// <summary>
    /// Reference counting component for entities.
    /// 
    /// Allows simplified management of short lived entities that may be shared amongst systems.
    /// 
    /// The count is initially 0 so the first referencee becomes the owner of the
    /// entity (this is sometimes called a floating reference).
    /// </summary>
    public class MyEntityReferenceComponent : MyEntityComponentBase
    {
        private int m_references;

        public override string ComponentTypeDebugString
        {
            get { return "ReferenceCount"; }
        }

        /// <summary>
        /// Increase the reference count of this entity.
        /// </summary>
        public void Ref()
        {
            m_references++;
        }

        /// <summary>
        /// Decrease the entitie's reference count.
        /// </summary>
        /// <returns>Weather the count reached 0 and the entity was marked for close.</returns>
        public bool Unref()
        {
            m_references--;
            if (m_references <= 0)
            {
                Entity.Close();
                return true;
            }

            return false;
        }
    }
}
