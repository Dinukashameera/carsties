using System;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;

public class DBInitializer
{
    public static async Task InitDB(WebApplication app)
    {

        await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        await DB.Index<Item>()
            .Key(x => x.Model, KeyType.Text)
            .Key(x => x.Make, KeyType.Text)
            .Key(x => x.Color, KeyType.Text)
            .CreateAsync();

        var count = await DB.CountAsync<Item>();

        using var scope = app.Services.CreateScope();
        var httpCleint = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();
        var item = await httpCleint.GetItemsforSearchDB();

        Console.WriteLine(item.Count + " return from the auction sevice");
        if (item.Count > 0) await DB.SaveAsync(item);
    }
}
