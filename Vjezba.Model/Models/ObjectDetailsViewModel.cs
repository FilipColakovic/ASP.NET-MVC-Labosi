namespace Vjezba.Model.Models
{
    public class ObjectReferenceLink
    {
        public string Type { get; init; } = string.Empty;
        public int Id { get; init; }
    }

    public class ObjectDetailsViewModel
    {
        public string ObjectType { get; init; } = string.Empty;
        public int ObjectId { get; init; }
        public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, ObjectReferenceLink> Links { get; init; } = new Dictionary<string, ObjectReferenceLink>();
    }
}
