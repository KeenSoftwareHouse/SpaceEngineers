using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage
{
	public abstract class MyCommandArgs
	{
	}

	public delegate MyCommandArgs ParserDelegate(List<string> args);
	public delegate StringBuilder ActionDelegate(MyCommandArgs commandArgs);


    public abstract class MyCommand
    {
        
        protected class MyCommandAction
        {
            public StringBuilder AutocompleteHint = new StringBuilder("");
            public ParserDelegate Parser;
            public ActionDelegate CallAction;
        }


        protected Dictionary<string, MyCommandAction> m_methods;
        
        public List<string> Methods
        {
            get
            {
                return m_methods.Keys.ToList();
            }
        }

        public abstract string Prefix();

        public MyCommand()
        {
            m_methods = new Dictionary<string, MyCommandAction>();
        }

        public StringBuilder Execute(string method, List<string> args)
        {
            MyCommandAction action;
            if (m_methods.TryGetValue(method, out action))
            {
                try
                {
                    var commandArgs = action.Parser.Invoke(args);
                    var output = action.CallAction.Invoke(commandArgs);
                    Debug.Assert(output != null, "Call Action returning null StringBuilder. Command: {0}, Method: {1}", Prefix(), method);
                    return output;
                }

                //Every exception we stumble across when parsing or calling action, we'll assume arguments were passed the wrong way
                catch
                {
                    throw new MyConsoleInvalidArgumentsException();
                }
            }
            throw new MyConsoleMethodNotFoundException();
        }

        public StringBuilder GetHint(string method)
        {
            MyCommandAction args;
            if (m_methods.TryGetValue(method, out args))
                return args.AutocompleteHint;

            return null;
        }
    }
}
