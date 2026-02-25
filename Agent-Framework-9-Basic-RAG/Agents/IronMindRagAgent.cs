using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using OpenAI.Chat;

namespace Agent_Framework_9_Basic_RAG.Agents;

public class IronMindRagAgent
{
    private const int EmbeddingDimensions = 3072;
    private const string CollectionName = "iron-mind-ai-tips";

    private readonly TextSearchStore _textSearchStore;

    public IronMindRagAgent(OpenAIClient openAIClient, string embeddingModel)
    {
        var vectorStore = new InMemoryVectorStore(new()
        {
            EmbeddingGenerator = openAIClient.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator()
        });

        _textSearchStore = new TextSearchStore(vectorStore, CollectionName, EmbeddingDimensions);
    }


    public async Task<AIAgent> CreateAgentAsync(OpenAIClient openAIClient, string model)
    {
        await _textSearchStore.UpsertDocumentsAsync(TextSearchStore.GetSampleDocuments());

        var textSearchOptions = new TextSearchProviderOptions
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
            CitationsPrompt = "Always cite sources at the end of your response using the format: **Source:** [SourceName](SourceLink)",
        };

        return openAIClient
            .GetChatClient(model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = "You are Iron Mind AI, a knowledgeable personal trainer. " +
                                   "You MUST base your answers on the provided context documents. " +
                                   "Always cite your sources by name and link at the end of your response. " +
                                   "If the context does not contain relevant information, say so."
                },

                AIContextProviderFactory = (ctx, ct) => new ValueTask<AIContextProvider>(
                    new TextSearchProvider(SearchAsync, ctx.SerializedState, ctx.JsonSerializerOptions, textSearchOptions)),
                
                ChatHistoryProviderFactory = (ctx, ct) => new ValueTask<ChatHistoryProvider>(
                    new InMemoryChatHistoryProvider().WithAIContextProviderMessageRemoval()),

            });
    }

    private async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(string text, CancellationToken ct)
    {
        var searchResults = await _textSearchStore.SearchAsync(text, 2, ct);

        return searchResults.Select(r => new TextSearchProvider.TextSearchResult
        {
            SourceName = r.SourceName,
            SourceLink = r.SourceLink,
            Text = r.Text,
            RawRepresentation = r
        });
    }
}
