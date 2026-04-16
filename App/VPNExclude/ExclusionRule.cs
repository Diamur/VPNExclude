using System.Text.Json.Serialization;

namespace VPNExclude
{
    internal sealed class ExclusionRule
    {
        public string Target { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public List<string> Ips { get; set; } = new();

        public string Gateway { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public string? CreatedAt { get; set; }

        public string? UpdatedAt { get; set; }

        [JsonPropertyName("LastCheckedAt")]
        public string? CheckedAt { get; set; }
    }
}
