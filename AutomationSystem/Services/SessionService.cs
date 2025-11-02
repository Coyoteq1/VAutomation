using System.Collections.Generic;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing user session history.
    /// </summary>
    public class SessionService
    {
        private readonly Dictionary<string, SessionData> sessions = new Dictionary<string, SessionData>();

        public void SaveSession(string userId, SessionData data)
        {
            sessions[userId] = data;
        }

        public SessionData GetSession(string userId)
        {
            return sessions.TryGetValue(userId, out var data) ? data : null;
        }
    }

    public class SessionData
    {
        public string UserId { get; set; }
        public List<string> Events { get; set; } = new List<string>();
    }
}
