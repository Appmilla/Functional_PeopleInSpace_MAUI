using FunctionalPeopleInSpaceMaui.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Unit = System.Reactive.Unit;

namespace FunctionalPeopleInSpaceMaui.ViewModels;

public class DetailPageViewModel : ReactiveObject, IQueryAttributable
{
    [Reactive]
    public string PageTitle { get; set; } = "Biography";

    [Reactive] 
    public CrewDetailModel? CrewMember { get; private set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var crewMemberOption = ParseQueryAttributes(query);
        _ = crewMemberOption.Match(
            Some: crewMember => 
            {
                CrewMember = crewMember;
                return Unit.Default;
            },
            None: () => throw new ArgumentException("Invalid or missing query parameters.")
        );
    }

    private static Option<CrewDetailModel> ParseQueryAttributes(IDictionary<string, object> query)
    {
        var nameOption = GetQueryValue(query, "name");
        var imageOption = GetQueryValue(query, "image")
            .Bind(image => Uri.TryCreate(image, UriKind.Absolute, out var imageUri) ? Some(imageUri) : None);
        
        var wikipediaOption = GetQueryValue(query, "wikipedia")
            .Bind(wikipedia => Uri.TryCreate(wikipedia, UriKind.Absolute, out var wikipediaUri) ? Some(wikipediaUri) : None);

        return nameOption.Bind(name => imageOption.Bind(image => wikipediaOption.Map(wikipedia => new CrewDetailModel(name, image, wikipedia))));
    }

    private static Option<string> GetQueryValue(IDictionary<string, object> query, string key)
    {
        return query.TryGetValue(key, out var value) && value is string stringValue
            ? Some(Uri.UnescapeDataString(stringValue))
            : None;
    }
}