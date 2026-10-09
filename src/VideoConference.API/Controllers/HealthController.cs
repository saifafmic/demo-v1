using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly ILiveKitService _liveKitService;

    public HealthController(IAppDbContext dbContext, ILiveKitService liveKitService)
    {
        _dbContext = dbContext;
        _liveKitService = liveKitService;
    }

    /// <summary>
    /// Overall health status check.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var dbOk = false;
        try
        {
            dbOk = await _dbContext.Users.AnyAsync() || true;
        }
        catch
        {
            dbOk = false;
        }

        var liveKitConfigured = !string.IsNullOrWhiteSpace(_liveKitService.GetServerUrl());

        var status = new
        {
            status = dbOk ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            services = new
            {
                database = dbOk ? "Connected" : "Disconnected",
                liveKit = liveKitConfigured ? "Configured" : "MissingConfig",
                liveKitUrl = _liveKitService.GetServerUrl()
            }
        };

        return Ok(status);
    }

    /// <summary>
    /// Liveness probe: responds if the Web API process is running.
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLive()
    {
        return Ok(new { status = "Live", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe: responds if the Web API can connect to database.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReady()
    {
        try
        {
            await _dbContext.Users.Take(1).ToListAsync();
            return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "NotReady",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
