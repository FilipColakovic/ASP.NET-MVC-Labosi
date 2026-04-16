namespace Vjezba.Model.Models
{
    public class ObjectDetailsViewModel
    {
        public string ObjectType { get; init; } = string.Empty;
        public int ObjectId { get; init; }
        public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    }
}
