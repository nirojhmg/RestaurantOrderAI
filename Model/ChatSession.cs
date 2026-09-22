public class ChatSession
{
    public string SessionId { get; set; }
    public string MobileNumber { get; set; } = "9810164520";
    public string RrNo { get; set; } = "202210140758222";
    public string TableNum { get; set; } // will be set by user
    public RestaurantInfo Restaurant { get; set; }
    public List<MenuGroup> MenuGroups { get; set; }
    public string SelectedGroup { get; set; }
    public List<MenuItem> MenuItems { get; set; }
    public List<OrderLine> CurrentOrder { get; set; } = new();
    public ConversationStep Step { get; set; } = ConversationStep.Initial;
}



public class OrderLine
{
    public MenuItem Item { get; set; }
    public int Quantity { get; set; } = 1;
}

public enum ConversationStep
{
    Initial,          // Start state
    TableNumber,      // Waiting for table number
    CategorySelection,// Waiting for category
    ItemSelection,    // Waiting for items
    Confirmation,     // Waiting for yes/no
    Completed
}