
namespace DataBento.Net.Dbn;

public enum RecordType : byte
{
    Mbp0 = 0x00,
    Mbp1 = 0x01,
    Mbp10 = 0x0A,
    // Deprecated: Separated into separate rtypes for each OHLCV schema.
    OhlcvDeprecated = 0x11,
    Ohlcv1S = 0x20,
    Ohlcv1M = 0x21,
    Ohlcv1H = 0x22,
    Ohlcv1D = 0x23,
    OhlcvEod = 0x24,
    Status = 0x12,
    InstrumentDef = 0x13,
    Imbalance = 0x14,
    Error = 0x15,
    SymbolMapping = 0x16,
    System = 0x17,
    Statistics = 0x18,
    Mbo = 0xA0,
    Cmbp1 = 0xB1,
    Cbbo1S = 0xC0,
    Cbbo1M = 0xC1,
    Tcbbo = 0xC2,
    Bbo1S = 0xC3,
    Bbo1M = 0xC4
}
