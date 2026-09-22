using System;
using System.IO;

namespace CampusRelay.Api.Services;

/// <summary>
/// Helper class to get the application base directory for Firebase service account file lookup.
/// </summary>
public static class FirebaseAppContext
{
    public static string BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
}
