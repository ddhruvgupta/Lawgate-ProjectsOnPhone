namespace LegalDocSystem.Domain.Enums;

public enum DocumentType
{
    /// <summary>
    /// Legal contract document
    /// </summary>
    Contract = 1,
    
    /// <summary>
    /// Brief or memorandum
    /// </summary>
    Brief = 2,
    
    /// <summary>
    /// Motion document
    /// </summary>
    Motion = 3,
    
    /// <summary>
    /// Pleading document
    /// </summary>
    Pleading = 4,
    
    /// <summary>
    /// Agreement document
    /// </summary>
    Agreement = 5,
    
    /// <summary>
    /// Evidence or exhibit
    /// </summary>
    Evidence = 6,
    
    /// <summary>
    /// Correspondence letter or email
    /// </summary>
    Correspondence = 7,
    
    /// <summary>
    /// Research document
    /// </summary>
    Research = 8,
    
    /// <summary>
    /// General document
    /// </summary>
    Other = 99
}
