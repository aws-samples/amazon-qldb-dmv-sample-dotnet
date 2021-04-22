# Amazon QLDB .NET DMV Sample App

The samples in this project demonstrate several uses of Amazon Quantum Ledger Database (QLDB).

For our tutorial, see [.NET and Amazon QLDB](https://docs.aws.amazon.com/qldb/latest/developerguide/getting-started.dotnet.html).

## Requirements

### Basic Configuration

See [Accessing Amazon QLDB](https://docs.aws.amazon.com/qldb/latest/developerguide/accessing.html) for information on connecting to AWS.

See [Setting Region](https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-region-selection.html) page for more information on using the AWS SDK for .NET. You will need to set a region before running the sample code.

## Running the Sample code

First we must create our ledger, tables, indices, and insert our data. This can be done with the following command:

```
dotnet run --project Amazon.QLDB.DMVSample.LedgerSetup
```

Then we will use [AWS SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) to deploy the AWS Lambda functions. Install AWS SAM with the following:

```
brew tap aws/tap
brew install aws-sam-cli
```

Enter into the Api project, build the project, and deploy the lambda functions with the following:

```
cd Amazon.QLDB.DMVSample.Api
sam build
sam deploy --guided
```

For the guided deploy, feel free to give default values and answer 'y' (yes) for all questions.

If something goes wrong, you can delete your stack before re-deploying with the following:

```
aws cloudformation delete-stack --stack-name <stack name>
```

Once deployment is successful, API endpoint URLs will be output for each lambda function with a description indicating whether it's meant for a GET, PUT, or POST request.

eg.
```
Key            FindVehiclesApi
Description    API Gateway endpoint URL for Find Vehicles GET requests
Value          https://eaxk5adkhb.execute-api.us-east-1.amazonaws.com/vehicles
```

## License

This library is licensed under the MIT-0 License. See the LICENSE file.
