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
using Microsoft.Data.Sqlite;

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

        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "dataset",
            "names.db"
        );

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM PersonNames WHERE Name = $name COLLATE NOCASE LIMIT 1";
        command.Parameters.AddWithValue("$name", query.Trim());

        var result = await command.ExecuteScalarAsync();

        return Ok(result != null);
    }
}