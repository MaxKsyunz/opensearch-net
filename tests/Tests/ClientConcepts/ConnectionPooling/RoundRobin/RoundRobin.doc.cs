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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using OpenSearch.Net;
using FluentAssertions;
using Tests.Framework;

namespace Tests.ClientConcepts.ConnectionPooling.RoundRobin
{
	public class RoundRobin
	{
		/**[[round-robin]]
		 * == Round robin behaviour
		*
		* <<sniffing-connection-pool, Sniffing>> and <<static-connection-pool, Static>> connection pools
		* round robin over the `live` nodes to evenly distribute requests over all known nodes.
		*
		* [float]
		* === CreateView
		*
		* This is the method on an `IConnectionPool` that creates a view of all the live nodes in the cluster that the client
		* knows about. Different connection pool implementations can decide on the view to return, for example,
		*
		* - `SingleNodeConnectionPool` is only ever seeded with and hence only knows about one node
		* - `StickyConnectionPool` can return a view of live nodes with the same starting position as the last live node a request was made against
		* - `SniffingConnectionPool` returns a view with a changing starting position that wraps over on each call
		*
		* `CreateView` is implemented in a lock free thread safe fashion, meaning each callee gets returned
		* its own cursor to advance over the internal list of nodes. This to guarantee each request that needs to
		* fall over tries all the nodes without suffering from noisy neighbours advancing a global cursor.
		*/
		protected int NumberOfNodes = 10;

		/**
		* Here we have setup a Static connection pool seeded with 10 nodes. We force randomization OnStartup to false
		* so that we can test the nodes being returned are in the order we expect them to be.
		*/
		[U] public void EachViewStartsAtNexPositionAndWrapsOver()
		{
			var uris = Enumerable.Range(9200, NumberOfNodes).Select(p => new Uri("http://localhost:" + p));
			var staticPool = new StaticConnectionPool(uris, randomize: false);
			var sniffingPool = new SniffingConnectionPool(uris, randomize: false);

			this.AssertCreateView(staticPool);
			this.AssertCreateView(sniffingPool);
		}

		private void AssertCreateView(IConnectionPool pool)
		{
			/** So what order do we expect? Imagine the following:
			*
			* . Thread A calls `CreateView()` first without a local cursor and takes the current value from the internal global cursor, which is `0`
			* . Thread B calls `CreateView()` second without a local cursor and therefore starts at `1`
			* . After this, each thread should walk the nodes in successive order using their local cursor. For example, Thread A might
			* get 0,1,2,3,5 and thread B will get 1,2,3,4,0.
			*/
			var startingPositions = Enumerable.Range(0, NumberOfNodes)
				.Select(i => pool.CreateView().First())
				.Select(n => n.Uri.Port)
				.ToList();

			var expectedOrder = Enumerable.Range(9200, NumberOfNodes);
			startingPositions.Should().ContainInOrder(expectedOrder);

			/**
			* What the above code just proved is that each call to `CreateView()` gets assigned the next available node.
			*
			* Lets up the ante:
			*
			* . Call `CreateView()` over `NumberOfNodes * 2` threads
			* . On each thread, call `CreateView()` `NumberOfNodes * 10` times using a local cursor.
			*
			* We'll validate that each thread sees all the nodes and that they wrap over, for example, after node 9209
			* comes 9200 again
			*/
			var threadedStartPositions = new ConcurrentBag<int>();
			var threads = Enumerable.Range(0, 20)
				.Select(i => CreateThreadCallingCreateView(pool, threadedStartPositions))
				.ToList();

			foreach (var t in threads) t.Start();
			foreach (var t in threads) t.Join();

			/**
			* Each thread reported the first node it started off. Let's make sure we see each node twice
			* because we started `NumberOfNodes * 2` threads
			*/
			var grouped = threadedStartPositions.GroupBy(p => p).ToList();
			grouped.Count.Should().Be(NumberOfNodes);
			grouped.Select(p => p.Count()).Should().OnlyContain(p => p == 2);
		}

		// hide
		public Thread CreateThreadCallingCreateView(IConnectionPool pool, ConcurrentBag<int> startingPositions) => new Thread(() =>
		{
			/** `CallCreateView` is a generator that calls `CreateView()` indefinitely, using a local cursor */
			var seenPorts = CallCreateView(pool).Take(NumberOfNodes * 10).ToList();
			var startPosition = seenPorts.First();
			startingPositions.Add(startPosition);
			var i = (startPosition - 9200) % NumberOfNodes; // <1> first seenNode is e.g 9202 then start counting at 2
			foreach (var port in seenPorts)
				port.Should().Be(9200 + (i++ % NumberOfNodes));
		});

		//hide
		private IEnumerable<int> CallCreateView(IConnectionPool pool)
		{
			foreach(var n in pool.CreateView()) yield return n.Uri.Port;
		}
	}
}
