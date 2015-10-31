using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Attribute which assigns ID to class, this id persists after obfuscation and it's same on both x86 and X64 build.
    /// ID can change with different versions of source code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ClassIdAttribute : Attribute, IComparable<ClassIdAttribute>
    {
        string m_path;
        int m_line;

        public ClassIdAttribute([CallerFilePathAttribute] string path = "", [CallerLineNumber] int line = 0)
        {
            m_path = path;
            m_line = line;
        }

        public int CompareTo(ClassIdAttribute other)
        {
            int result = m_path.CompareTo(other.m_path);
            return result != 0 ? result : m_line.CompareTo(other.m_line);
        }

        public static ClassIdAttribute Get(Type t)
        {
            return (ClassIdAttribute)Attribute.GetCustomAttribute(t, typeof(ClassIdAttribute));
        }

        public static ClassIdAttribute Get<T>()
        {
            return (ClassIdAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(ClassIdAttribute));
        }
    }
}
