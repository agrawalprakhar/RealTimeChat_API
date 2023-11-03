using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

        // Send Messages Method
        // Description: This HTTP POST method allows users to send messages. It receives a request containing the receiver's ID and the 
        // message content. The method validates the request, sends the message to the specified receiver, and broadcasts the sent message 
        // to all connected clients using SignalR.
        // This method is accessible via a POST request to the corresponding route.
        [HttpPost("/api/messages")]
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
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                Timestamp = message.Timestamp
            };
            await _chatHub.Clients.All.SendAsync("ReceiveMessage", response);
            return Ok(response);
        }

        // GetCurrentUserId Method
        // Description: This method retrieves the unique identifier (ID) of the currently authenticated user from the HttpContext. 
        // It accesses the user's claims to find the claim with the type "NameIdentifier," which typically stores the user's ID.
        // The method returns the current user's ID as a string.
        private string GetCurrentUserId()
        {
            var currentUser = HttpContext.User;
            var currentUserId = (currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return currentUserId;
        }

        // EditMessage Method
        // Description: This method handles the PUT request to edit a specific message. It verifies the current user's authorization,
        // validates the request parameters, edits the message using the message repository, and broadcasts the edited message to all clients.
        [HttpPut("/api/messages/{messageId}")]
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
                    timestamp = DateTime.Now 
                }
            });
        }

        // DeleteMessage Method
        // Description: This method handles the DELETE request to delete a specific message. It verifies the current user's authorization,
        // attempts to delete the message using the message repository, and broadcasts the deleted message ID to all clients.
        [HttpDelete("/api/messages/{messageId}")]
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


        // GetConversationHistory Method
        // Description: This method handles the GET request to retrieve the conversation history between the current user and another user.
        // It verifies the current user's authorization, retrieves the conversation history using the message repository,
        // and returns the messages as a response.
        [HttpGet("/api/messages")]
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


        // SearchConversations Method
        // Description: This method handles the GET request to search for conversations based on a query string.
        // It verifies the current user's authorization, performs the search operation using the message repository,
        // and returns the search results as a response.
        [HttpGet("/api/conversation/search")]
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
                return Ok(new { searchResult });
                }
                else
                {
                    return BadRequest(new 
                    {
                        
                        Message = " Invalid request parameters",
                        
                    });
                }
        }

        [HttpPost("markasread/{messageId}")]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            try
            {
                await _messageRepository.MarkMessageAsRead(messageId);

                await _chatHub.Clients.All.SendAsync("ReceiveNotification", "You have a new read message!");

                await _chatHub.Clients.All.SendAsync("MessagesMarkedAsRead", messageId);

                return Ok(new { message = "Message marked as read successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, new { error = "An error occurred while marking the message as read." });
            }
        }

        [HttpPut("read")]
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] int[] array)
        {
                await _messageRepository.MarkMessagesAsRead(array);

              // Notify connected clients about the updated message state
                await _chatHub.Clients.All.SendAsync("MessagesRead", array);

                return new OkObjectResult(new { message = "Messages marked as read successfully" });

        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetAllUnReadMessages()
        {
            var authenticatedUser =  GetCurrentUserId();

            return await _messageRepository.GetAllUnReadMessages(authenticatedUser);

 
        }

    }
}
