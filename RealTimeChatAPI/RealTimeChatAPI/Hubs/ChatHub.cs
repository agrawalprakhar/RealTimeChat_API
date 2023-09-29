using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Domain.Models;

namespace RealTimeChatAPI.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(Message message)
        {
            // Store the message in the database

            // Send the message to the intended recipient
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public async Task EditMessage(int messageId, string content)
        {
            await Clients.All.SendAsync("ReceiveEditedMessage", messageId, content);
        }

        public async Task DeleteMessage(int messageId)
        {
            await Clients.All.SendAsync("ReceiveDeletedMessage", messageId);
        }
    }
}
