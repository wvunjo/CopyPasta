using Newtonsoft.Json;

namespace CopyPastaNative.Security
{
    public static class SnippetJson
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MaxDepth = SnippetLimits.MaxJsonDepth,
            DateParseHandling = DateParseHandling.DateTime,
            FloatParseHandling = FloatParseHandling.Double,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.Indented
        };
    }
}
