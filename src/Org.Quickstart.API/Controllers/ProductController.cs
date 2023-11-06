using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.Quickstart.API.Models;
using Swashbuckle.AspNetCore.Annotations;
using Couchbase.KeyValue;
using Couchbase.Transactions.Config;
using Couchbase.Transactions;
using Couchbase.Transactions.Error;
using System.Transactions;

namespace Org.Quickstart.API.Controllers
{
    [ApiController]
    [Route("/api/v1/product")]
    public class ProductController : ControllerBase
    {
        private readonly IClusterProvider _clusterProvider;
        private readonly IBucketProvider _bucketProvider;
        private readonly ILogger _logger;
        private readonly CouchbaseConfig _couchbaseConfig;

        public ProductController(
           IClusterProvider clusterProvider,
           IBucketProvider bucketProvider,
           IOptions<CouchbaseConfig> options,
           ILogger<ProductController> logger)
        {
            _clusterProvider = clusterProvider;
            _bucketProvider = bucketProvider;
            _logger = logger;
            _couchbaseConfig = options.Value;
        }

        [HttpGet("{id}")]
        [SwaggerResponse(200, "Returns a report")]
        [SwaggerResponse(404, "Report not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName1);

                var scope = bucket.Scope(_couchbaseConfig.ScopeName);
                var collection = await scope.CollectionAsync(_couchbaseConfig.CollectionName);

                var transactions = Transactions.Create(bucket.Cluster,
                              TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                            .Build());
                Product result = default;
                await transactions.RunAsync(async ctx =>
                {
                    var docOpt = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    if (docOpt != null)
                    {
                        result = docOpt.ContentAs<Product>();
                    }
                }).ConfigureAwait(false);

                return Ok(result);
                
            }
            catch (DocumentNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message} {ex.StackTrace} {Request.GetDisplayUrl()}");
            }
        }
        [HttpPost]
        [SwaggerResponse(201, "Create a product")]
        [SwaggerResponse(409, "productwith this name and description already exists")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Post([FromBody] Product request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.Name) && !string.IsNullOrEmpty(request.Description))
                {
                    var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName1);
                    var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
                    Product product = default;
                    var newid = "";
                    var transactions = Transactions.Create(bucket.Cluster,
                              TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                            .Build());
                    try
                    {
                        await transactions.RunAsync(async ctx =>
                        {
                            product = request.GetProduct();
                            newid = product.GenerateNextProductId(GetLastProductId().Result);
                            var docOpt = await ctx.InsertAsync(collection, newid.ToString(), product).ConfigureAwait(false);
                            if (docOpt != null)
                            {
                                product = docOpt.ContentAs<Product>();
                            }

                        }).ConfigureAwait(false);

                        return Created($"/api/v1/product/{newid}", product);

                    }
                    catch (TransactionFailedException err)
                    {
                        return NotFound(err);
                    }
                   // await collection.InsertAsync(newid, product);

                    
                }
                else
                {
                    return UnprocessableEntity();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message} {ex.StackTrace} {Request.GetDisplayUrl()}");
            }

        }
       
        [HttpPut("{id}")]
        [SwaggerResponse(200, "Update product")]
        [SwaggerResponse(404, "product not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] Product request)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName1);
                var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
                var transactions = Transactions.Create(bucket.Cluster,
                               TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                             .Build());
                await transactions.RunAsync(async ctx =>
                {
                    var old = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    var updated = new Product
                    {
                        ImageUrl = request.ImageUrl,
                        Name = request.Name,
                        Price = request.Price,
                        Description = request.Description,
                        Quantity = request.Quantity,
                        Category =  request.Category,
                        Manufacturer = request.Manufacturer,
                        Coupons = request.Coupons
                    };

                    // Replace the document in the collection with the updated content.
                    _ = await ctx.ReplaceAsync(old, updated).ConfigureAwait(false);
                }).ConfigureAwait(false);
               // var updateResult = await collection.ReplaceAsync<Product>(id, request.GetProduct());
                return Ok(request);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound(); // Document with the provided id not found
                //return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message} {ex.StackTrace} {Request.GetDisplayUrl()}");
            }
        }

        [HttpDelete("{id}")]
        [SwaggerResponse(200, "Delete a product")]
        [SwaggerResponse(404, "product not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName1);
                var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
                var transactions = Transactions.Create(bucket.Cluster,
                               TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                             .Build());
                await transactions.RunAsync(async ctx =>
                {
                    var product = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    await ctx.RemoveAsync(product).ConfigureAwait(false);
                }).ConfigureAwait(false);
                //await collection.RemoveAsync(id);
                return this.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }











        [HttpGet]
        public async Task<string> GetLastProductId()
        {
            var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName1);
            var scope = bucket.Scope(_couchbaseConfig.ScopeName);
            var collection = await scope.CollectionAsync(_couchbaseConfig.CollectionName);
            var cluster = await _clusterProvider.GetClusterAsync();

            var query = $"SELECT MAX(META().id) FROM {_couchbaseConfig.BucketName1}.{_couchbaseConfig.ScopeName}.{_couchbaseConfig.CollectionName}";
            var result = await cluster.QueryAsync<MaxIdResult>(query);
            if (result != null)
            {
                var maxId = result.Rows.FirstAsync().Result?.MaxId;
                return maxId;
            }
            else
                return null;
        }
    }
}
