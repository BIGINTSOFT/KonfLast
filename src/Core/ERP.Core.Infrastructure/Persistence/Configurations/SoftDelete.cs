namespace ERP.Core.Infrastructure.Persistence.Configurations;

public static class SoftDelete
{
    /// <summary>Kod alanları yalnızca silinmemiş kayıtlar arasında benzersizdir.</summary>
    public const string ActiveRowsFilter = "[IsDeleted] = 0";
}
