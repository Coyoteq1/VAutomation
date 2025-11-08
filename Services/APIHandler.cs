using System;
using ProjectM;
using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Handles API requests for the mod.
    /// </summary>
    public static class APIHandler
    {
        public static void HandleRequest(string endpoint, Entity playerEntity)
        {
            switch (endpoint)
            {
                case "getProgression":
                    Console.WriteLine("Getting progression for player: " + playerEntity);
                    break;
                case "setArenaMode":
                    Console.WriteLine("Setting arena mode for player: " + playerEntity);
                    break;
                default:
                    Console.WriteLine("Unknown API endpoint: " + endpoint);
                    break;
            }
        }
    }
}
