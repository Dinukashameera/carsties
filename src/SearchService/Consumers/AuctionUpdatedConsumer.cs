using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("INSIDE THE UPDATE CONUSMER");
        var auction = _mapper.Map<Item>(context.Message);
        var result = await DB.Update<Item>().Match(a => a.ID == context.Message.Id).ModifyOnly(x => new
        {
            x.Color,
            x.Make,
            x.Model,
            x.Year,
            x.Mileage
        }, auction).ExecuteAsync();
        if (!result.IsAcknowledged)
        {
            throw new MessageException(typeof(AuctionUpdated), "Failed to update auction in Search DB");
        }
        // Handle the auction updated event (e.g., update search index)
    }
}
