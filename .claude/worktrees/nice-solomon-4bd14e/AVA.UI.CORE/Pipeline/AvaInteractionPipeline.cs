// ─────────────────────────────────────────────────────────────────────────────
//  Class    : AvaInteractionPipeline
//  Namespace: AVA.UI.CORE.Pipeline
//  Purpose  : Orchestrates a single AVA interaction — formats the prompt,
//             sends it to the core, traces memory, and returns a response.
// ─────────────────────────────────────────────────────────────────────────────

using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models;

namespace AVA.UI.CORE.Pipeline
{
    public class AvaInteractionPipeline
    {
        private readonly IAvaCoreInterface _core;
        private readonly IMemoryTracer _memory;
        private readonly ILogService _logger;

        public AvaInteractionPipeline(
            IAvaCoreInterface core,
            IMemoryTracer memory,
            ILogService logger)
        {
            _core = core;
            _memory = memory;
            _logger = logger;
        }

        /// <summary>
        /// Processes an input through the AVA pipeline.
        /// Accepts any input type T and a formatter delegate that
        /// converts it to the final prompt string.
        /// </summary>
        public async Task<AvaResponse> ProcessAsync<T>(
            T input,
            Func<T, string> formatter)
        {
            string compiledPrompt = formatter(input);

            _memory.Append($"📝 Prompt: {compiledPrompt}");

            string response;

            try
            {
                response = await _core.ProcessInputAsync(compiledPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Pipeline error: {ex.Message}");

                return new AvaResponse
                {
                    Text = string.Empty,
                    Error = ex.Message,
                    Source = "pipeline",
                    Confidence = 0f
                };
            }

            _memory.Append($"🤖 AVA: {response}");

            return new AvaResponse
            {
                Text = response,
                Confidence = 1.0f,
                Source = "core"
            };
        }
    }
}