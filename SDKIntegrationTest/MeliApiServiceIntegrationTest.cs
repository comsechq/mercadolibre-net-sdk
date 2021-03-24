using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HttpParamsUtility;
using MercadoLibre.SDK.Meta;
using MercadoLibre.SDK.Models;
using NUnit.Framework;

namespace MercadoLibre.SDK
{
    [TestFixture]
    [Ignore("This is an example of how you would use the MeliApiService, you must set valid credentials")]
    public class MeliApiServiceIntegrationTest
    {
        private readonly MeliApiService service = new MeliApiService(new HttpClient())
        {
            // Danger danger: DO NOT commit real credentials to the repository!
            Credentials = new MeliCredentials(
                MeliSite.Argentina,
                1234567890123456,
                "client_secret",
                "APP_USR-access-token",
                "TG-refresh-token")
        };

        [Test]
        public async Task TestGetSites()
        {
            var results = (await service.GetAsync<MercadoLibreSiteModel[]>("/sites")).ToArray();

            Assert.LessOrEqual(0, results.Length);

            Assert.That(results[0].Id, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Name, Is.Not.Null.Or.Empty);
        }

        [Test]
        public async Task TestItem()
        {
            var itemId = "MLM123456789";

            var result = await service.GetAsync<MercadoLibreItemModel>($"/items", new HttpParams().Add("ids", itemId));
            
            Assert.That(result.ItemId, Is.Not.Null.Or.Empty);
            Assert.That(result.Title, Is.Not.Null.Or.Empty);
            Assert.That(result.SiteId, Is.Not.Null.Or.Empty);
            Assert.LessOrEqual(0, result.Price);
            Assert.That(result.Currency, Is.Not.Null.Or.Empty);
            Assert.LessOrEqual(0, result.Quantity);
            Assert.LessOrEqual(0, result.QuantitySold);
            Assert.LessOrEqual(0, result.InitialQuantity);
            Assert.NotNull(result.BuyingMode);
            Assert.NotNull(result.Condition);
            Assert.That(result.Status, Is.Not.Null.Or.Empty);
            Assert.That(result.Url, Is.Not.Null.Or.Empty);
            Assert.NotNull(result.StartTime);
            Assert.NotNull(result.EndTime);

            Assert.That(result.SellerId, Is.Not.Null.Or.Empty);
            
            Assert.LessOrEqual(0, result.Pictures.Count);
            Assert.That(result.Pictures[0].Url, Is.Not.Null.Or.Empty);
        }
    }
}
