using FunctionalPeopleInSpaceMaui.ViewModels;
using ReactiveUI;

namespace FunctionalPeopleInSpaceMaui.Views;

public partial class DetailPage : ReactiveUI.Maui.ReactiveContentPage<DetailPageViewModel>
{
    public DetailPage(DetailPageViewModel viewModel)
    {
        BindingContext = viewModel;
        ViewModel = viewModel;
        
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
        });
    }
}