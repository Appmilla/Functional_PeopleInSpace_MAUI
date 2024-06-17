using System.Reactive;
using Akavache;
using epj.RouteGenerator;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Refit;
using CommunityToolkit.Maui;
using FunctionalPeopleInSpaceMaui.Alerts;
using FunctionalPeopleInSpaceMaui.Apis;
using FunctionalPeopleInSpaceMaui.Navigation;
using FunctionalPeopleInSpaceMaui.Network;
using FunctionalPeopleInSpaceMaui.Reactive;
using FunctionalPeopleInSpaceMaui.Repositories;
using FunctionalPeopleInSpaceMaui.ViewModels;
using FunctionalPeopleInSpaceMaui.Views;

namespace FunctionalPeopleInSpaceMaui;

[AutoRoutes("Page")]
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Akavache.Registrations.Start("PeopleInSpace");

        RxApp.DefaultExceptionHandler = new AnonymousObserver<Exception>(ex =>
        {
            App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        });
        
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        builder.Services.AddSingleton<IBlobCache>(BlobCache.LocalMachine);
        
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddScoped<MainPageViewModel>();
        builder.Services.AddTransient<DetailPage>();
        builder.Services.AddScoped<DetailPageViewModel>();
        builder.Services.AddSingleton<ICrewRepository, CrewRepository>();
        builder.Services.AddSingleton<ISchedulerProvider, SchedulerProvider>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IUserAlerts, UserAlerts>();
        builder.Services.AddRefitClient<ISpaceXApi>().ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.spacexdata.com/v4"));
        builder.Services.AddSingleton<IConnectivity>(provider => Connectivity.Current);
        builder.Services.AddSingleton<INetworkStatusObserver, NetworkStatusObserver>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}