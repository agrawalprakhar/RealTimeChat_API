using Microsoft.EntityFrameworkCore;
using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly RealTimeChatContext _context;

        public MessageRepository(RealTimeChatContext db) : base(db)
        {
            _context = db;
        }


        public async Task<Message> SendMessageAsync(string senderId, string receiverId, string content)
        {
            var message = new Message
            {  
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }



        public async Task<bool> EditMessageAsync(int messageId, string userId, string newContent)
        {
            var existingMessage = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && (m.SenderId == userId || m.ReceiverId == userId));

            if (existingMessage == null)
            {
                return false; // Message not found or unauthorized access
            }

            // Validate the request content
            if (string.IsNullOrWhiteSpace(newContent))
            {
                return false; // Invalid message content
            }

            // Update the message content
            existingMessage.Content = newContent;

            // Save the changes to the database
            await _context.SaveChangesAsync();

            return true; // Message edited successfully
        }

        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .Where(m => m.Id == messageId && m.SenderId == userId)
                .SingleOrDefaultAsync();

            if (message == null)
            {
                return false; // Message not found or unauthorized access
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return true; // Message deleted successfully
        }

        public async Task<List<Message>> GetConversationHistoryAsync(ConversationRequest request, string currentUserId)
        {
            try
            {
                var query = _context.Messages
                    .Where(m => ((m.SenderId == currentUserId && m.ReceiverId == request.UserId)
                                 || (m.SenderId == request.UserId && m.ReceiverId == currentUserId)) &&
                                (!request.Before.HasValue || m.Timestamp < request.Before))
                    .AsQueryable();

                if (request.Sort == "asc")
                {
                    query = query.OrderBy(m => m.Timestamp);
                }
                else if (request.Sort == "desc")
                {
                    query = query.OrderByDescending(m => m.Timestamp);
                }

                var messages = await query
                    .Take(request.Count)
                    .ToListAsync();

                return messages;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Message>> SearchConversationsAsync(string userId, string query)
        {
            var searchedConversation = await _context.Messages
              .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && m.Content.Contains(query))
              .ToListAsync();
           

            var message = searchedConversation.Select(message => new Message
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Timestamp = message.Timestamp
            }).ToList();

            return message;
        }



    }
}
