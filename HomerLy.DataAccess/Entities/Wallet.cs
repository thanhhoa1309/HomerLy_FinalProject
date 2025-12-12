using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Entities;

namespace EVAuctionTrader.DataAccess.Entities
{
    public class Wallet : BaseEntity
    {
        public Guid UserId { get; set; }
        public decimal Balance { get; set; }

        public Account Account { get; set; }
        public ICollection<WalletTransaction> Transactions { get; set; }
    }
}
