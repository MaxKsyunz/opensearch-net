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
using System.Collections.Generic;
using System.Linq;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using FluentAssertions;
using Osc;
using Tests.Core.Extensions;
using Tests.Core.ManagedOpenSearch.Clusters;
using Tests.Domain;
using Tests.Framework.EndpointTests.TestState;
using static Osc.Infer;

namespace Tests.Aggregations.Metric.TopMetrics
{
	/**
	 * The top metrics aggregation selects metrics from the document with the largest or smallest "sort" value.
	 *
	 * Top metrics is fairly similar to "top hits" in spirit but because it is more limited it is able to do its job using less memory and is often faster.
	 *
	 * Be sure to read the OpenSearch documentation on {ref_current}/search-aggregations-metrics-top-metrics.html[Top Metrics Aggregation]
	 */
	public class TopMetricsAggregationUsageTests : AggregationUsageTestBase<ReadOnlyCluster>
	{
		public TopMetricsAggregationUsageTests(ReadOnlyCluster i, EndpointUsage usage) : base(i, usage) { }

		protected override object AggregationJson => new
		{
			tm = new
			{
				top_metrics = new
				{
					metrics = new []
					{
						new
						{
							field = "numberOfContributors"
						}
					},
					size = 10,
					sort = new[] { new { numberOfContributors = new { order = "asc" } } }
				}
			}
		};

		protected override Func<AggregationContainerDescriptor<Project>, IAggregationContainer> FluentAggs => a => a
			.TopMetrics("tm", st => st
				.Metrics(m => m.Field(p => p.NumberOfContributors))
				.Size(10)
				.Sort(sort => sort
					.Ascending("numberOfContributors")
				)
			);

		protected override AggregationDictionary InitializerAggs =>
			new TopMetricsAggregation("tm")
			{
				Metrics = new List<ITopMetricsValue>
				{
					new TopMetricsValue(Field<Project>(p => p.NumberOfContributors))
				},
				Size = 10,
				Sort = new List<ISort> { new FieldSort { Field = "numberOfContributors", Order = SortOrder.Ascending } }
			};

		protected override void ExpectResponse(ISearchResponse<Project> response)
		{
			response.ShouldBeValid();
			var topMetrics = response.Aggregations.TopMetrics("tm");
			topMetrics.Should().NotBeNull();
			topMetrics.Top.Should().NotBeNull();
			topMetrics.Top.Count.Should().BeGreaterThan(0);

			var tipTop = topMetrics.Top.First();
			tipTop.Sort.Should().Should().NotBeNull();
			tipTop.Sort.Count.Should().BeGreaterThan(0);
			tipTop.Metrics.Should().NotBeNull();
			tipTop.Metrics.Count.Should().BeGreaterThan(0);
		}
	}
}
