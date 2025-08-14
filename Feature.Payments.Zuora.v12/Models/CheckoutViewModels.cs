namespace Your.Feature.Payments.Models
{
  public class Step1_BillingAddressModel {
    public string FirstName { get; set; }
    public string LastName  { get; set; }
    public string Email     { get; set; }
    public string Address1  { get; set; }
    public string City      { get; set; }
    public string State     { get; set; }
    public string PostalCode{ get; set; }
    public string Country   { get; set; }
  }
  public class Step2_PreviewModel {
    public string PlanSku { get; set; }
    public int Quantity { get; set; } = 1;
    public string AccountNumber { get; set; }
    public string AccountId { get; set; }
    public dynamic PreviewPayload { get; set; }
  }
  public class Step3_PaymentModel {
    public string AccountNumber { get; set; }
    public string AccountId { get; set; }
    public string PublishableKey { get; set; }
    public string Environment { get; set; }
    public string RatePlanId { get; set; }
    public string ChargeId { get; set; }
    public int Quantity { get; set; }
  }
}
