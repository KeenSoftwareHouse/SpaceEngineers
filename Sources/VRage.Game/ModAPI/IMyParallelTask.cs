using System;
using System.Collections.Generic;
using ParallelTasks;

namespace VRage.Game.ModAPI
{
    public interface IMyParallelTask
    {
        Task StartBackground(IWork work, Action completionCallback);
        Task StartBackground(IWork work);
        Task StartBackground(Action action);
        Task StartBackground(Action action, Action completionCallback);

        void Do(IWork a, IWork b);
        void Do(params IWork[] work);
        void Do(Action action1, Action action2);
        void Do(params Action[] actions);

        void For(int startInclusive, int endExclusive, Action<int> body);
        void For(int startInclusive, int endExclusive, Action<int> body, int stride);

        void ForEach<T>(IEnumerable<T> collection, Action<T> action);

        Task Start(Action action, WorkOptions options, Action completionCallback);
        Task Start(Action action, WorkOptions options);
        Task Start(Action action, Action completionCallback);
        Task Start(Action action);
        Task Start(IWork work, Action completionCallback);
        Task Start(IWork work);
    }
}
