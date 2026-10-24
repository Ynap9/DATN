using System.Collections.Concurrent;

namespace sh.be.infrastructure.external.SignalR.Base
{
    public class SignalRConnectionTracker
    {
        private readonly ConcurrentDictionary<string, string?> _userIdByConnectionId = new();

        public int ConnectionCount => _userIdByConnectionId.Count;

        public void Add(string connectionId, string? userId)
        {
            _userIdByConnectionId[connectionId] = userId;
        }

        public void Remove(string connectionId)
        {
            _userIdByConnectionId.TryRemove(connectionId, out _);
        }

        public int CountByUserId(string userId)
        {
            return _userIdByConnectionId.Values.Count(connectionUserId => connectionUserId == userId);
        }
    }
}
