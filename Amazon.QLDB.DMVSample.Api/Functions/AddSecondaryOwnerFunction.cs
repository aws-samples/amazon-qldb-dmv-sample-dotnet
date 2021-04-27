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
using Amazon.IonDotnet.Builders;
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
    /// <para>Finds and adds secondary owners for a vehicle.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class AddSecondaryOwnerFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;

        public AddSecondaryOwnerFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            VehicleRegistration vehicleRegistration = JsonConvert.DeserializeObject<VehicleRegistration>(request.Body);

            return this.qldbDriver.Execute(transactionExecutor =>
            {
                context.Logger.Log($"Checking vehicle registration already exists for VIN {vehicleRegistration.Vin}.");
                if (!CheckIfVinAlreadyExists(transactionExecutor, vehicleRegistration.Vin))
                {
                    context.Logger.Log($"Vehicle registration does not exist for VIN {vehicleRegistration.Vin}, returning not found.");
                    return new APIGatewayProxyResponse 
                    { 
                        StatusCode = (int)HttpStatusCode.NotFound
                    };
                }

                context.Logger.Log($"Vehicle registration already exists, checking what's changed between current value and request.");

                IEnumerable<string> existingSecondaryOwners = GetSecondaryOwners(transactionExecutor, vehicleRegistration.Vin);
                IEnumerable<string> newSecondaryOwnersGovIds = vehicleRegistration.Owners.SecondaryOwners
                                                                .Select(x => x.PersonId)
                                                                .Except(existingSecondaryOwners);
                if (!newSecondaryOwnersGovIds.Any())
                {
                    context.Logger.Log($"Nothing changed, returning not modified.");
                    return new APIGatewayProxyResponse 
                    { 
                        StatusCode = (int)HttpStatusCode.NotModified
                    };
                }

                string secondaryOwnerPersonDocumentId = this.tableMetadataService.GetDocumentId(transactionExecutor, PersonTableName, "GovId", newSecondaryOwnersGovIds.First());
                if (!CheckIfSecondaryOwnerExists(existingSecondaryOwners, secondaryOwnerPersonDocumentId))
                {
                    context.Logger.Log($"Secondary owner does not exists with document ID {secondaryOwnerPersonDocumentId}, adding secondary owner.");
                    IIonValue ionVin = this.valueFactory.NewString(vehicleRegistration.Vin);
                    IIonValue ionSecondaryOwner = ConvertOwnerToIonValue(new Owner { PersonId = secondaryOwnerPersonDocumentId });

                    IResult result = transactionExecutor.Execute($"FROM VehicleRegistration AS v WHERE v.VIN = ? " +
                        "INSERT INTO v.Owners.SecondaryOwners VALUE ?", ionVin, ionSecondaryOwner);
                    
                    context.Logger.Log($"Secondary owner added with document ID {secondaryOwnerPersonDocumentId}, returning OK.");
                    return new APIGatewayProxyResponse 
                    { 
                        StatusCode = (int)HttpStatusCode.OK
                    };
                } 
                else
                {
                    context.Logger.Log($"Secondary owner already exists, returning not modified.");
                    return new APIGatewayProxyResponse 
                    { 
                        StatusCode = (int)HttpStatusCode.NotModified
                    };
                }
            });
        }

        private bool CheckIfSecondaryOwnerExists(IEnumerable<string> secondaryOwners, string secondaryOwnerPersonDocumentId)
        {
            return secondaryOwners.Any(x => x == secondaryOwnerPersonDocumentId);
        }

        private bool CheckIfVinAlreadyExists(TransactionExecutor transactionExecutor, string vin)
        {
            IIonValue ionVin = this.valueFactory.NewString(vin);
            IResult selectResult = transactionExecutor.Execute("SELECT VIN FROM VehicleRegistration AS v WHERE v.VIN = ?", ionVin);

            return selectResult.Any(x => x.GetField("VIN").StringValue == vin);
        }

        private IEnumerable<string> GetSecondaryOwners(TransactionExecutor transactionExecutor, string vin)
        {
            IIonValue ionVin = this.valueFactory.NewString(vin);
            IResult selectResult = transactionExecutor.Execute("SELECT Owners.SecondaryOwners FROM VehicleRegistration AS v WHERE v.VIN = ?", ionVin);

            IIonList secondaryOwners = selectResult.First().GetField("SecondaryOwners") as IIonList;
            if (secondaryOwners != null)
            {   
                List<string> secondaryOwnerIds = new List<string>();
                foreach(var owner in secondaryOwners)
                {
                    secondaryOwnerIds.Add(owner?.GetField("PersonId")?.StringValue);
                }
                return secondaryOwnerIds;
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

        private IIonValue ConvertOwnerToIonValue(Owner secondaryOwner)
        {
            return IonLoader.Default.Load(JsonConvert.SerializeObject(secondaryOwner));
        }
    }
}
