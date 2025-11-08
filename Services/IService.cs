namespace VAutomationEvents.Services
{
    /// <summary>
    /// Base interface for all services
    /// </summary>
    public interface IService
    {
        void Initialize();
        void Update();
        void Shutdown();
    }
}
