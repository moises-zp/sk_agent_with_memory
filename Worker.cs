using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace Sumaris.Model
{
    public class Worker
    {
        private readonly ILogger<Worker> _logger;
        private readonly ProjectMemoryPlugin _projectMemoryPlugin;
        private readonly AIProductAssistantService _aiProductAssistantService;
        private readonly Kernel _kernel;

        public Worker(
            ILogger<Worker> logger,
            Kernel kernel,
            ProjectMemoryPlugin projectMemoryPlugin,
            AIProductAssistantService aiProductAssistantService
        )
        {
            _logger = logger;
            _kernel = kernel;
            _projectMemoryPlugin = projectMemoryPlugin;
            _aiProductAssistantService = aiProductAssistantService;
        }

        public async Task RunAsync()
        {
            var projectId = "inventory-project-001";

            try
            {
                _logger.LogInformation("✅ Collection 'pizzas' is ready");

                var response1 = await _aiProductAssistantService.ChatAsync(
                    projectId,
                    @"I'm starting a new project: Inventory Management System.
Tech stack: .NET 8 with Blazor, PostgreSQL, Azure.
Scrum with 2-week sprints.
User stories in Connextra format with INVEST criteria."
                );

                _logger.LogInformation("Answer {Response}", response1);
                _logger.LogInformation("=====================");

                // -------------------------
                // Interactive user loop
                // -------------------------
                Console.WriteLine("Interactive mode started. Type your prompt for the AI Product Assistant.");
                Console.WriteLine("Type 'exit' to end the execution.\n");

                while (true)
                {
                    Console.Write("You> ");
                    var userInput = Console.ReadLine();

                    if (userInput is null)
                    {
                        // In case of redirected input or EOF
                        _logger.LogWarning("No input detected (EOF). Exiting interactive loop.");
                        break;
                    }

                    if (string.Equals(userInput.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Exiting interactive chat loop.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        // Skip empty lines
                        continue;
                    }

                    var response = await _aiProductAssistantService.ChatAsync(projectId, userInput);
                    Console.WriteLine($"Assistant> {response}");
                }

                _logger.LogInformation("✅ Memory test completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during memory test");
            }
        }
    }
}