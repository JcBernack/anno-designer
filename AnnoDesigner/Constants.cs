﻿using System;
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// The folder containing all icon files.
        /// </summary>
        public const string IconFolder = "icons";

        /// <summary>
        /// Filter used to retrieve the icons within the IconFolder.
        /// </summary>
        public const string IconFolderFilter = "*.png";

        /// <summary>
        /// File extension used for saved layouts.
        /// </summary>
        public const string SavedLayoutExtension = ".ad";

        /// <summary>
        /// Filter used for the open, save and save as dialogs.
        /// </summary>
        public const string SaveOpenDialogFilter = "Anno Designer Files (*.ad)|*.ad|All Files (*.*)|*.*";

        /// <summary>
        /// BitmapEncoder used for encoding exported images.
        /// </summary>
        /// <returns></returns>
        public static Func<BitmapEncoder> GetExportImageEncoder = () => new PngBitmapEncoder();

        /// <summary>
        /// File extension used for exported images. Should correspong to the used encoding.
        /// </summary>
        public const string ExportedImageExtension = ".png";

        /// <summary>
        /// Filter used for the export image dialog.
        /// </summary>
        public const string ExportDialogFilter = "PNG (*.png)|*.png|All Files (*.*)|*.*";
    }
}