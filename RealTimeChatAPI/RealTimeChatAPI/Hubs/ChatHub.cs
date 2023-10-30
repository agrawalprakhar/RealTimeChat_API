using Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using RealTimeChat.DAL.Data;
using RealTimeChat.Domain.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Message = RealTimeChat.Domain.Models.Message;

namespace RealTimeChatAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly RealTimeChatContext _context;
        private static int Count = 0;
        private static readonly Dictionary<string, DateTime> LastSeenTimestamps = new Dictionary<string, DateTime>();
        private static readonly List<string> ConnectedUsers = new List<string>();

        public ChatHub(RealTimeChatContext context)
        {
            _context = context;
        }

        // SendMessage Method
        // Description: This method in an ASP.NET SignalR hub sends a message to all connected clients.
        // It takes a Message object as a parameter and asynchronously sends the message  using the "ReceiveMessage" method.
        public async Task SendMessage(Message message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }


        // EditMessage Method
        // Description: This method in an ASP.NET SignalR hub is used to broadcast edited message content to all connected clients.
        // It takes a messageId (int) and content (string) as parameters, indicating the edited message ID and its updated content respectively.
        public async Task EditMessage(int messageId, string content)
        {
            await Clients.All.SendAsync("ReceiveEditedMessage", messageId, content);
        }

        // DeleteMessage Method
        // Description: This method in an ASP.NET SignalR hub is used to broadcast deleted message ID.
        // It takes a messageId (int) as a parameter, indicating the ID of the message to be deleted.
        public async Task DeleteMessage(int messageId)
        {
            await Clients.All.SendAsync("ReceiveDeletedMessage", messageId);
        }

        // SendUserStatus Method
        // Description: This method in an ASP.NET SignalR hub is used to broadcast user status updates to all connected clients.
        // It takes two parameters: userId (string) representing the user's ID, and status (string) indicating the user's status message.
        public async Task SendUserStatus(string userId, string status)
        {
            await Clients.All.SendAsync("ReceiveUserStatus", userId, status);
        }

        // OnConnectedAsync Method Override
        // Description: This method is an override of the OnConnectedAsync method in an ASP.NET SignalR hub.
        // It is called when a new client is connected to the hub. This method initializes and updates user-related data,
        // such as adding the user to the list of connected users, updating last seen timestamps, and broadcasting these updates to all clients.
        public override async Task OnConnectedAsync()
        {
            var user = GetUserName();
            if (!ConnectedUsers.Contains(user))
            {
                ConnectedUsers.Add(user);
                var allLastSeenRecords = await _context.LastSeenRecords.ToDictionaryAsync(x => x.UserId, x => x.Timestamp);
                var lastSeenRecord = await _context.LastSeenRecords.FirstOrDefaultAsync(x => x.UserId == user);

                if (lastSeenRecord != null)
                {
                    LastSeenTimestamps[user] = lastSeenRecord.Timestamp;
                }
                else
                {
                    _context.LastSeenRecords.Add(new LastSeen { UserId = user, Timestamp = DateTime.Now });
                }
      
                Count++;
                await base.OnConnectedAsync();
                await Clients.Caller.SendAsync("SetUserIdentifier", user);
                await Clients.All.SendAsync("updateCount", Count);
                await Clients.All.SendAsync("ReceiveConnectedUsers", ConnectedUsers);
                await Clients.All.SendAsync("ReceiveLastSeenTimestamps", allLastSeenRecords);
            }
        }

        // OnDisconnectedAsync Method Override
        // Description: This method is an override of the OnDisconnectedAsync method in an ASP.NET SignalR hub.
        // It is called when a client disconnects from the hub. This method handles user removal from the list of connected users,
        // updates last seen timestamps, and broadcasts these updates to all clients.
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user= GetUserName();

            if (ConnectedUsers.Contains(user))
            {
                ConnectedUsers.Remove(user);
                var lastSeenRecord = await _context.LastSeenRecords.FirstOrDefaultAsync(x => x.UserId == user);
                if (lastSeenRecord != null)
                {
                    lastSeenRecord.Timestamp = DateTime.Now;
                }
                else
                {
                    _context.LastSeenRecords.Add(new LastSeen { UserId = user, Timestamp = DateTime.Now });
                }
                await _context.SaveChangesAsync();
                var allLastSeenRecords = await _context.LastSeenRecords.ToDictionaryAsync(x => x.UserId, x => x.Timestamp);

                Count--;
                await base.OnDisconnectedAsync(exception);
                await Clients.All.SendAsync("updateCount", Count);
                await Clients.All.SendAsync("ReceiveConnectedUsers", ConnectedUsers);
                await Clients.All.SendAsync("ReceiveLastSeenTimestamps", allLastSeenRecords);
            }
        }

        // SendTypingIndicator Method
        // Description: This method sends a typing indicator to other clients in the SignalR hub.
        // It broadcasts the typing status of a user (specified by userId) to the receiver (specified by receiverId).
        // The boolean parameter isTyping indicates whether the user is typing or has stopped typing.
        public async Task SendTypingIndicator(string userId, string receiverId , bool isTyping)
        {
            await Clients.Others.SendAsync("ReceiveTypingIndicator", userId,receiverId, isTyping);
        }

        // GetReceiverStatus Method
        // Description: This method retrieves the status message of a specified user from the database and sends it to the caller.
        // It is used to obtain the status of a user (specified by userId) and broadcast it to the caller.
        public async Task GetReceiverStatus(string userId)
        {
            // Get the status of the specified user from the database
            var status = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.StatusMessage)
                .FirstOrDefaultAsync();
            // Send the status to the caller
            await Clients.Others.SendAsync("ReceiveReceiverStatus", userId, status);
        }

        // GetUserName Method
        // Description: This private method extracts the user ID from an access token in the query string of an HTTP request.
        // It is used to retrieve the user ID from the JWT token in the query string, enabling authentication and authorization.
        private string GetUserName()
        {
            var query = Context.GetHttpContext().Request.Query;
            var token = query["access_token"];

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Missing access_token in query string");
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

            if (userIdClaim == null)
            {
                throw new InvalidOperationException("User ID claim not found in JWT token");
            }
            var userId = userIdClaim.Value;

            return userId;
        }

        // GetUserId Method
        // Description: This private method extracts the user ID from an access token in the query string of an HTTP request.
        // It is used to retrieve the user ID from the JWT token in the query string, enabling authentication and authorization.
        private string GetUserId()
        {
            var query = Context.GetHttpContext().Request.Query;
            var token = query["access_token"];

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Missing access_token in query string");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                throw new InvalidOperationException("User ID claim not found in JWT token");
            }

            var userId = userIdClaim.Value;

            return userId;
        }

    }
}
