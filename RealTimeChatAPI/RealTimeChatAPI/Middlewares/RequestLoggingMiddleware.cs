using RealTimeChat.DAL.Data;
using RealTimeChat.Domain.Models;
using System.Security.Claims;
using System.Text;


namespace MinimalChatApplication.Middlewares
{
    public class RequestLoggingMiddleware : IMiddleware
    {
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly RealTimeChatContext _dbcontext;

        public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger, RealTimeChatContext dbcontext)
        {
            _logger = logger;
            _dbcontext = dbcontext;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
            //Console.WriteLine(context.User);

            string IP = context.Connection.RemoteIpAddress?.ToString();
            string RequestBody = await getRequestBodyAsync(context.Request);
            DateTime TimeStamp = DateTime.Now;
            string Username = userName;


            string log = $"IP: {IP}, Username: {Username}, Timestamp: {TimeStamp}, Request Body: {RequestBody}";

            _logger.LogInformation(log);

            _dbcontext.Logs.Add(new Logs
            {
                IP = IP,
                RequestBody = RequestBody,
                Timestamp = TimeStamp,
                Username = Username,
            });

            await _dbcontext.SaveChangesAsync();

            await next(context);
        }
        public async Task<string> getRequestBodyAsync(HttpRequest req)
        {
            req.EnableBuffering();

            using var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            string requestBody = await reader.ReadToEndAsync();

            req.Body.Position = 0;

            return requestBody;
        }
    }

}