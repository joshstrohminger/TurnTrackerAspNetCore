namespace TurnTrackerAspNetCore.Services
{
    public class EventIds
    {
        public const int EmailError = 1;
        public const int EmailErrorUnknown = 2;
        public const int UserDeleted = 3;
        public const int UserRegistered = 4;
        public const int UserLoggedIn = 5;
        public const int UserLoggedOut = 6;
        public const int UserLockedOut = 7;
        public const int UserRoleAdded = 8;
        public const int UserRoleRemoved = 9;
        public const int UserUpdatedProfile = 10;
        public const int UserProfileModifiedByAdmin = 11;
        public const int EmailConfirmationSent = 12;
        public const int SiteSettingLoadError = 13;
        public const int SiteSettingSaveError = 14;
        public const int SiteSettingRemoved = 15;
        public const int SiteSettingAdded = 16;
        public const int EmailInviteSent = 17;
    }
}
