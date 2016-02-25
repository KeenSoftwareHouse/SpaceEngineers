using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Definitions;
using VRage.Utils;

namespace VRage.Game
{
    public class MyDefinitionSet
    {
        public MyModContext Context;

        public readonly Dictionary<Type, Dictionary<MyStringHash, MyDefinitionBase>> Definitions = new Dictionary<Type, Dictionary<MyStringHash, MyDefinitionBase>>();

        /**
         * Add a new definition to the set.
         * 
         * Crashes if existing.
         */
        public void AddDefinition(MyDefinitionBase def)
        {
            Dictionary<MyStringHash, MyDefinitionBase> dict;
            if (!Definitions.TryGetValue(def.Id.TypeId, out dict))
            {
                dict = new Dictionary<MyStringHash, MyDefinitionBase>();
                Definitions[def.Id.TypeId] = dict;
            }

            dict.Add(def.Id.SubtypeId, def);
        }

        /**
         * Add or replace an existing definition.
         */
        public bool AddOrRelaceDefinition(MyDefinitionBase def)
        {
            Dictionary<MyStringHash, MyDefinitionBase> dict;
            if (!Definitions.TryGetValue(def.Id.TypeId, out dict))
            {
                dict = new Dictionary<MyStringHash, MyDefinitionBase>();
                Definitions[def.Id.TypeId] = dict;
            }

            var contains = dict.ContainsKey(def.Id.SubtypeId);

            dict[def.Id.SubtypeId] = def;

            return contains;
        }

        /**
         * Remove a definition if on the set.
         */
        public void RemoveDefinition(ref MyDefinitionId defId)
        {
            Dictionary<MyStringHash, MyDefinitionBase> dict;
            if (Definitions.TryGetValue(defId.TypeId, out dict))
            {
                dict.Remove(defId.SubtypeId);
            }
        }

        /**
         * Get all definitions of a given type.
         */
        public IEnumerable<T> GetDefinitionsOfType<T>() where T : MyDefinitionBase
        {
            return Definitions[MyDefinitionManagerBase.GetObjectBuilderType(typeof(T))].Values.Cast<T>();
        }

        public bool ContainsDefinition(MyDefinitionId id)
        {
            Dictionary<MyStringHash, MyDefinitionBase> dictionary;

            return Definitions.TryGetValue(id.TypeId, out dictionary) && dictionary.ContainsKey(id.SubtypeId);
        }

        public T GetDefinition<T>(MyStringHash subtypeId) where T : MyDefinitionBase
        {
            MyDefinitionBase definitionBase = null;

            var ob = MyDefinitionManagerBase.GetObjectBuilderType(typeof(T));
            Dictionary<MyStringHash, MyDefinitionBase> dictionary;

            if (Definitions.TryGetValue(ob, out dictionary)) dictionary.TryGetValue(subtypeId, out definitionBase);

            return (T)definitionBase;
        }

        public T GetDefinition<T>(MyDefinitionId id) where T : MyDefinitionBase
        {
            MyDefinitionBase definitionBase = null;

            Dictionary<MyStringHash, MyDefinitionBase> dictionary;

            if (Definitions.TryGetValue(id.TypeId, out dictionary)) dictionary.TryGetValue(id.SubtypeId, out definitionBase);

            return (T)definitionBase;
        }

        /**
         * Override the contents of this definition set with another.
         */
        public virtual void OverrideBy(MyDefinitionSet definitionSet)
        {
            MyDefinitionPostprocessor.Bundle myBundle = new MyDefinitionPostprocessor.Bundle()
            {
                Set = this,
                Context = Context
            };

            MyDefinitionPostprocessor.Bundle thyBundle = new MyDefinitionPostprocessor.Bundle()
            {
                Set = definitionSet,
                Context = definitionSet.Context
            };

            foreach (var defgrp in definitionSet.Definitions)
            {
                Dictionary<MyStringHash, MyDefinitionBase> dict;
                if (!Definitions.TryGetValue(defgrp.Key, out dict))
                {
                    dict = new Dictionary<MyStringHash, MyDefinitionBase>();
                    Definitions[defgrp.Key] = dict;
                }

                var pp = MyDefinitionManagerBase.GetPostProcessor(defgrp.Key);

                myBundle.Definitions = dict;
                thyBundle.Definitions = defgrp.Value;

                pp.OverrideBy(ref myBundle, ref thyBundle);
            }
        }

        public void Clear()
        {
            foreach (var defMap in Definitions)
            {
                defMap.Value.Clear();
            }
        }
    }
}
