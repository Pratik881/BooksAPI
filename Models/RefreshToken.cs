using Microsoft.Identity.Client;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace BookStoreApi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token {  get; set; }

        public int UserId {  get; set; }

        public DateTime Expires { get; set; }

        public bool IsRevoked { get; set; }
    }
}
