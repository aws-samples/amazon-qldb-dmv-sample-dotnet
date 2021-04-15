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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.QLDB.DMVSample.Model;
using Amazon.QLDB.DMVSample.Api.Services;
using Amazon.QLDB.Driver;
using Newtonsoft.Json;

namespace Amazon.QLDB.DMVSample.Api.Functions
{
    /// <summary>
    /// <para>Find all vehicles registered under a person.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class FindVehiclesFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;
        
        public FindVehiclesFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string govId = request.QueryStringParameters["GovId"];
            
            IResult selectResult = this.qldbDriver.Execute(transactionExecutor =>
            {
                string personDocumentId = this.tableMetadataService.GetDocumentId(transactionExecutor, "Person", "GovId", govId);
                context.Logger.Log($"Searching for vehicles where primary owner ID is {personDocumentId}.");

                IIonValue ionPersonDocumentId = this.valueFactory.NewString(personDocumentId);
                IResult result = transactionExecutor.Execute("SELECT v FROM Vehicle AS v INNER JOIN VehicleRegistration AS r "
                    + "ON v.VIN = r.VIN WHERE r.Owners.PrimaryOwner.PersonId = ?", ionPersonDocumentId);
                return result;
            });

            IEnumerable<Vehicle> vehicles = selectResult.Select(x => 
            {
                var ionVehicle = x.GetField("v");
                return new Vehicle 
                {
                    Vin = ionVehicle.GetField("VIN").StringValue,
                    Type = ionVehicle.GetField("Type").StringValue,
                    Year = ionVehicle.GetField("Year").IntValue,
                    Make = ionVehicle.GetField("Make").StringValue,
                    Model = ionVehicle.GetField("Model").StringValue,
                    Color = ionVehicle.GetField("Color").StringValue
                };
            });

            return new APIGatewayProxyResponse 
            { 
                StatusCode = vehicles.Any() ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound,
                Body = JsonConvert.SerializeObject(vehicles)
            };
        }
    }
}
