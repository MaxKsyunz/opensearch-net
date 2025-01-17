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
using Osc;
using Tests.Core.ManagedOpenSearch.Clusters;
using Tests.Domain;
using Tests.Framework.EndpointTests.TestState;

#pragma warning disable 618 //Testing an obsolete method

namespace Tests.QueryDsl.Specialized.Pinned
{
	/**
	 * Promotes selected documents to rank higher than those matching a given query. This feature is typically used to
	 * guide searchers to curated documents that are promoted over and above any "organic" matches for a search. The promoted or "pinned"
	 * documents are identified using the document IDs stored in the _id field.
	 * See the OpenSearch documentation on {ref_current}/query-dsl-pinned-query.html[pinned query] for more details.
	*/
	public class PinnedQueryUsageTests : QueryDslUsageTestsBase
	{
		public PinnedQueryUsageTests(ReadOnlyCluster i, EndpointUsage usage) : base(i, usage) { }

		protected override ConditionlessWhen ConditionlessWhen => new ConditionlessWhen<IPinnedQuery>(a => a.Pinned)
		{
			q =>
			{
				q.Ids = null;
				q.Organic = null;
			},
			q =>
			{
				q.Ids = Array.Empty<Id>();
				q.Organic = ConditionlessQuery;
			},
		};

		protected override NotConditionlessWhen NotConditionlessWhen => new NotConditionlessWhen<IPinnedQuery>(a => a.Pinned)
		{
			q => q.Organic = VerbatimQuery,
		};

		protected override QueryContainer QueryInitializer => new PinnedQuery()
		{
			Name = "named_query",
			Boost = 1.1,
			Organic = new MatchAllQuery { Name = "organic_query" },
			Ids = new Id[] { 1,11,22 },
		};

		protected override object QueryJson => new
		{
			pinned = new
			{
				_name = "named_query",
				boost = 1.1,
				organic = new
				{
					match_all = new { _name = "organic_query" }
				},
				ids = new [] { 1, 11, 22},
			}
		};

		protected override QueryContainer QueryFluent(QueryContainerDescriptor<Project> q) => q
			.Pinned(c => c
				.Name("named_query")
				.Boost(1.1)
				.Organic(qq => qq.MatchAll(m => m.Name("organic_query")))
				.Ids(1, 11, 22)
			);
	}
}
