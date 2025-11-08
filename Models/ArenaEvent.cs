using System;

namespace CrowbaneArena
{
    /// <summary>
    /// Represents events in the arena system.
    /// </summary>
    public class ArenaEvent
    {
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public ArenaEvent(string type, string desc)
        {
            EventType = type;
            Description = desc;
            Timestamp = DateTime.Now;
        }
    }
}
