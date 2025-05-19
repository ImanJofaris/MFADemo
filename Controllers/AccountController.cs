using MFADemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Google.Authenticator;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace MFADemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                if (_context.Users.Any(u => u.Username == model.Username || u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Username or email already in use");
                    return View(model);
                }

                // Create password hash and salt
                CreatePasswordHash(model.Password, out string passwordHash, out string passwordSalt);

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Initialize MFA settings (disabled by default)
                var mfaSettings = new MfaSettings
                {
                    UserId = user.UserId,
                    IsEnabled = false,
                    CreatedDate = DateTime.UtcNow
                };

                _context.MfaSettings.Add(mfaSettings);
                await _context.SaveChangesAsync();

                // Redirect to login page
                return RedirectToAction("Login");
            }

            return View(model);
        }

        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SetupMfa()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Include(u => u.MfaSettings)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            // Generate a new secret key if one doesn't exist
            if (string.IsNullOrEmpty(user.MfaSettings.SecretKey))
            {
                var twoFactorAuthenticator = new TwoFactorAuthenticator();
                var secretKey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);

                user.MfaSettings.SecretKey = secretKey;
                await _context.SaveChangesAsync();
            }

            // Generate QR code
            var twoFactorSetup = new TwoFactorAuthenticator();
            var setupInfo = twoFactorSetup.GenerateSetupCode(
                "iSEM.ai",
                user.Email,
                user.MfaSettings.SecretKey,
                false,
                3);

            var model = new MfaSetupViewModel
            {
                QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl,
                ManualEntryKey = setupInfo.ManualEntryKey
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupMfa(MfaSetupViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Include(u => u.MfaSettings)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            // Verify the code
            var twoFactorAuthenticator = new TwoFactorAuthenticator();
            var result = twoFactorAuthenticator.ValidateTwoFactorPIN(
                user.MfaSettings.SecretKey,
                model.VerificationCode);

            if (!result)
            {
                ModelState.AddModelError("", "Invalid verification code");

                // Regenerate QR code
                var setupInfo = twoFactorAuthenticator.GenerateSetupCode(
                    "iSEM.ai",
                    user.Email,
                    user.MfaSettings.SecretKey,
                    false,
                    3);

                model.QrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
                model.ManualEntryKey = setupInfo.ManualEntryKey;

                return View(model);
            }

            // Enable MFA
            user.MfaSettings.IsEnabled = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // For debugging
            if (!ModelState.IsValid)
            {
                // Log model state errors
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Error in {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
                return View(model);
            }

            // Find user by username or email
            var user = await _context.Users
                .Include(u => u.MfaSettings)
                .FirstOrDefaultAsync(u => u.Username == model.UsernameOrEmail || u.Email == model.UsernameOrEmail);

            if (user == null)
            {
                Console.WriteLine("User not found"); // Debugging
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            // Verify password
            if (!VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                Console.WriteLine("Password verification failed"); // Debugging
                ModelState.AddModelError("", "Invalid login attempt");

                // Log failed attempt
                _context.AuthenticationHistory.Add(new AuthenticationHistory
                {
                    UserId = user.UserId,
                    LoginStatus = false,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });
                await _context.SaveChangesAsync();

                return View(model);
            }

            Console.WriteLine("Password verified successfully"); // Debugging

            // Check if MFA is required
            if (user.MfaSettings?.IsEnabled == true)
            {
                Console.WriteLine("MFA is enabled for this user"); // Debugging

                if (string.IsNullOrEmpty(model.MfaCode))
                {
                    Console.WriteLine("MFA code required but not provided"); // Debugging
                    model.RequiresMfa = true;
                    return View(model);
                }

                // Verify MFA code
                var twoFactorAuthenticator = new TwoFactorAuthenticator();
                var result = twoFactorAuthenticator.ValidateTwoFactorPIN(
                    user.MfaSettings.SecretKey,
                    model.MfaCode);

                if (!result)
                {
                    Console.WriteLine("Invalid MFA code"); // Debugging
                    ModelState.AddModelError("", "Invalid MFA code");
                    model.RequiresMfa = true;

                    return View(model);
                }

                Console.WriteLine("MFA code validated successfully"); // Debugging
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;

            // Log successful login
            _context.AuthenticationHistory.Add(new AuthenticationHistory
            {
                UserId = user.UserId,
                LoginStatus = true,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = Request.Headers["User-Agent"].ToString()
            });

            await _context.SaveChangesAsync();

            // Create authentication cookie
            await SignInUser(user, model.RememberMe);

            Console.WriteLine("User signed in successfully, redirecting to Home/Index"); // Debugging

            return RedirectToAction("Index", "Home");
        }

        private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            using var hmac = new System.Security.Cryptography.HMACSHA512(saltBytes);
            var computedHash = Convert.ToBase64String(
                hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            return computedHash == storedHash;
        }

        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent
                });
        }

        [HttpGet]
        [Authorize]
        public IActionResult Logout()
        {
            // Render a confirmation page or directly redirect to the POST action
            return View("LogoutConfirmation"); // Optional: Create a confirmation view
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutConfirmed()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
