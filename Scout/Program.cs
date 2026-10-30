using Dex.Application.Agent;
using Dex.Infrastructure.Llm;
using Dex.Infrastructure.Tools;

// TODO: set your own paths
const string ModelPath = @"C:\Users\mumla\Desktop\Scout\models\qwen2.5-3b-instruct-q4_k_m.gguf";
const string ProjectRoot = @"C:\Users\mumla\Desktop\Scout";

Console.WriteLine("Loading model...");
using var llm = new LLamaSharpClient(new LlmOptions { ModelPath = ModelPath });

var agent = new AgentService(llm, [new FileSearchTool(ProjectRoot)], new AgentOptions());

agent.OnTrace = line => Console.WriteLine($"  {line}");

Console.WriteLine("Scout is ready. Type 'exit' to quit.");
while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    Console.WriteLine(await agent.RunAsync(input));
}