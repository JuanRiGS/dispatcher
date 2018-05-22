namespace dispatcher.Common
{
    public class Transformation
    {
        public string CheckBalance(string invoiceRef)
        {
            //var json = "{" + $"invoiceref:{invoiceRef}, balancetopay:150000" + "}";
            var json = "{\"invoiceref\":\"" + invoiceRef + "\",\"balancetopay\":15000}";

            return json;
        }
    }
}