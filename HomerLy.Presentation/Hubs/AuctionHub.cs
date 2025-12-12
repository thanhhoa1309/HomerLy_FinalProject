using Microsoft.AspNetCore.SignalR;

namespace Homerly.Presentation.Hubs;

public sealed class AuctionHub : Hub
{
    public async Task JoinAuction(string auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction_{auctionId}");
        await Clients.Group($"auction_{auctionId}").SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveAuction(string auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction_{auctionId}");
        await Clients.Group($"auction_{auctionId}").SendAsync("UserLeft", Context.ConnectionId);
    }

    public async Task NotifyNewBid(string auctionId, string bidderName, decimal amount, string timestamp)
    {
        await Clients.Group($"auction_{auctionId}").SendAsync("ReceiveBid", new
        {
            bidderName,
            amount,
            timestamp
        });
    }

    public async Task NotifyAuctionUpdate(string auctionId, string status, decimal currentPrice)
    {
        await Clients.Group($"auction_{auctionId}").SendAsync("AuctionUpdated", new
        {
            status,
            currentPrice
        });
    }
}
