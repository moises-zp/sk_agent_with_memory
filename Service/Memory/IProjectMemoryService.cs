namespace Sumaris.Model{
    
    public interface IProjectMemoryService
{
    /// <summary>
    /// Ingesta/Aprendizaje. Almacena un nuevo fragmento de información de un proyecto.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="information">Aquí se usará  el contenido ingresado por el cliente</param>
    /// <param name="category"></param>
    /// <param name="sprintNumber"></param>
    /// <returns></returns>
    Task SaveProjectInfoAsync(
        string projectId,
        string information,
        string? category = null,
        string? sprintNumber = null
    );

    /// <summary>
    /// Búsqueda Semántica (RAG). 
    /// Busca la información más similar semánticamente a la query.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="query"></param>
    /// <param name="limit"></param>
    /// <param name="minRelevanceScore"></param>
    /// <returns></returns>
    Task<IEnumerable<ProjectMemoryRecord>> RecallProjectInfoAsync(
        string projectId,
        string query,
        int limit = 5,
        double minRelevanceScore = 0.7
    );

    /// <summary>
    /// Búsqueda Cronológica. Recupera los últimos N registros de memoria de un proyecto, posiblemente filtrados por categoría.
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="category"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    Task<IEnumerable<ProjectMemoryRecord>> GetProjectHistoryAsync(
        string projectId,
        string? category = null,
        int count = 10
    );
    
}

}