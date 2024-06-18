namespace FunctionalPeopleInSpaceMaui.Models;

public abstract record CrewError(string Message);

public record NetworkError(string Message) : CrewError(Message);

public record ParsingError(string Message) : CrewError(Message);

public record CacheError(string Message) : CrewError(Message);