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
    IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> GetCrew(bool forceRefresh = false);
}

public class CrewRepository(ISchedulerProvider schedulerProvider, ISpaceXApi spaceXApi, IBlobCache cache)
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
            var fetchObservable = forceRefresh ? FetchAndCacheCrew() : FetchFromCacheOrApi();
            return fetchObservable.Do(_ => IsBusy = false);
        }).SubscribeOn(schedulerProvider.ThreadPool);
    }

    private IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> FetchFromCacheOrApi()
    {
        DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;
        return cache.GetOrFetchObject(CrewCacheKey,
                async () => await FetchAndProcessCrew(), expiration)
            .Catch((Exception ex) =>
                Observable.Return(Either<CrewError, IReadOnlyList<CrewModel>>.Left(new CacheError(ex.Message))));
    }

    private async Task<Either<CrewError, IReadOnlyList<CrewModel>>> FetchAndProcessCrew()
    {
        try
        {
            var crewJson = await spaceXApi.GetAllCrew().ConfigureAwait(false);
            var result = CrewModel.FromJson(crewJson);
            return result.Match(
                Right: crew => Right<CrewError, IReadOnlyList<CrewModel>>(crew.ToList().AsReadOnly()),
                Left: Left<CrewError, IReadOnlyList<CrewModel>>
            );
        }
        catch (HttpRequestException ex)
        {
            return Left<CrewError, IReadOnlyList<CrewModel>>(new NetworkError("Network error: " + ex.Message));
        }
        catch (Exception ex)
        {
            return Left<CrewError, IReadOnlyList<CrewModel>>(new CacheError("Unexpected error: " + ex.Message));
        }
    }

    private IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> FetchAndCacheCrew()
    {
        return Observable.FromAsync(FetchAndProcessCrew)
            .Catch((Exception ex) =>
                Observable.Return(Either<CrewError, IReadOnlyList<CrewModel>>.Left(new NetworkError(ex.Message))))
            .SubscribeOn(schedulerProvider.ThreadPool);
    }
}

