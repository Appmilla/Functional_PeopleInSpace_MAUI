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
    IObservable<Either<Exception, IReadOnlyList<CrewModel>>>GetCrew(bool forceRefresh = false);
}

public class CrewRepository(
    ISchedulerProvider schedulerProvider,
    ISpaceXApi spaceXApi,
    IBlobCache cache)
    : ReactiveObject, ICrewRepository
{
    private const string CrewCacheKey = "crew_cache_key";
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromDays(1);

    [Reactive]
    public bool IsBusy { get; set; }

    public IObservable<Either<Exception, IReadOnlyList<CrewModel>>> GetCrew(bool forceRefresh = false)
    {
        return Observable.Defer(() =>
        {
            IsBusy = true;
            if (forceRefresh)
            {
                return FetchAndCacheCrew();
            }

            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;
            return cache.GetOrFetchObject<IReadOnlyList<CrewModel>>(CrewCacheKey, FetchAndProcessCrew, expiration)
                .Select(crew => Either<Exception, IReadOnlyList<CrewModel>>.Right(crew))
                .Catch<Either<Exception, IReadOnlyList<CrewModel>>, Exception>(ex => Observable.Return(Either<Exception, IReadOnlyList<CrewModel>>.Left(ex)))
                .Do(_ => IsBusy = false);
        }).SubscribeOn(schedulerProvider.ThreadPool);
    }

    private IObservable<Either<Exception, IReadOnlyList<CrewModel>>> FetchAndCacheCrew()
    {
        return Observable.FromAsync(async () =>
            {
                try
                {
                    var crew = await FetchAndProcessCrew().ConfigureAwait(false);
                    return Either<Exception, IReadOnlyList<CrewModel>>.Right(crew);
                }
                catch (Exception ex)
                {
                    return Either<Exception, IReadOnlyList<CrewModel>>.Left(ex);
                }
            }).Do(_ => IsBusy = false)
            .SubscribeOn(schedulerProvider.ThreadPool);
    }

    private async Task<IReadOnlyList<CrewModel>> FetchAndProcessCrew()
    {
        var crewJson = await spaceXApi.GetAllCrew().ConfigureAwait(false);
        var crew = CrewModel.FromJson(crewJson).ToList().AsReadOnly();
        await cache.InsertObject(CrewCacheKey, crew, DateTimeOffset.Now + _cacheLifetime);
        return crew;
    }
}