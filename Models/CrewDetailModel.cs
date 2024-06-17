namespace FunctionalPeopleInSpaceMaui.Models;

public class CrewDetailModel(
    string name,
    Uri image,
    Uri wikipedia)
{
    public string Name { get; } = name;
    
    public Uri Image { get; } = image;
    
    public Uri Wikipedia { get; } = wikipedia;
}