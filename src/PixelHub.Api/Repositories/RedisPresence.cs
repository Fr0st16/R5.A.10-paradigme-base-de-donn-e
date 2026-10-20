using StackExchange.Redis;

namespace PixelHub.Api.Repositories;

// Joueurs en ligne : sorted set dont le score est l'horodatage (ms) du dernier signe de vie.
// Un joueur est « en ligne » si son score est plus récent que (maintenant - fenêtre).
public class RedisPresence
{
    private const string Key = "enligne";
    private readonly IDatabase _db;
    private readonly TimeSpan _window;

    public RedisPresence(IConnectionMultiplexer redis, IConfiguration config)
    {
        _db = redis.GetDatabase();
        _window = TimeSpan.FromSeconds(config.GetValue("Presence:WindowSeconds", 60));
    }

    private static double Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public async Task TouchAsync(string pseudo) =>
        await _db.SortedSetAddAsync(Key, pseudo, Now());                 // ZADD enligne <ts> <pseudo>

    public async Task<long> CountOnlineAsync()
    {
        var limite = Now() - _window.TotalMilliseconds;
        await _db.SortedSetRemoveRangeByScoreAsync(Key, double.NegativeInfinity, limite);   // purge les expirés
        return await _db.SortedSetLengthAsync(Key);                                         // ZCARD
    }
}
