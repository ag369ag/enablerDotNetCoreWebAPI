namespace testASPWebAPI
{
    public class Tax
    {
        public int id { get; set; }
        public string name { get; set; }
        public decimal rate { get; set; }
        public int inclusive { get; set; }
        public string legend { get; set; }

        public static string GetTaxName(List<Tax> tx, int id)
        {
            string txName = (from tax in tx
                             where tax.id == id
                             select tax.name
                                 ).Single();

            return txName;
        }
        public static int GetTaxID(List<Tax> tx, string name)
        {
            int id = (from tax in tx
                      where tax.name == name
                      select tax.id
                      ).Single();
            return id;
        }
        public static decimal GetTaxRateByID(List<Tax> tx, int id)
        {
            decimal rate = (from tax in tx
                            where tax.id == id
                            select tax.rate
                       ).Single();
            return rate;
        }
        public static decimal GetTaxRateByName(List<Tax> tx, string name)
        {
            decimal rate = (from tax in tx
                            where tax.name == name
                            select tax.rate
                      ).Single();
            return rate;
        }
    }
}
