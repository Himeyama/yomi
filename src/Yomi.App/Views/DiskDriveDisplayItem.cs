namespace Yomi.App.Views;

/// <summary>OverlayWindowのディスク一覧をItemsControlにバインドするための表示専用モデル。</summary>
public sealed class DiskDriveDisplayItem
{
    public required string Name { get; init; }
    public required string UsageText { get; init; }
    public required double UsagePercent { get; init; }
}
