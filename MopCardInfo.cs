namespace testASPWebAPI
{
    public class MopCardInfo
    {
        public string CardNumber { get; set; }
        public string CardHolderName { get; set; }
        public string BankCode { get; set; }

        public MopCardInfo(string number, string name, string code)
        {
            CardNumber = number;
            CardHolderName = name;
            BankCode = code;
        }
    }
}
