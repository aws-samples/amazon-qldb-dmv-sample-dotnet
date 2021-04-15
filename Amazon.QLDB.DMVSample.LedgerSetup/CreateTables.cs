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
using System.Linq;
using System.Threading.Tasks;
using Amazon.QLDB.Driver;

namespace Amazon.QLDB.DMVSample.LedgerSetup
{
    /// <summary>
    /// <para>Create tables in a QLDB ledger.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class CreateTables
    {
        private IAsyncQldbDriver qldbDriver;

        public CreateTables(IAsyncQldbDriver qldbDriver)
        {
            this.qldbDriver = qldbDriver;
        }

        public async Task Run()
        {
            await Task.Run(async () =>
            {
                await CreateTableAsync("VehicleRegistration");
                await CreateTableAsync("Vehicle");
                await CreateTableAsync("Person");
                await CreateTableAsync("DriversLicense");
            });
        }

        private async Task CreateTableAsync(string tableName)
        {
            if (!await CheckTableExistsAsync(tableName))
            {
                Console.WriteLine($"Table does not exist, creating table with name {tableName}.");
                await qldbDriver.Execute(async transactionExecutor => await transactionExecutor.Execute($"CREATE TABLE {tableName}"));
            }
            else 
            {
                Console.WriteLine($"Table {tableName} already exists, ignoring.");
            }        
        }

        private async Task<bool> CheckTableExistsAsync(string tableName)
        {
            return await Task.Run(async () =>
            {
                return (await qldbDriver.ListTableNames()).Any(x => x == tableName);
            });
        }
    }
}
