using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
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

        protected Dictionary<string, BehaviorData> m_botDataByBehaviorType;
        protected Dictionary<string, LogicData> m_logicDataByBehaviorSubtype;
        protected Dictionary<Type, BehaviorTypeData> m_botTypeByDefinitionType;

        public MyBotFactoryBase()
        {
            m_botDataByBehaviorType = new Dictionary<string, BehaviorData>();
            m_logicDataByBehaviorSubtype = new Dictionary<string, LogicData>();
            m_botTypeByDefinitionType = new Dictionary<Type, BehaviorTypeData>();

            var baseAssembly = Assembly.GetAssembly(typeof(MyAgentBot));
            LoadBotData(baseAssembly);
            LoadBotData(VRage.Plugins.MyPlugins.GameAssembly);
        }

        protected void LoadBotData(Assembly assembly)
        {
            var allTypes = assembly.GetTypes();

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
                    var typeAttrs = type.GetCustomAttributes(true);

                    foreach (var typeAttr in typeAttrs)
                    {
                        if (typeAttr is BehaviorLogicAttribute)
                        {
                            var subtypeAttr = typeAttr as BehaviorLogicAttribute;
                            m_logicDataByBehaviorSubtype[subtypeAttr.BehaviorSubtype] = new LogicData(type);
                        }
                    }
                }
                else if (!type.IsAbstract && typeof(IMyBot).IsAssignableFrom(type))
                {
                    var typeAttrs = type.GetCustomAttributes(true);

                    foreach (var typeAttr in typeAttrs)
                    {
                        if (typeAttr is BehaviorTypeAttribute)
                        {
                            var behTypeAttr = typeAttr as BehaviorTypeAttribute;
                            m_botTypeByDefinitionType[behTypeAttr.BehaviorType] = new BehaviorTypeData(type);
                        }
                    }
                }
            }
        }

        public abstract int MaximumUncontrolledBotCount { get; }
        public abstract int MaximumBotPerPlayer { get; }

        public IMyBot CreateBot(MyPlayer player, MyObjectBuilder_Bot botBuilder, MyBotDefinition botDefinition)
        {
            Debug.Assert(m_botDataByBehaviorType.ContainsKey(botDefinition.BehaviorType), "Undefined behavior type. Bot is not going to be created");
            if (!m_botDataByBehaviorType.ContainsKey(botDefinition.BehaviorType))
                return null;
            Debug.Assert(m_botTypeByDefinitionType.ContainsKey(botDefinition.TypeDefinitionId.TypeId), "Type not found. Bot is not going to be created!");
            if (!m_botTypeByDefinitionType.ContainsKey(botDefinition.TypeDefinitionId.TypeId))
                return null;
            var botData = m_botDataByBehaviorType[botDefinition.BehaviorType];
            var behaviorTypeData = m_botTypeByDefinitionType[botDefinition.TypeDefinitionId.TypeId];
            IMyBot output = CreateBot(behaviorTypeData.BotType, player, botDefinition);
            CreateActions(output, botData.BotActionsType);
            CreateLogic(output, botData.LogicType, botDefinition.BehaviorSubtype);
            if (botBuilder != null)
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
            var constructor = actionImplType.GetConstructor(new Type[] { bot.GetType() });
            if (constructor == null)
                bot.BotActions = Activator.CreateInstance(actionImplType) as MyBotActionsBase;
            else
                bot.BotActions = Activator.CreateInstance(actionImplType, bot) as MyBotActionsBase;
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
