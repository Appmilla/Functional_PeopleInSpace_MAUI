using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FunctionalPeopleInSpaceMaui.Alerts;
using FunctionalPeopleInSpaceMaui.Models;
using FunctionalPeopleInSpaceMaui.Navigation;
using FunctionalPeopleInSpaceMaui.Reactive;
using FunctionalPeopleInSpaceMaui.Repositories;
using FunctionalPeopleInSpaceMaui.Extensions;
using Unit = System.Reactive.Unit;

namespace FunctionalPeopleInSpaceMaui.ViewModels;

public class MainPageViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly ICrewRepository _crewRepository;
    private readonly INavigationService _navigationService;
    private readonly IUserAlerts _userAlerts;

    [Reactive]
    public string PageTitle { get; private set; } = "People In Space Functional MAUI";

    [ObservableAsProperty]
    public bool IsRefreshing { get; }

    public ReactiveCommand<bool, Either<CrewError, IReadOnlyList<CrewModel>>> LoadCommand { get; }

    public ReactiveCommand<CrewModel, Unit> NavigateToDetailCommand { get; private set; }

    public ReadOnlyObservableCollection<CrewModel> Crew { get; private set; }

    private readonly SourceCache<CrewModel, string> _crewCache = new(crew => crew.Id);

    public ViewModelActivator Activator { get; } = new();

    public MainPageViewModel(
        ISchedulerProvider schedulerProvider,
        ICrewRepository crewRepository,
        INavigationService navigationService,
        IUserAlerts userAlerts)
    {
        _schedulerProvider = schedulerProvider;
        _crewRepository = crewRepository;
        _navigationService = navigationService;
        _userAlerts = userAlerts;

        var crewSort = SortExpressionComparer<CrewModel>.Ascending(c => c.Name);

        var crewSubscription = _crewCache.Connect()
            .Sort(crewSort)
            .Bind(out var crew)
            .ObserveOn(_schedulerProvider.MainThread)        
            .DisposeMany()                              
            .Subscribe();

        Crew = crew;

        LoadCommand = ReactiveCommand.CreateFromObservable<bool, Either<CrewError, IReadOnlyList<CrewModel>>>(
            crewRepository.GetCrew,
            outputScheduler: _schedulerProvider.MainThread);

        LoadCommand.ThrownExceptions.Subscribe(ex => ShowError(ex.Message));
        LoadCommand.Subscribe(result => result.Match(
            Right: UpdateCrew,
            Left: error => ShowError(error.Message)));

        NavigateToDetailCommand = ReactiveCommand.Create<CrewModel>(NavigateToDetail);

        this.WhenActivated(disposables =>
        {
            /*
            LoadCommand.IsExecuting.ToPropertyEx(
                    this,
                    x => x.IsRefreshing,
                    scheduler: _schedulerProvider.MainThread)
                .DisposeWith(disposables);*/
            
            
            this.WhenAnyValue(x => x._crewRepository.IsBusy)
                .ObserveOn(_schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsRefreshing, scheduler: _schedulerProvider.MainThread)
                .DisposeWith(disposables);
            
            disposables.Add(crewSubscription);
        });
    }

    private void UpdateCrew(IReadOnlyList<CrewModel> crew)
    {
        _crewCache.Edit(cache => cache.AddOrUpdate(crew));
    }

    private void ShowError(string message)
    {
        _userAlerts.ShowToast(message).FireAndForgetSafeAsync();
    }

    private void NavigateToDetail(CrewModel crewMember)
    {
        var parameters = new Dictionary<string, string>
        {
            ["name"] = Uri.EscapeDataString(crewMember.Name),
            ["image"] = Uri.EscapeDataString(crewMember.Image.ToString()),
            ["wikipedia"] = Uri.EscapeDataString(crewMember.Wikipedia.ToString())
        };
        var route = $"{Routes.DetailPage}?{string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"))}";
        _navigationService.NavigateAsync(route);
    }
}