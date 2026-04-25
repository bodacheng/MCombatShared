namespace MCombat.Shared.CombatGroup
{
    public interface IGroupUnitCountEntry
    {
        string Id { get; }
        int Count { get; set; }
        int OriginCount { get; set; }
    }
}
