﻿namespace testASPWebAPI
{
    public class DiscountPresets
    {
        public int presetId { get; set; }
        public string presetName { get; set; }
        public int presetDiscountId { get; set; }
        public int discountType { get; set; }
        public double presetValue { get; set; }

        /*
        public int presetDiscountType { get; set; }
        public int presetItemType { get; set; }
        public int presetItemID { get; set; }
        public string presetStartDate { get; set; }
        public string presetEndtDate { get; set; }

        public bool isBIRDisc { get; set; }*/

        public DiscountPresets(int id, string name, int discID, int discountType, double presetValue)
        {
            presetId = id;
            presetName = name;
            presetDiscountId = discID;
            this.discountType = discountType;
            this.presetValue = presetValue;

        }
    }
}
