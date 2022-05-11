using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunctionApp
{
    public static class Durable
    {
        [FunctionName("Durable")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var entityId = new EntityId(nameof(Counter), context.InstanceId);

            var count = await context.CallEntityAsync<int>(entityId, nameof(Counter.Get));

            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            
            outputs.Add(await context.CallActivityAsync<string>("Durable_Hello", "Tokyo"));
            context.SignalEntity(entityId, nameof(Counter.Add), 1);
            context.SetCustomStatus("Tokyo");

            outputs.Add(await context.CallActivityAsync<string>("Durable_Hello", "Seattle"));
            context.SignalEntity(entityId, nameof(Counter.Add), 1);
            context.SetCustomStatus("Seattle");

            outputs.Add(await context.CallActivityAsync<string>("Durable_Hello", "London"));
            context.SignalEntity(entityId, nameof(Counter.Add), 1);
            context.SetCustomStatus("London");


            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Durable_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Durable_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            //var id = Guid.NewGuid().ToString();
            var id = "OR001";
            log.LogInformation("Created Id {id}", id);
            string instanceId = await starter.StartNewAsync("Durable", id);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}