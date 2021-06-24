using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HttpParamsUtility;
using MercadoLibre.SDK.Meta;
using MercadoLibre.SDK.Models;
using Polly;
using Polly.Retry;

namespace MercadoLibre.SDK
{
    /// <summary>
    /// Service to wrap access to the Mercado Libre REST API.
    /// </summary>
    public class MeliApiService : IMeliApiService
    {
        private readonly HttpClient client;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="httpClient"></param>
        public MeliApiService(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient), "HttpClient required. Did you forget to register it as a dependency?");
            }

            httpClient.BaseAddress = new Uri("https://api.mercadolibre.com", UriKind.Absolute);

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MELI-NET-SDK", Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            client = httpClient;
        }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>
        /// The credentials.
        /// </value>
        public MeliCredentials Credentials { get; set; }

        /// <summary>
        /// Generate an URL to get an access token for other users.
        /// </summary>
        /// <param name="clientId">The client identifier (meli app ID).</param>
        /// <param name="site">The site.</param>
        /// <param name="redirectUri">The call back URI redirect URL to (Mercado Libre with append ?code=YOUR_SECRET_CODE to this URL).</param>
        /// <returns>
        /// The authentication URL to redirect your user to.
        /// </returns>
        public string GetAuthUrl(long clientId, MeliSite site, string redirectUri)
        {
            var parameters = new HttpParams().Add("response_type", "code")
                                             .Add("client_id", clientId)
                                             .Add("redirect_uri", redirectUri);

            var domain = site.ToDomain();

            return $"https://auth.{domain}/authorization?{parameters}";
        }

        /// <summary>
        /// Requests an access and refresh token from the code provided by the mercado libre callback.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="redirectUri">The redirect URI.</param>
        /// <returns>
        /// Return True when the operation is successful.
        /// </returns>
        public async Task<bool> AuthorizeAsync(string code, string redirectUri)
        {
            if (Credentials == null)
            {
                throw new ApplicationException("Credentials property not initialized.");
            }

            var success = false;

            var parameters = new HttpParams().Add("grant_type", "authorization_code")
                                             .Add("client_id", Credentials.ClientId)
                                             .Add("client_secret", Credentials.ClientSecret)
                                             .Add("code", code)
                                             .Add("redirect_uri", redirectUri);

            try
            {
                var tokens = await SendAsync<TokenResponse>(HttpMethod.Post, "/oauth/token", parameters);

                if (tokens != null)
                {
                    Credentials.SetTokens(tokens);

                    success = true;
                }
            }
            catch(HttpRequestException _)
            {
                success = false;
            }
            
            return success;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpRequestMessage"/> from given parameters.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="resource"></param>
        /// <param name="parameters"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private HttpRequestMessage ToRequestMessage(HttpMethod method, string resource, HttpParams parameters, object content = null)
        {
            var requestUrl = parameters == null
                                 ? resource
                                 : $"{resource}?{parameters}";

            var request = new HttpRequestMessage
                          {
                              RequestUri = new Uri(requestUrl, UriKind.Relative),
                              Method = method,
                          };

            if (!string.IsNullOrEmpty(Credentials?.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credentials.AccessToken);
            }

            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);

                request.Content = new StringContent(json);
            }

            return request;
        }

        /// <summary>
        /// Refreshes the access token.
        /// </summary>
        /// <returns></returns>
        private async Task<TokenResponse> RefreshAccessToken()
        {
            TokenResponse newTokens = null;

            var request = new HttpRequestMessage
                          {
                              RequestUri = new Uri("/oauth/token", UriKind.Relative),
                              Method = HttpMethod.Post,
                              Content = new FormUrlEncodedContent(new Dictionary<string, string>
                                                                  {
                                                                      {"grant_type", "refresh_token"},
                                                                      {"client_id", Credentials.ClientId.ToString()},
                                                                      {"client_secret", Credentials.ClientSecret},
                                                                      {"refresh_token", Credentials.RefreshToken}
                                                                  })
                          };

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStreamAsync();

                newTokens = await JsonSerializer.DeserializeAsync<TokenResponse>(json);

                if (newTokens != null)
                {
                    // Will fire a MeliTokenEventArgs if the tokens have changed (and need to be persisted)
                    Credentials.SetTokens(newTokens);
                }
            }

            return newTokens;
        }

        /// <summary>
        /// Creates the token refresh policy.
        /// </summary>
        /// <returns></returns>
        private AsyncRetryPolicy<HttpResponseMessage> CreateTokenRefreshPolicy()
        {
            var policy = Policy.HandleResult<HttpResponseMessage>(msg =>
                                                                      msg.StatusCode == System.Net.HttpStatusCode.Unauthorized
                                                                      && Credentials != null
                                                                      && !string.IsNullOrEmpty(Credentials.RefreshToken))
                               .RetryAsync(1, async (result, retryCount, context) =>
                                              {
                                                  var newAccessToken = await RefreshAccessToken();
                                              });

            return policy;
        }

        /// <summary>
        /// Sends the specified client.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="resource">The relative resource (e.g. "/users/me").</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content (will be serialized to JSON).</param>
        /// <returns></returns>
        protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string resource, HttpParams parameters, object content = null)
        {
            // Inspired by https://www.jerriepelser.com/blog/refresh-google-access-token-with-polly/
            var policy = CreateTokenRefreshPolicy();

            var response = await policy.ExecuteAsync(() =>
                                                     {
                                                         // Important not to re-use the request message between attempts
                                                         var request = ToRequestMessage(method, resource, parameters, content);
                                                         return client.SendAsync(request);
                                                     });
            
            return response;
        }

        /// <summary>
        /// Sends the specified client and deserializes the JSON response to the given <see cref="T" /> model.
        /// </summary>
        /// <typeparam name="T">The type of the model expected as a JSON response.</typeparam>
        /// <param name="method">The method.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content (will be serialized to JSON).</param>
        /// <returns>
        ///   <see cref="T" />
        /// </returns>
        protected async Task<T> SendAsync<T>(HttpMethod method, string resource, HttpParams parameters, object content = null)
        {
            T result;

            var response = await SendAsync(method, resource, parameters, content);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverterWithAttributeSupport());

                var json = await response.Content.ReadAsStreamAsync();
                result = await JsonSerializer.DeserializeAsync<T>(json, options);
            }
            else
            {
                throw new HttpRequestException(response.ReasonPhrase);
            }
            
            return result;
        }

        /// <summary>
        /// Sends a GET request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetAsync(string resource, HttpParams parameters = null)
        {
            return await SendAsync(HttpMethod.Get, resource, parameters);
        }

        /// <summary>
        /// Sends a GET request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string resource, HttpParams parameters = null)
        {
            return await SendAsync<T>(HttpMethod.Get, resource, parameters);
        }

        /// <summary>
        /// Sends a POST request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The payload for the content of the HTTP request.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostAsync(string resource, HttpParams parameters = null, object content = null)
        {
            return await SendAsync(HttpMethod.Post, resource, parameters, content);
        }

        /// <summary>
        /// Sends a POST request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The payload for the content of the HTTP request.</param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string resource, HttpParams parameters = null, object content = null)
        {
            return await SendAsync<T>(HttpMethod.Post, resource, parameters, content);
        }

        /// <summary>
        /// Sends a PUT request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutAsync(string resource, HttpParams parameters = null, object content = null)
        {
            return await SendAsync(HttpMethod.Put, resource, parameters, content);
        }

        /// <summary>
        /// Sends a PUT request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public async Task<T> PutAsync<T>(string resource, HttpParams parameters = null, object content = null)
        {
            return await SendAsync<T>(HttpMethod.Put, resource, parameters, content);
        }

        /// <summary>
        /// Sends a DELETE request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteAsync(string resource, HttpParams parameters = null)
        {
            return await SendAsync(HttpMethod.Delete, resource, parameters);
        }
        
        /// <summary>
        /// Sends a DELETE request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<T> DeleteAsync<T>(string resource, HttpParams parameters = null)
        {
            return await SendAsync<T>(HttpMethod.Delete, resource, parameters);
        }
    }
}