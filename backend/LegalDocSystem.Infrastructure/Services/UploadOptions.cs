namespace LegalDocSystem.Infrastructure.Services
{
    /// <summary>
    /// Configuration for SAS upload URL expiry.
    /// Expiry is dynamically calculated based on declared file size:
    ///   expiryMinutes = Clamp(MinExpiryMinutes + Ceil(fileSizeMb * ExpiryMinutesPerMb), Min, Max)
    /// </summary>
    public sealed class UploadOptions
    {
        public const string SectionName = "Upload";

        /// <summary>Floor for the SAS expiry window (minutes). Default: 15.</summary>
        public int MinExpiryMinutes { get; set; } = 15;

        /// <summary>Ceiling for the SAS expiry window (minutes). Default: 120.</summary>
        public int MaxExpiryMinutes { get; set; } = 120;

        /// <summary>
        /// Minutes of SAS expiry added per megabyte of declared file size.
        /// At the default of 0.2, a 500 MB file adds 100 extra minutes (→ 115 min total before cap).
        /// </summary>
        public double ExpiryMinutesPerMb { get; set; } = 0.2;
    }
}
