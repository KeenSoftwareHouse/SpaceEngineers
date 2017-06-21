using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.AI.BehaviorTree;
using System.Reflection;
using VRage;
using VRage.Utils;
using VRage.Game;
using VRage.Game.AI;

namespace Sandbox.Game.AI
{
    public class ActionCollection
    {
        public class BotActionDesc
        {
            public Action<IMyBot> InitAction;
            public object[] ActionParams;
            public Dictionary<int, MyTuple<Type, MyMemoryParameterType>> ParametersDesc;
            public Func<IMyBot, object[], MyBehaviorTreeState> _Action;
            public Action<IMyBot> PostAction;
            public bool ReturnsRunning;

            public BotActionDesc()
            {
            }
        }

        private Dictionary<MyStringId, BotActionDesc> m_actions = new Dictionary<MyStringId, BotActionDesc>(MyStringId.Comparer);

        private ActionCollection()
        {
        }

        public void AddInitAction(string actionName, Action<IMyBot> action)
        {
            AddInitAction(MyStringId.GetOrCompute(actionName), action);
        }

        public void AddInitAction(MyStringId actionName, Action<IMyBot> action)
        {
            if (!m_actions.ContainsKey(actionName))
                AddBotActionDesc(actionName);

            Debug.Assert(m_actions[actionName].InitAction == null, "Adding a bot init action under the same name!");

            m_actions[actionName].InitAction = action;
        }

        public void AddAction(string actionName, MethodInfo methodInfo, bool returnsRunning, Func<IMyBot, object[], MyBehaviorTreeState> action)
        {
            AddAction(MyStringId.GetOrCompute(actionName), methodInfo, returnsRunning, action);
        }

        public void AddAction(MyStringId actionId, MethodInfo methodInfo, bool returnsRunning, Func<IMyBot, object[], MyBehaviorTreeState> action)
        {
            if (!m_actions.ContainsKey(actionId))
                AddBotActionDesc(actionId);

            Debug.Assert(m_actions[actionId]._Action == null, "Adding a bot action under the same name!");

            var actionDesc = m_actions[actionId];
            var parameters = methodInfo.GetParameters();
            actionDesc._Action = action;
            actionDesc.ActionParams = new object[parameters.Length];
            actionDesc.ParametersDesc = new Dictionary<int, MyTuple<Type, MyMemoryParameterType>>();
            actionDesc.ReturnsRunning = returnsRunning;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramAttr = parameters[i].GetCustomAttribute<BTMemParamAttribute>(true);
                if (paramAttr == null)
                    continue;
                actionDesc.ParametersDesc.Add(i, new MyTuple<Type, MyMemoryParameterType>(parameters[i].ParameterType.GetElementType(), paramAttr.MemoryType));
            }
        }

        public void AddPostAction(string actionName, Action<IMyBot> action)
        {
            AddPostAction(MyStringId.GetOrCompute(actionName), action);
        }

        public void AddPostAction(MyStringId actionId, Action<IMyBot> action)
        {
            if (!m_actions.ContainsKey(actionId))
                AddBotActionDesc(actionId);

            Debug.Assert(m_actions[actionId].PostAction == null, "Adding a bot post action under the same name!");

            m_actions[actionId].PostAction = action;
        }

        private void AddBotActionDesc(MyStringId actionId)
        {
            m_actions.Add(actionId, new BotActionDesc());
        }

        public void PerformInitAction(IMyBot bot, MyStringId actionId)
        {
            Debug.Assert(m_actions.ContainsKey(actionId), "Given bot action does not exist!");

            var action = m_actions[actionId];
            if (action == null) return;

            action.InitAction(bot);
        }

        public MyBehaviorTreeState PerformAction(IMyBot bot, MyStringId actionId, object[] args)
        {
            Debug.Assert(m_actions.ContainsKey(actionId), "Given bot action does not exist!");

            var action = m_actions[actionId];
            if (action == null) return MyBehaviorTreeState.ERROR;

            var botMemory = bot.BotMemory.CurrentTreeBotMemory;
            if (action.ParametersDesc.Count == 0)
            {
                return action._Action(bot, args);
            }
            else
            {
                Debug.Assert(args != null, "Args were not provided, aborting action");
                if (args == null)
                    return MyBehaviorTreeState.FAILURE;

                LoadActionParams(action, args, botMemory);
                var state = action._Action(bot, action.ActionParams);
                SaveActionParams(action, args, botMemory);
                return state;
            }
        }

