using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HttpParamsUtility;
using MercadoLibre.SDK.Meta;
using MercadoLibre.SDK.Models;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace MercadoLibre.SDK
{
    [TestFixture]
    [Parallelizable]
    public class MeliApiServiceTest
    {
        public MeliApiService Setup(out HttpClient client, out MockHttpMessageHandler handler)
        {
            handler = new MockHttpMessageHandler();
            client = new HttpClient(handler);

            var credentials = new MeliCredentials(MeliSite.Argentina, 123456, "secret");

            var service = new MeliApiService(client)
            {
                Credentials = credentials
            };

            return service;
        }

        [Test]
        public void TestSdkUserAgent()
        {
            var _ = Setup(out var client, out var _);

            Assert.AreEqual("application/json", client.DefaultRequestHeaders.Accept.ToString());
            Assert.IsTrue(client.DefaultRequestHeaders.UserAgent.ToString().StartsWith("MELI-NET-SDK/"));
        }
        
        [Test]
        public void TestGetAuthUrl()
        {
            var service = Setup(out _, out var _);

            var url = service.GetAuthUrl(123456, MeliSite.Mexico, "http://someurl.com");

            Assert.AreEqual("https://auth.mercadolibre.com.mx/authorization?response_type=code&client_id=123456&redirect_uri=http%3a%2f%2fsomeurl.com", url);
        }

        [Test]
        public async Task TestAuthorizeAsyncIsSuccessful()
        {
            var service = Setup(out _, out var mockHttp);

            service.Credentials.Site = MeliSite.Mexico;

            var eventArgs = new List<MeliTokenEventArgs>();

            service.Credentials.TokensChanged += (sender, args) => eventArgs.Add(args);

            service.Credentials.Site = MeliSite.Mexico;

            var response = new TokenResponse
                           {
                               AccessToken = "valid token",
                               RefreshToken = "valid refresh token"
                           };

            mockHttp.Expect(HttpMethod.Post, "https://api.mercadolibre.com/oauth/token")
                    .WithQueryString("grant_type", "authorization_code")
                    .WithQueryString("client_id", "123456")
                    .WithQueryString("client_secret", "secret")
                    .WithQueryString("code", "valid code with refresh token")
                    .WithQueryString("redirect_uri", "https://someurl.com")
                    .Respond("application/json", JsonSerializer.Serialize(response));

            var success = await service.AuthorizeAsync("valid code with refresh token", "https://someurl.com");

            Assert.IsTrue(success);

            Assert.AreEqual("valid token", service.Credentials.AccessToken);
            Assert.AreEqual("valid refresh token", service.Credentials.RefreshToken);

            Assert.AreEqual(1, eventArgs.Count);
            Assert.AreEqual("valid token", eventArgs[0].Info.AccessToken);
            Assert.AreEqual("valid refresh token", eventArgs[0].Info.RefreshToken);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test]
        public async Task TestAuthorizeAsyncIsNotSuccessful()
        {
            var service = Setup(out _, out var mockHttp);

            service.Credentials.Site = MeliSite.Ecuador;

            mockHttp.Expect(HttpMethod.Post, "https://api.mercadolibre.com/oauth/token")
                    .WithQueryString("grant_type", "authorization_code")
                    .WithQueryString("code", "invalid code")
                    .WithQueryString("redirect_uri", "https://someurl.com")
                    .Respond(HttpStatusCode.Unauthorized);

            var success = await service.AuthorizeAsync("invalid code", "https://someurl.com");

            Assert.IsFalse(success);

            mockHttp.VerifyNoOutstandingExpectation();
        }
        
        public class SiteModel
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("country")]
            public CountryEnumModel Country { get; set; }
        }

        public enum CountryEnumModel
        {
            [EnumMember(Value = "Argentina")]
            AR,
            [EnumMember(Value = "Brazil")]
            BR
        }

        [Test]
        public async Task TestGetAsyncToGetSites()
        {
            var service = Setup(out _, out var mockHttp);

            service.Credentials.Site = MeliSite.Peru;

            var responsePayload = new[] {new {id = "MLA", name = "Argentina", country = "Argentina"}, new {id = "MLB", name = "Brazil", country = "Brazil"}};

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverterWithAttributeSupport());

            mockHttp.Expect(HttpMethod.Get, "https://api.mercadolibre.com/sites")
                    .Respond("application/json", JsonSerializer.Serialize(responsePayload, options));

            var response = await service.GetAsync("/sites");

            Assert.IsTrue(response.IsSuccessStatusCode);

            var json = await response.Content.ReadAsStringAsync();

            var sites = JsonSerializer.Deserialize<SiteModel[]>(json, options);

            Assert.Less(0, sites.Length);
            Assert.AreEqual("MLA", sites[0].Id);
            Assert.AreEqual("Argentina", sites[0].Name);
            Assert.AreEqual(CountryEnumModel.AR, sites[0].Country);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test]
        public async Task TestGetAsyncHandleErrors()
        {
            var service = Setup(out _, out var mockHttp);

            mockHttp.Expect(HttpMethod.Get, "/users/me")
                    .Respond(HttpStatusCode.InternalServerError);

            var response = await service.GetAsync("/users/me");

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test]
        public async Task TestPostAsync()
        {
            var service = Setup(out _, out var mockHttp);

            mockHttp.Expect(HttpMethod.Post, "/items")
                    .WithQueryString("hello", "boo boo")
                    .WithContent(@"{""foo"":""bar""}")
                    .Respond(HttpStatusCode.Created);

            var response = await service.PostAsync("/items", new HttpParams().Add("hello", "boo boo"), new {foo = "bar"});

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test]
        public async Task TestPutAsync()
        {
            var service = Setup(out _, out var mockHttp);

            mockHttp.Expect(HttpMethod.Put, "/items/123")
                    .WithContent(@"{""foo"":""bar""}")
                    .Respond(HttpStatusCode.OK);

            var response = await service.PutAsync("/items/123", null, new {foo = "bar"});

            Assert.IsTrue(response.IsSuccessStatusCode);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test]
        public async Task TestDeleteAsync()
        {
            var service = Setup(out _, out var mockHttp);

            mockHttp.Expect(HttpMethod.Delete, "/items/123")
                    .Respond(HttpStatusCode.OK);

            var response = await service.DeleteAsync("/items/123");

            Assert.IsTrue(response.IsSuccessStatusCode);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}