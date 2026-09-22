// Restaurant details response wrapper
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Result { get; set; } // JSON string that needs to be deserialized further
}

// Restaurant info
public class RestaurantInfo
{
    public string CoName { get; set; }
    public string CoAddress { get; set; }
    public string CoTelMail { get; set; }
}

// Menu group
public class MenuGroup
{
    public string GroupName { get; set; }
}

// Menu item
public class MenuItem
{
    public string ItemName { get; set; }
    public string INum { get; set; }
    public double Rate { get; set; }
    public string GroupName { get; set; }
}