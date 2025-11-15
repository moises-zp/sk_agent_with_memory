using Microsoft.SemanticKernel;
using System.ComponentModel;


namespace Sumaris.Model
{
    /// <summary>
    /// Plugin de Semantic Kernel que expone las operaciones de memoria del proyecto (RAG) al LLM.
    /// Esta clase actúa como la interfaz entre el modelo de IA y la base de datos vectorial.
    /// </summary>
    public class ProjectMemoryPlugin
    {
        private readonly IProjectMemoryService _memoryService;

        /// <summary>
        /// Constructor que recibe la implementación del servicio de memoria a través de Inyección de Dependencias.
        /// </summary>
        /// <param name="memoryService">Servicio que maneja la lógica de embeddings y la persistencia de datos.</param>
        public ProjectMemoryPlugin(IProjectMemoryService memoryService)
        {
            _memoryService = memoryService;
        }

        /// <summary>
        /// Obtiene el projectId desde el contexto del Kernel
        /// </summary>
        private string GetProjectId(Kernel kernel)
        {
            if (kernel.Data.TryGetValue("projectId", out var projectIdObj) && projectIdObj is string projectId)
            {
                return projectId;
            }

            return "default-project";
        }

        /// <summary>
        /// Permite al LLM guardar información para consulta futura (Memoria a Largo Plazo).
        /// El LLM debe invocar esta función cuando se le pide registrar o recordar un hecho.
        /// </summary>
        /// <param name="information">El texto real a vectorizar y guardar.</param>
        /// <param name="category">Categoría para un filtrado más preciso (metadato).</param>
        /// <param name="sprintNumber">Número de sprint para contextualizar la memoria (metadato).</param>
        /// <param name="kernel">Kernel de SK para obtener el contexto (inyectado automáticamente).</param>
        /// <returns>Un mensaje de confirmación formateado para que el LLM continúe la conversación.</returns>
        [KernelFunction]
        [Description("Guarda información importante sobre el proyecto de software")]
        public async Task<string> SaveProjectInfo(
            [Description("Información a recordar del proyecto")]
        string information,

            [Description("Categoría: tech_stack, methodology, standards, decision, sprint_planning, estimation, user_story")]
        string? category = null,

            [Description("Número de sprint si aplica")]
        string? sprintNumber = null,

            Kernel? kernel = null)
        {
            var projectId = kernel != null ? GetProjectId(kernel) : "default-project";

            await _memoryService.SaveProjectInfoAsync(
                projectId,
                information,
                category,
                sprintNumber
            );

            return $"✅ Guardado: {information}";
        }

        /// <summary>
        /// Permite al LLM buscar información semánticamente relevante para una consulta específica (RAG).
        /// El LLM invoca esta función para responder preguntas fácticas sobre el proyecto.
        /// </summary>
        /// <param name="query">La pregunta del usuario a convertir en vector de búsqueda.</param>
        /// <param name="kernel">Kernel de SK para obtener el contexto (inyectado automáticamente).</param>
        /// <returns>Los 3 fragmentos de memoria más relevantes o un mensaje de error.</returns>
        [KernelFunction]
        [Description("Busca información previamente guardada sobre el proyecto")]
        public async Task<string> RecallProjectInfo(
            [Description("¿Qué información del proyecto necesitas recordar?")]
        string query,

            Kernel? kernel = null)
        {
            var projectId = kernel != null ? GetProjectId(kernel) : "default-project";

            var memories = await _memoryService.RecallProjectInfoAsync(
                projectId,
                query,
                limit: 3,
                minRelevanceScore: 0.75
            );

            if (!memories.Any())
            {
                return "❌ No encontré información relevante sobre ese tema en el proyecto.";
            }

            var memoriesText = string.Join("\n",
                memories.Select(m =>
                    $"  • {m.MemoryText} " +
                    $"[{m.Category ?? "general"}] " +
                    $"(guardado: {m.Timestamp:yyyy-MM-dd})")
            );

            return $"📝 Información del proyecto:\n{memoriesText}";
        }

        /// <summary>
        /// Permite al LLM recuperar información basada en metadatos y orden cronológico.
        /// Útil para peticiones de historial o revisión de planes de sprint (búsqueda no vectorial).
        /// </summary>
        /// <param name="category">Categoría específica para filtrar el historial.</param>
        /// <param name="count">Número máximo de elementos del historial a devolver.</param>
        /// <param name="kernel">Kernel de SK para obtener el contexto (inyectado automáticamente).</param>
        /// <returns>Una lista formateada de eventos históricos.</returns>
        [KernelFunction("GetProjectHistory")]
        [Description("Obtiene el historial cronológico del proyecto, filtrado opcionalmente por categoría")]
        public async Task<string> GetProjectHistory(
            [Description("Categoría específica a buscar: tech_stack, methodology, sprint_planning, etc.")]
        string? category = null,

            [Description("Número de registros históricos a recuperar")]
        int count = 5,

            Kernel? kernel = null)
        {
            var projectId = kernel != null ? GetProjectId(kernel) : "default-project";

            var history = await _memoryService.GetProjectHistoryAsync(
                projectId,
                category,
                count
            );

            if (!history.Any())
            {
                var noHistoryMessage = "❌ No hay historial previo para este proyecto.";
                return noHistoryMessage;
            }

            var categoryLabel = string.IsNullOrEmpty(category)
                ? "Historial general"
                : $"Historial de {category}";

            var historyText = string.Join("\n",
                history.Select(m =>
                    $"  • [{m.Timestamp:yyyy-MM-dd HH:mm}] {m.MemoryText}")
            );

            var resultMessage = $"📅 {categoryLabel}:\n{historyText}";
            return resultMessage;
        }
    }
}