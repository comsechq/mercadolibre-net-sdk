using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// Model returned by the API when requesting or refreshing a token.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>
        /// The access token.
        /// </value>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the previous access token.
        /// </summary>
        /// <value>
        /// The previous access token.
        /// </value>
        /// <remarks>
        /// Handy when dealing with access tokens from multiple users.
        /// </remarks>
        public string PreviousAccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the token.
        /// </summary>
        /// <value>
        /// The type of the token.
        /// </value>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds the tokens expires in (i.e. 6 hours or 21600 seconds).
        /// </summary>
        /// <value>
        /// The expires in.
        /// </value>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the scope (a combination of "read", "write" and "offline_access", space separated).
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the mercado libre user identifier.
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        /// <value>
        /// The refresh token.
        /// </value>
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
