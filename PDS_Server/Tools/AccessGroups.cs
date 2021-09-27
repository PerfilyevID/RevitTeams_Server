namespace PDS_Server
{
    public static class AccessGroups
    {
        public const string ADMIN = "Admin";
        public const string APPROVED = "Admin,TeamOwner,TeamManager,TeamMate";
        public const string MODERATOR = "Admin,TeamOwner,TeamManager";
        public const string OWNER = "Admin,TeamOwner";

        public static string GetRole(Roles role)
        {
            switch(role)
            {
                case Roles.Admin:
                    return role.ToString("G");
                case Roles.Expired:
                    return role.ToString("G");
                case Roles.TeamManager:
                    return role.ToString("G");
                case Roles.TeamMate:
                    return role.ToString("G");
                case Roles.TeamOwner:
                    return role.ToString("G");
                case Roles.UnVerified:
                    return role.ToString("G");
                default:
                    return GetRole(Roles.UnVerified);
            }
        }
        public enum Roles
        {
            Admin,
            TeamOwner,
            TeamManager,
            TeamMate,
            UnVerified,
            Expired
        }
    }
}
