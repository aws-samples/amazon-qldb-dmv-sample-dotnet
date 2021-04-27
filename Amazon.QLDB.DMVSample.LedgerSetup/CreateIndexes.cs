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
using System.Threading.Tasks;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.QLDB.Driver;

using static Amazon.QLDB.DMVSample.Model.Constants;

namespace Amazon.QLDB.DMVSample.LedgerSetup
{
    /// <summary>
    /// <para>Create indexes on tables in a particular ledger.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class CreateIndexes
    {
        private IAsyncQldbDriver qldbDriver;
        private readonly IValueFactory valueFactory;

        public CreateIndexes(IAsyncQldbDriver qldbDriver)
        {
            this.qldbDriver = qldbDriver;
            this.valueFactory = new ValueFactory();
        }

        public async Task Run()
        {
            await Task.Run(async () =>
            {
                await CreateIndexAsync(VehicleRegistrationTableName, "VIN");
                await CreateIndexAsync(VehicleRegistrationTableName, "LicensePlateNumber");
                await CreateIndexAsync(VehicleTableName, "VIN");
                await CreateIndexAsync(PersonTableName, "GovId");
                await CreateIndexAsync(DriversLicenseTableName, "LicenseNumber");
                await CreateIndexAsync(DriversLicenseTableName, "PersonId");
            });
        }

        private async Task CreateIndexAsync(string tableName, string field) 
        {
            if (!await CheckIndexExistsAsync(tableName, field))
            {
                Console.WriteLine($"Index does not exist, creating index on {tableName} for {field}.");
                await qldbDriver.Execute(async transactionExecutor => await transactionExecutor.Execute($"CREATE INDEX ON {tableName}({field})"));
            }
            else
            {
                Console.WriteLine($"Index already exists for {tableName} and {field}.");
            }   
        }

        private async Task<bool> CheckIndexExistsAsync(string tableName, string field)
        {
            return await Task.Run(async () =>
            {
                Amazon.QLDB.Driver.IAsyncResult result = await qldbDriver.Execute(async transactionExecutor =>
                {
                    IIonValue ionTableName = this.valueFactory.NewString(tableName);
                    return await transactionExecutor.Execute($"SELECT * FROM information_schema.user_tables WHERE name = ?", ionTableName);
                });

                List<IIonValue> ionValues = await result.ToListAsync();
                if (ionValues.Any())
                {
                    IIonList indexes = ionValues.First().GetField("indexes");
                    foreach (IIonValue index in indexes)
                    {
                        string expr = index.GetField("expr").StringValue;
                        if (expr.Contains(field))
                        {
                            return true;
                        }
                    }    
                }

                return false;
            });
        }
    }
}
