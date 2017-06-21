using System;
using System.Collections.Generic;
using Sandbox.Game.Components;
using Sandbox.Game.SessionComponents;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    public class MyTriggerManipulator
    {
        // Selected trigger
        // Position of last query
        private Vector3D m_currentPosition;
        // All currently queried triggers
        private readonly List<MyTriggerComponent> m_currentQuery = new List<MyTriggerComponent>();
        // Predicate used to evaluate triggers
        private readonly Predicate<MyTriggerComponent> m_triggerEvaluationpPredicate;
        private MyTriggerComponent m_selectedTrigger;

        #region Properties

        /// <summary>
        /// Used to change current position and obtain new quary of triggers.
        /// </summary>
        public Vector3D CurrentPosition
        {
            get { return m_currentPosition; }
            set
            {
                if(value == m_currentPosition)
                    return;

                var oldPosition = m_currentPosition;
                m_currentPosition = value;
                OnPositionChanged(oldPosition, m_currentPosition);
            }
        }

        /// <summary>
        /// Accessor for quaries triggers.
        /// </summary>
        public List<MyTriggerComponent> CurrentQuery
        {
            get { return m_currentQuery; }
        }

        /// <summary>
        /// Selected trigger.
        /// </summary>
        public MyTriggerComponent SelectedTrigger
        {
            get { return m_selectedTrigger; }
            set
            {
                if(m_selectedTrigger == value)
                    return;

                // Back to default color
                if (m_selectedTrigger != null)
                {
                    m_selectedTrigger.CustomDebugColor = Color.Red;
                }

                m_selectedTrigger = value;

                // highlight color
                if (m_selectedTrigger != null)
                {
                    m_selectedTrigger.CustomDebugColor = Color.Yellow;
                }
            }
        }

        #endregion

        public MyTriggerManipulator(Predicate<MyTriggerComponent> triggerEvaluationPredicate = null)
        {
            m_triggerEvaluationpPredicate = triggerEvaluationPredicate;
        }

        protected virtual void OnPositionChanged(Vector3D oldPosition, Vector3D newPosition)
        {
            // Query triggers from current position
            var query = MySessionComponentTriggerSystem.Static.GetIntersectingTriggers(newPosition);

            m_currentQuery.Clear();
            foreach (var trigger in query)
            {
                // Try to evaluate the triggers by predicate.
                if (m_triggerEvaluationpPredicate != null)
                {
                    if(m_triggerEvaluationpPredicate(trigger))
                        m_currentQuery.Add(trigger);
                }
                else
                {
                    m_currentQuery.Add(trigger);
                }
            }
        }

        /// <summary>
        /// Selects the closest trigger to provided position.
        /// </summary>
        /// <param name="position">Considered position.</param>
        public void SelectClosest(Vector3D position)
        {
            var minLength = double.MaxValue;

            // Change the debug draw color from previously selected to default color.
            if (SelectedTrigger != null)
                SelectedTrigger.CustomDebugColor = Color.Red;

            // Find the min length
            foreach (var trigger in m_currentQuery)
            {
                var length = (trigger.Center - position).LengthSquared();
                if (length < minLength)
                {
                    minLength = length;
                    SelectedTrigger = trigger;
                }
            }

            if (Math.Abs(minLength - double.MaxValue) < double.Epsilon)
                SelectedTrigger = null;
            
            // Change the trigger debug draw color to selected.
            if (SelectedTrigger != null)
                SelectedTrigger.CustomDebugColor = Color.Yellow;
        }
    }
}
