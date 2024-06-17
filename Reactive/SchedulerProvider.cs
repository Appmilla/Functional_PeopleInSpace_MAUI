using System.Reactive.Concurrency;
using ReactiveUI;

namespace FunctionalPeopleInSpaceMaui.Reactive;

public interface ISchedulerProvider
{
    IScheduler MainThread { get; }
    IScheduler ThreadPool { get; }
}

public sealed class SchedulerProvider : ISchedulerProvider
{
    public IScheduler MainThread => RxApp.MainThreadScheduler;
    public IScheduler ThreadPool => Scheduler.Default;
}