namespace testASPWebAPI
{
    public class TransactionItemClass
    {
        public int itemNumber { get; set; }
        public int itemType { get; set; }
        public string itemDesc { get; set; }
        public double itemPrice { get; set; }
        public double itemQTY { get; set; }
        public double itemValue { get; set; }
        public int itemID { get; set; }
        public double itemTaxAmount { get; set; }
        public int deliveryID { get; set; }
        public int itemTaxID { get; set; }
        public string gcNumber { get; set; }
        public string gcAmount { get; set; }
        public double? originalItemValuePreTaxChange { get; set; }
        public int isTaxExemptItem { get; set; }
        public int isZeroRatedTaxItem { get; set; }
        public double itemDiscTotal { get; set; }
        public int departmentID { get; set; }
        public string itemDiscCodeType { get; set; }
        public double? itemDBPrice { get; set; }
        public string itemGCNumber { get; set; }
        public int discountPresetID { get; set; }
        public int? pumpID { get; set; }
       

        public TransactionItemClass(int itemNumber, int itemType, string itemDesc, double itemPrice, double itemQTY, double itemValue, int itemID, double itemTaxAmount, int deliveryID, int itemTaxID, string gcNumber, string gcAmount,
            double? originalItemValuePreTaxChange, int isTaxExemptItem, int isZeroRatedTaxItem, double itemDiscTotal, int departmentID, string itemDiscCodeType, double? itemDBPrice, string itemGCNumber,
            int discountPresetID, int? pumpID)
        {
            this.itemNumber = itemNumber;
            this.itemType = itemType;
            this.itemDesc = itemDesc;
            this.itemPrice = itemPrice;
            this.itemQTY = itemQTY;
            this.itemValue = itemValue;
            this.itemID = itemID;
            this.itemTaxAmount = itemTaxAmount;
            this.deliveryID = deliveryID;
            this.itemTaxID = itemTaxID;
            this.gcNumber = gcNumber;
            this.gcAmount = gcAmount;
            this.originalItemValuePreTaxChange = originalItemValuePreTaxChange;
            this.isTaxExemptItem = isTaxExemptItem;
            this.isZeroRatedTaxItem = isZeroRatedTaxItem;
            this.itemDiscTotal = itemDiscTotal;
            this.departmentID = departmentID;
            this.itemDiscCodeType = itemDiscCodeType;
            this.itemDBPrice = itemDBPrice;
            this.itemGCNumber = itemGCNumber;
            this.discountPresetID = discountPresetID;
            this.pumpID = pumpID;
        }

        public void ApplyDiscount(int id, int type, double value)
        {
            double discValue = GetDiscountValue(type, value);
            originalItemValuePreTaxChange = itemValue;
            itemDBPrice = itemPrice;

            discountPresetID = id;
            itemDiscTotal = discValue;
        }

        private double GetDiscountValue(int type, double value)
        {
            double discountValuue = 0;
            switch (type)
            {
                case 1:
                    {
                        double percentDiscValue = value / 100;
                        discountValuue = itemValue * percentDiscValue;
                    }
                    break;
                case 2:
                    {
                        discountValuue = itemQTY * value;
                    }
                    break;
                case 3:
                    {
                        discountValuue = value;
                    }
                    break;
                default:
                    {

                    }
                    break;
            }

            return discountValuue;
        }


    }
}
