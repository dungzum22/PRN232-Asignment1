using Microsoft.AspNetCore.Mvc;
using ShopNew.Services;
using Stripe;
using System.Text;

namespace ShopNew.Controllers
{
    [ApiController]
    [Route("webhook/stripe")]
    public class WebhookController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly OrderService _orderService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(PaymentService paymentService, OrderService orderService, ILogger<WebhookController> logger)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _paymentService.GetWebhookSecret()
                );

                _logger.LogInformation($"Received webhook event: {stripeEvent.Type}");

                switch (stripeEvent.Type)
                {
                    case Events.PaymentIntentSucceeded:
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;
                    case Events.PaymentIntentPaymentFailed:
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;
                    case Events.PaymentIntentCanceled:
                        await HandlePaymentIntentCanceled(stripeEvent);
                        break;
                    default:
                        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error: {ex.Message}");
                return BadRequest();
            }
        }

        private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            _logger.LogInformation($"Payment succeeded: {paymentIntent.Id}");

            var orders = await _orderService.GetAllOrdersAsync();
            var order = orders.FirstOrDefault(o => o.PaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.Status = "Paid";
                order.PaymentStatus = "succeeded";
                await _orderService.UpdateOrderAsync(order.Id, order);
                await _orderService.UpdateProductStockAsync(order);
                _logger.LogInformation($"Order {order.Id} marked as paid");
            }
        }

        private async Task HandlePaymentIntentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            _logger.LogWarning($"Payment failed: {paymentIntent.Id}");

            var orders = await _orderService.GetAllOrdersAsync();
            var order = orders.FirstOrDefault(o => o.PaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.Status = "Failed";
                order.PaymentStatus = "failed";
                await _orderService.UpdateOrderAsync(order.Id, order);
                _logger.LogInformation($"Order {order.Id} marked as failed");
            }
        }

        private async Task HandlePaymentIntentCanceled(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            _logger.LogInformation($"Payment canceled: {paymentIntent.Id}");

            var orders = await _orderService.GetAllOrdersAsync();
            var order = orders.FirstOrDefault(o => o.PaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.Status = "Canceled";
                order.PaymentStatus = "canceled";
                await _orderService.UpdateOrderAsync(order.Id, order);
                _logger.LogInformation($"Order {order.Id} marked as canceled");
            }
        }
    }
}
