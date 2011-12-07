﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElasticSearch.Client;
using ElasticSearch.Client.Mapping;
using ElasticSearch.Client.Settings;
using HackerNews.Indexer.Domain;
using Nest.TestData;
using Nest.TestData.Domain;
using NUnit.Framework;

namespace ElasticSearch.Tests
{
	/// <summary>
	///  Tests that test whether the query response can be successfully mapped or not
	/// </summary>
	[TestFixture]
	public class IndicesTest : BaseElasticSearchTests
	{
		private string _LookFor = NestTestData.Data.First().Followers.First().FirstName;


		protected void TestDefaultAssertions(QueryResponse<ElasticSearchProject> queryResponse)
		{
			Assert.True(queryResponse.IsValid);
			Assert.Null(queryResponse.ConnectionError);
			Assert.True(queryResponse.Total > 0, "No hits");
			Assert.True(queryResponse.Documents.Any());
			Assert.True(queryResponse.Documents.Count() > 0);
			Assert.True(queryResponse.Shards.Total > 0);
			Assert.True(queryResponse.Shards.Successful == queryResponse.Shards.Total);
			Assert.True(queryResponse.Shards.Failed == 0);
				
		}
		[Test]
		public void test_clear_cache()
		{
			var client = this.ConnectedClient;
			var status = client.ClearCache();
			Assert.True(status.Success);
		}
		[Test]
		public void test_clear_cache_specific()
		{
			var client = this.ConnectedClient;
			var status = client.ClearCache(ClearCacheOptions.Filter | ClearCacheOptions.Bloom);
			Assert.True(status.Success);
		}
		[Test]
		public void test_clear_cache_generic_specific()
		{
			var client = this.ConnectedClient;
			var status = client.ClearCache<ElasticSearchProject>(ClearCacheOptions.Filter | ClearCacheOptions.Bloom);
			Assert.True(status.Success);
		}
		[Test]
		public void test_clear_cache_generic_specific_indices()
		{
			var client = this.ConnectedClient;
			var status = client.ClearCache(new List<string> { Settings.DefaultIndex, Settings.DefaultIndex + "_clone" }, ClearCacheOptions.Filter | ClearCacheOptions.Bloom);
			Assert.True(status.Success);
		}

        [Test]
        public void CreateIndex()
        {
            var client = this.ConnectedClient;
            var typeMapping = this.ConnectedClient.GetMapping(Test.Default.DefaultIndex + "_clone",
                                                  "elasticsearchprojects2");

            typeMapping.Name = "mytype";
            var settings = new IndexSettings();
            settings.Mappings.Add(typeMapping);
            settings.NumberOfReplicas = 1;
            settings.NumberOfShards = 5;
            settings.Analysis.Analyzer.Add("snowball", new SnowballAnalyzerSettings { Language = "English" });

            var indexName = Guid.NewGuid().ToString();
            var response = client.CreateIndex(indexName, settings);

            Assert.IsTrue(response.Success);

            Assert.IsNotNull(this.ConnectedClient.GetMapping(indexName, "mytype"));

            response = client.DeleteIndex(indexName);

            Assert.IsTrue(response.Success);
        }

        [Test]
        public void CreateIndexMultiFieldMap()
        {
            var client = this.ConnectedClient;

            var typeMapping = new TypeMapping(Guid.NewGuid().ToString("n"));
            var property = new TypeMappingProperty
                           {
                               Type = "multi_field"
                           };

            var primaryField = new TypeMappingProperty
                               {
                                   Type = "string", 
                                   Index = "not_analyzed"
                               };

            var analyzedField = new TypeMappingProperty
                                {
                                    Type = "string", 
                                    Index = "analyzed"
                                };

            property.Fields = new Dictionary<string, TypeMappingProperty>();
            property.Fields.Add("name", primaryField);
            property.Fields.Add("name_analyzed", analyzedField);

            typeMapping.Properties.Add("name", property);

            var settings = new IndexSettings();
            settings.Mappings.Add(typeMapping);
            settings.NumberOfReplicas = 1;
            settings.NumberOfShards = 5;
            settings.Analysis.Analyzer.Add("snowball", new SnowballAnalyzerSettings { Language = "English" });

            var indexName = Guid.NewGuid().ToString();
            var response = client.CreateIndex(indexName, settings);

            Assert.IsTrue(response.Success);

            Assert.IsNotNull(this.ConnectedClient.GetMapping(indexName, typeMapping.Name));

            response = client.DeleteIndex(indexName);

            Assert.IsTrue(response.Success);
        }

        [Test]
        public void CreateIndexWithTokenFilter()
        {
            var client = this.ConnectedClient;

            var settings = new IndexSettings();
            settings.Analysis.Analyzer.Add("test", new CustomAnalyzerSettings
                                                       {
                                                           Tokenizer = "standard",
                                                           Filter = new List<string>
                                                                        {
                                                                            "standard",
                                                                            "lowercase",
                                                                            "stop",
                                                                            "shingle"
                                                                        }
                                                       });

            var indexName = Guid.NewGuid().ToString();
            var response = client.CreateIndex(indexName, settings);

            Assert.IsTrue(response.Success);

            response = client.DeleteIndex(indexName);

            Assert.IsTrue(response.Success);
        }

        [Test]
        public void CreateIndexWithCustomTokenFilter()
        {
            var client = this.ConnectedClient;

            var settings = new IndexSettings();
            settings.Analysis.Analyzer.Add("test", new CustomAnalyzerSettings
            {
                Tokenizer = "standard",
                Filter = new List<string>
                                                                        {
                                                                            "standard",
                                                                            "lowercase",
                                                                            "stop_custom",
                                                                            "word_del_custom",
                                                                            "shingle"
                                                                        }
            });

            settings.Analysis.TokenFilters.Add("stop_custom", new StopTokenFilter
                                                                  {
                                                                      EnablePositionIncrements = false,
                                                                      IgnoreCase = true,
                                                                      Stopwords = "this,that"
                                                                  });

            settings.Analysis.TokenFilters.Add("word_del_custom", new WordDelimiterTokenFilter
                                                                  {
                                                                     PreserveOriginal = true
                                                                  });

            var indexName = Guid.NewGuid().ToString();
            var response = client.CreateIndex(indexName, settings);

            Assert.IsTrue(response.Success);

            response = client.DeleteIndex(indexName);

            Assert.IsTrue(response.Success);
        }
	}
}
