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

        // SendMessageAsync Method
        // Description: This asynchronous method creates a new message, saves it to the database, and returns the created message.
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


        // EditMessageAsync Method
        // Description: This asynchronous method edits an existing message's content in the database.
        public async Task<bool> EditMessageAsync(int messageId, string userId, string newContent)
        {
            var existingMessage = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && (m.SenderId == userId || m.ReceiverId == userId));

            if (existingMessage == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newContent))
            {
                return false; 
            }

            existingMessage.Content = newContent;

            await _context.SaveChangesAsync();

            return true; 
        }

        // DeleteMessageAsync Method
        // Description: This asynchronous method deletes a message from the database based on its ID and the sender's user ID
        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .Where(m => m.Id == messageId && m.SenderId == userId)
                .SingleOrDefaultAsync();

            if (message == null)
            {
                return false; 
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return true; 
        }

        // GetConversationHistoryAsync Method
        // Description: This asynchronous method retrieves a list of messages representing the conversation history between two users based on the provided ConversationRequest object and the current user's ID.
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

        // SearchConversationsAsync Method
        // Description: This asynchronous method searches for messages in conversations involving the specified user that contain a specific query string.
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
