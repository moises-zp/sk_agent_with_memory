using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Sumaris.Model;

public class AIProductAssistantService
{
    private readonly Kernel _kernel;
    private readonly ChatHistory _chatHistory;
    private readonly ILogger<AIProductAssistantService> _logger;

    private readonly IProjectMemoryService _projectMemoryService;

    private readonly IChatCompletionService _iChatCompletionService;

    public AIProductAssistantService(
        Kernel kernel,
        //string systemPrompt,
        ILogger<AIProductAssistantService> logger,
        IChatCompletionService iChatCompletionService,
        IProjectMemoryService projectMemoryService
        )
    {
        _kernel = kernel;
        
        _logger = logger;
        _iChatCompletionService = iChatCompletionService;
        _projectMemoryService = projectMemoryService;


        const string systemPromptPath = "Plugin/Prompts/ProductAssistantRAG/README.md";

        var systemPrompt2 = File.ReadAllText(systemPromptPath); 
        _chatHistory = new ChatHistory(systemPrompt2);

    }

    public async Task<string> ChatAsync(string projectId, string userMessage)
    {
        _logger.LogInformation("Procesando mensaje para proyecto {ProjectId}: {Message}", projectId, userMessage);

        // 1. Agregar mensaje del usuario
        _chatHistory.AddUserMessage(userMessage);

        // 2. CRÍTICO: Guardar projectId en Kernel.Data
        _kernel.Data["projectId"] = projectId;


        // 3. Configurar function calling
        var executionSettings = new GeminiPromptExecutionSettings
        {
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions, // ,EnableKernelFunctions
            Temperature = 0.0,
            MaxTokens = 4096 * 4
        };

        try
        {
            // 4. Obtener respuesta
            var result = await _iChatCompletionService.GetChatMessageContentAsync(
                _chatHistory,
                executionSettings,
                _kernel
            );

            if (result == null)
            {
                _logger.LogWarning("⚠️ Resultado nulo del LLM");
                return string.Empty;
            }

            LogLlmAnswer(result);

            // 5. Agregar respuesta al historial
            if (result.Content != null)
            {
                _chatHistory.AddAssistantMessage(result.Content);
                _logger.LogInformation("✅ Respuesta generada exitosamente");
                return result.Content;
            }
            else
            {
                _logger.LogWarning("⚠️ Content es null - devolviendo string vacío");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"\n❌ EXCEPCIÓN CAPTURADA:");
            _logger.LogInformation($"   Tipo: {ex.GetType().Name}");
            _logger.LogInformation($"   Mensaje: {ex.Message}");
            _logger.LogInformation($"   StackTrace:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _logger.LogInformation($"\n   Inner Exception:");
                _logger.LogInformation($"   Tipo: {ex.InnerException.GetType().Name}");
                _logger.LogInformation($"   Mensaje: {ex.InnerException.Message}");
            }

            _logger.LogError(ex, "❌ Error en ChatAsync");
            return $"Error: {ex.Message}";
        }
    }
    

    private void LogLlmAnswer(ChatMessageContent result)
    {
         _logger.LogInformation($"✅ Result recibido (Type: {result.GetType().Name})");
         _logger.LogInformation($"\n📝 Content: '{result.Content ?? "NULL"}'");
         _logger.LogInformation($"   Length: {result.Content?.Length ?? 0}");
         _logger.LogInformation($"\n👤 Role: {result.Role}");
         _logger.LogInformation($"🔢 Items Count: {result.Items?.Count ?? 0}");

        // Analizar Items
        if (result.Items != null && result.Items.Count > 0)
        {
             _logger.LogInformation($"\n📦 ITEMS ({result.Items.Count}):");
            for (int i = 0; i < result.Items.Count; i++)
            {
                var item = result.Items[i];
                 _logger.LogInformation($"\n   [{i}] Type: {item.GetType().Name}");

                if (item is TextContent textContent)
                {
                     _logger.LogInformation($"       Text: '{textContent.Text}'");
                }
                else if (item is FunctionCallContent functionCall)
                {
                     _logger.LogInformation($"       ✅ FUNCTION CALL DETECTADO!");
                     _logger.LogInformation($"       Plugin: {functionCall.PluginName}");
                     _logger.LogInformation($"       Function: {functionCall.FunctionName}");
                     _logger.LogInformation($"       Id: {functionCall.Id}");
                     _logger.LogInformation($"       Arguments:");
                    if (functionCall.Arguments != null)
                    {
                        foreach (var arg in functionCall.Arguments)
                        {
                             _logger.LogInformation($"          - {arg.Key}: {arg.Value}");
                        }
                    }
                    else
                    {
                         _logger.LogInformation($"          (null)");
                    }
                }
                else if (item is FunctionResultContent functionResult)
                {
                     _logger.LogInformation($"       ✅ FUNCTION RESULT DETECTADO!");
                     _logger.LogInformation($"       Plugin: {functionResult.PluginName}");
                     _logger.LogInformation($"       Function: {functionResult.FunctionName}");
                     _logger.LogInformation($"       CallId: {functionResult.CallId}");
                     _logger.LogInformation($"       Result: {functionResult.Result}");
                }
                else
                {
                     _logger.LogInformation($"       (Tipo desconocido: {item.GetType().FullName})");
                }
            }
        }
        else
        {
             _logger.LogInformation($"\n⚠️ Items es null o vacío");
        }

        // Metadata
        if (result.Metadata != null && result.Metadata.Count > 0)
        {
             _logger.LogInformation($"\n🏷️ METADATA ({result.Metadata.Count}):");
            foreach (var kvp in result.Metadata)
            {
                 _logger.LogInformation($"   - {kvp.Key}: {kvp.Value}");
            }
        }

         _logger.LogInformation("\n" + "=".PadRight(80, '=') + "\n");
    }
}