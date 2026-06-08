using EcomApi.Models;
using EcomApi.Repositories;

namespace EcomApi.Services;

public interface IBotService
{
    Task<(string reply, bool needsEscalation)> ProcessMessageAsync(string message, int conversationId, int? userId, int? orderId = null);
}

public class BotService : IBotService
{
    private readonly IReturnPolicyRepository _returnPolicyRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ISupportRepository _supportRepository;

    public BotService(
        IReturnPolicyRepository returnPolicyRepository,
        IOrderRepository orderRepository,
        ISupportRepository supportRepository)
    {
        _returnPolicyRepository = returnPolicyRepository;
        _orderRepository = orderRepository;
        _supportRepository = supportRepository;
    }

    public async Task<(string reply, bool needsEscalation)> ProcessMessageAsync(
        string message, int conversationId, int? userId, int? orderId = null)
    {
        var lower = message.ToLowerInvariant().Trim();

        var intents = new List<(string[] keywords, Func<string, Task<(string, bool)>> handler)>
        {
            (new[] { "hello", "hi", "hey", "good morning", "good evening", "good afternoon" }, _ =>
                Task.FromResult(("Hello! Welcome to ShopKart Support. How can I help you today? You can ask about:\n\n- Order status & tracking\n- Return policy & process\n- Shipping information\n- Cancellation\n- Or type \"agent\" to speak with a human.", false))),

            (new[] { "agent", "human", "real person", "talk to", "escalate", "customer service", "support agent" }, _ =>
                Task.FromResult(("I've escalated your conversation to our support team. One of our agents will respond shortly. Thank you for your patience!", true))),

            (new[] { "return", "refund", "replace", "exchange", "send back" }, _ =>
                HandleReturnQueryAsync()),

            (new[] { "order", "tracking", "track", "ship", "delivery", "delivered", "shipped" }, _ =>
                HandleOrderQueryAsync(message, userId)),

            (new[] { "cancel", "cancellation" }, _ =>
                Task.FromResult(("To cancel an order:\n\n1. Go to **My Orders** from your account\n2. Find the order you want to cancel\n3. If the order status is **Pending** or **Processing**, you'll see a Cancel button\n4. Once shipped, cancellation may not be possible — you can request a return after delivery instead.\n\nNeed help with a specific order? Tell me your order ID and I'll check!", false))),

            (new[] { "shipping", "shipping charge", "delivery charge", "free shipping", "shipping cost", "shipping time" }, _ =>
                Task.FromResult(("**Shipping Information:**\n\n- **Standard Shipping:** Free on orders above ₹499 (3-5 business days)\n- **Express Shipping:** ₹99 (1-2 business days)\n- **Same-Day Delivery:** Available in select cities\n- **International Shipping:** Not currently available\n\nYou'll receive tracking details once your order is shipped.", false))),

            (new[] { "payment", "pay", "cod", "credit card", "debit card", "upi", "net banking", "wallet" }, _ =>
                Task.FromResult(("**Payment Options:**\n\n- Cash on Delivery (COD)\n- Credit / Debit Cards\n- UPI (Google Pay, PhonePe, Paytm)\n- Net Banking\n- Wallet\n\nAll payments are processed securely. Cash on Delivery is available for orders up to ₹50,000.", false))),

            (new[] { "coupon", "discount", "offer", "promo", "promotion" }, _ =>
                Task.FromResult(("**Coupons & Offers:**\n\n- Apply coupon codes at checkout to get discounts\n- Check the **Offers** section on the homepage for active deals\n- Coupons have minimum cart value requirements and expiry dates\n- Only one coupon can be applied per order\n\nHave a specific coupon code? Enter it at checkout!", false))),

            (new[] { "account", "profile", "password", "login", "sign in", "forgot" }, _ =>
                Task.FromResult(("**Account Help:**\n\n- **Change Password:** Go to My Profile > Change Password\n- **Update Profile:** Edit your name, phone, or email from My Profile\n- **Forgot Password:** Click \"Forgot Password?\" on the login page\n- **Manage Addresses:** Add or edit addresses from your Address Book in Profile\n\nFor security concerns, please contact support.", false))),
        };

        foreach (var (keywords, handler) in intents)
        {
            if (keywords.Any(k => lower.Contains(k)))
            {
                return await handler(message);
            }
        }

        return ("I'm not sure I understand. Could you please rephrase? You can ask about orders, returns, shipping, payments, or type \"agent\" to speak with a human.", false);
    }

    private async Task<(string reply, bool needsEscalation)> HandleReturnQueryAsync()
    {
        var policy = await _returnPolicyRepository.GetAsync();
        if (policy == null || !policy.IsActive)
            return ("Returns are currently not available. Please contact support for assistance.", true);

        return ($"**Return Policy:**\n\n" +
                $"• **Return Window:** You can return items within **{policy.ReturnWindowDays} days** of delivery\n" +
                $"• Items must be unused, in original packaging with all tags intact\n" +
                $"• For electronics, all accessories and manuals must be included\n\n" +
                $"**How to initiate a return:**\n" +
                $"1. Go to **My Orders** > click on the delivered order\n" +
                $"2. Click **Request Return** and select the reason\n" +
                $"3. Once approved, you'll receive return shipping instructions\n" +
                $"4. Refund is processed within **5-7 business days** after we receive the item\n\n" +
                $"For the full policy details, visit our Return Policy page.", false);
    }

    private async Task<(string reply, bool needsEscalation)> HandleOrderQueryAsync(string message, int? userId)
    {
        if (userId == null)
            return ("Please log in to check your order status. Once logged in, go to **My Orders** to view all your orders.", false);

        var orderIdStr = ExtractOrderId(message);
        if (orderIdStr != null)
        {
            if (int.TryParse(orderIdStr, out var orderId))
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order != null && order.UserId == userId)
                {
                    var items = string.Join(", ", order.Items.Select(i => i.ProductName).Take(3));
                    if (order.Items.Count > 3) items += $" and {order.Items.Count - 3} more";

                    return ($"**Order #{order.Id}**\n\n" +
                            $"• **Status:** {order.Status}\n" +
                            $"• **Items:** {items}\n" +
                            $"• **Total:** ₹{order.TotalAmount:N0}\n" +
                            $"• **Date:** {order.CreatedAt:dd MMM yyyy}\n" +
                            (order.TrackingNumber != null ? $"• **Tracking:** {order.TrackingNumber} ({order.Carrier})\n" : "") +
                            (order.EstimatedDeliveryDate != null ? $"• **Est. Delivery:** {order.EstimatedDeliveryDate:dd MMM yyyy}\n" : "") +
                            (order.ActualDeliveryDate != null ? $"• **Delivered:** {order.ActualDeliveryDate:dd MMM yyyy}\n" : "") +
                            $"\nFor more details, visit **My Orders** in your account.", false);
                }
                return ("I couldn't find an order with that ID associated with your account. Please check the order number and try again.", false);
            }
        }

        return ("To check your order, please visit **My Orders** in your account. If you have a specific order number, tell me and I'll look it up!", false);
    }

    private static string? ExtractOrderId(string message)
    {
        var patterns = new[] { "#{0}", "order {0}", "order #{0}", "id {0}", "number {0}" };
        foreach (var word in message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = word.TrimStart('#');
            if (int.TryParse(cleaned, out _) && cleaned.Length >= 3)
                return cleaned;
        }
        return null;
    }
}
