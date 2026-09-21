namespace EdgeGlow;
internal static class AppBrand
{
    internal static Icon CreateIcon()
    {
        using var stream=typeof(AppBrand).Assembly.GetManifestResourceStream("EdgeGlow.AppIcon")!;
        using var original=new Icon(stream);
        return (Icon)original.Clone();
    }
}
