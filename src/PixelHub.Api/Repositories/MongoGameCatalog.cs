using MongoDB.Bson;
using MongoDB.Driver;
using PixelHub.Api.Models;

namespace PixelHub.Api.Repositories;

public class MongoGameCatalog : IGameCatalog
{
    private readonly IMongoCollection<Game> _jeux;

    public MongoGameCatalog(IMongoDatabase database)
    {
        _jeux = database.GetCollection<Game>("jeux");
    }

    public async Task<List<Game>> GetAllAsync() =>
        await _jeux.Find(_ => true).ToListAsync();

    public async Task<List<Game>> GetByGenreAsync(string genre) =>
        await _jeux.Find(g => g.Genre == genre).ToListAsync();

    public async Task<List<Game>> GetTopRatedAsync(int count) =>
        await _jeux.Find(_ => true)
                   .SortByDescending(g => g.Note).ThenBy(g => g.Titre)
                   .Limit(count)
                   .ToListAsync();

    public async Task<Game?> GetByIdAsync(string id) =>
        ObjectId.TryParse(id, out var oid)
            ? await _jeux.Find(g => g.Id == oid).FirstOrDefaultAsync()
            : null;

    public async Task<List<GenreStats>> GetStatsByGenreAsync()
    {
        return await _jeux.Aggregate()
            .Group(g => g.Genre, group => new GenreStats(
                group.Key,
                group.Average(g => g.Note),
                group.Count()))
            .SortByDescending(s => s.NoteMoyenne)
            .ToListAsync();
    }
}