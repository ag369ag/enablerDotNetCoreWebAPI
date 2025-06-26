namespace testASPWebAPI
{
    public class CFFLS
    {
        public HEADER HEADER { get;set; }
        public DETAIL DETAIL { get;set; }
        
    }

    public class DETAIL
    {
        public DETAILITEM ITEM { get; set; }
    }

    public class HEADER
    {
        public string STNCODE { get; set; }
        public string CARDSERIALNO { get; set; }
        public string REFNO { get; set; }
    }

    public class DETAILITEM
    {
        public string ACCOUNTNO { get; set; }
        public string ACCOUNTNAME { get; set; }
        public string DISCAMT { get; set; }
        public string PREPAIDBALANCE { get; set; }
        public string PRODUCT { get; set; }
        public string TIMESTAMP { get; set; }
        public string DISCGROUP { get; set; }
        public string POINTSBALANCE { get; set; }
        public string CARDTYPE { get; set; }
        public string ENTITYNAME { get; set; }
        public string ENTITYCODE { get; set; }
        public string FLEETBALANCE { get; set; }
        public string CREDITLIMIT { get; set; }
        public string LITERSLIMIT { get; set; }
        public string REMARKS { get; set; }
        public string MOPTYPE { get; set; }
        public string MOPAMT { get; set; }
        public string PAEMPID { get; set; }
        public string CUSADD { get; set; }
        public string TINNO { get; set; }
        public string FLTUPRICE { get; set; }

    }
}
