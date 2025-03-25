namespace TryToDo_Api.Classes;

public class Item
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? AprxHours { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? Priority { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsLooped { get; set; }
    public string UserGuid { get; set; }
}