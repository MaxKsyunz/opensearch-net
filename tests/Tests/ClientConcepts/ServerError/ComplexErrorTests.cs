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

using System.Collections.Generic;
using System.Linq;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using OpenSearch.Net;
using FluentAssertions;
using Tests.Core.Extensions;

namespace Tests.ClientConcepts.ServerError
{
	public class ComplexErrorTests : ServerErrorTestsBase
	{
		protected override string Json => @"{
	""root_cause"" : [
	{
		""type"" : ""parse_exception"",
		""reason"" : ""failed to parse date field [-1m] with format [strict_date_optional_time||epoch_millis]""
	}],
	""type"" : ""search_phase_execution_exception"",
	""reason"" : ""all shards failed"",
	""phase"" : ""query"",
	""grouped"" : true,
	""failed_shards"" : [
	{
		""shard"" : 0,
		""index"" : ""project"",
		""node"" : ""Uo6PBln_QrmD8Y9o1NKdQw"",
		""unknown_prop"" : ""x"",
		""reason"" : {
			""type"" : ""parse_exception"",
			""reason"" : ""failed to parse date field [-1m] with format [strict_date_optional_time||epoch_millis]"",
			""caused_by"" : {
				""type"" : ""illegal_argument_exception"",
				""reason"" : ""Parse failure at index [2] of [-1m]""
			}
		},
		""status"" : ""x""
	}
	],
	""headers"" : {
		""WWW-Authenticate"" : ""Bearer: ..."",
		""x"" : ""y""
	},
	""caused_by"" : {
		""type"" : ""parse_exception"",
		""reason"" : ""failed to parse date field [-1m] with format [strict_date_optional_time||epoch_millis]"",
		""index"" : null,
		""resource.id"" : [""alias1"", ""alias2""],
		""script_stack"" : [""alias1"", ""alias2""],
		""unknown_prop"" : [""alias1"", ""alias2""],
		""caused_by"" : {
			""type"" : ""illegal_argument_exception"",
			""reason"" : ""Parse failure at index [2] of [-1m]"",
			""caused_by"" : ""x""
		}
	},
	""index"" : ""index"",
	""index_uuid"" : ""x9h1ks"",
	""unknown_prop"" : {},
	""unknown_prop2"" : false,
	""resource.type"" : ""aliases"",
	""resource.id"" : ""alias1"",
	""shard"" : ""1"",
	""line"" : 12,
	""col"" : 199,
	""bytes_wanted"" : 1298312,
	""bytes_limit"" : 8912031,
	""script_stack"" : ""x"",
	""script"" : ""some script"",
	""lang"" : ""c#""
}";

		[U] protected override void AssertServerError() => base.AssertServerError();

		protected override void AssertResponseError(string origin, Error error)
		{
			AssertCausedBy(origin, error);
			AssertCausedBy(origin, error.CausedBy);
			AssertCausedBy(origin, error.CausedBy.CausedBy);
			error.CausedBy.CausedBy.CausedBy.Should().NotBeNull();
			error.CausedBy.CausedBy.CausedBy.Reason.Should().Be("x");
			error.RootCause.Should().NotBeEmpty(origin);
			error.Headers.Should().HaveCount(2, origin);
			AssertMetadata(origin, error);
			error.CausedBy.Should().NotBeNull();
			error.CausedBy.ScriptStack.Should().HaveCount(2);
			error.CausedBy.ResourceId.Should().HaveCount(2);
			error.AdditionalProperties.Should().ContainKeys("unknown_prop", "unknown_prop2");
		}

		private void AssertMetadata(string origin, ErrorCause errorMetadata)
		{
			errorMetadata.Should().NotBeNull(origin);
			errorMetadata.Grouped.Should().BeTrue(origin);
			errorMetadata.Phase.Should().Be("query", origin);
			errorMetadata.Index.Should().Be("index", origin);
			errorMetadata.IndexUUID.Should().NotBeNullOrWhiteSpace(origin);
			errorMetadata.ResourceType.Should().NotBeNullOrWhiteSpace(origin);
			errorMetadata.ResourceId.Should().HaveCount(1, origin);
			errorMetadata.Shard.Should().Be(1, origin);
			errorMetadata.Line.Should().Be(12, origin);
			errorMetadata.Column.Should().Be(199, origin);
			errorMetadata.BytesWanted.Should().BeGreaterThan(1, origin);
			errorMetadata.BytesLimit.Should().BeGreaterThan(1, origin);
			errorMetadata.ScriptStack.Should().HaveCount(1, origin);
			errorMetadata.Script.Should().NotBeNullOrWhiteSpace(origin);
			errorMetadata.Language.Should().NotBeNullOrWhiteSpace(origin);
			AssertFailedShards(origin, errorMetadata.FailedShards);
		}

		private static void AssertFailedShards(string origin, IReadOnlyCollection<ShardFailure> errorMetadataFailedShards)
		{
			errorMetadataFailedShards.Should().NotBeEmpty(origin).And.HaveCount(1, origin);
			var f = errorMetadataFailedShards.First();
			f.Index.Should().NotBeNullOrWhiteSpace(origin);
			f.Node.Should().NotBeNullOrWhiteSpace(origin);
			f.Status.Should().NotBeNullOrWhiteSpace(origin);
			AssertCausedBy(origin, f.Reason);
			f.Shard.Should().NotBeNull(origin);
		}

		private static void AssertCausedBy(string origin, ErrorCause causedBy)
		{
			causedBy.Should().NotBeNull(origin);
			causedBy.Type.Should().NotBeNullOrWhiteSpace(origin);
			causedBy.Reason.Should().NotBeNullOrWhiteSpace(origin);
		}
	}
}
