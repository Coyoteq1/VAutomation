using ProjectM.Network;
using Unity.Entities;
using Stunlock.Core;

namespace CrowbaneArena.Helpers
{
    /// <summary>
    /// Helper for sending chat messages to players
    /// </summary>
    public static class ChatHelper
    {
        /// <summary>
        /// Send a system message to a specific player
        /// </summary>
        public static void SendSystemMessageToClient(EntityManager entityManager, User user, string message)
        {
            try
            {
                // Use ServerChatUtils if available, otherwise fallback to logging
                var serverChatUtilsType = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetType("ProjectM.ServerChatUtils");

                if (serverChatUtilsType != null)
                {
                    var method = serverChatUtilsType.GetMethod("SendSystemMessageToClient",
                        new[] { typeof(EntityManager), typeof(User), typeof(string) });
                    method?.Invoke(null, new object[] { entityManager, user, message });
                }
                else
                {
                    Plugin.Logger?.LogInfo($"[CHAT] {user.CharacterName}: {message}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to send chat message: {ex.Message}");
            }
        }

        /// <summary>
        /// Send multiple lines to a player
        /// </summary>
        public static void SendSystemMessages(EntityManager entityManager, User user, params string[] messages)
        {
            foreach (var msg in messages)
            {
                SendSystemMessageToClient(entityManager, user, msg);
            }
        }
    }
}
