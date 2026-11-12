public enum DebuffType
{
    Vulnerable = 0,
    Weak = 1,
    Frail = 2
}

public static class DebuffNames
{
    public static string Get(DebuffType type)
    {
        switch (type)
        {
            case DebuffType.Vulnerable: return "취약";
            case DebuffType.Weak: return "약화";
            case DebuffType.Frail: return "손상";
            default: return "";
        }
    }
}

