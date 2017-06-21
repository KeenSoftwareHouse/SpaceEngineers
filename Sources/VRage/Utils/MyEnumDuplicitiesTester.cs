using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using VRage.Library.Utils;

namespace VRage.Utils
{
    public static class MyEnumDuplicitiesTester
    {
        private const string m_keenSWHCompanyName = "Keen Software House";

        [Conditional("DEBUG")]
        public static void CheckEnumNotDuplicitiesInRunningApplication()
        {
            CheckEnumNotDuplicities(m_keenSWHCompanyName);
        }

        static void CheckEnumNotDuplicities(string companyName)
        {
#if !XB1
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            string[] dlls = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            List<Assembly> assembliesToTest = new List<Assembly>(assemblies.Length + dlls.Length);

            foreach (var assembly in assemblies)
            {
                if (companyName == null || GetCompanyNameOfAssembly(assembly) == companyName)
                {
                    assembliesToTest.Add(assembly);
                }
            }

            foreach (var dllPath in dlls)
            {
                if (!IsLoaded(assemblies, dllPath))
                {
                    if (companyName == null || System.Diagnostics.FileVersionInfo.GetVersionInfo(dllPath).CompanyName == companyName)
                    {
                        assembliesToTest.Add(Assembly.LoadFrom(dllPath));
                    }
                }
            }

            HashSet<object> hashSet = new HashSet<object>();

            foreach (Assembly assembly in assembliesToTest)
            {
                TestEnumNotDuplicitiesInAssembly(assembly, hashSet);
            }
#endif
        }
#if !XB1
        static bool IsLoaded(Assembly[] assemblies, string assemblyPath)
        {
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic || (!string.IsNullOrEmpty(assembly.Location) && Path.GetFullPath(assembly.Location) == assemblyPath))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The company name of the calling assembly.
        /// </summary>
        static string GetCompanyNameOfAssembly(Assembly assembly)
        {
            var attr = Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false) as AssemblyCompanyAttribute;
            return attr != null ? attr.Company : String.Empty;
        }

        static void TestEnumNotDuplicitiesInAssembly(Assembly assembly, HashSet<object> hashSet)
        {
            //foreach (Type type in assembly.GetTypes())
            //{
            //    if (!type.IsEnum) continue;
            //    if (type.IsGenericType) continue; // don't check enums in generic classes
            //    if (type.IsDefined(typeof(DontCheckAttribute), false)) continue;
            //    AssertEnumNotDuplicities(type, hashSet);
            //}
        }

        static void AssertEnumNotDuplicities(Type enumType, HashSet<object> hashSet)
        {
            hashSet.Clear();

            foreach (var key in Enum.GetValues(enumType))
            {
                if (!hashSet.Add(key))
                {
                    throw new Exception("Duplicate enum found: " + key + " in " + enumType.AssemblyQualifiedName);
                }
            }
        }
#endif
    }
}
