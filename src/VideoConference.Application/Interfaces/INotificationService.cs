using VideoConference.Application.Common;
using VideoConference.Application.DTOs;

namespace VideoConference.Application.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(Guid userId);
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ApiResponse<bool>> MarkAllAsReadAsync(Guid userId);
    Task CreateNotificationAsync(Guid userId, string type, string title, string message);
}
