namespace AnnoDesigner
{
    /// <summary>
    /// Contains application wide constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Version number of the application.
        /// Will be increased with each release.
        /// </summary>
        public const int Version = 6;

        /// <summary>
        /// Version number of the saved file format.
        /// Will be increased every time the file format is changed.
        /// </summary>
        public const int FileVersion = 2;

        /// <summary>
        /// The minimal grid size to which the user can zoom out.
        /// </summary>
        public const int GridStepMin = 8;

        /// <summary>
        /// The maximum grid size to which the user can zoom in.
        /// </summary>
        public const int GridStepMax = 100;

        /// <summary>
        /// The default grid size.
        /// </summary>
        public const int GridStepDefault = 20;
    }
}
