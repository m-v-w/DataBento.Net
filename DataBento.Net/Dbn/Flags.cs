namespace DataBento.Net.Dbn;

[Flags]
public enum RecordFlags : byte
{
    Last = 1 << 7, // Marks the last record in a single event for a given instrument_id.
    Tob = 1 << 6, // Top-of-book message, not an individual order.
    Snapshot = 1 << 5, // Message sourced from a replay, such as a snapshot server.
    Mbp = 1 << 4, // Aggregated price level message, not an individual order.
    BadTsRecv = 1 << 3, // The ts_recv value is inaccurate due to clock issues or packet reordering.
    MaybeBadBook = 1 << 2, // An unrecoverable gap was detected in the channel.
    PublisherSpecific = 1 << 1, // Semantics depend on the publisher_id. Refer to the relevant dataset supplement for more details.
    Reserved = 1 << 0 // Reserved for internal use can safely be ignored. May be set or unset.
}

public enum RecordAction : byte
{
    Add = (byte)'A', // Insert a new order into the book.
    Modify = (byte)'M', // Change an order's price and/or size.
    Cancel = (byte)'C', // Fully or partially cancel an order from the book.
    Clear = (byte)'R', // Remove all resting orders for the instrument.
    Trade = (byte)'T', // An aggressing order traded. Does not affect the book.
    Fill = (byte)'F', // A resting order was filled. Does not affect the book.
    None = (byte)'N' // No action: does not affect the book, but may carry flags or other information.
}

public enum RecordSide : byte
{
    Ask = (byte)'A', // The trade aggressor was a seller | A resting sell order was filled | A resting sell order updated the book
    Bid = (byte)'B', // The trade aggressor was a buyer | A resting buy order was filled | A resting buy order updated the book
    None = (byte)'N' // No side specified.
}