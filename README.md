# Amazon QLDB .NET DMV Sample App

The samples in this project demonstrate several uses of Amazon Quantum Ledger Database (QLDB).

For a .NET QLDB tutorial, see [.NET and Amazon QLDB](https://docs.aws.amazon.com/qldb/latest/developerguide/getting-started.dotnet.html).

## Requirements

### Basic Configuration

See [Accessing Amazon QLDB](https://docs.aws.amazon.com/qldb/latest/developerguide/accessing.html) for information on connecting to AWS.

### .NET

The sample app targets .NET Core 3.1. Please see the link below for more information on compatibility:

* [.NET Core](https://dotnet.microsoft.com/download/dotnet-core)

## Project Explanation

This app is split among three projects. 
* [Amazon.QLDB.DMVSample.Model](https://github.com/aws-samples/amazon-qldb-dmv-sample-dotnet/tree/master/Amazon.QLDB.DMVSample.Model) defines the nature of the data.
* [Amazon.QLDB.DMVSample.LedgerSetup](https://github.com/aws-samples/amazon-qldb-dmv-sample-dotnet/tree/master/Amazon.QLDB.DMVSample.LedgerSetup) runs our initial setup (creating a ledger, tables, indices, and inserting data).
* [Amazon.QLDB.DMVSample.Api](https://github.com/aws-samples/amazon-qldb-dmv-sample-dotnet/tree/master/Amazon.QLDB.DMVSample.Api) defines the AWS Lambda functions that can be used to handle HTTP requests when deployed.
    These functions include:
    * Adding a person to the DMV.
    * Adding a vehicle to the DMV.
    * Registering a vehicle with a person.
    * Registering a secondary owner of a vehicle.
    * Finding a vehicle registered to a given person.
    * Querying the vehicle registration history of a given vehicle.

## Using the Sample code

First we must set up our data. This can be done with the following command in the command line:

```
dotnet run --project Amazon.QLDB.DMVSample.LedgerSetup
```

Then we will use [AWS SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) to deploy the AWS Lambda functions. See these [installation instructions](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html) if you do not already have AWS SAM installed.

Navigate to the Api project directory, build the project, and deploy the lambda functions with the following commands:

```
cd Amazon.QLDB.DMVSample.Api
sam build
sam deploy --guided
```

For the guided deploy, feel free to give default values and answer 'y' (yes) for all questions.

[Visit the AWS CloudFormation console](https://console.aws.amazon.com/cloudformation) to see more detailed information regarding your deployed stack. If something went wrong wrong during deployment, you can delete your stack from the console and then deploy again. Alternatively you could use the [AWS CLI](https://aws.amazon.com/cli/) and delete your stack with the following command.

```
aws cloudformation delete-stack --stack-name <stack name>
```

Once deployment is successful, API endpoint URLs will be output for each lambda function with a description indicating whether it's meant for a GET, PUT, or POST request.

eg.
```
Key            FindVehiclesApi
Description    API Gateway endpoint URL for Find Vehicles GET requests
Value          https://odcqo191dk.execute-api.us-east-1.amazonaws.com/vehicles
```

If we want to query the vehicles for a given person (such as the person with GovId = "LEWISR261LL") using the above API, then we submit a GET request for https://odcqo191dk.execute-api.us-east-1.amazonaws.com/vehicles?GovId=LEWISR261LL and get the following data returned:

```
[{"VIN":"1N4AL11D75C109151","Type":"Sedan","Year":2011,"Make":"Audi","Model":"A5","Color":"Silver"}]
```

## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the LICENSE file.
