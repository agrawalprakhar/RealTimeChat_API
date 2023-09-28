using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository.IRepository
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<Message> SendMessageAsync(string senderId, string receiverId, string content);

        Task<bool> EditMessageAsync(int messageId, string userId, string newContent);

        Task<bool> DeleteMessageAsync(int messageId, string userId);

        Task<List<Message>> GetConversationHistoryAsync(ConversationRequest request, string currentUserId);

        Task<IEnumerable<Message>> SearchConversationsAsync(string userId, string query);
    }
}
