using System.ComponentModel;

namespace DataBento.Net.Dbn;

public enum SchemaId : ushort
{
    [Description("mbp-1")]
    Mbp1=1,
    [Description("definition")]
    InstrumentDef = 0x13,
    [Description("trades")]
    Trades = 0x00,
    Live=65535
}
