namespace Alpha.Models;

public class LocalizationModel
{
    #nullable disable
    public string Key { get; set; }
    public string Value { get; set; }
    public string Comment { get; set; }
    public string Culture { get; set; } = "";
}
