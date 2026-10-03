namespace SkyTech.Services
{
    public interface IPushNotificationService
    {
        Task SendToStaffAsync(
            int staffId,
            string title,
            string body,
            string type,
            string referenceId,
            TimeSpan timeToLive);
    }
}