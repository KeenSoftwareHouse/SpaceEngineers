using Sandbox.Definitions;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Game;
using VRage.Game.AI;
using VRage.Game.Common;
using VRage.ObjectBuilders;
using VRageMath;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.AI
{
    public class MyBotTypeAttribute : MyFactoryTagAttribute
    {
        public MyBotTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    public abstract class MyBotFactoryBase
    {
        protected class BehaviorData
        {
            public readonly Type BotActionsType;
            public Type LogicType;
            public BehaviorData(Type t)
            {
                BotActionsType = t;
            }
        }

        protected class LogicData
        {
            public readonly Type LogicType;

            public LogicData(Type t)
            {
                LogicType = t;
            }
        }

        protected class BehaviorTypeData
        {
            public Type BotType;

            public BehaviorTypeData(Type botType)
            {
                BotType = botType;
            }
        }

#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_botDataLoaded = false;
#endif // XB1

        protected Dictionary<string, Type> m_TargetTypeByName;
        protected Dictionary<string, BehaviorData> m_botDataByBehaviorType;
        protected Dictionary<string, LogicData> m_logicDataByBehaviorSubtype;
        protected Dictionary<Type, BehaviorTypeData> m_botTypeByDefinitionTypeRemoveThis;

        private Type[] m_tmpTypeArray;
        private object[] m_tmpConstructorParamArray;

        private static MyObjectFactory<MyBotTypeAttribute, IMyBot> m_objectFactory;

        static MyBotFactoryBase()
        {
            m_objectFactory = new MyObjectFactory<MyBotTypeAttribute, IMyBot>();

#if XB1 // XB1_ALLINONEASSEMBLY
            m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            var baseAssembly = Assembly.GetAssembly(typeof(MyAgentBot));
            m_objectFactory.RegisterFromAssembly(baseAssembly);
            m_objectFactory.RegisterFromAssembly(VRage.Plugins.MyPlugins.GameAssembly);

            foreach (var plugin in VRage.Plugins.MyPlugins.Plugins)
                m_objectFactory.RegisterFromAssembly(plugin.GetType().Assembly);
#endif // !XB1
        }

        public MyBotFactoryBase()
        {
            m_TargetTypeByName = new Dictionary<string, Type>();
            m_botDataByBehaviorType = new Dictionary<string, BehaviorData>();
            m_logicDataByBehaviorSubtype = new Dictionary<string, LogicData>();

            m_tmpTypeArray = new Type[1] { null };
            m_tmpConstructorParamArray = new object[1] { null };

#if XB1 // XB1_ALLINONEASSEMBLY
            LoadBotData(MyAssembly.AllInOneAssembly);
#else // !XB1
            var baseAssembly = Assembly.GetAssembly(typeof(MyAgentBot));
            LoadBotData(baseAssembly);
            LoadBotData(VRage.Plugins.MyPlugins.GameAssembly);

            foreach (var plugin in VRage.Plugins.MyPlugins.Plugins)
                LoadBotData(plugin.GetType().Assembly);
#endif // !XB1
        }

        protected void LoadBotData(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_botDataLoaded == false);
            if (m_botDataLoaded == true)
                return;
            m_botDataLoaded = true;
            var allTypes = MyAssembly.GetTypes();
#else // !XB1
            var allTypes = assembly.GetTypes();
#endif // !XB1

            foreach (var type in allTypes)
            {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(MyBotActionsBase)))
                {
                    var typeAttrs = type.GetCustomAttributes(true);
                    string behaviorName = "";
                    var behaviorData = new BehaviorData(type);
                    foreach (var typeAttr in typeAttrs)
                    {
                        if (typeAttr is MyBehaviorDescriptorAttribute)
                        {
                            var behaviorPropertiesAttr = typeAttr as MyBehaviorDescriptorAttribute;
                            Debug.Assert(!m_botDataByBehaviorType.ContainsKey(behaviorPropertiesAttr.DescriptorCategory), "Bot type already declared in the factory");
                            behaviorName = behaviorPropertiesAttr.DescriptorCategory;
                        }
                        else if (typeAttr is BehaviorActionImplAttribute)
                        {
                            var behaviorImplAttr = typeAttr as BehaviorActionImplAttribute;
                            behaviorData.LogicType = behaviorImplAttr.LogicType;
                        }
                    }

                    if (!string.IsNullOrEmpty(behaviorName) && behaviorData.LogicType != null)
                    {
                        m_botDataByBehaviorType[behaviorName] = behaviorData;
                    }
                    else
                    {
                        Debug.Assert(false, "Invalid bot data. Definition will be removed");
                    }
                }
                else if (!type.IsAbstract && type.IsSubclassOf(typeof(MyBotLogic)))
                {
                    foreach (var typeAttr in type.GetCustomAttributes(typeof(BehaviorLogicAttribute), true))
                    {
                        var subtypeAttr = typeAttr as BehaviorLogicAttribute;
                        m_logicDataByBehaviorSubtype[subtypeAttr.BehaviorSubtype] = new LogicData(type);
                    }
                }
                else if (!type.IsAbstract && typeof(MyAiTargetBase).IsAssignableFrom(type))
                {
                    foreach (var typeAttr in type.GetCustomAttributes(typeof(TargetTypeAttribute), true))
                    {
                        var tarTypeAttr = typeAttr as TargetTypeAttribute;
                        m_TargetTypeByName[tarTypeAttr.TargetType] = type;
                    }
                }
            }
        }

        public abstract int MaximumUncontrolledBotCount { get; }
        public abstract int MaximumBotPerPlayer { get; }

        public MyObjectBuilder_Bot GetBotObjectBuilder(IMyBot myAgentBot)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_Bot>(myAgentBot);
        }

        public IMyBot CreateBot(MyPlayer player, MyObjectBuilder_Bot botBuilder, MyBotDefinition botDefinition)
        {
            MyObjectBuilderType obType = MyObjectBuilderType.Invalid;
            if (botBuilder == null)
            {
                obType = botDefinition.Id.TypeId;
                botBuilder = m_objectFactory.CreateObjectBuilder<MyObjectBuilder_Bot>(m_objectFactory.GetProducedType(obType));
            }
            else
            {
                obType = botBuilder.TypeId;
                Debug.Assert(botDefinition.Id == botBuilder.BotDefId, "Bot builder type does not match bot definition type!");
            }

            Debug.Assert(m_botDataByBehaviorType.ContainsKey(botDefinition.BehaviorType), "Undefined behavior type. Bot is not going to be created");
            if (!m_botDataByBehaviorType.ContainsKey(botDefinition.BehaviorType))
                return null;
            var botData = m_botDataByBehaviorType[botDefinition.BehaviorType];
            IMyBot output = CreateBot(m_objectFactory.GetProducedType(obType), player, botDefinition);
            CreateActions(output, botData.BotActionsType);
            CreateLogic(output, botData.LogicType, botDefinition.BehaviorSubtype);
            output.Init(botBuilder);
            return output;
        }

        private void CreateLogic(IMyBot output, Type defaultLogicType, string definitionLogicType)
        {
            Type logicType = null;
            if (m_logicDataByBehaviorSubtype.ContainsKey(definitionLogicType))
            {
                logicType = m_logicDataByBehaviorSubtype[definitionLogicType].LogicType;
                if (!logicType.IsSubclassOf(defaultLogicType) && logicType != defaultLogicType)
                {
                    logicType = defaultLogicType;
                }
            }
            else
            {
                logicType = defaultLogicType;
            }

            var logic = Activator.CreateInstance(logicType, output) as MyBotLogic;
            output.InitLogic(logic);
        }

        private void CreateActions(IMyBot bot, Type actionImplType)
        {
            m_tmpTypeArray[0] = bot.GetType();
            var constructor = actionImplType.GetConstructor(m_tmpTypeArray);
            if (constructor == null)
                bot.BotActions = Activator.CreateInstance(actionImplType) as MyBotActionsBase;
            else
                bot.BotActions = Activator.CreateInstance(actionImplType, bot) as MyBotActionsBase;
            m_tmpTypeArray[0] = null;
        }

        public MyAiTargetBase CreateTargetForBot(MyAgentBot bot)
        {
            MyAiTargetBase retval = null;

            m_tmpConstructorParamArray[0] = bot;
            Type targetType = null;
            m_TargetTypeByName.TryGetValue(bot.AgentDefinition.TargetType, out targetType);
            if (targetType != null)
            {
                retval = Activator.CreateInstance(targetType, m_tmpConstructorParamArray) as MyAiTargetBase;
            }
            m_tmpConstructorParamArray[0] = null;

            return retval;
        }

        private IMyBot CreateBot(Type botType, MyPlayer player, MyBotDefinition botDefinition)
        {
            return Activator.CreateInstance(botType, player, botDefinition) as IMyBot; // MW:TODO so far agent and humanoid have players so let's keep it like this
        }
  
        public abstract bool CanCreateBotOfType(string behaviorType, bool load);
        public abstract bool GetBotSpawnPosition(string behaviorType, out Vector3D spawnPosition);
        public abstract bool GetBotGroupSpawnPositions(string behaviorType, int count, List<Vector3D> spawnPositions);
    }
}
