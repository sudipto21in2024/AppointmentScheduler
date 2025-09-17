using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;

namespace NotificationService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(SendNotificationDto notificationDto)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = notificationDto.UserId,
                Title = notificationDto.Title,
                Message = notificationDto.Message,
                Type = notificationDto.Type,
                IsRead = false,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // In a real-world scenario, this would involve integrating with
            // email/SMS/push notification providers based on notificationDto.Type
            Console.WriteLine($"Notification sent: {notification.Title} to user {notification.UserId}");
        }

        public async Task<NotificationPreferenceDto> GetNotificationPreferencesAsync(Guid userId)
        {
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                // Return default preferences if none are found
                return new NotificationPreferenceDto
                {
                    UserId = userId,
                    EmailEnabled = true,
                    SmsEnabled = true,
                    PushEnabled = true
                };
            }

            return new NotificationPreferenceDto
            {
                UserId = preferences.UserId,
                EmailEnabled = preferences.EmailEnabled,
                SmsEnabled = preferences.SmsEnabled,
                PushEnabled = preferences.PushEnabled,
                PreferredTimezone = preferences.PreferredTimezone
            };
        }

        public async Task UpdateNotificationPreferencesAsync(Guid userId, NotificationPreferenceDto preferencesDto)
        {
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                preferences = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EmailEnabled = preferencesDto.EmailEnabled,
                    SmsEnabled = preferencesDto.SmsEnabled,
                    PushEnabled = preferencesDto.PushEnabled,
                    PreferredTimezone = preferencesDto.PreferredTimezone
                };
                _context.NotificationPreferences.Add(preferences);
            }
            else
            {
                preferences.EmailEnabled = preferencesDto.EmailEnabled;
                preferences.SmsEnabled = preferencesDto.SmsEnabled;
                preferences.PushEnabled = preferencesDto.PushEnabled;
                preferences.PreferredTimezone = preferencesDto.PreferredTimezone;
                _context.NotificationPreferences.Update(preferences);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<NotificationHistoryDto[]> GetNotificationHistoryAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Select(n => new NotificationHistoryDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    SentAt = n.SentAt,
                    Status = "Sent", // Placeholder, as delivery status tracking is not fully implemented here
                    RelatedEntityId = n.RelatedEntityId
                })
                .ToArrayAsync();
        }
    }
}