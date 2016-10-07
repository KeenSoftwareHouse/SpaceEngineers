#if !XB1
using Sandbox.Common;
using VRage.Game.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage.Game;
using VRage.Game.ObjectBuilders.AI;
using VRage.ObjectBuilders;
using VRage.Plugins;

namespace Sandbox.Engine.AI
{
    [PreloadRequired]
    public static class MyAIActionsParser
    {
        private static bool ENABLE_PARSING = true;
        private static string SERIALIZE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MedievalEngineers", "BehaviorDescriptors.xml");

        static MyAIActionsParser()
        {
            if (!ENABLE_PARSING)
                return;

            if (!MyFinalBuildConstants.IS_DEBUG)
                return;

            var types = GetAllTypesFromAssemblies();
            var methods = ParseMethods(types);
            SerializeToXML(SERIALIZE_PATH, methods);
        }

        public static HashSet<Type> GetAllTypesFromAssemblies()
        {
            HashSet<Type> types = new HashSet<Type>();
            GetTypesFromAssembly(MyPlugins.SandboxGameAssembly, types);
            GetTypesFromAssembly(MyPlugins.GameAssembly, types);
            GetTypesFromAssembly(MyPlugins.UserAssembly, types);
            return types;
        }

        private static void GetTypesFromAssembly(Assembly assembly, HashSet<Type> outputTypes)
        {
            if (assembly == null)
                return;
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    if (attr is MyBehaviorDescriptorAttribute)
                    {
                        outputTypes.Add(type);
                    }
                }
            }
        }

        private static Dictionary<string, List<MethodInfo>> ParseMethods(HashSet<Type> types)
        {
            Dictionary<string, List<MethodInfo>> methodsPerCategory = new Dictionary<string, List<MethodInfo>>();

            foreach (var type in types)
            {
                var descAttr = type.GetCustomAttribute<MyBehaviorDescriptorAttribute>();
                MethodInfo[] methods = null;
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    var behaviorActionAttr = method.GetCustomAttribute<MyBehaviorTreeActionAttribute>();

                    if (behaviorActionAttr == null)
                        continue;

                    // we are exporting only body actions
                    if (behaviorActionAttr.ActionType != MyBehaviorTreeActionType.BODY)
                        continue;

                    bool valid = true;

                    // methods with parameters marked with attributes BTParam or BTIn/Out/In_Out are ONLY valid
                    foreach (var param in method.GetParameters())
                    {
                        var paramAttr = param.GetCustomAttribute<BTParamAttribute>();
                        var memParamAttr = param.GetCustomAttribute<BTMemParamAttribute>();

                        if (paramAttr == null && memParamAttr == null)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        List<MethodInfo> methodInfos = null;
                        if (!methodsPerCategory.TryGetValue(descAttr.DescriptorCategory, out methodInfos))
                        {
                            methodInfos = new List<MethodInfo>();
                            methodsPerCategory[descAttr.DescriptorCategory] = methodInfos;
                        }
                        methodInfos.Add(method);
                    }
                }
            }

            return methodsPerCategory;
        }

        private static void SerializeToXML(string path, Dictionary<string, List<MethodInfo>> data)
        {
            var aiData = MyObjectBuilderSerializer.CreateNewObject<MyAIBehaviorData>();
            aiData.Entries = new MyAIBehaviorData.CategorizedData[data.Count];

            int entryIdx = 0;
            foreach (var categoryPair in data)
            {
                var categoryData = new MyAIBehaviorData.CategorizedData();
                categoryData.Category = categoryPair.Key;
                categoryData.Descriptors = new MyAIBehaviorData.ActionData[categoryPair.Value.Count];

                int actionDataIdx = 0;
                foreach (var methodInfo in categoryPair.Value)
                {
                    var actionData = new MyAIBehaviorData.ActionData();
                    var btActionAttr = methodInfo.GetCustomAttribute<MyBehaviorTreeActionAttribute>();
                    actionData.ActionName = btActionAttr.ActionName;
                    actionData.ReturnsRunning = btActionAttr.ReturnsRunning;

                    var methodParams = methodInfo.GetParameters();
                    actionData.Parameters = new MyAIBehaviorData.ParameterData[methodParams.Length];

                    int paramIdx = 0;
                    foreach (var param in methodParams)
                    {
                        var memParamAttr = param.GetCustomAttribute<BTMemParamAttribute>();
                        var paramAttr = param.GetCustomAttribute<BTParamAttribute>();
                        var actionParam = new MyAIBehaviorData.ParameterData();

                        actionParam.Name = param.Name;
                        actionParam.TypeFullName = param.ParameterType.FullName;

                        if (memParamAttr != null)
                        {
                            actionParam.MemType = memParamAttr.MemoryType;
                        }
                        else if (paramAttr != null)
                        {
                            actionParam.MemType = MyMemoryParameterType.PARAMETER;
                        }
                        else
                        {
                            Debug.Assert(false, "No behavior attribute on parameter. Category: " + categoryPair.Key + ", method name: " + methodInfo.Name);
                        }

                        actionData.Parameters[paramIdx] = actionParam;
                        paramIdx++;
                    }

                    categoryData.Descriptors[actionDataIdx] = actionData;
                    actionDataIdx++;
                }

                aiData.Entries[entryIdx] = categoryData;
                entryIdx++;
            }

            MyObjectBuilderSerializer.SerializeXML(path, false, aiData);
        }
    }
}
#endif // !XB1
