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

        // InvokeAsync Method
        // Description: This middleware method logs incoming requests along with IP address, username (if available),
        // request timestamp, and request body. The logged information is then stored in a database.
        // The method also passes the incoming request to the next middleware in the pipeline.
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
 
            if (userName==null)
            {
                userName = "";
            }

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

        // getRequestBodyAsync Method
        // Description: This asynchronous method reads the request body content and returns it as a string.
        // It also ensures that the request body stream is rewound to its original position after reading.
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