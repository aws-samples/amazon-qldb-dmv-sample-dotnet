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
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tree;
using Amazon.QLDB.DMVSample.Model;
using Amazon.QLDB.Driver;
using Newtonsoft.Json;

namespace Amazon.QLDB.DMVSample.LedgerSetup
{
    /// <summary>
    /// <para>Sample domain objects for use throughout this tutorial.</para>
    /// </summary>
    public class SampleData
    {
        private IAsyncQldbDriver qldbDriver;
        
        public List<Person> people = new List<Person>
        {
            new Person { FirstName = "Raul", LastName = "Lewis", DateOfBirth = DateTime.Parse("1963-08-19"), 
                GovId = "LEWISR261LL", GovIdType = "Driver License", Address = "1719 University Street, Seattle, WA, 98109" },
            new Person { FirstName = "Brent", LastName = "Logan", DateOfBirth = DateTime.Parse("1967-07-03"), 
                GovId = "LOGANB486CG", GovIdType = "Driver License", Address = "43 Stockert Hollow Road, Everett, WA, 98203" },
            new Person { FirstName = "Alexis", LastName = "Pena", DateOfBirth = DateTime.Parse("1974-02-10"), 
                GovId = "744 849 301", GovIdType = "SSN", Address = "4058 Melrose Street, Spokane Valley, WA, 99206" },
            new Person { FirstName = "Melvin", LastName = "Parker", DateOfBirth = DateTime.Parse("1976-05-22"), 
                GovId = "P626-168-229-765", GovIdType = "Passport", Address = "4362 Ryder Avenue, Seattle, WA, 98101" },
            new Person { FirstName = "Salvatore", LastName = "Specer", DateOfBirth = DateTime.Parse("1997-11-15"), 
                GovId = "S152-780-97-415-0", GovIdType = "Passport", Address = "4450 Honeysuckle Lane, Seattle, WA, 98101" }
        };

        public SampleData(IAsyncQldbDriver qldbDriver)
        {
            this.qldbDriver = qldbDriver;
        }

        public async Task Run()
        {
            await Task.Run(async () =>
            {
                await InsertVehicles();

                Amazon.QLDB.Driver.IAsyncResult insertPeopleResult = await InsertPeople();
                List<string> peopleDocumentIds = await insertPeopleResult.Select(x => x.GetField("documentId").StringValue).ToListAsync();

                InsertDriversLicenses(peopleDocumentIds);
                InsertVehicleRegistrations(peopleDocumentIds);
            });
        }

        private async Task InsertVehicles() 
        {
            await qldbDriver.Execute(async transactionExecutor =>
            {
                await Insert(transactionExecutor, "Vehicle", new Vehicle { Vin = "1N4AL11D75C109151", Type = "Sedan", Year= 2011, 
                    Make = "Audi", Model = "A5", Color= "Silver" });
                await Insert(transactionExecutor, "Vehicle", new Vehicle { Vin = "KM8SRDHF6EU074761", Type = "Sedan", Year= 2015, 
                    Make = "Tesla", Model = "Model S", Color= "Blue" });
                await Insert(transactionExecutor, "Vehicle", new Vehicle { Vin = "3HGGK5G53FM761765", Type = "Motorcycle", Year= 2011, 
                    Make = "Ducati", Model = "Monster", Color= "Yellowr" });
                await Insert(transactionExecutor, "Vehicle", new Vehicle { Vin = "1HVBBAANXWH544237", Type = "Semi", Year= 2009, 
                    Make = "Ford", Model = "F 150", Color= "Black" });
                await Insert(transactionExecutor, "Vehicle", new Vehicle { Vin = "1C4RJFAG0FC625797", Type = "Sedan", Year= 2019, 
                    Make = "Mercedes", Model = "CLK 350", Color= "White" });
            }); 
        }

        private async Task<Amazon.QLDB.Driver.IAsyncResult> InsertPeople() 
        {
            return await qldbDriver.Execute(async transactionExecutor => await Insert(transactionExecutor, "Person", people)); 
        }

