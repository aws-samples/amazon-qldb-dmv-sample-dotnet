/*
 * Copyright 2021 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Linq;
using System.Net;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.QLDB.DMVSample.Api.Services;
using Amazon.QLDB.DMVSample.Model;
using Amazon.QLDB.Driver;
using Newtonsoft.Json;

using static Amazon.QLDB.DMVSample.Api.Functions.ConvertToIonValue;

namespace Amazon.QLDB.DMVSample.Api.Functions
{
    /// <summary>
    /// <para>Add a new vehicle if its VIN does not already exist.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class AddVehicleFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;

        public AddVehicleFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Vehicle vehicle = JsonConvert.DeserializeObject<Vehicle>(request.Body);
            APIGatewayProxyResponse response = new APIGatewayProxyResponse();

            this.qldbDriver.Execute(transactionExecutor =>
            {
                context.Logger.Log($"Checking vehicle already exists for VIN {vehicle.Vin}.");
                if (CheckIfVinAlreadyExists(transactionExecutor, vehicle.Vin))
                {
                    context.Logger.Log($"Vehicle does exist for VIN {vehicle.Vin}, returning not modified.");
                    response.StatusCode = (int)HttpStatusCode.NotModified;
                }
                else
                {
                    context.Logger.Log($"Inserting vehicle for VIN {vehicle.Vin}.");

                    IIonValue ionVehicle = ConvertObjectToIonValue(vehicle);
                    transactionExecutor.Execute($"INSERT INTO Vehicle ?", ionVehicle);

                    context.Logger.Log($"Inserted ionVehicle for VIN {vehicle.Vin}, returning OK.");
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
            });

            return response;
        }

        private bool CheckIfVinAlreadyExists(TransactionExecutor transactionExecutor, string vin)
        {
            IIonValue ionVin = this.valueFactory.NewString(vin);
            IResult selectResult = transactionExecutor.Execute("SELECT VIN FROM Vehicle AS v WHERE v.VIN = ?", ionVin);

            return selectResult.Any(x => x.GetField("VIN").StringValue == vin);
        }
    }
}
