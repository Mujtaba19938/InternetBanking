using InternetBanking.Models;
using InternetBanking.Data;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type, int? relatedEntityId = null, string? relatedEntityType = null, string status = "info");
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool includeRead = false);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task MarkAllNotificationsAsReadAsync(string userId);
        
        // Card request specific methods
        Task CreateCardRequestApprovalNotificationAsync(string userId, int requestId);
        Task CreateCardRequestRejectionNotificationAsync(string userId, int requestId);
        Task CreateCardReadyNotificationAsync(string userId, int requestId);
        Task CheckAndCreateCardReadyNotificationsAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type, int? relatedEntityId = null, string? relatedEntityType = null, string status = "info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Status = status,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedDate = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool includeRead = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateCardRequestApprovalNotificationAsync(string userId, int requestId)
        {
            var title = "Response to Debit Card Request";
            var message = "Your request has been accepted. Your card will be available at your nearest branch within 5–10 business days.";
            
            await CreateNotificationAsync(
                userId,
                title,
                message,
                "CardRequest",
                requestId,
                "ServiceRequest",
                "success"
            );
        }

        public async Task CreateCardRequestRejectionNotificationAsync(string userId, int requestId)
        {
            var title = "Response to Debit Card Request";
            var message = "Your Debit Card Request has been rejected.";
            
            await CreateNotificationAsync(
                userId,
                title,
                message,
                "CardRequest",
                requestId,
                "ServiceRequest",
                "danger"
            );
        }

        public async Task CreateCardReadyNotificationAsync(string userId, int requestId)
        {
            var title = "Card Ready for Pickup";
            var message = "Your card has arrived at your nearest branch. Please collect it as soon as possible.";
            
            await CreateNotificationAsync(
                userId,
                title,
                message,
                "CardRequest",
                requestId,
                "ServiceRequest",
                "info"
            );
        }

        public async Task CheckAndCreateCardReadyNotificationsAsync()
        {
            // Find all approved card requests where ETA has been reached but status is not yet "ready"
            var readyCards = await _context.ServiceRequests
                .Where(sr => sr.RequestType == "Debit Card Request" 
                    && sr.CardStatus == "approved" 
                    && sr.EtaDate.HasValue 
                    && sr.EtaDate <= DateTime.Now)
                .ToListAsync();

            foreach (var cardRequest in readyCards)
            {
                // Update status to ready
                cardRequest.CardStatus = "ready";
                
                // Create notification
                await CreateCardReadyNotificationAsync(cardRequest.UserId, cardRequest.RequestId);
            }

            if (readyCards.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
