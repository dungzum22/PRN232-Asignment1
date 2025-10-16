using Stripe;
using Stripe.Checkout;
using ShopNew.Models;

namespace ShopNew.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _configuration;

        public PaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<Session> CreateCheckoutSessionAsync(decimal amount, string currency = "usd", string? orderId = null)
        {
            var service = new SessionService();
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5276";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amount * 100), // Convert to cents
                            Currency = currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Order Purchase",
                                Description = $"Order ID: {orderId}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{baseUrl}/Order/OrderSuccess?session_id={{CHECKOUT_SESSION_ID}}&orderId=" + orderId,
                CancelUrl = $"{baseUrl}/Order/Checkout",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", orderId ?? "" }
                }
            };

            return await service.CreateAsync(options);
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            var service = new SessionService();
            return await service.GetAsync(sessionId);
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "source", "shopnew_ecommerce" }
                }
            };

            return await service.CreateAsync(options);
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId);
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
                return stripeEvent != null;
            }
            catch
            {
                return false;
            }
        }

        public string GetPublishableKey()
        {
            return _configuration["Stripe:PublishableKey"] ?? "";
        }

        public string GetWebhookSecret()
        {
            return _configuration["Stripe:WebhookSecret"] ?? "";
        }
    }
}
