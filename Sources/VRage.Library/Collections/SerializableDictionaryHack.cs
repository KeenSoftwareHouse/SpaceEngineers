﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

namespace VRage.Serialization
{
    [ProtoContract]
    [XmlRoot("Dictionary")]
    [System.Reflection.Obfuscation(Feature = Obfuscator.NoRename, Exclude = true, ApplyToMembers = true)]
    public class SerializableDictionaryHack<T, U>
    {
        public SerializableDictionaryHack() { }

        public SerializableDictionaryHack(Dictionary<T, U> dict) { Dictionary = dict; }

        [ProtoMember]
        private Dictionary<T, U> m_dictionary = new Dictionary<T, U>();
        /// <summary>
        /// Public stuff dictionary.
        /// </summary>
        /// <remarks>
        /// Note the XmlIgnore attribute.
        /// </remarks>
        [XmlIgnore()]
        public Dictionary<T, U> Dictionary
        {
            set { m_dictionary = value; }
            get { return m_dictionary; }
        }
        /// <summary>
        /// Property created expressly for the XmlSerializer
        /// </summary>
        /// <remarks>
        /// Note the XML Serialiazation attributes; they control what elements are named when this object is serialized.
        /// </remarks>
        [XmlArray("dictionary")]
        [XmlArrayItem("item", Type = typeof(DictionaryEntry))]
        public DictionaryEntry[] DictionaryEntryProp
        {
            get
            {
                //Make an array of DictionaryEntries to return
                DictionaryEntry[] ret = new DictionaryEntry[Dictionary.Count];
                int i = 0;
                DictionaryEntry de;
                //Iterate through Stuff to load items into the array.
                foreach (KeyValuePair<T, U> line in Dictionary)
                {
                    de = new DictionaryEntry();
                    de.Key = line.Key;
                    de.Value = line.Value;
                    ret[i] = de;
                    i++;
                }
                return ret;
            }
            set
            {
                Dictionary.Clear();
                for (int i = 0; i < value.Length; i++)
                {
                    try
                    {
                        Dictionary.Add((T)value[i].Key, (U)value[i].Value);
                    }
                    catch (Exception)
                    { //Conversion issues
                        System.Diagnostics.Debug.Fail("There was an error during conversion");
                    }
                }
            }
        }

        public U this[T key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Dictionary[key] = value;
            }
        }
    }
}