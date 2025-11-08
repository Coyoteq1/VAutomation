using System.Numerics;

namespace VAutomation.Config
{
    public class ZoneConfig
    {
        public Vector3 Center { get; set; }
        public float EnterRadius { get; set; } = 50f;
        public float ExitRadius { get; set; } = 75f;

        public bool IsInside(Vector3 position) => Vector3.Distance(position, Center) <= EnterRadius;
        public bool IsOutside(Vector3 position) => Vector3.Distance(position, Center) >= ExitRadius;
    }
}
