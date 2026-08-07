using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SabEngine.Agent.Tests;

/// <summary>
/// A test double for IChatCompletionService that returns a canned
/// response instead of calling a real model. This is what lets
/// SabAgent's prompt-building, response-parsing, and hard-rule
/// validation logic be tested fully offline — no API key, no network
/// call, deterministic and fast. See pre-development-checklist.md, PD-6.
/// </summary>
public sealed class FakeChatCompletionService(string cannedResponse) : IChatCompletionService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatMessageContent> result = [new ChatMessageContent(AuthorRole.Assistant, cannedResponse)];
        return Task.FromResult(result);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        // SabAgent never uses streaming — not implemented on purpose.
        throw new NotImplementedException("SabAgent does not use streaming chat completion.");
    }
}
