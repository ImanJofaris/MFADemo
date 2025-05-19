namespace MFADemo.Models
{
    public class MfaSettings
    {
        public int MfaSettingsId { get; set; }
        public int UserId { get; set; }
        public bool IsEnabled { get; set; }
        public string SecretKey { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public User User { get; set; } 
    }
}
