using System.Collections.Concurrent;
using System.Text;

public class OrderTakingAI
{
    private readonly RestaurantApiService _apiService;
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();

    public OrderTakingAI(RestaurantApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<string> ProcessMessageAsync(string sessionId, string userMessage)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            session = new ChatSession { SessionId = sessionId };
            _sessions[sessionId] = session;
            return await StartConversationAsync(session);
        }

        userMessage = userMessage.Trim().ToLowerInvariant();

        // Handle completed conversation: wait for a new greeting to restart
        if (session.Step == ConversationStep.Completed)
        {
            if (IsGreeting(userMessage))
            {
                ResetSession(session);
                return await StartConversationAsync(session);
            }
            else
            {
                return "Would you like to place a new order? Type 'hi' to start.";
            }
        }

        switch (session.Step)
        {
            case ConversationStep.Initial:
                return await StartConversationAsync(session);
            case ConversationStep.TableNumber:
                return await HandleTableNumberAsync(session, userMessage);
            case ConversationStep.CategorySelection:
                return await HandleCategorySelectionAsync(session, userMessage);
            case ConversationStep.ItemSelection:
                return await HandleItemSelectionAsync(session, userMessage);
            case ConversationStep.Confirmation:
                return await HandleConfirmationAsync(session, userMessage);
            default:
                return "I'm sorry, I didn't understand. Please start again.";
        }
    }
    private string ShowCategoryList(ChatSession session)
    {
        session.Step = ConversationStep.CategorySelection;
        var response = "Here are the menu categories:\n";
        for (int i = 0; i < session.MenuGroups.Count; i++)
        {
            response += $"{i + 1}. {session.MenuGroups[i].GroupName}\n";
        }
        response += "Please choose a category by number or name.";
        return response;
    }

    private async Task<string> StartConversationAsync(ChatSession session)
    {
        session.Restaurant = await _apiService.GetRestaurantInfoAsync();
        session.MenuGroups = await _apiService.GetMenuGroupsAsync(session.MobileNumber, session.RrNo);

        if (session.Restaurant == null || session.MenuGroups == null || session.MenuGroups.Count == 0)
        {
            return "Sorry, we are unable to fetch restaurant information at the moment.";
        }

        // Ask for table number before showing menu
        session.Step = ConversationStep.TableNumber;

        var greeting = $"Welcome to {session.Restaurant.CoName}, {session.Restaurant.CoAddress}. {session.Restaurant.CoTelMail}\n";
        greeting += "We are happy to serve you today!\n";
        greeting += "Please provide your table number (e.g., 'Table 5' or just '5').";

        return greeting;
    }

    private async Task<string> HandleTableNumberAsync(ChatSession session, string userMessage)
    {
        // Extract table number from message (e.g., "table 5", "5", "table number 5")
        var tableNumber = ExtractTableNumber(userMessage);
        if (string.IsNullOrEmpty(tableNumber))
        {
            return "I couldn't understand the table number. Please provide it again (e.g., 'Table 5' or '5').";
        }

        session.TableNum = tableNumber;
        session.Step = ConversationStep.CategorySelection;

        var response = $"Thank you! Table {session.TableNum}.\n";
        response += "Please choose a category from the following menu groups:\n";
        for (int i = 0; i < session.MenuGroups.Count; i++)
        {
            response += $"{i + 1}. {session.MenuGroups[i].GroupName}\n";
        }
        response += "You can type the number or the group name.";

        return response;
    }


    private async Task<string> HandleCategorySelectionAsync(ChatSession session, string userMessage)
    {
        // Try to match by number first
        if (int.TryParse(userMessage, out int index) && index >= 1 && index <= session.MenuGroups.Count)
        {
            session.SelectedGroup = session.MenuGroups[index - 1].GroupName;
        }
        else
        {
            // Match by name (case-insensitive, trimmed)
            var group = session.MenuGroups.FirstOrDefault(g => g.GroupName.Trim().Equals(userMessage, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                return "Sorry, that category is not available. Please choose from the list.";
            }
            session.SelectedGroup = group.GroupName;
        }

        session.MenuItems = await _apiService.GetMenuItemsAsync(session.MobileNumber, session.RrNo, session.SelectedGroup);
        if (session.MenuItems == null || session.MenuItems.Count == 0)
        {
            return "Sorry, there are no items in this category. Please choose another.";
        }

        session.Step = ConversationStep.ItemSelection;

        var response = $"Great! Here are the items in {session.SelectedGroup}:\n";
        foreach (var item in session.MenuItems)
        {
            response += $"- {item.ItemName} (Rs. {item.Rate})\n";
        }
        response += "\nPlease tell me which items you'd like to order, with quantities if needed. For example: '2 chicken momo and 1 coke' or just 'momo, coke'. Type 'done' when finished, or 'change category' to browse another menu group.";

        return response;
    }

    private async Task<string> HandleItemSelectionAsync(ChatSession session, string userMessage)
    {
        // Check if user wants to change category
        var categoryChangeCommands = new[]
        {
        "change category", "another category", "other group", "show categories",
        "categories", "menu groups", "switch category", "different group", "browse category"
    };
        if (categoryChangeCommands.Any(cmd => userMessage.Contains(cmd, StringComparison.OrdinalIgnoreCase)))
        {
            return ShowCategoryList(session);
        }

        if (userMessage == "done" || userMessage == "that's all" || userMessage == "finish")
        {
            if (session.CurrentOrder.Count == 0)
            {
                return "Your order is empty. Please add at least one item.";
            }

            // Show summary and ask for confirmation
            session.Step = ConversationStep.Confirmation;
            return BuildOrderSummary(session) + "\nWould you like to confirm this order? (yes/no)";
        }

        // Parse items from the message
        var parsedItems = ParseItemRequest(userMessage, session.MenuItems);
        if (parsedItems.Count == 0)
        {
            return "I couldn't understand which items you want. Please mention item names from the list, e.g., '2 momo and 1 coke'.";
        }

        foreach (var parsed in parsedItems)
        {
            var existing = session.CurrentOrder.FirstOrDefault(o => o.Item.INum == parsed.Item.INum);
            if (existing != null)
            {
                existing.Quantity += parsed.Quantity;
            }
            else
            {
                session.CurrentOrder.Add(new OrderLine { Item = parsed.Item, Quantity = parsed.Quantity });
            }
        }

        return $"Added to your order: {string.Join(", ", parsedItems.Select(p => $"{p.Quantity} x {p.Item.ItemName}"))}.\nAnything else? (or type 'done' to finish)";
    }
    private async Task<string> HandleConfirmationAsync(ChatSession session, string userMessage)
    {
        if (userMessage == "yes" || userMessage == "confirm" || userMessage == "ok")
        {
            foreach (var line in session.CurrentOrder)
            {
                var saved = await _apiService.SaveOrderAsync(
                    session.MobileNumber, session.RrNo, session.TableNum, // <-- use session.TableNum
                    line.Item.INum, "AD", line.Quantity, "Order from AI");
                if (!saved)
                {
                    return "Sorry, there was an error saving your order. Please try again.";
                }
            }

            session.Step = ConversationStep.Completed;
            session.CurrentOrder.Clear();
            return "Thank you for your order! It has been placed successfully. Enjoy your meal!\nType 'hi' to start a new order.";
        }
        else if (userMessage == "no" || userMessage == "cancel")
        {
            session.Step = ConversationStep.Completed;
            session.CurrentOrder.Clear();
            return "Your order has been cancelled. Type 'hi' to start a new order.";
        }
        else
        {
            return "Please answer 'yes' to confirm or 'no' to cancel.";
        }
    }

    private string ExtractTableNumber(string message)
    {
        // Remove common words
        var cleaned = message.ToLowerInvariant()
            .Replace("table", "")
            .Replace("number", "")
            .Trim();

        // Try to find a number in the string
        var match = System.Text.RegularExpressions.Regex.Match(cleaned, @"\d+");
        return match.Success ? match.Value : null;
    }

    private bool IsGreeting(string message)
    {
        var greetings = new[] { "hi", "hello", "hey", "start", "new order", "begin", "restart", "order again" };
        return greetings.Any(g => message.Contains(g, StringComparison.OrdinalIgnoreCase));
    }

    private string BuildOrderSummary(ChatSession session)
    {
        double total = 0;
        var sb = new StringBuilder();
        sb.AppendLine("Your order summary:");
        foreach (var line in session.CurrentOrder)
        {
            sb.AppendLine($"{line.Quantity} x {line.Item.ItemName} @ Rs. {line.Item.Rate} = Rs. {line.Quantity * line.Item.Rate}");
            total += line.Quantity * line.Item.Rate;
        }
        sb.AppendLine($"Total: Rs. {total}");
        return sb.ToString();
    }


    private void ResetSession(ChatSession session)
    {
        session.Restaurant = null;
        session.MenuGroups = null;
        session.SelectedGroup = null;
        session.MenuItems = null;
        session.TableNum = null; // clear table number
        session.CurrentOrder.Clear();
        session.Step = ConversationStep.Initial;
    }
    private List<OrderLine> ParseItemRequest(string input, List<MenuItem> menuItems)
    {
        var result = new List<OrderLine>();
        // Normalize input: replace "and" with comma, split by comma or semicolon
        var parts = input.Replace(" and ", ",").Replace("&", ",").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Try to find a number at the beginning or end (e.g., "2 momo" or "momo 2")
            int quantity = 1;
            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0 && int.TryParse(words[0], out int qtyStart))
            {
                quantity = qtyStart;
                trimmed = string.Join(" ", words.Skip(1));
            }
            else if (words.Length > 1 && int.TryParse(words[^1], out int qtyEnd))
            {
                quantity = qtyEnd;
                trimmed = string.Join(" ", words.Take(words.Length - 1));
            }

            // Find the best matching menu item by name (case-insensitive, contains)
            var matchedItem = menuItems.FirstOrDefault(m =>
                m.ItemName.Trim().Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                m.ItemName.ToLowerInvariant().Contains(trimmed.ToLowerInvariant()) ||
                trimmed.ToLowerInvariant().Contains(m.ItemName.ToLowerInvariant())
            );

            if (matchedItem != null)
            {
                result.Add(new OrderLine { Item = matchedItem, Quantity = quantity });
            }
        }
        return result;
    }

}