using MFADemo.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MFADemo.Controllers
{
    public class MfaSetupViewModel
    {
        public string? QrCodeImageUrl { get; set; }
        public string? ManualEntryKey { get; set; }

        [Required]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = string.Empty;
    }

}
