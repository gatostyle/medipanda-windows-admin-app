namespace medipanda_windows_admin.Converters
{
    public interface ISettlementConverter
    {
        string SettlementMonth { get; set; }
        string DrugCompanyName { get; set; }
        void LoadFile(string filePath);
        Task ParseAsync();
        void SaveConverted(string outputPath);
        void Dispose();
    }
}