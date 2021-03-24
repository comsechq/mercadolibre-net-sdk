using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MercadoLibre.SDK.Models
{
    /// <summary>
    /// JSON friendly model representing a Mercado Libre item.
    /// </summary>
    public class MercadoLibreItemModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MercadoLibreItemModel"/> class.
        /// </summary>
        public MercadoLibreItemModel()
        {
            Pictures = new List<MercadoLibrePictureModel>();
        }

        /// <summary>
        /// Gets or sets the Watchdog primary key for that result.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the mercado libre identifier (i.e. their primary key for this result).
        /// </summary>
        /// <value>
        /// The item id.
        /// </value>
        [JsonPropertyName("id")]
        public string ItemId { get; set; }
        
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the result.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        [JsonPropertyName("permalink")]
        public string Url { get; set; }
        
        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        /// <value>
        /// The site identifier.
        /// </value>
        [JsonPropertyName( "site_id")]
        public string SiteId { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>
        /// The price.
        /// </value>
        [JsonPropertyName("price")]
        public double? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency identifier.
        /// </summary>
        /// <value>
        /// The currency identifier.
        /// </value>
        [JsonPropertyName("currency_id")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets the initial quantity.
        /// </summary>
        /// <value>
        /// The initial quantity.
        /// </value>
        [JsonPropertyName("initial_quantity")]
        public int InitialQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>
        /// The quantity.
        /// </value>
        [JsonPropertyName("available_quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity sold.
        /// </summary>
        /// <value>
        /// The quantity sold.
        /// </value>
        [JsonPropertyName("sold_quantity")]
        public int QuantitySold { get; set; }

        /// <summary>
        /// Gets or sets the buying mode.
        /// </summary>
        /// <value>
        /// The buying mode.
        /// </value>
        [JsonPropertyName("buying_mode")]
        public string BuyingMode { get; set; }

        /// <summary>
        /// Gets or sets the condition.
        /// </summary>
        /// <value>
        /// The condition.
        /// </value>
        [JsonPropertyName("condition")]
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        /// <value>
        /// The start time.
        /// </value>
        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        /// <value>
        /// The end time.
        /// </value>
        [JsonPropertyName("stop_time")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the seller identifier.
        /// </summary>
        /// <value>
        /// The seller identifier.
        /// </value>
        [JsonPropertyName("seller_id")]
        public string SellerId { get; set; }
        
        /// <summary>
        /// Gets or sets the pictures.
        /// </summary>
        /// <value>
        /// The pictures.
        /// </value>
        [JsonPropertyName("pictures")]
        public IList<MercadoLibrePictureModel> Pictures { get; set; }

        /// <summary>
        /// Gets or sets the category identifier.
        /// </summary>
        /// <value>
        /// The category identifier.
        /// </value>
        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; }
    }
}