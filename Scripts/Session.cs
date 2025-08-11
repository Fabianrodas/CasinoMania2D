public static class Session
{
    public static string PlayFabId;
    public static string Username;
    public static string Email;
    public static int    Wallet;
    public static string Ticket; // SessionTicket

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Ticket);

    public static void Clear()
    {
        PlayFabId = null;
        Username  = null;
        Email     = null;
        Wallet    = 0;
        Ticket    = null;
        LoginGuard.I?.ResetLoginGate();
    }
}
