using Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using RealTimeChat.DAL.Data;
using RealTimeChat.Domain.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Message = RealTimeChat.Domain.Models.Message;

namespace RealTimeChatAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly RealTimeChatContext _context;


        private static readonly Dictionary<string, DateTime> LastSeenTimestamps = new Dictionary<string, DateTime>();
        private static readonly List<string> ConnectedUsers = new List<string>();
        public ChatHub(RealTimeChatContext context)
        {
            _context = context;
        }

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

        public async Task SendUserStatus(string userId, string status)
        {
            await Clients.All.SendAsync("ReceiveUserStatus", userId, status);
        }

        private static int Count = 0;

        public override async Task OnConnectedAsync()
        {
            //string userId = Context.UserIdentifier;
            var user = GetUserName();


            if (!ConnectedUsers.Contains(user))
            {
                ConnectedUsers.Add(user);
                // Retrieve last seen timestamp from the database
                // Fetch last seen timestamps for all users from the database
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

                //LastSeenTimestamps[user] = DateTime.UtcNow;
      

                Count++;
                await base.OnConnectedAsync();

            // Notify the connected client about its user identifier and the updated count
                await Clients.Caller.SendAsync("SetUserIdentifier", user);
                await Clients.All.SendAsync("updateCount", Count);
                await Clients.All.SendAsync("ReceiveConnectedUsers", ConnectedUsers);
                await Clients.All.SendAsync("ReceiveLastSeenTimestamps", allLastSeenRecords);
            }
        }
    

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user= GetUserName();

            if (ConnectedUsers.Contains(user))
            {
                ConnectedUsers.Remove(user);

                // Update last seen timestamp in the database
                var lastSeenRecord = await _context.LastSeenRecords.FirstOrDefaultAsync(x => x.UserId == user);
                if (lastSeenRecord != null)
                {
                    lastSeenRecord.Timestamp = DateTime.Now;
                }
                else
                {
                    _context.LastSeenRecords.Add(new LastSeen { UserId = user, Timestamp = DateTime.Now });
                }

                //// Update last seen timestamp for the disconnected user
                //LastSeenTimestamps[user] = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var allLastSeenRecords = await _context.LastSeenRecords.ToDictionaryAsync(x => x.UserId, x => x.Timestamp);

                Count--;
                await base.OnDisconnectedAsync(exception);
                await Clients.All.SendAsync("updateCount", Count);
                await Clients.All.SendAsync("ReceiveConnectedUsers", ConnectedUsers);
                await Clients.All.SendAsync("ReceiveLastSeenTimestamps", allLastSeenRecords);
            }
        }
   

        public async Task SendTypingIndicator(string userId, string receiverId , bool isTyping)
        {
            await Clients.Others.SendAsync("ReceiveTypingIndicator", userId,receiverId, isTyping);
        }



      
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
