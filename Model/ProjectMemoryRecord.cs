using Microsoft.Extensions.VectorData;

namespace Sumaris.Model
{


    public record ProjectMemoryRecord
    {
        [VectorStoreKey]
        public required Guid Id { get; init; }

        [VectorStoreData]
        public required string ProjectId { get; init; }

        [VectorStoreData]
        public required string MemoryText { get; init; }

        [VectorStoreVector(Dimensions: 768)]
        public ReadOnlyMemory<float> Embedding { get; init; }

        [VectorStoreData]
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        [VectorStoreData]
        public string? Category { get; init; }

        [VectorStoreData]
        public string? SprintNumber { get; init; }
    }

}