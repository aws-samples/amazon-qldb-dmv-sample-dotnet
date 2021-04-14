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
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.QLDB.Driver;

namespace VehicleRegistration.Api.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IValueFactory valueFactory;

        public MetadataService()
        {
            this.valueFactory = new ValueFactory();
        }

        public string GetDocumentId(TransactionExecutor transactionExecutor, string tableName, string identifer, string value)
        {
            IIonValue ionValue = this.valueFactory.NewString(value);
            IResult selectResult = 
                transactionExecutor.Execute($"SELECT metadata.id FROM _ql_committed_{tableName} AS p WHERE p.data.{identifer} = ?", ionValue);

            IEnumerable<string> documentIds = selectResult.Select(x => x.GetField("id").StringValue).ToList();
            return documentIds.Any() ? documentIds.First() : string.Empty;
        }
    }
}
