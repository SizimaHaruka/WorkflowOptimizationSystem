namespace dxpmt.Services;

public sealed class BaselineDiffService
{
    public IReadOnlyList<BaselineDifference> Compare(
        IReadOnlyList<BaselineContentItem> previous,
        IReadOnlyList<BaselineContentItem> current)
    {
        var previousItems = Index(previous);
        var currentItems = Index(current);
        var keys = previousItems.Keys.Union(currentItems.Keys).OrderBy(x => x.Section).ThenBy(x => x.Label).ThenBy(x => x.Occurrence);
        var differences = new List<BaselineDifference>();

        foreach (var key in keys)
        {
            var hasPrevious = previousItems.TryGetValue(key, out var previousItem);
            var hasCurrent = currentItems.TryGetValue(key, out var currentItem);
            if (hasPrevious && hasCurrent && previousItem!.Value == currentItem!.Value) continue;

            differences.Add(new BaselineDifference(
                key.Section,
                key.Label,
                hasPrevious ? previousItem!.Value : null,
                hasCurrent ? currentItem!.Value : null));
        }

        return differences;
    }

    private static IReadOnlyDictionary<ContentKey, BaselineContentItem> Index(IReadOnlyList<BaselineContentItem> items)
    {
        var occurrences = new Dictionary<(string Section, string Label), int>();
        var indexed = new Dictionary<ContentKey, BaselineContentItem>();
        foreach (var item in items)
        {
            var baseKey = (item.Section, item.Label);
            var occurrence = occurrences.GetValueOrDefault(baseKey) + 1;
            occurrences[baseKey] = occurrence;
            indexed.Add(new ContentKey(item.Section, item.Label, occurrence), item);
        }

        return indexed;
    }

    private sealed record ContentKey(string Section, string Label, int Occurrence);
}

public sealed record BaselineDifference(string Section, string Label, string? PreviousValue, string? CurrentValue)
{
    public string ChangeType => PreviousValue is null ? "追加" : CurrentValue is null ? "削除" : "変更";
}
