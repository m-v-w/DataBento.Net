namespace DataBento.Net.Dbn;

public class DbnSerializationError : Exception
{
    public DbnSerializationError(string message) : base(message) { }
}