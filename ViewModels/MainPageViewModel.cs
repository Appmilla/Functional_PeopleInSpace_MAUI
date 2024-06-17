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
    public string PageTitle { get; set; }
    
    [ObservableAsProperty]
    public bool IsRefreshing { get; }
    
    public ReactiveCommand<bool, Either<CrewError, IReadOnlyList<CrewModel>>> LoadCommand { get; }
    
    public ReactiveCommand<CrewModel, Unit> NavigateToDetailCommand { get; private set; }
    
    private ReadOnlyObservableCollection<CrewModel> _crew;

    public ReadOnlyObservableCollection<CrewModel> Crew
    {
        get => _crew;
        set => this.RaiseAndSetIfChanged(ref _crew, value);
    }

    private static readonly Func<CrewModel, string> KeySelector = crew => crew.Id;
    private readonly SourceCache<CrewModel, string> _crewCache = new(KeySelector);
        
    public ViewModelActivator Activator { get; } = new();
    
    public MainPageViewModel(ISchedulerProvider schedulerProvider,
        ICrewRepository crewRepository,
        INavigationService navigationService,
        IUserAlerts userAlerts)
    {
        _schedulerProvider = schedulerProvider;
        _crewRepository = crewRepository;
        _navigationService = navigationService;
        _userAlerts = userAlerts;
        
        PageTitle = "People In Space Functional MAUI";
        
        var crewSort = SortExpressionComparer<CrewModel>
            .Ascending(c => c.Name);

        var crewSubscription = _crewCache.Connect()
            .Sort(crewSort)
            .Bind(out _crew)
            .ObserveOn(_schedulerProvider.MainThread)        
            .DisposeMany()                              
            .Subscribe();
        
        LoadCommand = ReactiveCommand.CreateFromObservable<bool, Either<CrewError, IReadOnlyList<CrewModel>>>(
            forceRefresh =>  _crewRepository.GetCrew(forceRefresh),
            this.WhenAnyValue(x => x.IsRefreshing).Select(x => !x), 
            outputScheduler: _schedulerProvider.MainThread); 
        LoadCommand.ThrownExceptions.Subscribe(Crew_OnException);
        LoadCommand.Subscribe(Crew_OnNext);
        
        NavigateToDetailCommand = ReactiveCommand.Create<CrewModel>(NavigateToDetail);
        
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x._crewRepository.IsBusy)
                .ObserveOn(_schedulerProvider.MainThread)
                .ToPropertyEx(this, x => x.IsRefreshing, scheduler: _schedulerProvider.MainThread)
                .DisposeWith(disposables);
            
            disposables.Add(crewSubscription);
        });
    }
    
    private void Crew_OnNext(Either<CrewError, IReadOnlyList<CrewModel>> result)
    {
        result.Match(
            crew =>
            {
                if (crew != null) UpdateCrew(crew);
            },
            Crew_OnError);
    }
    
    private void UpdateCrew(IReadOnlyList<CrewModel> crew)
    {
        _crewCache.Edit(innerCache =>
        {
            innerCache.AddOrUpdate(crew);
        });
    }

    private void Crew_OnException(Exception e)
    {
        ShowError(e.Message);
    }
    
    private void Crew_OnError(CrewError e)
    {
        switch (e)
        {
            case NetworkError:
                ShowError(e.Message);
                break;
            case ParsingError:
                _userAlerts.ShowSnackbar(e.Message, TimeSpan.FromSeconds(5));
                break;
            default:
                ShowError(e.Message);
                break;
        }
    }

    private void ShowError(string message)
    {
        _userAlerts.ShowToast(message).FireAndForgetSafeAsync();
    }
    
    private void NavigateToDetail(CrewModel crewMember)
    {
        var name = Uri.EscapeDataString(crewMember.Name);
        var image = Uri.EscapeDataString(crewMember.Image.ToString());
        var wikipedia = Uri.EscapeDataString(crewMember.Wikipedia.ToString());
        
        var route = $"{Routes.DetailPage}?name={name}&image={image}&wikipedia={wikipedia}";
        _navigationService.NavigateAsync(route);
    }
}   