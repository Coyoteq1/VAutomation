namespace CrowbaneArena.Data
{
    public class BuildItemData
    {
        public string Name { get; set; } = string.Empty;
        public int GUID { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsValid { get; set; } = true;
        
        public BuildItemData() { }
        
        public BuildItemData(string name, int guid, int quantity = 1)
        {
            Name = name;
            GUID = guid;
            Quantity = quantity;
        }
    }
}