using testASPWebAPI.Data;

namespace testASPWebAPI
{
    public class DirectDBContext
    {
        private readonly ApplicationDBContext _dbContext;

        public DirectDBContext(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Hose_Delivery getHoseDel(int deliveryID)
        {
            var res = _dbContext.Hose_Delivery.ToList().Where(a=>a.Delivery_ID == deliveryID).First();

            return res;

        }
    }
}
