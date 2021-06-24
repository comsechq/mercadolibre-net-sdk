using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// Model to store Mercado Libre item Denounce result from JSON
    /// </summary>
    public class MercadoLibreCategoryDenounceModel
    {
        /// <summary>
        /// Gets or sets the description from removal the item.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the mercado libre identifier (i.e. their primary key for this result).
        /// </summary>
        /// <value>
        /// The item id.
        /// </value>
        [JsonPropertyName("id")]
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        [JsonPropertyName("group")]
        public string Group { get; set; }
    }
}