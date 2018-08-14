using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Amazon.Lambda.Core;
using System.Text;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace IOT_Lambda
{
    public class Function
    {
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static AmazonSimpleNotificationServiceClient snsClient = new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.USWest2);
        private static AmazonDynamoDBClient dynamoClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USWest2);
        private static DynamoDBContext dynamoContext = new DynamoDBContext(dynamoClient);


        public async Task FunctionHandler(ButtonInput input, ILambdaContext context)
        {
            //place amazon order
     
            Users user = await GetUser(input.serialNumber);
            context.Logger.LogLine("Order Placed:" + user.orderPlaced.ToString());
            if (!user.orderPlaced)
            {
                await ZincRequest(user, context);
            }
        }

        public async Task ZincRequest(Users user, ILambdaContext context)
        {
            context.Logger.LogLine("inside Zinc function");
            Order newOrder = new Order(user.firstName, user.lastName, user.address, user.city, user.state, user.zip, user.phone, user.productId);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.zinc.io");
                client.DefaultRequestHeaders.Accept.Clear();
                var byteArray = Encoding.ASCII.GetBytes("22F7462B4C97B3EE889DAF33:");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(newOrder.ToJson().ToString(), Encoding.UTF8, "application/json");
                var result = await client.PostAsync("/v1/orders", content);
                result.EnsureSuccessStatusCode();
                string resultContent = await result.Content.ReadAsStringAsync();
            
                //if order was placed on the queue send text message
                if (result.IsSuccessStatusCode)
                {
                    Dictionary<string, MessageAttributeValue> smsAttributes = new Dictionary<string, MessageAttributeValue>();

                    smsAttributes.Add("AWS.SNS.SMS.SMSType", new MessageAttributeValue()
                    {
                        StringValue = "Transactional",
                        DataType = "String"

                    });

                    //publish requestid to topic
                    await PublishRequestIdToTopic(resultContent, smsAttributes, context);
                    //send message to user
                    await SendSMSMessage("Your order has been entered into the queue", user.phone, smsAttributes, context);

                    //save order request id to User
                    await UpdateUser(user.id, resultContent, context);

                }


            }
        }

        public async Task UpdateUser(string id, string requestId, ILambdaContext context)
        {
            Users user = await dynamoContext.LoadAsync<Users>(id);

            user.orderPlaced = true;
            user.requestId = requestId;

            await dynamoContext.SaveAsync(user);
   
        }

        public async Task SendSMSMessage(string message,
        string phoneNumber, Dictionary<string, MessageAttributeValue> smsAttributes, ILambdaContext context)
        {
            PublishRequest request = new PublishRequest();
            request.Message = message;
            request.PhoneNumber = phoneNumber;
            request.MessageAttributes = smsAttributes;

            await snsClient.PublishAsync(request);

        }

        public async Task PublishRequestIdToTopic(string requestId, Dictionary<string, MessageAttributeValue> smsAttributes, ILambdaContext context)
        {
            PublishRequest request = new PublishRequest();
            request.TopicArn = "arn:aws:sns:us-west-2:805580953652:UserOrder";
            request.MessageAttributes = smsAttributes;
            request.Message = requestId;
            await snsClient.PublishAsync(request);
        }
        public async Task<Users> GetUser(string id)
        {
            Users user = await dynamoContext.LoadAsync<Users>(id);
            return user;
        }

    }

    public partial class Users
    {
        public string firstName { get; set; }

        public string lastName { get; set; }

        public bool orderPlaced { get; set; }
        public string requestId { get; set; }
        public string address { get; set; }

        public string city { get; set; }

        public string state { get; set; }

        public string zip { get; set; }

        public string phone { get; set; }

        public string productId { get; set; }
    }


    [DynamoDBTable("Users")]
    public partial class Users
    {
        [DynamoDBHashKey]
        public string id { get; set;}
    }

    public class ButtonInput
    {
        public string serialNumber { get; set; }

        public string batteryVoltage { get; set; }

        public string clickType { get; set; }
    }
}
