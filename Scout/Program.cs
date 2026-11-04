using Dex.Application.Agent;
using Dex.Infrastructure.Llm;
using Dex.Infrastructure.Tools;
using Dex.Application.Abstractions;
using Dex.Infrastructure.Storage;

// TODO: set your own paths
const string ModelPath = @"C:\Users\mumla\Desktop\Scout\models\qwen2.5-3b-instruct-q4_k_m.gguf";
const string ProjectRoot = @"C:\Users\mumla\Desktop\Scout";
const string TodoFile = @"C:\Users\mumla\Desktop\Scout\data\todos.json";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Loading model...");

using var llm = new LLamaSharpClient(new LlmOptions { ModelPath = ModelPath });

var tools = new ITool[]
{
    new FileSearchTool(ProjectRoot),
    new FindFilesTool(ProjectRoot),
    new GitLogTool(ProjectRoot),
    new TodoTool(new TodoStore(TodoFile))
};
var agent = new AgentService(llm, tools, new AgentOptions());

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