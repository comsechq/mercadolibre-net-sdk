using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// Model to store Mercado Libre item result from JSON.
    /// </summary>
    public class MercadoLibreItemResponseModel
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        [JsonPropertyName("body")]
        public MercadoLibreItemModel Body { get; set; }
    }
}