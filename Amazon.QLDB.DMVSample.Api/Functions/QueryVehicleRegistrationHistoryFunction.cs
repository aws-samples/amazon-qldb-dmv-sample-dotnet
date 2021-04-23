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

using System;
using System.Collections.Generic;
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

using static Amazon.QLDB.DMVSample.Model.Constants;

namespace Amazon.QLDB.DMVSample.Api.Functions
{
    /// <summary>
    /// <para>Query the 'VehicleRegistration' history table to find all previous primary ownders for a VIN.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class QueryVehicleRegistrationHistoryFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;
        
        public QueryVehicleRegistrationHistoryFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string vin = request.QueryStringParameters["VIN"];
            IEnumerable<IIonValue> selectResult = this.qldbDriver.Execute(transactionExecutor =>
            {
                string vehicleRegistrationDocumentId = this.tableMetadataService.GetDocumentId(transactionExecutor, VehicleRegistrationTableName, "VIN", vin);
                context.Logger.Log($"Getting history for vehicle registration where document ID is {vehicleRegistrationDocumentId}.");

                IIonValue ionVehicleRegistrationDocumentId = this.valueFactory.NewString(vehicleRegistrationDocumentId);
                IResult result = transactionExecutor.Execute("SELECT data, data.Owners.PrimaryOwner AS PrimaryOwner, data.Owners.SecondaryOwners AS SecondaryOwners, metadata.version "
                                           + $"FROM history(VehicleRegistration) "
                                           + "AS history WHERE history.metadata.id = ?", ionVehicleRegistrationDocumentId);
                return result;
            });

            IEnumerable<VehicleRegistrationHistory> vehicles = selectResult.Select(x => 
            {
                IIonValue ionData = x.GetField("data");
                IIonValue ionVersion = x.GetField("version");

                Owners owners = new Owners
                {
                    PrimaryOwner = new Owner { PersonId = x.GetField("PrimaryOwner").GetField("PersonId").StringValue }
                }; 

                var ionSecondaryOwners = x.GetField("SecondaryOwners") as IIonList;
                foreach (var secondaryOwner in ionSecondaryOwners)
                {
                    owners.SecondaryOwners.Add(new Owner { PersonId = secondaryOwner.GetField("PersonId").StringValue });
                }

                return new VehicleRegistrationHistory
                {
                    Version = ionVersion.IntValue,
                    VehicleRegistration = new VehicleRegistration
                    {
                        Vin = ionData.GetField("VIN").StringValue,
                        LicensePlateNumber = ionData.GetField("LicensePlateNumber").StringValue,
                        State = ionData.GetField("State").StringValue,
                        PendingPenaltyTicketAmount = Convert.ToDouble(ionData.GetField("PendingPenaltyTicketAmount").DecimalValue),
                        ValidFromDate = DateTime.Parse(ionData.GetField("ValidFromDate").StringValue),
                        ValidToDate = DateTime.Parse(ionData.GetField("ValidToDate").StringValue),
                        Owners = owners
                    }
                };
            });

            return new APIGatewayProxyResponse 
            { 
                StatusCode = vehicles.Any() ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound,
                Body = JsonConvert.SerializeObject(vehicles, Formatting.Indented)
            };
        }
    }
}
