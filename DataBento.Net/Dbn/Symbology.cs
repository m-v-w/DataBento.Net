using System.ComponentModel;

namespace DataBento.Net.Dbn;

public enum SymbolType : byte
{
    /*#[pyo3(name = "INSTRUMENT_ID")]
    InstrumentId = 0,
    /// Symbology using the original symbols provided by the publisher.
    #[pyo3(name = "RAW_SYMBOL")]
    RawSymbol = 1,
    /// A set of Databento-specific symbologies for referring to groups of symbols.
    #[deprecated(since = "0.5.0", note = "Smart was split into continuous and parent.")]
    #[pyo3(name = "SMART")]
    Smart = 2,
    /// A Databento-specific symbology where one symbol may point to different
    /// instruments at different points of time, e.g. to always refer to the front month
    /// future.
    #[pyo3(name = "CONTINUOUS")]
    Continuous = 3,
    /// A Databento-specific symbology for referring to a group of symbols by one
    /// "parent" symbol, e.g. ES.FUT to refer to all ES futures.
    #[pyo3(name = "PARENT")]
    Parent = 4,
    /// Symbology for US equities using NASDAQ Integrated suffix conventions.
    #[pyo3(name = "NASDAQ_SYMBOL")]
    NasdaqSymbol = 5,
    /// Symbology for US equities using CMS suffix conventions.
    #[pyo3(name = "CMS_SYMBOL")]
    CmsSymbol = 6,
    /// Symbology using International Security Identification Numbers (ISIN) - ISO 6166.
    #[pyo3(name = "ISIN")]
    Isin = 7,
    /// Symbology using US domestic Committee on Uniform Securities Identification Procedure (CUSIP) codes.
    #[pyo3(name = "US_CODE")]
    UsCode = 8,
    /// Symbology using Bloomberg composite global IDs.
    #[pyo3(name = "BBG_COMP_ID")]
    BbgCompId = 9,
    /// Symbology using Bloomberg composite tickers.
    #[pyo3(name = "BBG_COMP_TICKER")]
    BbgCompTicker = 10,
    /// Symbology using Bloomberg FIGI exchange level IDs.
    #[pyo3(name = "FIGI")]
    Figi = 11,
    /// Symbology using Bloomberg exchange level tickers.
    #[pyo3(name = "FIGI_TICKER")]
    FigiTicker = 12,*/
    [Description("instrument_id")]
    InstrumentId = 0, // Symbology using the original symbols provided by the publisher.
    [Description("raw_symbol")]
    RawSymbol = 1, // Symbology using the original symbols provided by the publisher.
    [Description("continuous")]
    Continuous = 3, // A Databento-specific symbology
    [Description("parent")]
    Parent = 4, // A Databento-specific symbology
}