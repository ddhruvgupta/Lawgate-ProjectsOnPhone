namespace LegalDocSystem.Domain.Enums;

public enum SubscriptionTier
{
    /// <summary>
    /// Free trial (14 days, limited features)
    /// </summary>
    Trial = 1,
    
    /// <summary>
    /// Basic plan (up to 5 users, 100 GB storage)
    /// </summary>
    Basic = 2,
    
    /// <summary>
    /// Professional plan (up to 20 users, 500 GB storage)
    /// </summary>
    Professional = 3,
    
    /// <summary>
    /// Enterprise plan (unlimited users, 2 TB storage, advanced features)
    /// </summary>
    Enterprise = 4
}
