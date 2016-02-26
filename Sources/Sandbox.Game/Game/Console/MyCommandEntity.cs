using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;

namespace Sandbox.Game.GUI
{

    [PreloadRequired]
    public class MyCommandEntity : MyCommand
    {
        private class MyCommandArgsDisplayName : MyCommandArgs
        {
            public long EntityId;
            public string newDisplayName;
        }

        static MyCommandEntity()
        {
            MyConsole.AddCommand(new MyCommandEntity());
        }

        public override string Prefix()
        {
            return "Entity";
        }

        private MyCommandEntity()
        {
            m_methods.Add(
                "SetDisplayName",
                new MyCommandAction()
                {
                    AutocompleteHint = new StringBuilder("long_EntityId string_NewDisplayName"),
                    Parser = (x) => ParseDisplayName(x),
                    CallAction = (x) => ChangeDisplayName(x)
                }
            );

            m_methods.Add(
                "MethodA",
                new MyCommandAction()
                {
                    Parser = (x) => ParseDisplayName(x),
                    CallAction = (x) => ChangeDisplayName(x)
                }
            );

            m_methods.Add(
                "MethodB",
                new MyCommandAction()
                {
                    Parser = (x) => ParseDisplayName(x),
                    CallAction = (x) => ChangeDisplayName(x)
                }
            );

            m_methods.Add(
                "MethodC",
                new MyCommandAction()
                {
                    Parser = (x) => ParseDisplayName(x),
                    CallAction = (x) => ChangeDisplayName(x)
                }
            );

            m_methods.Add(
                "MethodD",
                new MyCommandAction()
                {
                    Parser = (x) => ParseDisplayName(x),
                    CallAction = (x) => ChangeDisplayName(x)
                }
            );

        }

        private StringBuilder ChangeDisplayName(MyCommandArgs args)
        {
            var argsdn = args as MyCommandArgsDisplayName;

            MyEntity entity;
            if (MyEntities.TryGetEntityById(argsdn.EntityId, out entity))
            {
                if (argsdn.newDisplayName != null)
                {
                    var oldDisplayName = entity.DisplayName;
                    entity.DisplayName = argsdn.newDisplayName;
                    return new StringBuilder().Append("Changed name from entity ").Append(argsdn.EntityId).Append(" from ").Append(oldDisplayName).Append(" to ").Append(entity.DisplayName);
                }
                else
                    return new StringBuilder().Append("Invalid Display name");
            }
            return new StringBuilder().Append("Entity not found");
        }

        private MyCommandArgs ParseDisplayName(List<string> args)
        {
            MyCommandArgsDisplayName output = new MyCommandArgsDisplayName();
            output.EntityId = Int64.Parse(args[0]);
            output.newDisplayName = args[1];

            return output;
        }
    }
}
