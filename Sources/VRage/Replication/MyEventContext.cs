using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public struct MyEventContext
    {
        public struct Token : IDisposable
        {
            MyEventContext m_oldContext;

            public Token(MyEventContext newContext)
            {
                m_oldContext = m_current;
                m_current = newContext;
            }

            void IDisposable.Dispose()
            {
                m_current = m_oldContext;
            }
        }

        [ThreadStatic]
        private static MyEventContext m_current;

        public static MyEventContext Current
        {
            get { return m_current; }
        }

        public static void ValidationFailed()
        {
            m_current.m_validationFailed = true;
        }

        /// <summary>
        /// Event sender, default(EndpointId) when invoked locally.
        /// </summary>
        public readonly EndpointId Sender;

        /// <summary>
        /// Event sender client data, valid only when invoked remotely on server, otherwise null.
        /// </summary>
        public readonly MyClientStateBase ClientState;

        /// <summary>
        /// True if validation is required.
        /// </summary>
        public readonly bool IsValidationRequired;

        private bool m_validationFailed;

        public bool IsLocallyInvoked { get { return Sender.Value == 0; } }

        public bool HasValidationFailed { get { return m_validationFailed; } }

        private MyEventContext(EndpointId sender, MyClientStateBase clientState, bool validate)
        {
            Sender = sender;
            ClientState = clientState;
            IsValidationRequired = validate;
            m_validationFailed = false;
        }

        public static Token Set(EndpointId endpoint, MyClientStateBase client, bool validate)
        {
            return new Token(new MyEventContext(endpoint, client, validate));
        }
    }
}
