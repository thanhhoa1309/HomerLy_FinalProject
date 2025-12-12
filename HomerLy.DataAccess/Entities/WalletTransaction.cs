using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;

namespace EVAuctionTrader.DataAccess.Entities
{
    public class WalletTransaction : BaseEntity
    {
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public decimal? BalanceAfter { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public Guid? PostId { get; set; }
        public Guid? AuctionId { get; set; }

        public Wallet Wallet { get; set; }

    }
}
