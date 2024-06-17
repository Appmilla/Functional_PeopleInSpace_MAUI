using FunctionalPeopleInSpaceMaui.ViewModels;
using ReactiveUI;

namespace FunctionalPeopleInSpaceMaui.Views;


public partial class MainPage : ReactiveUI.Maui.ReactiveContentPage<MainPageViewModel>
{
    public MainPage(MainPageViewModel viewModel)
    {
        BindingContext = viewModel;
        ViewModel = viewModel;
        
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
        });
    }

    protected override void OnAppearing()
    {
        ViewModel?.LoadCommand.Execute(false).Subscribe();
        
        base.OnAppearing();
    }
}