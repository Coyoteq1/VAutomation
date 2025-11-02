using System.Collections.Generic;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing vector embeddings for arena data.
    /// </summary>
    public class VectorService
    {
        private readonly Dictionary<string, float[]> vectorStore = new Dictionary<string, float[]>();

        public void StoreEmbedding(string key, float[] embedding)
        {
            vectorStore[key] = embedding;
        }

        public float[] GetEmbedding(string key)
        {
            return vectorStore.TryGetValue(key, out var embedding) ? embedding : null;
        }
    }
}
