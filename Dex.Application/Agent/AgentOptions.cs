using System;
using System.Collections.Generic;
using System.Text;

namespace Dex.Application.Agent
{
    public class AgentOptions
    {
        /// <summary>Maximum number of model calls per user question.</summary>
        public int MaxSteps { get; set; } = 5;

        /// <summary>How many times the model may return invalid JSON before the agent gives up.</summary>
        public int MaxParseRetries { get; set; } = 2;
    }
}
