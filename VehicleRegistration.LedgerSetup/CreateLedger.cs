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
using System.Threading;
using System.Threading.Tasks;
using Amazon.QLDB;
using Amazon.QLDB.Model;

namespace VehicleRegistration.LedgerSetup
{
    /// <summary>
    /// <para>Create a ledger and wait for it to be active.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class CreateLedger
    {
        private const string LEDGER_NAME = "vehicle-registration";

        private IAmazonQLDB qldbClient;

        public CreateLedger()
        {
            qldbClient = new AmazonQLDBClient();
        }

        public async Task Run()
        {
            if (!await CheckIfLedgerExistsAsync()) 
            {
                Console.WriteLine("Ledger doesn't exist, creating.");
                await CreateLedgerAsync();
            }
            
            await CheckIfLedgerActiveAsync();
        }

        private async Task CreateLedgerAsync()
        {
            await qldbClient.CreateLedgerAsync(new CreateLedgerRequest 
            {
                DeletionProtection = true,
                Name = LEDGER_NAME,
                PermissionsMode = PermissionsMode.ALLOW_ALL
            });
        }

        private async Task CheckIfLedgerActiveAsync()
        {
            Console.WriteLine("Waiting for ledger to become active.");
            LedgerState ledgerState = LedgerState.CREATING;
            do 
            {
                DescribeLedgerResponse describeResponse = await qldbClient.DescribeLedgerAsync(new DescribeLedgerRequest
                {
                    Name = LEDGER_NAME
                });

                ledgerState = describeResponse.State;
                Thread.Sleep(2000);
            } 
            while (ledgerState != LedgerState.ACTIVE);
            Console.WriteLine("Ledger is active.");
        }

        private async Task<bool> CheckIfLedgerExistsAsync() 
        {
            Console.WriteLine("Checking if ledger exists.");
            ListLedgersResponse response = await qldbClient.ListLedgersAsync(new ListLedgersRequest());
            return response.Ledgers != null && response.Ledgers.Any(x => x.Name == LEDGER_NAME);
        }
    }
}
