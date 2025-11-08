namespace VAutomation.Services
{
    public interface IArenaState
    {
        void MarkInArena(string userId, bool value);
        bool IsInArena(string userId);
        void Clear(string userId);
        void ClearAll();
    }
}
