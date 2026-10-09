using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConference.Application.Common;
using VideoConference.Application.DTOs;
using VideoConference.Application.Interfaces;

namespace VideoConference.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Get all registered users (Admin only).
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _adminService.GetUsersAsync();
        return Ok(result);
    }

    /// <summary>
    /// Activate a user account (Admin only).
    /// </summary>
    [HttpPut("users/{id}/activate")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var result = await _adminService.ToggleUserStatusAsync(id, true);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deactivate a user account and immediately revoke access (Admin only).
    /// </summary>
    [HttpPut("users/{id}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var result = await _adminService.ToggleUserStatusAsync(id, false);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all meetings across the platform (Admin only).
    /// </summary>
    [HttpGet("meetings")]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMeetings()
    {
        var result = await _adminService.GetAllMeetingsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get complete global audit history logs (Admin only).
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<MeetingHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGlobalHistory()
    {
        var result = await _adminService.GetAllHistoryAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get overall platform statistics (total users, meetings, recordings) (Admin only).
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<AdminStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _adminService.GetStatisticsAsync();
        return Ok(result);
    }
}
