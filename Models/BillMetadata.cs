namespace TGSaveUtilityBillsBot.Models;

public class BillMetadata
{
    public int Year { get; set; }
    public Month Month { get; set; }
    public Company Company { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
}

