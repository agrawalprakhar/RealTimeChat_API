using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.DAL.Repository;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using RealTimeChatAPI.Hubs;
using System.Security.Claims;

namespace RealTimeChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository ;

        private readonly IHubContext<ChatHub> _chatHub;

        public MessageController(IMessageRepository messageRepository, IHubContext<ChatHub> chatHub)
        {
            _messageRepository = messageRepository;
            _chatHub = chatHub;
        }


        [HttpPost]
        public async Task<ActionResult<sendMessageResponse>> SendMessages(sendMessageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Message sending failed due to validation errors." });
            }

            var senderId = GetCurrentUserId(); // Implement this method to get the current user's ID.

            var message = await _messageRepository.SendMessageAsync(senderId, request.ReceiverId, request.Content);

            var response = new sendMessageResponse
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Timestamp = message.Timestamp
            };
            await _chatHub.Clients.All.SendAsync("ReceiveMessage", response);

            return Ok(response);
        }

        private string GetCurrentUserId()
        {
            var currentUser = HttpContext.User;
            var currentUserId = (currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return currentUserId;
        }

        [HttpPut("{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessage editMessage)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new { message = "Unauthorized access" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request parameter." });
            }

            var edited = await _messageRepository.EditMessageAsync(messageId, userId, editMessage.Content);

            if (!edited)
            {
                return NotFound(new { error = "Message not found or you are not allowed to edit it." });
            }

            await _chatHub.Clients.All.SendAsync("ReceiveEditedMessage", messageId, editMessage.Content);

            return Ok(new
            {
                success = true,
                message = "Message edited successfully",
                editedMessage = new
                {
                    messageId,
                    senderId = userId,
                 
                    content = editMessage.Content,
                    timestamp = DateTime.Now // You can update the timestamp here if needed
                }
            });
        }


        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new { message = "Unauthorized access" });
            }

            var deleted = await _messageRepository.DeleteMessageAsync(messageId, userId);

            if (!deleted)
            {
                return NotFound(new { message = "Message not found or you are not allowed to delete it." });
            }

            await _chatHub.Clients.All.SendAsync("ReceiveDeletedMessage", messageId);

            return Ok(new { message = "Message deleted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetConversationHistory([FromQuery] ConversationRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                if (currentUserId == null)
                {
                    return Unauthorized();
                }

                if (request.UserId == null)
                {
                    return NotFound(new { error = "Receiver user not found" });
                }

                var messages = await _messageRepository.GetConversationHistoryAsync(request, currentUserId);

                if (messages.Count == 0)
                {
                    return NotFound("Conversation not found");
                }

                return Ok(new { messages });
            }
            catch (Exception ex)
            {
                return BadRequest($"Bad Request: {ex.Message}");
            }
        }

        

        [HttpGet("search")]
        public async Task<IActionResult> SearchConversations([FromQuery] string query)
        {
            
            
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized(new 
                    {
                      
                        Message = "Unauthorized access",
                        
                    });
                }

                var searchResult = await _messageRepository.SearchConversationsAsync(userId, query);

                if (searchResult != null && searchResult.Any())
                {
                    return Ok(new 
                    {
                   
                        Message = "Conversation searched successfully",
                        Data = searchResult
                    });
                }
                else
                {
                    return BadRequest(new 
                    {
                        
                        Message = " Invalid request parameters",
                        
                    });
                }
            
            
        }

    }
}
