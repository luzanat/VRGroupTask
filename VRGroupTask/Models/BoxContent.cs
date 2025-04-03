
namespace VRGroupTask.Models
{
    public class BoxContent
    {
        public required string PoNumber { get; init; }

        public required string Isbn { get; init; }

        public int Quantity { get; init; }          

        public required string BoxIdentifier { get; init; }
    }
}
