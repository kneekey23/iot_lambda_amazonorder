# iot_lambda_amazonorder

This Serverless .NET Core Lambda Function uses a third party API called Zinc to send an Amazon order to a user pulled from a Dynamo Table. A text message is sent when the order is placed on the queue using SNS. The function is triggered by an AWS IOT Dash Button.

In order to make this work, you will need to add 2 json files for payment method and retailer credentials that will look like the below objects:

amazonCreds.json
```
{
  "email": "",
  "password": "",
  "totp_2fa_key": "" //optional if you have two factor auth turned on.
}
```

payment.json
```
{
  "name_on_card": "",
  "expiration_year": "",
  "expiration_month": "",
  "security_code": "",
  "number": "",
  "use_gift": false //or true
}
```

You will also need a dynamo Table called Users inside the same region you deploy this lambda to with the following properties:
```
{
  "address": "",
  "city": "",
  "firstName": "",
  "id": "", //serial number of the iot dash button
  "lastName": "",
  "orderPlaced": 0, // becomes true after an order is placed
  "phone": "",
  "productId": "B078ZPRYKB", //grab ASIN from amazon product
  "requestId": "", //empty until an order is placed
  "state": "",
  "zip": ""
}
```
