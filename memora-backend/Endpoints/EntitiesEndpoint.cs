using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using CsvHelper.Configuration;
using System.Globalization;

namespace AuthApi.Endpoints;

[ApiController]
[Route("api/entities")]
public class EntitesController : BaseApiController
{
    private readonly AppDbContext _db;

    public EntitesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(false);

        query = query.Trim().ToLower();

        var path = Path.Combine(AppContext.BaseDirectory, "dataset", "person_names.csv");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);

        // Skip header if needed
        await reader.ReadLineAsync();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) continue;

            var name = line.Split(',')[0].Trim().ToLower();

            if (name == query)
            {
                return Ok(true);
            }
        }

        return Ok(false);
    }
}