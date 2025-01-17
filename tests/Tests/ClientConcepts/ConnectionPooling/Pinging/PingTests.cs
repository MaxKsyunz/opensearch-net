/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*
* Modifications Copyright OpenSearch Contributors. See
* GitHub history for details.
*
*  Licensed to Elasticsearch B.V. under one or more contributor
*  license agreements. See the NOTICE file distributed with
*  this work for additional information regarding copyright
*  ownership. Elasticsearch B.V. licenses this file to you under
*  the Apache License, Version 2.0 (the "License"); you may
*  not use this file except in compliance with the License.
*  You may obtain a copy of the License at
*
* 	http://www.apache.org/licenses/LICENSE-2.0
*
*  Unless required by applicable law or agreed to in writing,
*  software distributed under the License is distributed on an
*  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
*  KIND, either express or implied.  See the License for the
*  specific language governing permissions and limitations
*  under the License.
*/

using System;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using OpenSearch.Net;
using FluentAssertions;
using Osc;
using Tests.ClientConcepts.Connection;
using Tests.Core.ManagedOpenSearch.Clusters;


namespace Tests.ClientConcepts.ConnectionPooling.Pinging
{
	public class PingTests : IClusterFixture<ReadOnlyCluster>
	{
		private readonly ReadOnlyCluster _cluster;

		public PingTests(ReadOnlyCluster cluster) => _cluster = cluster;

#if DOTNETCORE
		[I]
		public void UsesRelativePathForPing()
		{
			var pool = new StaticConnectionPool(new[] { new Uri("http://localhost:9200/opensearch/") });
			var settings = new ConnectionSettings(pool,
				new HttpConnectionTests.TestableHttpConnection(response =>
				{
					response.RequestMessage.RequestUri.AbsolutePath.Should().StartWith("/opensearch/");
				}));

			var client = new OpenSearchClient(settings);
			var healthResponse = client.Ping();
		}
#else
		[I]
		public void UsesRelativePathForPing()
		{
			var pool = new StaticConnectionPool(new[] { new Uri("http://localhost:9200/opensearch/") });
			var connection = new HttpWebRequestConnectionTests.TestableHttpWebRequestConnection();
			var settings = new ConnectionSettings(pool, connection);

			var client = new OpenSearchClient(settings);
			var healthResponse = client.Ping();

			connection.LastRequest.Address.AbsolutePath.Should().StartWith("/opensearch/");
		}
#endif
	}
}

