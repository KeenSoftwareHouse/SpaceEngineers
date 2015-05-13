using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GameSystems.Electricity
{
    delegate void RequiredPowerChangeDelegate(MyPowerReceiver receiver, float oldRequirement, float newRequirement);

    public class MyPowerReceiver
    {
        private static StringBuilder m_textCache = new StringBuilder();
        private Func<float> m_requiredInputFunc;
        private float m_requiredInput;
        private bool m_firstTime = true;

        #region Properties
        /// <summary>
        /// Higher priority groups get more power than lower priority ones.
        /// If there is not enough power for everything, lower priority groups
        /// are turned off first.
        /// </summary>
        internal readonly MyConsumerGroupEnum Group;

        /// <summary>
        /// Adaptible consumers can work on less than their required input,
        /// but they will be less effective.
        /// </summary>
        public readonly bool IsAdaptible;

        /// <summary>
        /// Theoretical maximum of required input. This can be different from RequiredInput, but
        /// it has to be >= RequiredInput. It is used to check whether current power supply can meet
        /// demand under stress.
        /// </summary>
        public float MaxRequiredInput;

        /// <summary>
        /// Current required input in [MW].
        /// </summary>
        public float RequiredInput
        {
            get { return m_requiredInput; }
            private set
            {
                if (m_requiredInput != value)
                {
                    var oldValue = m_requiredInput;
                    m_requiredInput = value;
                    if (RequiredInputChanged != null)
                        RequiredInputChanged(this, oldValue, value);
                }
            }
        }

        public float SuppliedRatio
        {
            get;
            private set;
        }

        public float CurrentInput
        {
            get;
            private set;
        }

        public bool IsPowered
        {
            get;
            private set;
        }
        #endregion

        internal event RequiredPowerChangeDelegate RequiredInputChanged;
        public event Action IsPoweredChanged;
        public event Action SuppliedRatioChanged;
        public event Action CurrentInputChanged;

        public MyPowerReceiver(MyConsumerGroupEnum group, bool isAdaptible, float maxRequiredInput, Func<float> requiredInputFunc)
        {
            Group               = group;
            IsAdaptible         = isAdaptible;
            MaxRequiredInput    = maxRequiredInput;
            m_requiredInputFunc = requiredInputFunc;
        }

        /// <summary>
        /// This should be called only from MyPowerDistributor.
        /// </summary>
        public void SetInputFromDistributor(float newCurrentInput)
        {
            float newSuppliedRatio;
            bool newIsPowered;
            if (newCurrentInput > 0f)
            {
                newIsPowered = IsAdaptible || newCurrentInput >= RequiredInput;
                newSuppliedRatio = newCurrentInput / RequiredInput;
            }
            else
            {
                newIsPowered = false;
                newSuppliedRatio = 0f;
            }

            var currentInputChanged  = newCurrentInput != CurrentInput;
            var isPoweredChanged     = (newIsPowered != IsPowered) || m_firstTime;
            var suppliedRatioChanged = newSuppliedRatio != SuppliedRatio;
            m_firstTime = false;

            IsPowered     = newIsPowered;
            SuppliedRatio = newSuppliedRatio;
            CurrentInput  = newCurrentInput;

            if (currentInputChanged && CurrentInputChanged != null)
                CurrentInputChanged();
            if (isPoweredChanged && IsPoweredChanged != null)
                IsPoweredChanged();
            if (suppliedRatioChanged && SuppliedRatioChanged != null)
                SuppliedRatioChanged();
        }

        public void Update()
        {
            // This will fire an event which will update IsPowered and RequiredInputRatio values.
            RequiredInput = m_requiredInputFunc();
        }

        public override string ToString()
        {
            string separator = "; \n";
            m_textCache.Clear();
            m_textCache.AppendFormat("IsPowered: {0}", IsPowered).Append(separator);
            m_textCache.Append("Input: "); MyValueFormatter.AppendWorkInBestUnit(CurrentInput, m_textCache); m_textCache.Append(separator);
            m_textCache.Append("Required: "); MyValueFormatter.AppendWorkInBestUnit(RequiredInput, m_textCache); m_textCache.Append(separator);
            m_textCache.AppendFormat("Ratio: {0}%", SuppliedRatio * 100f);
            return m_textCache.ToString();
        }

        public void DebugDraw(Matrix worldMatrix)
        {
            DebugDraw(ref worldMatrix);
        }

        public void DebugDraw(ref Matrix worldMatrix)
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_POWER_RECEIVERS)
            {
                var position = worldMatrix.Translation + worldMatrix.Up;
                VRageRender.MyRenderProxy.DebugDrawText3D(position, ToString(), Color.White, 0.5f, true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            }
        }

    }
}
