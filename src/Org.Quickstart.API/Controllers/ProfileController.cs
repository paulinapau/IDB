using System;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;
using Couchbase.Transactions;
using Couchbase.Transactions.Config;
using Couchbase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.Quickstart.API.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics.Metrics;
using Couchbase.Management.Users;
using static Couchbase.Core.Diagnostics.Tracing.OuterRequestSpans.ManagerSpan;
using System.Transactions;
using Couchbase.Transactions.Error;
using Couchbase.Core.IO.Operations;

namespace Org.Quickstart.API.Controllers
{
    [ApiController]
    [Route("/api/v1/profile")]
    public class ProfileController
        : Controller
    {
        private readonly IClusterProvider _clusterProvider;
        private readonly IBucketProvider _bucketProvider;
        private readonly ILogger _logger;

        private readonly CouchbaseConfig _couchbaseConfig;

        public ProfileController(
            IClusterProvider clusterProvider,
            IBucketProvider bucketProvider,
	        IOptions<CouchbaseConfig> options,
            ILogger<ProfileController> logger)
        {
	        _clusterProvider = clusterProvider;
	        _bucketProvider = bucketProvider;
            _logger = logger;
	        _couchbaseConfig = options.Value;

        }
      
        [HttpGet("{id}")]
       // [HttpGet("{username:string}", Name = "UserProfile-GetById")]
        //[SwaggerOperation(OperationId = "UserProfile-GetById", Summary = "Get user profile by Id", Description = "Get a user profile by Id from the request")]
        [SwaggerResponse(200, "Returns a report")]
        [SwaggerResponse(404, "Report not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);

		        var scope = bucket.Scope(_couchbaseConfig.ScopeName);
                var collection = await scope.CollectionAsync(_couchbaseConfig.CollectionName);

                var transactions = Transactions.Create(bucket.Cluster,
                               TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                             .Build());
                Profile result = default; 
                await transactions.RunAsync(async ctx =>
                {
                    var docOpt = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    if (docOpt!=null)
                    {
                        result = docOpt.ContentAs<Profile>(); 
                    }
                }).ConfigureAwait(false);

                 return Ok(result);
               
            }
            catch (TransactionCommitAmbiguousException e)
            {
                return NotFound(("Transaction possibly committed", e));
               
            }
            catch (TransactionFailedException e)
            {
                return NotFound(("Transaction did not reach commit point", e));
               
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
        [SwaggerOperation(OperationId = "UserProfile-Post", Summary = "Create a user profile", Description = "Create a user profile from the request")]
        [SwaggerResponse(201, "Create a user profile")]
        [SwaggerResponse(409, "the email of the user already exists")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Post([FromBody] ProfileCreateRequestCommand request)
        {
            try
            {
		        if (!string.IsNullOrEmpty(request.email) && !string.IsNullOrEmpty(request.password))
		        {
		            var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);
		            var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
		            var profile = request.GetProfile();
                    var newid = profile.GenerateNextUserId(GetLastUserId().Result);

                    var transactions = Transactions.Create(bucket.Cluster,
                                TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                              .Build());
                    try
                    {
                            await transactions.RunAsync(async ctx =>
                            {
                                var docOpt = await ctx.InsertAsync(collection, newid.ToString(), profile).ConfigureAwait(false);
                                if (docOpt != null)
                                {
                                    profile = docOpt.ContentAs<Profile>();
                                }
                               
                            }).ConfigureAwait(false);
                           
                            return Created($"/api/v1/profile/{newid}", profile);

                    }
                    catch (TransactionFailedException err)
                    {
                        return NotFound(err);
                    }
                    return UnprocessableEntity();
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
       // [SwaggerOperation(OperationId = "UserProfile-Update", Summary = "Update a user profile", Description = "Update a user profile from the request")]
        [SwaggerResponse(200, "Update a user profile")]
        [SwaggerResponse(404, "user profile not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] ProfileCreateRequestCommand request)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);
                var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
                //var result = await collection.GetAsync(id);
                var transactions = Transactions.Create(bucket.Cluster,
                               TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                             .Build());
                await transactions.RunAsync(async ctx =>
                {
                    var old = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    var updatedProfile = new Profile
                    {
                        username = request.username,
                        phoneNumber = request.phoneNumber,
                        firstName = request.firstName,
                        lastName = request.lastName,
                        email = request.email,
                        password = request.password,
                        gender = request.gender,
                        registrationDate = request.registrationDate,
                        Address = request.Address,
                        Orders = request.Orders,
                    };

                    // Replace the document in the collection with the updated content.
                    _ = await ctx.ReplaceAsync(old, updatedProfile).ConfigureAwait(false);
                }).ConfigureAwait(false);
                /*
                await transactions.RunAsync(async ctx =>
                {
                    var anotherDoc = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    var content = anotherDoc.ContentAs<dynamic>();
                    content.put("transactions", "are awesome");
                    _ = await ctx.ReplaceAsync(anotherDoc, content);
                }).ConfigureAwait(false);
                // var updateResult = await collection.ReplaceAsync<Profile>(id, request.GetProfile());
                */
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
      // [SwaggerOperation(OperationId = "UserProfile-Delete", Summary = "Delete a profile", Description = "Delete a profile from the request")]
        [SwaggerResponse(200, "Delete a profile")]
        [SwaggerResponse(404, "profile not found")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            try
            {
		        var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);
		        var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);
                var transactions = Transactions.Create(bucket.Cluster,
                              TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None)
                            .Build());
                await transactions.RunAsync(async ctx =>
                {
                    var profile = await ctx.GetAsync(collection, id).ConfigureAwait(false);
                    await ctx.RemoveAsync(profile).ConfigureAwait(false);
                }).ConfigureAwait(false);
               // await collection.RemoveAsync(id);
                return this.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        /*
        [HttpGet]
	    [Route("/api/v1/profiles")]
        [SwaggerOperation(OperationId = "UserProfile-List", Summary = "Search for user profiles", Description = "Get a list of user profiles from the request")]
        [SwaggerResponse(200, "Returns the list of user profiles")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> List([FromQuery] ProfileListRequestQuery request)
        {
            try
            {
                var cluster = await _clusterProvider.GetClusterAsync();
                var query = $@"SELECT p.*
FROM `{_couchbaseConfig.BucketName}`.`{_couchbaseConfig.ScopeName}`.`{_couchbaseConfig.CollectionName}` p
WHERE lower(p.firstName) LIKE '%' || $search || '%'
OR lower(p.lastName) LIKE '%' || $search || '%'
LIMIT $limit OFFSET $skip";

                var results = await cluster.QueryAsync<Profile>(query, options =>
                {
                    options.Parameter("search", request.Search.ToLower());
                    options.Parameter("limit", request.Limit);
                    options.Parameter("skip", request.Skip);
                });
                var items = await results.Rows.ToListAsync<Profile>();
                if (items.Count == 0)
                    return NotFound();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message} {ex.StackTrace} {Request.GetDisplayUrl()}");
            }
        }
        */
        /*
       // [HttpPost]
       // [Route("/api/v1/transfer")]
        [SwaggerOperation(OperationId = "UserProfile-Transfer", Summary = "Transfer on-board credit", Description = "Transfer on-board credit between two profiles")]
        [SwaggerResponse(200, "On-board credit transferred")]
        [SwaggerResponse(500, "Returns an internal error")]
        public async Task<IActionResult> Transfer([FromBody] ProfileTransferCredit request)
        {
            try
            {
                var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);
                var collection = await bucket.CollectionAsync(_couchbaseConfig.CollectionName);

                // only use DurabilityLevel.None for single node (e.g. a local single-node install, not Capella)
                var tx = Transactions.Create(bucket.Cluster, TransactionConfigBuilder.Create().DurabilityLevel(DurabilityLevel.None));
                await tx.RunAsync(async (ctx) =>
                {
                    var fromProfileDoc = await ctx.GetAsync(collection, request.Pfrom.ToString());
                    var fromProfile = fromProfileDoc.ContentAs<Profile>();

                    var toProfileDoc = await ctx.GetAsync(collection, request.Pto.ToString());
                    var toProfile = toProfileDoc.ContentAs<Profile>();

                  //  fromProfile.TransferTo(toProfile, request.Amount);

                    await ctx.ReplaceAsync(fromProfileDoc, fromProfile);
                    await ctx.ReplaceAsync(toProfileDoc, toProfile);

                    await ctx.CommitAsync();
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message} {ex.StackTrace} {Request.GetDisplayUrl()}");
            }
        }
        
        */
        [HttpGet]
        public async Task<string> GetLastUserId()
        {
            var bucket = await _bucketProvider.GetBucketAsync(_couchbaseConfig.BucketName);
            var scope = bucket.Scope(_couchbaseConfig.ScopeName);
            var collection = await scope.CollectionAsync(_couchbaseConfig.CollectionName);
            var cluster = await _clusterProvider.GetClusterAsync();

            var query = $"SELECT MAX(META().id) FROM {_couchbaseConfig.BucketName}.{_couchbaseConfig.ScopeName}.{_couchbaseConfig.CollectionName}";
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