        private void LoadActionParams(BotActionDesc action, object[] args, MyPerTreeBotMemory botMemory)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is Boxed<MyStringId> && action.ParametersDesc.ContainsKey(i))
                {
                    var parameterDesc = action.ParametersDesc[i];
                    Boxed<MyStringId> stringId = arg as Boxed<MyStringId>;
                    MyBBMemoryValue value = null;

					if (botMemory.TryGetFromBlackboard(stringId, out value))
					{
						if (value == null || (value.GetType() == parameterDesc.Item1 && parameterDesc.Item2 != MyMemoryParameterType.OUT))
						{
							action.ActionParams[i] = value;
						}
						else
						{
							if (value.GetType() != parameterDesc.Item1)
								Debug.Assert(false, "Mismatch of types in the blackboard. Did you use a wrong identifier?");

							action.ActionParams[i] = null;
						}
					}
					else
						action.ActionParams[i] = null;
                }
                else
                {
                    action.ActionParams[i] = arg;
                }
            }
        }

        private void SaveActionParams(BotActionDesc action, object[] args, MyPerTreeBotMemory botMemory)
        {
            foreach (var key in action.ParametersDesc.Keys)
            {
                MyStringId stringId = args[key] as Boxed<MyStringId>;
				var parameterDesc = action.ParametersDesc[key];
				if(parameterDesc.Item2 != MyMemoryParameterType.IN)
				  botMemory.SaveToBlackboard(stringId, action.ActionParams[key] as MyBBMemoryValue);
            }
        }

        public void PerformPostAction(IMyBot bot, MyStringId actionId)
        {
            Debug.Assert(m_actions.ContainsKey(actionId), "Given bot action does not exist!");

            var action = m_actions[actionId];
            if (action == null) return;

            action.PostAction(bot);
        }

        public bool ContainsInitAction(MyStringId actionId)
        {
            return m_actions[actionId].InitAction != null;
        }

        public bool ContainsPostAction(MyStringId actionId)
        {
            return m_actions[actionId].PostAction != null;
        }

        public bool ContainsAction(MyStringId actionId)
        {
            return m_actions[actionId]._Action != null;
        }

        public bool ContainsActionDesc(MyStringId actionId)
        {
            return m_actions.ContainsKey(actionId);
        }

        public bool ReturnsRunning(MyStringId actionId)
        {
            return m_actions[actionId].ReturnsRunning;
        }

        public static ActionCollection CreateActionCollection(IMyBot bot)
        {
            var actions = new ActionCollection();
            var methodInfos = bot.BotActions.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var methodInfo in methodInfos)
            {
                ExtractAction(actions, methodInfo);
            }
            return actions;
        }

        //private static ActionCollection CreateStaticActionCollection()
        //{
        //    var actions = new ActionCollection();
        //    var types = MyAIActionsParser.GetAllTypesFromAssemblies();

        //    foreach (var type in types)
        //    {
        //        var attr = type.GetCustomAttribute<MyBehaviorDescriptorAttribute>();
        //        if (!string.IsNullOrEmpty(attr.DescriptorCategory))
        //            continue;
        //        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        //        foreach (var method in methods)
        //        {
        //            ExtractAction(actions, method);
        //        }
        //    }

        //    return actions;
        //}

        private static void ExtractAction(ActionCollection actions, MethodInfo methodInfo)
        {
            var btActionAttribute = methodInfo.GetCustomAttribute<MyBehaviorTreeActionAttribute>();
            if (btActionAttribute == null)
                return;
            switch (btActionAttribute.ActionType)
            {
                case MyBehaviorTreeActionType.INIT:
                    actions.AddInitAction(btActionAttribute.ActionName, (x) => methodInfo.Invoke(x.BotActions, null));
                    break;
                case MyBehaviorTreeActionType.BODY:
                    actions.AddAction(btActionAttribute.ActionName, methodInfo, btActionAttribute.ReturnsRunning, (x, y) => (MyBehaviorTreeState)methodInfo.Invoke(x.BotActions, y));
                    break;
                case MyBehaviorTreeActionType.POST:
                    actions.AddPostAction(btActionAttribute.ActionName, (x) => methodInfo.Invoke(x.BotActions, null));
                    break;
            }
        }
    }
}
