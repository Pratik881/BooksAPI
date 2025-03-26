using Microsoft.EntityFrameworkCore;

namespace BookStoreApi.DTO
{
    public class PutBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string Author { get; set; }

        [Precision(18, 4)]
        public decimal Price { get; set; }
    }
}
