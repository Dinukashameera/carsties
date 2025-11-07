using System;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controller;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _contex;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionDbContext contex, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        _contex = contex;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDTO>>> GetAllAuctions(string date)
    {
        var query = _contex.Auctions.OrderBy(x => x.Item.Make).AsQueryable();
        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }
        try
        {

            return await query.ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider).ToListAsync();
        }
        catch (Exception e)
        {
            Console.Write($"Error when gett auctions {e.Message}");

            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDTO>> GetAuctionById(Guid id)
    {
        var auction = await _contex.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
        if (auction == null) return NotFound();
        return _mapper.Map<AuctionDTO>(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDTO>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);
        // add current user as seller
        auction.Seller = "Test seller";
        _contex.Auctions.Add(auction);

        var newAuction = _mapper.Map<AuctionDTO>(auction);
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

        var result = await _contex.SaveChangesAsync() > 0;
        if (!result) return BadRequest("Could not save changes to the DB");

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AuctionDTO>> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _contex.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
        if (auction == null) return NotFound();

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        // publishing updates auction to the message bus
        var updatedAuction = _mapper.Map<AuctionUpdated>(auction);
        await _publishEndpoint.Publish(updatedAuction);

        var result = await _contex.SaveChangesAsync() > 0;

        if (result) return Ok();

        return BadRequest("Problem saving cahanges");
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _contex.Auctions.FindAsync(id);
        if (auction == null) return NotFound();

        _contex.Auctions.Remove(auction);

        // publishing deleted auction id to the message bus
        await _publishEndpoint.Publish<AuctionDeleted>(new { id = auction.Id.ToString() });

        var result = await _contex.SaveChangesAsync() > 0;
        if (!result) return BadRequest("could not update DB");

        return Ok();

    }

};
