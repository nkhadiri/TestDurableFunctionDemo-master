using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionApp
{
    public enum TransferTypes
    {
        FAX,
        FTP,
        MAIL,
        MQ,
        PRNT,
        HTTP
    }

    public static class Dispatch
    {
        [FunctionName("Dispatch")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var command = context.GetInput<DispatchOrder>();

            var entityId = new EntityId(nameof(Order), command.OrderId);
            var entityId2 = new EntityId(nameof(Order), command.OrderId);


            await context.CallEntityAsync(entityId, nameof(Order.SetOrderId), command.OrderId);
            await context.CallEntityAsync(entityId, nameof(Order.SetTransferType), command.TransferType);

            var transferStatus =
                await context.CallEntityAsync<TransferStatus>(entityId, nameof(Order.GetTransferStatus), command.TransferType);

            var orderXml = string.Empty;

            var outputs = new List<string>();

            if (transferStatus == TransferStatus.Success)
            {
                return outputs;
            }

            using (await context.LockAsync(entityId, entityId2))
            {
                var status = await (command.TransferType switch
                {
                    TransferTypes.FAX => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_Fax),
                        new OrderFaxDispatch(command.OrderId, orderXml, string.Empty)),

                    TransferTypes.FTP => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_Ftp),
                        new OrderFtpDispatch(command.OrderId, orderXml, string.Empty)),

                    TransferTypes.MAIL => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_Mail),
                        new OrderMailDispatch(command.OrderId, orderXml, string.Empty)),

                    TransferTypes.MQ => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_MQ),
                        new OrderMqDispatch(command.OrderId, orderXml, string.Empty)),

                    TransferTypes.PRNT => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_Print),
                        new OrderPrintDispatch(command.OrderId, orderXml, string.Empty)),

                    TransferTypes.HTTP => context.CallActivityAsync<TransferStatus>(nameof(Dispatch_Http),
                        new OrderHttpDispatch(command.OrderId, orderXml, string.Empty)),

                    _ => throw new ArgumentOutOfRangeException()
                });
                

                await context.CallEntityAsync(entityId, nameof(Order.SetTransferStatus), (command.TransferType, status));

                outputs.Add($"{command.TransferType}: {status}");
            }

            return outputs;
        }

        [FunctionName(nameof(Dispatch_Fax))]
        public static TransferStatus Dispatch_Fax([ActivityTrigger] OrderFaxDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to FAX", command.OrderId);

            return TransferStatus.Success;
        }


        [FunctionName(nameof(Dispatch_Ftp))]
        public static TransferStatus Dispatch_Ftp([ActivityTrigger] OrderFtpDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to FTP", command.OrderId);

            return TransferStatus.Success;
        }


        [FunctionName(nameof(Dispatch_Mail))]
        public static TransferStatus Dispatch_Mail([ActivityTrigger] OrderMailDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to MAIL", command.OrderId);

            return TransferStatus.Success;
        }


        [FunctionName(nameof(Dispatch_MQ))]
        public static TransferStatus Dispatch_MQ([ActivityTrigger] OrderMqDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to MQ", command.OrderId);

            return TransferStatus.Success;
        }


        [FunctionName(nameof(Dispatch_Print))]
        public static TransferStatus Dispatch_Print([ActivityTrigger] OrderPrintDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to PRINT", command.OrderId);

            return TransferStatus.Success;
        }


        [FunctionName(nameof(Dispatch_Http))]
        public static TransferStatus Dispatch_Http([ActivityTrigger] OrderHttpDispatch command, ILogger log)
        {
            log.LogInformation("Dispatching order {OrderId} to HTTP", command.OrderId);

            return TransferStatus.Success;
        }

        [FunctionName("Dispatch_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var queryString = req.RequestUri.ParseQueryString();

            var orderId = queryString.Get("orderId") ?? "OR1001";
            var transferTypeRaw = queryString.Get("transferType");
            var transferType = TransferTypes.FAX;

            if (!string.IsNullOrEmpty(transferTypeRaw) && Enum.TryParse(transferTypeRaw, out transferType))
            {
                
            }

            var dispatchCommand = new DispatchOrder(orderId, transferType);
            log.LogInformation("Dispatching {order}", dispatchCommand);

            string instanceId = await starter.StartNewAsync(nameof(Dispatch), dispatchCommand);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }




    public record DispatchOrder(string OrderId, TransferTypes TransferType);

    public record OrderFaxDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    public record OrderFtpDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    public record OrderMailDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    public record OrderMqDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    public record OrderPrintDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    public record OrderHttpDispatch(string OrderId, string OrderXml, string SupplierLocationId);
    
}
