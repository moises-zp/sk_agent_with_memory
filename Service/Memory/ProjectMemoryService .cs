using System.Linq.Expressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;

namespace Sumaris.Model
{
    public class ProjectMemoryService : IProjectMemoryService
    {
        private readonly VectorStore _vectorStore;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;

        private readonly VectorStoreCollection<Guid, ProjectMemoryRecord> _memoryCollection;
        private readonly ILogger<ProjectMemoryService> _logger;

        public ProjectMemoryService(
            VectorStore vectorStore,
            IEmbeddingGenerator<string, Embedding<float>> embeddingService,
            ILogger<ProjectMemoryService> logger)
        {
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
            _logger = logger;

            // Obtener o crear la colección
            _memoryCollection = _vectorStore.GetCollection<Guid, ProjectMemoryRecord>("project_memories");

        }

        public async Task SaveProjectInfoAsync(
            string projectId,
            string information,
            string? category = null,
            string? sprintNumber = null)
        {
            // 1. Generar embedding del texto
            var embeddings = await _embeddingService.GenerateAsync([information]);

            // 2. Crear registro
            var record = new ProjectMemoryRecord
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                MemoryText = information,
                Embedding = embeddings[0].Vector,
                Category = category,
                SprintNumber = sprintNumber
            };

            await _memoryCollection.EnsureCollectionExistsAsync();

            // 3. Guardar en vector store
            await _memoryCollection.UpsertAsync(record);

        }

        public async Task<IEnumerable<ProjectMemoryRecord>> RecallProjectInfoAsync(
            string projectId,
            string query,
            int limit = 5,
            double minRelevanceScore = 0.0)
        {

            var queryEmbeddings = await _embeddingService.GenerateAsync([query]);

            var searchResults = _memoryCollection.SearchAsync(
                queryEmbeddings[0].Vector,
                limit,
                new VectorSearchOptions<ProjectMemoryRecord>
                {
                    Filter = r => r.ProjectId == projectId
                }
            );


            var allResults = await searchResults.ToListAsync();

            _logger.LogInformation($"\n📊 Nro total de resultados de búsqueda para '{query}' es {allResults.Count}");
            foreach (var result in allResults)
            {
                _logger.LogInformation($"   • Score: {result.Score:F3} | Category: {result.Record.Category ?? "null"}");
                _logger.LogInformation($"     Text: {(result.Record.MemoryText.Length > 60 ? result.Record.MemoryText.Substring(0, 60) + "..." : result.Record.MemoryText)}");
            }

            var results = allResults
                .Where(r => r.Score >= minRelevanceScore)
                .Select(r => r.Record)
                .ToList();

            return results;
        }

        public async Task<IEnumerable<ProjectMemoryRecord>> GetProjectHistoryAsync(
            string projectId,
            string? category = null,
            int count = 10)
        {
            Expression<Func<ProjectMemoryRecord, bool>> filter = r => category == null ?
            (r.ProjectId == projectId) : (r.ProjectId == projectId && r.Category == category);

            var allMemories = _memoryCollection.SearchAsync(
                new float[768].AsMemory(),
                count,
                new VectorSearchOptions<ProjectMemoryRecord>
                {
                    Filter = filter
                }
                );

            return await allMemories
                .Select(r => r.Record)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync()
                ;
        }
    }

}