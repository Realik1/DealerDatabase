namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Interface for reading raw dealer records from various data sources.
/// </summary>
public interface IDataReader
{
    /// <summary>
    /// The name of the data source this reader handles.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Reads all available dealer records from the source.
    /// </summary>
    /// <param name="sourcePath">Path to the source file or directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of raw dealer records from this source.</returns>
    Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
}
