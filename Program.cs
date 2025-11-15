using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using Microsoft.Extensions.VectorData;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Qdrant.Client.Grpc;
using Sumaris.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.InMemory;




var builderHosting = Host.CreateApplicationBuilder();

// 1. Obtener la interfaz de configuración (ya inyectada por el builder)
var configuration = builderHosting.Configuration;

var modelId = configuration["GeminiConfiguration:ModelId"];
var apiKey = configuration["GeminiConfiguration:ApiKey"];
var embeddingModelId = configuration["GeminiConfiguration:TextEmbedding"];
var qdrantEndpoint = configuration["Memory:Qdrant:Endpoint"];
var useQdrant = configuration.GetValue<bool>("Memory:UseQdrant");
var certPath = configuration["Memory:Qdrant:CertPath"];


// 2. Verificación básica (opcional pero recomendado)
if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelId))
{
    Console.WriteLine("Error: La API Key o el Model ID no están configurados en appsettings.json.");
    return;
}


// Configuración de logging
builderHosting.Logging.ClearProviders();
builderHosting.Logging.AddConsole();
builderHosting.Logging.AddDebug();
builderHosting.Logging.SetMinimumLevel(LogLevel.Trace);


// 3. Registrar el Plugin (contenedor de KernelFunctions)
//Se registra el servicio que gestionará el chat entre el usuario y el llm
builderHosting.Services.AddTransient<AIProductAssistantService>();
//Servicio que gestiona la memoria. El LLM la usa por medio de las funciones definidas en el plugin
builderHosting.Services.AddTransient<IProjectMemoryService, ProjectMemoryService>();



if (string.IsNullOrWhiteSpace(qdrantEndpoint))
{
    Console.WriteLine("Error: No se ha configurado la URL de Qdrant.");
    return; // Termina la aplicación si faltan credenciales
}

if (string.IsNullOrWhiteSpace(embeddingModelId))
{
    Console.WriteLine("Error: No se ha configurado embeddingModelId.");
    return; // Termina la aplicación si faltan credenciales
}

// 4. REGISTRO DEL EMBEDDING GENERATOR (Sustituye a ITextEmbeddingGenerationService)
builderHosting.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    // Usamos el constructor directo de GoogleAIEmbeddingGenerator (debería estar en el paquete Google)
    return new GoogleAIEmbeddingGenerator(
        modelId: embeddingModelId,
        apiKey: apiKey
    );
});



if (useQdrant)
{
    // 5. REGISTRO DEL VECTOR STORE (Sustituye a IMemoryStore)
    builderHosting.Services.AddSingleton<QdrantClient>(sp =>
    {

        var caCertPath = certPath + "ca.crt";
        var clientPfxPath = certPath + "client.pfx";

        var clientCert = X509CertificateLoader.LoadPkcs12CollectionFromFile(
            clientPfxPath,
            password: "qdrant" // Usa tu contraseña real
        );

        var caCert = X509CertificateLoader.LoadCertificateFromFile(caCertPath);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCert[0]);
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // Esto es CLAVE. Verifica que el certificado del servidor esté firmado por nuestra CA.

            // Creamos una cadena de certificado para validación
            var validationChain = new X509Chain();
            validationChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            // Establecemos el certificado de la CA como la raíz de confianza
            validationChain.ChainPolicy.ExtraStore.Add(caCert);

            if (cert == null)
                return false;
            // Ejecutamos la validación
            var isValid = validationChain.Build((X509Certificate2)cert);

            // Si la validación falla, aún podemos aceptarlo si estamos en desarrollo
            if (!isValid)
            {
                // En un entorno de desarrollo con autofirmado, podemos aceptar si hay errores específicos.
                // Aquí aceptamos si el error es solo por la CA no ser conocida por el SO (UntrustedRoot).
                if (errors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    return true; // Aceptamos el riesgo si sabemos que es nuestro certificado
                }
                return false; // Rechazamos si hay otros errores
            }

            return isValid;
        };

        var channel = Grpc.Net.Client.GrpcChannel.ForAddress(
            qdrantEndpoint,
            new Grpc.Net.Client.GrpcChannelOptions
            {
                HttpClient = new HttpClient(handler)
                {
                    DefaultRequestVersion = HttpVersion.Version20
                }
            }
        );

        var grpcClient = new QdrantGrpcClient(channel);
        return new QdrantClient(grpcClient);
    });
}
else
{
    //Si no usamos Qdrant, usamos InMemory
    builderHosting.Services.AddSingleton<VectorStore>(sp =>
    {
        return new InMemoryVectorStore();
    });
}




// 6. Registro del IVectorStore usando el conector de Qdrant
builderHosting.Services.AddSingleton<VectorStore>(sp =>
{
    var qdrantClient = sp.GetRequiredService<QdrantClient>();
    return new QdrantVectorStore(qdrantClient, ownsClient: false);  // ✅ ownsClient especificado
});

// 7. Registramos:
//.  - La instancia del Kernel
//.  - Proveedor LLM y el Modelos
//.  - Los Plugins
// Se registra el plugin de Semantic Kernel que expone las funciones que puede llamar el LLM
builderHosting.Services.AddTransient<ProjectMemoryPlugin>();
builderHosting.Services
.AddKernel()
.AddGoogleAIGeminiChatCompletion(
        modelId: modelId,
        apiKey: apiKey
    )
.Plugins.AddFromType<ProjectMemoryPlugin>();

builderHosting.Services.AddTransient<Worker>();



var host = builderHosting.Build();

var worker = host.Services.GetRequiredService<Worker>();
await worker.RunAsync();