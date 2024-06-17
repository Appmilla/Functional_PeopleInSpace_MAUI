namespace FunctionalPeopleInSpaceMaui.Models;

public abstract class CrewError(string message)
{
    public string Message { get; } = message;
}

public class NetworkError(string message) : CrewError(message);

public class ParsingError(string message) : CrewError(message);

public class CacheError(string message) : CrewError(message);