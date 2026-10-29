using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EShop.Web.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true ||
            Context.User?.IsInRole("SuperAdmin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User?.IsInRole("Admin") == true ||
            Context.User?.IsInRole("SuperAdmin") == true)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
        }
        await base.OnDisconnectedAsync(exception);
    }
}