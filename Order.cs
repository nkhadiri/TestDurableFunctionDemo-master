using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableFunctionApp
{
    public class Order
    {
        public Order()
        {
        }

        public string OrderId { get; set; }
        public TransferTypes TransferType { get; set; }
        public Dictionary<TransferTypes, TransferStatus> TransferStatus { get; set; } = new Dictionary<TransferTypes, TransferStatus>();

        public void SetOrderId(string orderId) => OrderId = orderId;
        public void SetTransferType(TransferTypes transferType) => TransferType = transferType;

        public void SetTransferStatus((TransferTypes transferType, TransferStatus transferStatus) args) =>
            TransferStatus[args.transferType] = args.transferStatus;

        public TransferStatus GetTransferStatus(TransferTypes transferType) =>
            TransferStatus.TryGetValue(transferType, out var status)
                ? status
                : DurableFunctionApp.TransferStatus.NotStarted;


        [FunctionName(nameof(Order))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext context)
        {
            return context.DispatchAsync<Order>();
        }

    }

    public enum TransferStatus
    {
        NotStarted,
        Success,
        Failure
    }

    public enum OrderStatusEnum
    {
        [Description("In Pending Cart")]
        InPendingCart = 10,

        [Description("Sent to PM System")]
        SentToPMSystem = 15,

        [Description("Sent to MQ")]
        SentToMQ = 20,

        [Description("Dispatch Done")]
        DispatchDone = 50,

        [Description("Email Transfer Done")]
        EmailTransferDone = 60,

        [Description("Fax Transfer Done")]
        FaxTransferDone = 61,

        [Description("FTP Transfer Done")]
        FTPTransferDone = 62,

        [Description("MQ Transfer Done")]
        MQTransferDone = 63,

        [Description("Print Transfer Done")]
        PrintTransferDone = 64,

        [Description("Http Transfer Done")]
        HttpTransferDone = 65,

        [Description("Supplier Conversion Done")]
        SupplierConversionDone = 70,

        [Description("Http Transfer Failed")]
        HttpTransferFailed = 80,

        [Description("PostProcess Xsl Translation Failed")]
        PostProcessXslTranslationFailed = 81,

        [Description("ACK Done")]
        ACKDone = 99,

        [Description("Order Deleted(Login Delete)")]
        OrderDeletedLoginDelete = 959,

        [Description("Order deleted (cbu delete)")]
        OrderDeletedCBUDelete = 969,

        [Description("Order deleted (cbu/billing/shipping deleted)")]
        OrderDeletedCBSDelete = 979,

        [Description("Order deleted (from shopping cart)")]
        OrderDeletedFromShoppingCart = 989,

        [Description("Order archived")]
        OrderArchived = 999
    }
}
