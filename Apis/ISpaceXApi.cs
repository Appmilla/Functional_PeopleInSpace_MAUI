using Refit;

namespace FunctionalPeopleInSpaceMaui.Apis;

public interface ISpaceXApi
{        
    [Get("/crew")]
    Task<string> GetAllCrew();
}