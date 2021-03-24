using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// JSON friendly model representing a picture for a Mercado Libre item.
    /// </summary>
    public class MercadoLibrePictureModel
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}