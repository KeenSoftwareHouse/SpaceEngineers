using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;

using Sandbox.Engine.Utils;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game
{
    public class MyInventoryConstraint
    {
        public string Icon;
        public bool m_useDefaultIcon = false;
        public readonly String Description;
        private HashSet<MyDefinitionId> m_constrainedIds;
        private HashSet<MyObjectBuilderType> m_constrainedTypes;

        public bool IsWhitelist
        {
            get;
            set;
        }

        public IEnumerable<MyDefinitionId> ConstrainedIds
        {
            get { return m_constrainedIds.Skip(0); }
        }
        public IEnumerable<MyObjectBuilderType> ConstrainedTypes
        {
            get { return m_constrainedTypes.Skip(0); }
        }

        public MyInventoryConstraint(MyStringId description, string icon = null, bool whitelist = true)
        {
            Icon = icon;
            m_useDefaultIcon = icon == null;

            Description = MyTexts.GetString(description);
            m_constrainedIds = new HashSet<MyDefinitionId>();
            m_constrainedTypes = new HashSet<MyObjectBuilderType>();
            IsWhitelist = whitelist;
        }

        public MyInventoryConstraint(String description, string icon = null, bool whitelist = true)
        {
            Icon = icon;
            m_useDefaultIcon = icon == null;

            Description = description;
            m_constrainedIds = new HashSet<MyDefinitionId>();
            m_constrainedTypes = new HashSet<MyObjectBuilderType>();
            IsWhitelist = whitelist;
        }

        public MyInventoryConstraint Add(MyDefinitionId id)
        {
            m_constrainedIds.Add(id);
            UpdateIcon();
            return this;
        }

        public MyInventoryConstraint Remove(MyDefinitionId id)
        {

            m_constrainedIds.Remove(id);
            UpdateIcon();
            return this;
        }

        public MyInventoryConstraint AddObjectBuilderType(MyObjectBuilderType type)
        {
            m_constrainedTypes.Add(type);
            UpdateIcon();
            return this;
        }

        public MyInventoryConstraint RemoveObjectBuilderType(MyObjectBuilderType type)
        {
            m_constrainedTypes.Remove(type);
            UpdateIcon();
            return this;
        }

        public void Clear()
        {
            m_constrainedIds.Clear();
            m_constrainedTypes.Clear();
            UpdateIcon();
        }

        public bool Check(MyDefinitionId checkedId)
        {
            if (IsWhitelist)
            {
                if (m_constrainedTypes.Contains(checkedId.TypeId))
                    return true;

                if (m_constrainedIds.Contains(checkedId))
                    return true;
                return false;
            }
            else
            {
                if (m_constrainedTypes.Contains(checkedId.TypeId))
                    return false;

                if (m_constrainedIds.Contains(checkedId))
                    return false;
                return true;
            }
        }

        // Updates icon according to the filtered items
        // CH: TODO: This is temporary. It should be somewhere in the definitions:
        //     either in the block definitions or have an extra definition file for inventory constraints
        public void UpdateIcon()
        {
            if (!m_useDefaultIcon) return;

            if (m_constrainedIds.Count == 0 && m_constrainedTypes.Count == 1)
            {
                var type = m_constrainedTypes.First();
                if (type == typeof(MyObjectBuilder_Ore))
                    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_ORE;
                else if (type == typeof(MyObjectBuilder_Ingot))
                    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_INGOT;
                else if (type == typeof(MyObjectBuilder_Component))
                    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_COMPONENT;
            }
            else if (m_constrainedIds.Count == 1 && m_constrainedTypes.Count == 0)
            {
                var id = m_constrainedIds.First();
                if (id == new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium"))
                    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_URANIUM;
                // MW: Right now weapon can have multiple types of ammo magazines
                //else if (id == new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), "Missile200mm"))
                //    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_MISSILE;
                //else if (id == new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), "NATO_5p56x45mm"))
                //    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_AMMO_5_54MM;
                //else if (id == new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), "NATO_25x184mm"))
                //    Icon = MyGuiConstants.TEXTURE_ICON_FILTER_AMMO_25MM;
            }
            else Icon = null;
        }
    }
}
