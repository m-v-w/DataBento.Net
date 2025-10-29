using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataBento.Net.Dbn.SchemaRecords;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InstrumentDefStructV1
{
    internal const int StructSize = 360;
    public RecordHeader Header;
    public ulong TsRecv;
    public long MinPriceIncrement;
    public long DisplayFactor;
    public ulong Expiration;
    public ulong Activation;
    public long HighLimitPrice;
    public long LowLimitPrice;
    public long MaxPriceVariation;
    public long TradingReferencePrice;
    public long UnitOfMeasureQty;
    public long MinPriceIncrementAmount;
    public long PriceRatio;
    public int InstAttribValue;
    public uint UnderlyingInstrumentId;
    public uint RawInstrumentId;
    public int MarketDepthImplied;
    public int MarketDepth;
    public uint MarketSegmentId;
    public uint MaxTradeVol;
    public int MinLotSize;
    public int MinLotSizeBlock;
    public int MinLotSizeRoundLot;
    public uint MinTradeVol;
    public uint Reserved1;
    public int ContractMultiplier;
    public int DecayQuantity;
    public int OriginalContractSize;
    public uint Reserved2;
    public ushort TradingReferenceDate;
    public short ApplId;
    public ushort MaturityYear;
    public ushort DecayStartDate;
    public ushort ChannelId;
    public CurrencyInlineArray Currency;
    public CurrencyInlineArray SettlCurrency;
    public Byte6TypeInlineArray SubSecType;
    public SymbolInlineArrayV1 RawSymbol;
    public Byte21InlineArray Group;
    public ExchangeInlineArray Exchange;
    public AssetInlineArrayV1 Asset;
    public Byte7InlineArray Cfi;
    public Byte7InlineArray Byte7;
    public UnitOfMeasureInlineArray UnitOfMeasure;
    public Byte21InlineArray Underlying;
    public CurrencyInlineArray StrikePriceCurrency;
    public byte InstrumentClass;
    public ushort Reserved3;
    public long StrikePrice;
    public uint Reserved4;
    public ushort Reserved5;
    public byte MatchAlgorithm;
    public byte MdSecurityTradingStatus;
    public byte MainFraction;
    public byte PriceDisplayFormat;
    public byte SettlPriceType;
    public byte SubFraction;
    public byte UnderlyingProduct;
    public byte SecurityUpdateAction;
    public byte MaturityMonth;
    public byte MaturityDay;
    public byte MaturityWeek;
    public byte UserDefinedInstrument;
    public byte ContractMultiplierUnit;
    public byte FlowScheduleType;
    public byte TickRule;
    public ushort Dummy1;
    public byte Dummy2;
    
    public static ref InstrumentDefStructV1 UnsafeReference(ReadOnlySpan<byte> span)
    {
        if(span.Length != StructSize)
            throw new ArgumentException($"{nameof(InstrumentDefStructV1)} struct must be {StructSize} bytes");
        return ref Unsafe.As<byte, InstrumentDefStructV1>(ref MemoryMarshal.GetReference(span));
    }
}
