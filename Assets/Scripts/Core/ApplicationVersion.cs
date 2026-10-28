namespace Aevoria.Core
{
    // Developer-controlled target; builds never increment or declare a release.
    public static class ApplicationVersion
    {
        public const string Number="0.0.14";
        public const string Stage="Alpha";
        public const string Status="Development";
        public const string DisplayName="Aevoria "+Stage+" v"+Number;
    }
}
