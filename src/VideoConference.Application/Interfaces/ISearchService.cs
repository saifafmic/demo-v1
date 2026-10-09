using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface ISearchService
{
    Task<ApiResponse<SearchResultDto>> SearchAsync(string query, Guid currentUserId);
}
