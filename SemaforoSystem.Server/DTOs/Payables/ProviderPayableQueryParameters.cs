using SemaforoSystem.Server.DTOs.Common;

namespace SemaforoSystem.Server.DTOs.Payables;

public class ProviderPayableQueryParameters : QueryParameters
{
    public int? ProviderId { get; set; }
    public int? SiteId { get; set; }
    public int? ProviderPayableTypeId { get; set; }
    public int? ProviderPayableStatusId { get; set; }
    public string? TypeCode { get; set; }
    public string? StatusCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDateFrom { get; set; }
    public DateTime? DocumentDateTo { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? OnlyOpen { get; set; }
    public bool? OnlyOverdue { get; set; }
    public long? PurchaseOrderId { get; set; }
    public long? PurchaseReceiptId { get; set; }
}
