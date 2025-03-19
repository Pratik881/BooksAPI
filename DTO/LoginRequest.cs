using System.ComponentModel.DataAnnotations;

namespace BookStoreApi.DTO
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
