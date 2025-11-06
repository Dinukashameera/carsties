using System;
using AuctionService.Entities;
using Contracts;
using MassTransit;

namespace AuctionService.Consumer;

public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
{
    public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
    {
        Console.WriteLine("--> Consuming AuctionCreated Fault Message");
        var exceptions = context.Message.Exceptions.First();
        if (exceptions.ExceptionType == "System.ArgumentException")
        {
            context.Message.Message.Model = "FooBar";
            Console.WriteLine("---------- ******* > Consuming AuctionCreated Fault Message", context.Message.Message);

            await context.Publish(context.Message.Message);
        }
        else
        {
            Console.WriteLine("---------- ******* > Unknown exception type", exceptions.ExceptionType);
        }
    }
}
