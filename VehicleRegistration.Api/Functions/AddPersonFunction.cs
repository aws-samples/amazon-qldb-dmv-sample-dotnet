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
using Amazon.QLDB.Driver;
using Newtonsoft.Json;
using VehicleRegistration.Model;
using VehicleRegistration.Api.Services;

namespace VehicleRegistration.Api.Functions
{
    /// <summary>
    /// <para>Add a new person if their GovId does not already exist.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class AddPersonFunction
    {
        private readonly IQldbDriver qldbDriver;
        private readonly IMetadataService tableMetadataService;
        private readonly IValueFactory valueFactory;

        public AddPersonFunction()
        {
            this.qldbDriver = new QldbService().GetDriver();
            this.tableMetadataService = new MetadataService();
            this.valueFactory = new ValueFactory();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Person person = JsonConvert.DeserializeObject<Person>(request.Body, new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" });
            APIGatewayProxyResponse response = new APIGatewayProxyResponse();

            try
            {
                this.qldbDriver.Execute(transactionExecutor =>
                {
                    context.Logger.Log($"Checking person already exists for GovId {person.GovId}.");
                    if (CheckIfPersonAlreadyExists(transactionExecutor, person.GovId))
                    {
                        context.Logger.Log($"Person does exist for GovId {person.GovId}, aborting transaction and returning not modified.");
                        response.StatusCode = (int)HttpStatusCode.NotModified;

                        transactionExecutor.Abort();
                    }

                    context.Logger.Log($"Inserting person for GovId {person.GovId}.");

                    IIonValue ionPerson = ConvertRequestToIonValue(person);
                    transactionExecutor.Execute($"INSERT INTO Person ?", ionPerson);

                    context.Logger.Log($"Inserted person for GovId {person.GovId}, returning OK.");
                    response.StatusCode = (int)HttpStatusCode.OK;
                });
            }
            catch (TransactionAbortedException e)
            {
                context.Logger.Log($"Transaction aborted.");
                context.Logger.Log(e.ToString());
            }

            return response;
        }

        private bool CheckIfPersonAlreadyExists(TransactionExecutor transactionExecutor, string govId)
        {
            IIonValue ionGovId = this.valueFactory.NewString(govId);
            IEnumerable<IIonValue> selectResult = transactionExecutor.Execute("SELECT GovId FROM Person AS p WHERE p.GovId = ?", ionGovId);

            return selectResult.Any(x => x.GetField("GovId").StringValue == govId);
        }

        private IIonValue ConvertRequestToIonValue(Person person)
        {
            return IonLoader.Default.Load(JsonConvert.SerializeObject(person));
        }
    }
}
