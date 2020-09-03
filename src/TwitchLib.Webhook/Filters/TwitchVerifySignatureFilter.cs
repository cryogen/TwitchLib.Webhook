using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TwitchLib.Webhook.Filters
{
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using Microsoft.AspNetCore.WebUtilities;

    public class TwitchVerifySignatureFilter : WebHookVerifySignatureFilter, IAsyncResourceFilter
    {

        private readonly IConfiguration _config;
        public TwitchVerifySignatureFilter(IConfiguration configuration, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory) 
            : base(configuration, hostingEnvironment, loggerFactory)
        {

            _config = configuration;

        }

        public override string ReceiverName => TwitchConstants.ReceiverName;
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var routeData = context.RouteData;
            var request = context.HttpContext.Request;
            request.EnableBuffering();
            if (routeData.TryGetWebHookReceiverName(out var receiverName) &&
                IsApplicable(receiverName) &&
                HttpMethods.IsPost(request.Method))
            {
                // 1. Confirm a secure connection.
                var errorResult = EnsureSecureConnection(ReceiverName, context.HttpContext.Request);
                if (errorResult != null)
                {
                    context.Result = errorResult;
                    return;
                }

                // 2. Get the expected hash from the signature header.
                var headerValue = GetRequestHeader(request, TwitchConstants.SignatureHeaderName, out errorResult);
                if (errorResult == null)
                {
                    Logger.LogInformation($"{TwitchConstants.SignatureHeaderName} found, performing validation");


                    // comes across in the following format X-Hub-Signature	sha256=8199e7481c3efbf3bd7450ddb6c3599a915e893f2140bba663cb45c5347f4330 
                    // grab only the hex value
                    var fields = headerValue.Split('=');
                    if (fields.Length != 2)
                    {
                        context.Result = new BadRequestObjectResult(headerValue);
                        return;

                    }

                    var header = fields[1];
                    if (string.IsNullOrEmpty(header))
                    {
                        context.Result = new BadRequestObjectResult(fields);
                        return;
                    }

                    var expectedHash = FromHex(header, TwitchConstants.SignatureHeaderName);
                    if (expectedHash == null)
                    {
                        context.Result = CreateBadHexEncodingResult(TwitchConstants.SignatureHeaderName);
                        return;
                    }

                    // 3. Get the configured secret key from appsettings.json.
                    var secretKey = _config.GetSection("Twitch:SecretKey").Value;


                    var secret = Encoding.UTF8.GetBytes(secretKey);

                    // 4. Get the actual hash of the request body.
                    var actualHash = await ComputeRequestBodySha256HashAsyncNew(request, secret, (byte[])null, (byte[])null);

                    // 5. Verify that the actual hash matches the expected hash.
                    if (!SecretEqual(expectedHash, actualHash))
                    {
                        // Log about the issue and short-circuit remainder of the pipeline.
                        errorResult = CreateBadSignatureResult(TwitchConstants.SignatureHeaderName);

                        context.Result = errorResult;
                        return;
                    }


                }
                else
                {
                    // hub.secret is optional, if X-Hub-Signature not found, signature validation will be skipped, just continue
                    Logger.LogInformation($"{TwitchConstants.SignatureHeaderName} not found, skipping validation");

                }

            }

            await next();
        }

        private static async Task PrepareRequestBody(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering(30720);
                await request.Body.DrainAsync(CancellationToken.None);
            }
            request.Body.Seek(0L, SeekOrigin.Begin);
        }

        private async Task<byte[]> ComputeRequestBodySha256HashAsyncNew(
            HttpRequest request,
            byte[] secret,
            byte[] prefix,
            byte[] suffix)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            if (secret.Length == 0)
            {
                throw new ArgumentException();
            }

            await PrepareRequestBody(request);
            byte[] hash;
            using (var hasher = new HMACSHA256(secret))
            {
                try
                {
                    if (prefix != null && prefix.Length != 0)
                        hasher.TransformBlock(prefix, 0, prefix.Length, (byte[])null, 0);
                    var buffer = new byte[4096];
                    var inputStream = request.Body;
                    while (true)
                    {
                        int bytesRead;
                        if ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            hasher.TransformBlock(buffer, 0, bytesRead, (byte[])null, 0);
                        else
                            break;
                    }
                    if (suffix != null && suffix.Length != 0)
                        hasher.TransformBlock(suffix, 0, suffix.Length, (byte[])null, 0);
                    hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    hash = hasher.Hash;
                }
                finally
                {
                    request.Body.Seek(0L, SeekOrigin.Begin);
                }
            }
            return hash;
        }
    }
}
