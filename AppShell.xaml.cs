using FunctionalPeopleInSpaceMaui.Views;

namespace FunctionalPeopleInSpaceMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        foreach (var route in Routes.RouteTypeMap)
        {
            Routing.RegisterRoute(route.Key, route.Value);
        }

    }
}