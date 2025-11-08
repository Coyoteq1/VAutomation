using System.Numerics;

namespace VAutomation.Config
{
    public interface IZoneConfig
    {
        Vector3 Center { get; set; }
        float EnterRadius { get; set; }
        float ExitRadius { get; set; }

        bool IsInside(Vector3 position);
        bool IsOutside(Vector3 position);
    }
}
