﻿using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses.Negotiation;

namespace HttpProblemDetails.Nancy
{
    public static class HttpProblemDetails
    {
        private static string GetContentTypeForContext(NancyContext context)
        {
            var mediaRanges = context.Request.Headers.Accept
                .Select(x => x.Item1)
                .ToArray();

            if (mediaRanges.Any(x => x == "application/json"))
            {
                return "application/problem+json";
            }

            if (mediaRanges.Any(x => x == "application/xml"))
            {
                return "application/problem+xml";
            }

            return mediaRanges.FirstOrDefault();
        }

        /// <summary>
        /// Enable HttpProblemDetails support in the application
        /// </summary>
        /// <param name="pipelines">Application pipeline to hook into</param>
        /// <param name="responseNegotiator">An <see cref="IResponseNegotiator"/> instance.</param>
        public static void Enable(IPipelines pipelines, IResponseNegotiator responseNegotiator)
        {
            var httpProblemDetailsEnabled = pipelines.AfterRequest.PipelineItems.Any(ctx => ctx.Name == nameof(HttpProblemDetails));

            if (!httpProblemDetailsEnabled)
            {
                pipelines.OnError.AddItemToEndOfPipeline((context, exception) =>
                {
                    var ex = HttpProblemDetailException.FromException(exception);
                    if (ex == null)
                    {
                        return context.Response;
                    }

                    var negotiator = new Negotiator(context)
                        .WithContentType(GetContentTypeForContext(context))
                        .WithStatusCode(ex?.ProblemDetail?.Status ?? 0)
                        .WithModel(ex.ProblemDetail);

                    return responseNegotiator.NegotiateResponse(negotiator, context);
                });
            }
        }

        /// <summary>
        /// Disable HttpProblemDetails support in the application
        /// </summary>
        /// <param name="pipelines">Application pipeline to hook into</param>
        public static void Disable(IPipelines pipelines)
        {
            pipelines.AfterRequest.RemoveByName(nameof(HttpProblemDetails));
        }
    }
}
