using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

namespace FunctionalPeopleInSpaceMaui.Alerts;

public interface IUserAlerts    
{
    Task ShowToast(string message);
    
    Task ShowSnackbar(string message, TimeSpan duration);
}

public class UserAlerts : IUserAlerts
{
    private const ToastDuration Duration = ToastDuration.Short;
    private const double FontSize = 14;
    private const double CornerRadius = 4;
    
    public async Task ShowToast(string message)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        
        var toast = Toast.Make(message, Duration, FontSize);

        await toast.Show(cancellationTokenSource.Token);
    }

    public async Task ShowSnackbar(string message, TimeSpan duration)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var snackbarOptions = new SnackbarOptions
        {
            BackgroundColor = Color.FromArgb("#FFDE1920"), // FF for full opacity, followed by RGB
            TextColor = Colors.White,
            ActionButtonTextColor = Colors.White,
            CornerRadius = new CornerRadius(CornerRadius),
            Font = Font.SystemFontOfSize(FontSize),
            ActionButtonFont = Font.SystemFontOfSize(FontSize)
        };
        
        var snackbar = Snackbar.Make(message, duration: duration, visualOptions:snackbarOptions);

        await snackbar.Show(cancellationTokenSource.Token);
    }
}