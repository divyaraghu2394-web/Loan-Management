namespace LoanManagement.Utility.Helpers;

public static class EscapeLdapFilter
{
    // Escapes special characters for LDAP filter values.
    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
