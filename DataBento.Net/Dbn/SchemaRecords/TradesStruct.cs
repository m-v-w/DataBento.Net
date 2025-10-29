namespace DataBento.Net.Dbn.SchemaRecords;

public partial struct TradesStruct
{
    public override string ToString()
    {
        return $"{Header.InstrumentId}";
    }
}