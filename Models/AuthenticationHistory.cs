namespace MFADemo.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class AuthenticationHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuthHistoryId { get; set; }

        public int UserId { get; set; }
        public DateTime LoginDate { get; set; }
        public bool LoginStatus { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
