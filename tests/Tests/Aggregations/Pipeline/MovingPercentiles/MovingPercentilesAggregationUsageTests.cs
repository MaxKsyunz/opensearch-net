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
using System.Linq;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using FluentAssertions;
using Osc;
using Tests.Core.Extensions;
using Tests.Core.ManagedOpenSearch.Clusters;
using Tests.Domain;
using Tests.Framework.EndpointTests.TestState;

namespace Tests.Aggregations.Pipeline.MovingPercentiles
{
	/**
	 * Given an ordered series of percentiles, the Moving Percentile aggregation will slide a window across those
	 * percentiles and allow the user to compute the cumulative percentile.
     *
     * This is conceptually very similar to the Moving Function pipeline aggregation, except it works on the percentiles sketches instead of the actual buckets values.
	 */
	public class MovingPercentilesAggregationUsageTests : AggregationUsageTestBase<ReadOnlyCluster>
	{
		public MovingPercentilesAggregationUsageTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object AggregationJson => new
		{
			projects_started_per_month = new
			{
				date_histogram = new
				{
					field = "startedOn",
					calendar_interval = "month",
					min_doc_count = 0
				},
				aggs = new
				{
					percentiles = new
					{
						percentiles = new
						{
							field = "numberOfCommits"
						}
					},
					moving_percentiles = new
					{
						moving_percentiles = new
						{
							buckets_path = "percentiles",
							window = 10,
						}
					}
				}
			}
		};

		protected override Func<AggregationContainerDescriptor<Project>, IAggregationContainer> FluentAggs => a => a
			.DateHistogram("projects_started_per_month", dh => dh
				.Field(p => p.StartedOn)
				.CalendarInterval(DateInterval.Month)
				.MinimumDocumentCount(0)
				.Aggregations(aa => aa
					.Percentiles("percentiles", sm => sm
						.Field(p => p.NumberOfCommits)
					)
					.MovingPercentiles("moving_percentiles", mv => mv
						.BucketsPath("percentiles")
						.Window(10)
					)
				)
			);

		protected override AggregationDictionary InitializerAggs =>
			new DateHistogramAggregation("projects_started_per_month")
			{
				Field = "startedOn",
				CalendarInterval = DateInterval.Month,
				MinimumDocumentCount = 0,
				Aggregations =
					new PercentilesAggregation("percentiles", "numberOfCommits")
					&& new MovingPercentilesAggregation("moving_percentiles", "percentiles")
					{
						Window = 10
					}
			};

		protected override void ExpectResponse(ISearchResponse<Project> response)
		{
			response.ShouldBeValid();

			var projectsPerMonth = response.Aggregations.DateHistogram("projects_started_per_month");
			projectsPerMonth.Should().NotBeNull();
			projectsPerMonth.Buckets.Should().NotBeNull();
			projectsPerMonth.Buckets.Count.Should().BeGreaterThan(0);

			// percentiles not calculated for the first bucket
			foreach (var item in projectsPerMonth.Buckets.Skip(1))
			{
				var movingPercentiles = item.MovingPercentiles("moving_percentiles");
				movingPercentiles.Should().NotBeNull();
				movingPercentiles.Items.Should().NotBeNull();
			}
		}
	}
}
