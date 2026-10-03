namespace SkyTech.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(ILogger<PushNotificationService> logger)
        {
            _logger = logger;
        }

        public async Task SendToStaffAsync(
            int staffId,
            string title,
            string body,
            string type,
            string referenceId,
            TimeSpan timeToLive)
        {
            try
            {
                // TODO: look up the staff member's device token(s)
                // and send via your push provider (e.g. Firebase Cloud Messaging).
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // The controller comment says this must never throw,
                // so the approval can't be undone by a push failure.
                _logger.LogError(ex, "Push notification failed for staff {StaffId}", staffId);
            }
        }
    }
}