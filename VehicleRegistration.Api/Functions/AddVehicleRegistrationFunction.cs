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
using Amazon.QLDB.Driver;
using Newtonsoft.Json;
using VehicleRegistration.Api.Services;
using static VehicleRegistration.Api.Functions.ConvertToIonValue;

namespace VehicleRegistration.Api.Functions
{
    /// <summary>
    /// <para>Add a new vehicle registration for a given person's GovId and a given vehicle's VIN.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class AddVehicleRegistrationFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;

        public AddVehicleRegistrationFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            VehicleRegistration.Model.VehicleRegistration vehicleRegistration = JsonConvert.DeserializeObject<VehicleRegistration.Model.VehicleRegistration>(request.Body);
            APIGatewayProxyResponse response = new APIGatewayProxyResponse();

            this.qldbDriver.Execute(transactionExecutor =>
            {
                context.Logger.Log($"Looking for person document ID {vehicleRegistration.Owners?.PrimaryOwner?.PersonId}.");
                string primaryOwnerPersonDocumentId = this.tableMetadataService.GetDocumentId(transactionExecutor, "Person", "GovId", vehicleRegistration.Owners?.PrimaryOwner?.PersonId);
                
                if (string.IsNullOrWhiteSpace(primaryOwnerPersonDocumentId))
                {
                    context.Logger.Log($"No person found with GovId {vehicleRegistration.Owners?.PrimaryOwner?.PersonId}, returning not found.");
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    transactionExecutor.Abort();
                }

                context.Logger.Log($"Checking vehicle registration already exists for VIN {vehicleRegistration.Vin}.");
                if (CheckIfVinAlreadyExists(transactionExecutor, vehicleRegistration.Vin))
                {
                    context.Logger.Log($"Vehicle registration does exist for VIN {vehicleRegistration.Vin}, returning not modified.");
                    response.StatusCode = (int)HttpStatusCode.NotModified;
                    transactionExecutor.Abort();
                }

                context.Logger.Log($"Inserting vehicle registration for VIN {vehicleRegistration.Vin}.");
                vehicleRegistration.Owners.PrimaryOwner.PersonId = primaryOwnerPersonDocumentId;
                IIonValue ionVehicleRegistration = ConvertObjectToIonValue(vehicleRegistration);

                transactionExecutor.Execute($"INSERT INTO VehicleRegistration ?", ionVehicleRegistration);
            });

            context.Logger.Log($"Inserted vehicle registration for VIN {vehicleRegistration.Vin}, returning OK.");
            response.StatusCode = (int)HttpStatusCode.OK;

            return  response;
        }

        private bool CheckIfVinAlreadyExists(TransactionExecutor transactionExecutor, string vin)
        {
            IIonValue ionVin = this.valueFactory.NewString(vin);
            IResult selectResult = transactionExecutor.Execute("SELECT VIN FROM VehicleRegistration AS v WHERE v.VIN = ?", ionVin);

            return selectResult.Any(x => x.GetField("VIN").StringValue == vin);
        }
    }
}
