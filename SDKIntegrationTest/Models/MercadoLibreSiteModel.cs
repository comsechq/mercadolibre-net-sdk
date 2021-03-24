using System.Diagnostics;
using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// JSON friendly model representing a Mercado Libre site.
    /// </summary>
    [DebuggerDisplay("{Id} {Name}]")]
    public class MercadoLibreSiteModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}