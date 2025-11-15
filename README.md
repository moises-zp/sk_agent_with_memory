Semantic Kernel Pizza Order Agent
A practical example demonstrating how to build a conversational AI agent using Microsoft Semantic Kernel with Google Gemini as the LLM provider. This project shows how to transform traditional C# business logic into AI-powered, natural language interfaces through function calling.
🎯 What This Project Demonstrates

Plugin Architecture: How to expose C# methods as functions callable by an LLM
Function Calling: Automatic invocation of business logic based on natural language
Chat History Management: Maintaining conversational context across interactions
Dependency Injection: Integrating Semantic Kernel with .NET's DI container
Real-world Scenario: Complete pizza ordering workflow from menu browsing to checkout

🚀 Features
Users can interact with the agent using natural language to:

Browse the pizza menu
Add pizzas to their cart (specifying size, toppings, quantity)
View cart contents and totals
Remove items from the cart
Complete the checkout process

Example Interactions:
User > Show me the menu
User > I want two large pepperoni pizzas
User > What's in my cart?
User > Remove pizza #1
User > I'm ready to checkout
📋 Prerequisites

.NET 8.0 or higher
Google Gemini API key (Get one here)
Visual Studio 2022 / VS Code / Rider

🛠️ Installation

Clone the repository:

bashgit clone https://github.com/moises-zp/agent_for_pizzas.git

cd agent_for_pizzas

Install dependencies:

bashdotnet restore

Configure your API key in Program.cs:

csharpvar apiKey = "YOUR_GEMINI_API_KEY";
▶️ Running the Application
bashdotnet run
Type your messages in the console. Type exit or salir to quit.
🏗️ Project Structure
├── Models/
│   ├── Pizza.cs
│   ├── Cart.cs
│   ├── Menu.cs
│   └── DTOs/
│       ├── CartDelta.cs
│       ├── CheckoutResponse.cs
│       └── RemovePizzaResponse.cs
├── Services/
│   ├── IPizzaService.cs
│   ├── PizzaService.cs
│   ├── IUserContext.cs
│   ├── UserContext.cs
│   ├── IPaymentService.cs
│   └── PaymentService.cs
├── Plugins/
│   └── OrderPizzaPlugin.cs
├── Worker.cs
└── Program.cs
🔑 Key Concepts
Plugin Definition
Functions are exposed to the LLM using the [KernelFunction] attribute:
csharp[KernelFunction("add_pizza_to_cart")]
[Description("Add a pizza to the user's cart")]
public async Task<CartDelta> AddPizzaToCart(
    PizzaSize size,
    List<PizzaToppings> toppings,
    int quantity = 1)
{
    // Implementation
}
Kernel Configuration
csharp// Register plugin
_kernel.Plugins.AddFromObject(orderPizzaPlugin, "OrderPizza");

// Configure auto-invocation
var settings = new GeminiPromptExecutionSettings
{
    Temperature = 0.7,
    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
};
Chat Flow

User input is added to ChatHistory
LLM analyzes the request
Semantic Kernel automatically invokes the appropriate function(s)
Results are returned to the LLM
LLM generates a natural language response

📦 NuGet Packages Used

Microsoft.SemanticKernel - Core framework
Microsoft.SemanticKernel.Connectors.Google - Gemini integration
Microsoft.Extensions.Hosting - DI and hosting
Microsoft.Extensions.Logging - Logging infrastructure

🔧 Configuration Options
Temperature
Controls response randomness (0.0 - 1.0). Lower values are more deterministic.
Model Selection
Available Gemini models:

gemini-2.0-flash-exp - Fast and efficient
gemini-1.5-pro - More capable, slower

Function Invocation Modes

AutoInvokeKernelFunctions - Automatic execution (recommended)
EnableKernelFunctions - Manual approval required

📚 Learning Resources

Semantic Kernel Documentation
Google Gemini API
Blog Post: Part 1 - Getting Started (link to your blog)

🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
📄 License
This project is licensed under the MIT License - see the LICENSE file for details.
🙏 Acknowledgments

Microsoft Semantic Kernel team
Google Gemini team
.NET community

📧 Contact
Moisés Zapata Placencia 
Project Link: https://github.com/yourusername/semantic-kernel-pizza-agent

Note: This is a sample project for educational purposes. The payment processing is simulated and the data storage is in-memory only.