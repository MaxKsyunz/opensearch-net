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
using FluentAssertions;
using Osc;
using Tests.Core.Extensions;
using Tests.Core.ManagedOpenSearch.Clusters;
using Tests.Domain;
using Tests.Framework.EndpointTests.TestState;
using static Osc.Infer;

namespace Tests.Aggregations.Metric.TTest
{
	/**
	 * A t_test metrics aggregation that performs a statistical hypothesis test in which the test statistic follows a
	 * Student’s t-distribution under the null hypothesis on numeric values extracted from the aggregated documents or
	 * generated by provided scripts. In practice, this will tell you if the difference between two population means
	 * are statistically significant and did not occur by chance alone.
	 *
	 * Be sure to read the OpenSearch documentation on {ref_current}/search-aggregations-metrics-ttest-aggregation.html[T-Test Aggregation].
	 */
	public class TTestAggregationUsageTests : AggregationUsageTestBase<ReadOnlyCluster>
	{
		public TTestAggregationUsageTests(ReadOnlyCluster i, EndpointUsage usage) : base(i, usage) { }

		protected override object AggregationJson => new
		{
			commits_visibility = new
			{
				t_test = new
				{
					a = new
					{
						field = "numberOfCommits",
						filter = new
						{
							term = new
							{
								visibility = new
								{
									value = "Public"
								}
							}
						}
					},
					b = new
					{
						field = "numberOfCommits",
						filter = new
						{
							term = new
							{
								visibility = new
								{
									value = "Private"
								}
							}
						}
					},
					type = "heteroscedastic"
				}
			}
		};

		protected override Func<AggregationContainerDescriptor<Project>, IAggregationContainer> FluentAggs => a => a
			.TTest("commits_visibility", c => c
				.A(t => t
					.Field(f => f.NumberOfCommits)
					.Filter(f => f
						.Term(ff => ff.Visibility, Visibility.Public)
					)
				)
				.B(t => t
					.Field(f => f.NumberOfCommits)
					.Filter(f => f
						.Term(ff => ff.Visibility, Visibility.Private)
					)
				)
				.Type(TTestType.Heteroscedastic)
			);

		protected override AggregationDictionary InitializerAggs =>
			new TTestAggregation("commits_visibility")
			{
				A = new TTestPopulation
				{
					Field = Field<Project>(f => f.NumberOfCommits),
					Filter = new TermQuery
					{
						Field = Field<Project>(f => f.Visibility),
						Value = Visibility.Public
					}
				},
				B = new TTestPopulation
				{
					Field = Field<Project>(f => f.NumberOfCommits),
					Filter = new TermQuery
					{
						Field = Field<Project>(f => f.Visibility),
						Value = Visibility.Private
					}
				},
				Type = TTestType.Heteroscedastic
			};

		protected override void ExpectResponse(ISearchResponse<Project> response)
		{
			response.ShouldBeValid();
			var tTest = response.Aggregations.TTest("commits_visibility");
			tTest.Should().NotBeNull();
			tTest.Value.Should().BeGreaterThan(0);
		}
	}
}
