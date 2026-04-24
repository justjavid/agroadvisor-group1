using System.Net;

namespace Service.Handlers
{
    public class SimpleRetryHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int maxAttempts = 3;
            int delay = 1000;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return response;

                if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503 || (int)response.StatusCode >= 500)
                {
                    if (attempt == maxAttempts)
                        return response;

                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                    continue;
                }

                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "Retry attempts exhausted"
            };
        }
    }
}
