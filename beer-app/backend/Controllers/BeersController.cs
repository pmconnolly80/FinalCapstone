using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BeersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BeersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Beer>>> GetBeers()
    {
        return await _context.Beers.OrderBy(b => b.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Beer>> GetBeer(int id)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        return beer;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Beer>> PostBeer(Beer beer)
    {
        _context.Beers.Add(beer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBeer), new { id = beer.Id }, beer);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutBeer(int id, Beer beer)
    {
        if (id != beer.Id)
        {
            return BadRequest();
        }

        _context.Entry(beer).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBeer(int id)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        _context.Beers.Remove(beer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