        private void InsertDriversLicenses(List<string> peopleDocumentIds)
        {
            List<DriversLicense> licenses = new List<DriversLicense>
            {
                new DriversLicense { LicenseNumber = "LEWISR261LL", LicenseType = "Learner", 
                    ValidFromDate = DateTime.Parse("2016-12-20"), ValidToDate = DateTime.Parse("2020-11-15") },
                new DriversLicense { LicenseNumber = "LOGANB486CG", LicenseType = "Probationary", 
                    ValidFromDate = DateTime.Parse("2016-04-06"), ValidToDate = DateTime.Parse("2020-11-15") },
                new DriversLicense { LicenseNumber = "744 849 301", LicenseType = "Full", 
                    ValidFromDate = DateTime.Parse("2017-12-06"), ValidToDate = DateTime.Parse("2022-10-15") },
                new DriversLicense { LicenseNumber = "P626-168-229-765", LicenseType = "Learner", 
                    ValidFromDate = DateTime.Parse("2017-08-16"), ValidToDate = DateTime.Parse("2021-11-15") },
                new DriversLicense { LicenseNumber = "S152-780-97-415-0", LicenseType = "Probationary", 
                    ValidFromDate = DateTime.Parse("2015-08-15"), ValidToDate = DateTime.Parse("2021-08-21") }
            };

            for (int i = 0; i < peopleDocumentIds.Count; i++)
            {
                licenses[i].PersonId = peopleDocumentIds[i];
            }

            qldbDriver.Execute(async transactionExecutor =>
            {
                await Insert(transactionExecutor, "DriversLicense", licenses);
            }); 
        }

        private void InsertVehicleRegistrations(List<string> peopleDocumentIds)
        {
            List<Model.VehicleRegistration> registrations = new List<Model.VehicleRegistration>
            {
                new Model.VehicleRegistration { Vin = "1N4AL11D75C109151", LicensePlateNumber = "LEWISR261LL", 
                    State = "WA", City = "Seattle",  PendingPenaltyTicketAmount = 90.25, ValidFromDate = DateTime.Parse("2017-08-21"), 
                    ValidToDate = DateTime.Parse("2020-05-11"), Owners = new Owners {}},
                new Model.VehicleRegistration { Vin = "KM8SRDHF6EU074761", LicensePlateNumber = "CA762X", 
                    State = "WA", City = "Kent", PendingPenaltyTicketAmount = 130.75, ValidFromDate = DateTime.Parse("2017-09-14"), 
                    ValidToDate = DateTime.Parse("2020-06-25"), Owners = new Owners {} },
                new Model.VehicleRegistration { Vin = "3HGGK5G53FM761765", LicensePlateNumber = "CD820Z", 
                    State = "WA", City = "Everett", PendingPenaltyTicketAmount = 442.30, ValidFromDate = DateTime.Parse("2011-03-17"), 
                    ValidToDate = DateTime.Parse("2021-03-24"), Owners = new Owners {} },
                new Model.VehicleRegistration { Vin = "1HVBBAANXWH544237", LicensePlateNumber = "LS477D", 
                    State = "WA", City = "Tacoma", PendingPenaltyTicketAmount = 42.20, ValidFromDate = DateTime.Parse("2011-10-26"), 
                    ValidToDate = DateTime.Parse("2023-09-25"), Owners = new Owners {} },
                new Model.VehicleRegistration { Vin = "1C4RJFAG0FC625797", LicensePlateNumber = "TH393F", 
                    State = "WA", City = "Olympia", PendingPenaltyTicketAmount = 30.45, ValidFromDate = DateTime.Parse("2013-09-02"), 
                    ValidToDate = DateTime.Parse("2023-09-25"), Owners = new Owners {} }
            };

            for (int i = 0; i < peopleDocumentIds.Count; i++)
            {
                registrations[i].Owners.PrimaryOwner = new Owner { PersonId = peopleDocumentIds[i] };
            }

            qldbDriver.Execute(transactionExecutor => Insert(transactionExecutor, "VehicleRegistration", registrations)); 
        }

        private async Task<Amazon.QLDB.Driver.IAsyncResult> Insert<T>(AsyncTransactionExecutor transactionExecutor, string tableName, T value)
        {
            IIonValue ionValue = IonLoader.Default.Load(JsonConvert.SerializeObject(value));
            return await transactionExecutor.Execute($"INSERT INTO {tableName} ?", ionValue);
        }
    }
}
