using System.Reactive.Linq;
using Akavache;
using FunctionalPeopleInSpaceMaui.Apis;
using FunctionalPeopleInSpaceMaui.Models;
using FunctionalPeopleInSpaceMaui.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FunctionalPeopleInSpaceMaui.Repositories;

public interface ICrewRepository
{
    bool IsBusy { get; set; }
    IObservable<Either<CrewError, IReadOnlyList<CrewModel>>>GetCrew(bool forceRefresh = false);
}

public class CrewRepository(
    ISchedulerProvider schedulerProvider,
    ISpaceXApi spaceXApi,
    IBlobCache cache)
    : ReactiveObject, ICrewRepository
{
    private const string CrewCacheKey = "crew_cache_key";
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromDays(1);

    [Reactive] public bool IsBusy { get; set; }

    public IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> GetCrew(bool forceRefresh = false)
    {
        return Observable.Defer(() =>
        {
            IsBusy = true;
            if (forceRefresh)
            {
                return FetchAndCacheCrew();
            }

            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;
            return cache.GetOrFetchObject<IReadOnlyList<CrewModel>>(CrewCacheKey, () => FetchAndProcessCrew()
                    .Select(either => either.Match(
                        Right: crew => crew,
                        Left: error => throw new Exception(error.Message)
                    )), expiration)
                .Select(crew => Either<CrewError, IReadOnlyList<CrewModel>>.Right(crew))
                .Catch<Either<CrewError, IReadOnlyList<CrewModel>>, Exception>(ex =>
                    Observable.Return(Either<CrewError, IReadOnlyList<CrewModel>>.Left(new CacheError(ex.Message))))
                .Do(_ => IsBusy = false);
        }).SubscribeOn(schedulerProvider.ThreadPool);
    }

    private IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> FetchAndCacheCrew()
    {
        return Observable.FromAsync(FetchAndProcessCrew)
            .Do(_ => IsBusy = false)
            .SubscribeOn(schedulerProvider.ThreadPool);
    }

    private async Task<Either<CrewError, IReadOnlyList<CrewModel>>> FetchAndProcessCrew()
    {
        try
        {
            var crewJson = await spaceXApi.GetAllCrew().ConfigureAwait(false);
            var result = CrewModel.FromJson(crewJson);
            return result.Match(
                Right: crew =>
                {
                    var readOnlyCrew = crew.ToList().AsReadOnly();
                    cache.InsertObject(CrewCacheKey, readOnlyCrew, DateTimeOffset.Now + _cacheLifetime).Wait();
                    return Either<CrewError, IReadOnlyList<CrewModel>>.Right(readOnlyCrew);
                },
                Left: error => Either<CrewError, IReadOnlyList<CrewModel>>.Left(error)
            );
        }
        catch (HttpRequestException ex)
        {
            return Either<CrewError, IReadOnlyList<CrewModel>>.Left(new NetworkError("Network error: " + ex.Message));
        }
        catch (Exception ex)
        {
            return Either<CrewError, IReadOnlyList<CrewModel>>.Left(
                new NetworkError("Unexpected error: " + ex.Message));
        }
    }
}