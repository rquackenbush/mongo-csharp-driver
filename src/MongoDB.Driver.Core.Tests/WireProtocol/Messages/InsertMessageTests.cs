﻿/* Copyright 2013-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages
{
    [TestFixture]
    public class InsertMessageTests
    {
        private readonly string _collectionName = "collection";
        private readonly bool _continueOnError = true;
        private readonly string _databaseName = "database";
        private readonly Batch<BsonDocument> _documents = new FirstBatch<BsonDocument>(Enumerable.Empty<BsonDocument>().GetEnumerator());
        private readonly int _maxBatchCount = 1;
        private readonly int _maxMessageSize = 2;
        private readonly int _requestId = 3;
        private readonly IBsonSerializer<BsonDocument> _serializer = BsonDocumentSerializer.Instance;

        [Test]
        public void Constructor_should_initialize_instance()
        {
            var subject = new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, _serializer, _documents, _maxBatchCount, _maxMessageSize, _continueOnError);
            subject.CollectionName.Should().Be(_collectionName);
            subject.ContinueOnError.Should().Be(_continueOnError);
            subject.DatabaseName.Should().Be(_databaseName);
            subject.Documents.Should().BeSameAs(_documents);
            subject.MaxBatchCount.Should().Be(_maxBatchCount);
            subject.MaxMessageSize.Should().Be(_maxMessageSize);
            subject.RequestId.Should().Be(_requestId);
            subject.Serializer.Should().BeSameAs(_serializer);
        }

        [Test]
        public void Constructor_with_negative_maxBatchCount_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, _serializer, _documents, -1, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_with_negative_maxMessageSize_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, _serializer, _documents, _maxBatchCount, -1, _continueOnError);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Constructor_with_null_collectionName_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _databaseName, null, _serializer, _documents, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_databaseName_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, null, _collectionName, _serializer, _documents, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_documents_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, _serializer, null, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_serializer_should_throw()
        {
            Action action = () => new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, null, _documents, _maxBatchCount, _maxMessageSize, _continueOnError);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetEncoder_should_return_encoder()
        {
            var mockEncoder = Substitute.For<IMessageEncoder<InsertMessage<BsonDocument>>>();
            var mockEncoderFactory = Substitute.For<IMessageEncoderFactory>();
            mockEncoderFactory.GetInsertMessageEncoder(_serializer).Returns(mockEncoder);

            var subject = new InsertMessage<BsonDocument>(_requestId, _databaseName, _collectionName, _serializer, _documents, _maxBatchCount, _maxMessageSize, _continueOnError);
            var encoder = subject.GetEncoder(mockEncoderFactory);
            encoder.Should().BeSameAs(mockEncoder);
        }
    }
}
