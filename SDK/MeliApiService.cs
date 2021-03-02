using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using HttpParamsUtility;
using MercadoLibre.SDK.Http;
using MercadoLibre.SDK.Meta;
using MercadoLibre.SDK.Models;

namespace MercadoLibre.SDK
{
    /// <summary>
    /// Service to wrap access to the Mercado Libre REST API.
    /// </summary>
    public class MeliApiService : IMeliApiService
    {
        public static readonly string SdkVersion = Assembly.GetExecutingAssembly()
                                                           .GetName()
                                                           .Version
                                                           .ToString();
        public static string SdkUserAgent = "MELI-NET-SDK";
        public static Uri ApiUrl = new Uri("https://api.mercadolibre.com", UriKind.Absolute);

        /// <summary>
        /// Initializes a new instance of the <see cref="MeliApiService" /> class.
        /// </summary>
        public MeliApiService()
        {
            HttpClientProvider = new HttpClientProvider
                                 {
                                     RetryIntercept = RequestNewToken,
                                     InitialiseWith = client =>
                                         {
                                             client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SdkUserAgent, SdkVersion));
                                             client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                         }
                                 };
        }

        /// <summary>
        /// Gets or sets the HTTP client provider.
        /// </summary>
        /// <value>
        /// The HTTP client provider.
        /// </value>
        public IHttpClientProvider HttpClientProvider { get; set; }

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

            using (var client = HttpClientProvider.Create(false))
            {
                var tokens = await SendAsync<TokenResponse>(client, HttpMethod.Post, ApiUrl, "/oauth/token", parameters);

                if (tokens != null)
                {
                    Credentials.SetTokens(tokens);

                    success = true;
                }
            }

            return success;
        }

        private int refreshTokenAttempt = 0;

        /// <summary>
        /// Request a new token.
        /// </summary>
        /// <param name="originalRequest">The original request.</param>
        /// <param name="httpResponseMessage">The HTTP response message.</param>
        /// <returns>
        /// True to tell <see cref="RetryDelegatingHandler" /> to retry the original request.
        /// </returns>
        /// <remarks>
        /// Hook called automatically (set in constructor) by HttpClient after each request.
        /// </remarks>
        private async Task<bool> RequestNewToken(HttpRequestMessage originalRequest, HttpResponseMessage httpResponseMessage)
        {
            var shouldRetry = false;

            refreshTokenAttempt++;

            // Retry only once and if we have a refresh token and the token is invalid
            if (!httpResponseMessage.IsSuccessStatusCode
                && Credentials != null 
                && !string.IsNullOrEmpty(Credentials.RefreshToken)
                && refreshTokenAttempt <= 1)
            {
                var content = await httpResponseMessage.Content.ReadAsStreamAsync();

                var response = await JsonSerializer.DeserializeAsync<ErrorResponse>(content);

                if (response?.Message.Equals("invalid token", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    var parameters = new HttpParams().Add("grant_type", "refresh_token")
                                                     .Add("client_id", Credentials.ClientId)
                                                     .Add("client_secret", Credentials.ClientSecret)
                                                     .Add("refresh_token", Credentials.RefreshToken);

                    using (var client = new HttpClient())
                    {
                        var request = new HttpRequestMessage
                                      {
                                          RequestUri = new Uri($"{ApiUrl}oauth/token?{parameters}"),
                                          Method = HttpMethod.Post,
                                      };

                        var tokenResponse = await client.SendAsync(request);

                        if (tokenResponse.IsSuccessStatusCode)
                        {
                            var json = await tokenResponse.Content.ReadAsStreamAsync();

                            var newTokens = await JsonSerializer.DeserializeAsync<TokenResponse>(json);

                            if (newTokens != null)
                            {
                                Credentials.SetTokens(newTokens);

                                ReplaceAccessToken(originalRequest, newTokens.AccessToken);

                                shouldRetry = true;
                            }
                        }
                    }
                }
            }
            
            return shouldRetry;
        }

        /// <summary>
        /// Replaces the access token in the URI of the <see cref="request"/> (if present).
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="newAccessToken">The new access token.</param>
        public static void ReplaceAccessToken(HttpRequestMessage request, string newAccessToken)
        {
            var url = request.RequestUri.AbsoluteUri;

            if (url.Contains("access_token="))
            {
                var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                
                query["access_token"] = newAccessToken;

                var uriBuilder = new UriBuilder(request.RequestUri)
                                 {
                                     Query = query.ToString()
                                 };
                
                request.RequestUri = new Uri(uriBuilder.ToString());
            }
        }

        /// <summary>
        /// Sends the specified client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="method">The method.</param>
        /// <param name="baseAddress">The base address (e.g. "https://api.mercadolibre.com/").</param>
        /// <param name="resource">The relative resource (e.g. "/users/me").</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content (will be serialised to JSON).</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        protected async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, Uri baseAddress, 
                                                            string resource, HttpParams parameters, object content = null, string accessToken = null)
        {
            var requestUrl = parameters == null
                ? resource
                : $"{resource}?{parameters}";

            client.BaseAddress = baseAddress;

            var request = new HttpRequestMessage
                          {
                              RequestUri = new Uri(requestUrl, UriKind.Relative),
                              Method = method,
                          };

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);

                request.Content = new StringContent(json);
            }
            
            refreshTokenAttempt = 0;

            var response = await client.SendAsync(request);
            
            return response;
        }

        /// <summary>
        /// Sends the specified client and deserializes the JSON response to the given <see cref="T" /> model.
        /// </summary>
        /// <typeparam name="T">The type of the model expected as a JSON response.</typeparam>
        /// <param name="client">The HTTP client.</param>
        /// <param name="method">The method.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content (will be serialized to JSON).</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>
        ///   <see cref="T" />
        /// </returns>
        protected async Task<T> SendAsync<T>(HttpClient client, HttpMethod method, Uri baseAddress, 
                                             string resource, HttpParams parameters, object content = null, string accessToken = null)
        {
            var result = default(T);

            var response = await SendAsync(client, method, baseAddress, resource, parameters, content, accessToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStreamAsync();

                result = await JsonSerializer.DeserializeAsync<T>(json);
            }
            
            return result;
        }

        /// <summary>
        /// Sends a GET request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetAsync(string resource, HttpParams parameters = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync(client, HttpMethod.Get, ApiUrl, resource, parameters, accessToken);
            }
        }

        /// <summary>
        /// Sends a GET request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string resource, HttpParams parameters = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync<T>(client, HttpMethod.Get, ApiUrl, resource, parameters, accessToken);
            }
        }

        /// <summary>
        /// Sends a POST request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The payload for the content of the HTTP request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostAsync(string resource, HttpParams parameters = null, object content = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync(client, HttpMethod.Post, ApiUrl, resource, parameters, content, accessToken);
            }
        }

        /// <summary>
        /// Sends a POST request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The payload for the content of the HTTP request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string resource, HttpParams parameters = null, object content = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync<T>(client, HttpMethod.Post, ApiUrl, resource, parameters, content, accessToken);
            }
        }

        /// <summary>
        /// Sends a PUT request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutAsync(string resource, HttpParams parameters = null, object content = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync(client, HttpMethod.Put, ApiUrl, resource, parameters, content, accessToken);
            }
        }

        /// <summary>
        /// Sends a PUT request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="content">The content.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<T> PutAsync<T>(string resource, HttpParams parameters = null, object content = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync<T>(client, HttpMethod.Put, ApiUrl, resource, parameters, content, accessToken);
            }
        }

        /// <summary>
        /// Sends a DELETE request.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteAsync(string resource, HttpParams parameters = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync(client, HttpMethod.Delete, ApiUrl, resource, parameters, accessToken);
            }
        }

        /// <summary>
        /// Sends a DELETE request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="T">The class to use to deserialize the JSON response.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public async Task<T> DeleteAsync<T>(string resource, HttpParams parameters = null, HttpClientHandler handler = null, string accessToken = null)
        {
            handler = handler ?? new HttpClientHandler();

            using (var client = HttpClientProvider.Create(handler))
            {
                return await SendAsync<T>(client, HttpMethod.Delete, ApiUrl, resource, parameters, accessToken);
            }
        }
    }
}