namespace testASPWebAPI
{
    public class DiscountPresets
    {
        public int presetId { get; set; }
        public string presetName { get; set; }
        public int presetDiscountId { get; set; }
        /*public int presetValue { get; set; }
        public int presetDiscountType { get; set; }
        public int presetItemType { get; set; }
        public int presetItemID { get; set; }
        public string presetStartDate { get; set; }
        public string presetEndtDate { get; set; }

        public bool isBIRDisc { get; set; }*/

        public DiscountPresets(int id, string name, int discID)
        {
            presetId = id;
            presetName = name;
            presetDiscountId = discID;
        }
    }
}
