using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// Model returned by the API when a request is not successful.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the message (e.g. "invalid_token").
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the error (e.g. "not_found").
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        [JsonPropertyName("error")]
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status (e.g. 404).
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
