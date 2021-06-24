using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HttpParamsUtility;
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
                                                      Credentials = new MeliCredentials("APP_USR-access-token")
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
            // Replace this with a real Mercado Libre Argentina item id
            const string itemId = "MLM123456789";

            var results = (await service.GetAsync<MercadoLibreItemResponseModel[]>("/items", new HttpParams().Add("ids", itemId))).ToArray();
            
            Assert.AreEqual(1, results.Length);

            Assert.AreEqual(200, results[0].Code);

            Assert.NotNull(results[0].Body);
            Assert.That(results[0].Body.ItemId, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Body.Title, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Body.SiteId, Is.Not.Null.Or.Empty);
            Assert.LessOrEqual(0, results[0].Body.Price);
            Assert.That(results[0].Body.Currency, Is.Not.Null.Or.Empty);
            Assert.LessOrEqual(0, results[0].Body.Quantity);
            Assert.LessOrEqual(0, results[0].Body.QuantitySold);
            Assert.LessOrEqual(0, results[0].Body.InitialQuantity);
            Assert.NotNull(results[0].Body.BuyingMode);
            Assert.NotNull(results[0].Body.Condition);
            Assert.That(results[0].Body.CategoryId, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Body.Status, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Body.Url, Is.Not.Null.Or.Empty);
            Assert.NotNull(results[0].Body.StartTime);
            Assert.NotNull(results[0].Body.EndTime);

            Assert.LessOrEqual(0, results[0].Body.SellerId);

            Assert.LessOrEqual(0, results[0].Body.Pictures.Count);
            Assert.That(results[0].Body.Pictures[0].Url, Is.Not.Null.Or.Empty);
        }

        [Test]
        public async Task TestGetRemovalReasons()
        {
            var results = (await service.GetAsync<MercadoLibreCategoryDenounceModel[]>("/moderations/pppi/denounces/MLA/ITM/options")).ToArray();

            Assert.Less(0, results.Length);

            Assert.That(results[0].ItemId, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Description, Is.Not.Null.Or.Empty);
            Assert.That(results[0].Group, Is.Not.Null.Or.Empty);
        }
    }
}
