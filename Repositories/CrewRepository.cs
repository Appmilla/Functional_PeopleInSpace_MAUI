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
        return cache.GetOrFetchObject<IReadOnlyList<CrewModel>>(CrewCacheKey,
                FetchAndProcessCrew, expiration)
            .Select(Either<CrewError, IReadOnlyList<CrewModel>>.Right!)
            .Catch<Either<CrewError, IReadOnlyList<CrewModel>>, Exception>(ex =>
                Observable.Return(Either<CrewError, IReadOnlyList<CrewModel>>.Left(new CacheError(ex.Message))));
    }
    
    private async Task<IReadOnlyList<CrewModel>> FetchAndProcessCrew()
    {
        try
        {
            var crewJson = await spaceXApi.GetAllCrew().ConfigureAwait(false);
            var result = CrewModel.FromJson(crewJson);
            return result.Match(
                Right: crew => crew.ToList().AsReadOnly(),
                Left: error => throw new Exception(error.Message)
            );
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Network error: " + ex.Message);
        }
        catch (Exception ex)
        {
            throw new Exception("Unexpected error: " + ex.Message);
        }
    }

    private IObservable<Either<CrewError, IReadOnlyList<CrewModel>>> FetchAndCacheCrew()
    {
        return Observable.FromAsync(FetchAndProcessCrew)
            .Select(Either<CrewError, IReadOnlyList<CrewModel>>.Right)
            .Catch<Either<CrewError, IReadOnlyList<CrewModel>>, Exception>(ex =>
                Observable.Return(Either<CrewError, IReadOnlyList<CrewModel>>.Left(new NetworkError(ex.Message))))
            .SubscribeOn(schedulerProvider.ThreadPool);
    }
}

