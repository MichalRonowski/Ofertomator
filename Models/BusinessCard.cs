namespace Ofertomator.Models;

/// <summary>
/// Wizytówka użytkownika - dane kontaktowe wyświetlane w ofercie
/// </summary>
public class BusinessCard
{
    public int Id { get; set; } = 1; // Zawsze 1 - pojedynczy rekord
    
    public string Company { get; set; } = string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
}
