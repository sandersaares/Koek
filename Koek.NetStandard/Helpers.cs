namespace Koek
{
    /// <summary>
    /// Entry point to Koek helper methods.
    /// </summary>
    /// <remarks>
    /// This class references various container classes that are provided extension methods by the different platform variants.
    /// This style exists because there is some overlap between the different platforms (e.g. we want to offer
    /// general purpose XXX helper methods and also specialized XXX.NEtCore helper methods) and extension methods
    /// are the only sensible way to do this without forcing the caller to dig for the right platform-specific class.
    /// </remarks>
    public static partial class Helpers
    {
        public static readonly HelpersContainerClasses.Argument Argument = new HelpersContainerClasses.Argument();
        public static readonly HelpersContainerClasses.Async Async = new HelpersContainerClasses.Async();
        public static readonly HelpersContainerClasses.Convert Convert = new HelpersContainerClasses.Convert();
        public static readonly HelpersContainerClasses.Debug Debug = new HelpersContainerClasses.Debug();
        public static readonly HelpersContainerClasses.Environment Environment = new HelpersContainerClasses.Environment();
        public static readonly HelpersContainerClasses.Filesystem Filesystem = new HelpersContainerClasses.Filesystem();
        public static readonly HelpersContainerClasses.Guid Guid = new HelpersContainerClasses.Guid();
        public static readonly HelpersContainerClasses.Random Random = new HelpersContainerClasses.Random();
        public static readonly HelpersContainerClasses.Type Type = new HelpersContainerClasses.Type();
        public static readonly HelpersContainerClasses.XmlSerialization XmlSerialization = new HelpersContainerClasses.XmlSerialization();
    }
}