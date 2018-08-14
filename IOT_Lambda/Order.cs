using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IOT_Lambda
{
    public partial class Order
    {
        public Order(string firstName, string lastName, string address, string city, string state, string zip, string phone, string productId)
        {
            Retailer = "amazon";
            Products = new List<Product>();
            Product product = new Product
            {
                Quantity = 1,
                ProductId = productId
            };
            Products.Add(product);

            //shipping
            ShippingAddress = new IngAddress();
            ShippingAddress.PhoneNumber = phone;
            ShippingAddress.State = state;
            ShippingAddress.FirstName = firstName;
            ShippingAddress.LastName = lastName;
            ShippingAddress.City = city;
            ShippingAddress.AddressLine1 = address;
            ShippingAddress.ZipCode = zip;
            ShippingAddress.Country = "US";
            IsGift = false;
            MaxPrice = 10000;
            Shipping = new Shipping();
            Shipping.MaxDays = 2;
            Shipping.OrderBy = "price";

            //extract payment method from json file
            var path = "payment.json";
            using (StreamReader r = new StreamReader(path: path))
            {
                string json = r.ReadToEnd();
                PaymentMethod = JsonConvert.DeserializeObject<PaymentMethod>(json);
    
            }
            //billing
            BillingAddress = new IngAddress();
            BillingAddress.AddressLine1 = "2030 8th Ave";
            BillingAddress.AddressLine2 = "Unit 1805";
            BillingAddress.City = "Seattle";
            BillingAddress.State = "WA";
            BillingAddress.Country = "US";
            BillingAddress.FirstName = "Nicole";
            BillingAddress.LastName = "Klein";
            BillingAddress.PhoneNumber = "714-925-5830";
            BillingAddress.ZipCode = "98121";


            // get retailer creds from json file
            using (StreamReader r = new StreamReader("amazonCreds.json"))
            {
                string json = r.ReadToEnd();
                RetailerCredentials = JsonConvert.DeserializeObject<RetailerCredentials>(json);
            }

        }

        [JsonProperty("retailer")]
        public string Retailer { get; set; }

        [JsonProperty("products")]
        public List<Product> Products { get; set; }

        [JsonProperty("max_price")]
        public long MaxPrice { get; set; }

        [JsonProperty("shipping_address")]
        public IngAddress ShippingAddress { get; set; }

        [JsonProperty("is_gift")]
        public bool IsGift { get; set; }

        [JsonProperty("gift_message")]
        public string GiftMessage { get; set; }

        [JsonProperty("shipping")]
        public Shipping Shipping { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("billing_address")]
        public IngAddress BillingAddress { get; set; }

        [JsonProperty("retailer_credentials")]
        public RetailerCredentials RetailerCredentials { get; set; }

        [JsonProperty("webhooks")]
        public Webhooks Webhooks { get; set; }

        [JsonProperty("client_notes")]
        public ClientNotes ClientNotes { get; set; }
    }

    public partial class IngAddress
    {
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("address_line1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("address_line2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("zip_code")]
        public string ZipCode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }
    }

    public partial class ClientNotes
    {
        [JsonProperty("our_internal_order_id")]
        public string OurInternalOrderId { get; set; }

        [JsonProperty("any_other_field")]
        public List<string> AnyOtherField { get; set; }
    }

    public partial class PaymentMethod
    {
        [JsonProperty("name_on_card")]
        public string NameOnCard { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("security_code")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long SecurityCode { get; set; }

        [JsonProperty("expiration_month")]
        public long ExpirationMonth { get; set; }

        [JsonProperty("expiration_year")]
        public long ExpirationYear { get; set; }

        [JsonProperty("use_gift")]
        public bool UseGift { get; set; }
    }

    public partial class Product
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }
    }

    public partial class RetailerCredentials
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("totp_2fa_key")]
        public string Totp2FaKey { get; set; }
    }

    public partial class Shipping
    {
        [JsonProperty("order_by")]
        public string OrderBy { get; set; }

        [JsonProperty("max_days")]
        public long MaxDays { get; set; }

        [JsonProperty("max_price")]
        public long MaxPrice { get; set; }
    }

    public partial class Webhooks
    {
        [JsonProperty("request_succeeded")]
        public string RequestSucceeded { get; set; }

        [JsonProperty("request_failed")]
        public string RequestFailed { get; set; }

        [JsonProperty("tracking_obtained")]
        public string TrackingObtained { get; set; }
    }

    public partial class Order
    {
        public static Order FromJson(string json) => JsonConvert.DeserializeObject<Order>(json, IOT_Lambda.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Order self) => JsonConvert.SerializeObject(self, IOT_Lambda.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
