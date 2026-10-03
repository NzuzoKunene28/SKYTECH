public class JitsiService
{
    private const string JitsiBaseUrl = "https://meet.jit.si";

    public string GenerateMeetingLink(int interviewId, int customerId)
    {
        // Unique, hard-to-guess room name so candidates can't stumble into each other's interviews
        var roomName = $"SkyTech-Interview-{interviewId}-{customerId}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        return $"{JitsiBaseUrl}/{roomName}";
    }
}