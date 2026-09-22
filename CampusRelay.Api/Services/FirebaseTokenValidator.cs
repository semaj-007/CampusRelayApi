using System;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace CampusRelay.Api.Services;

/// <summary>
/// Validates Firebase ID tokens using Firebase Admin SDK.
/// This replaces Azure AD token validation with Firebase Authentication.
/// </summary>
public interface IFirebaseTokenValidator
{
    Task<FirebaseToken> ValidateTokenAsync(string token);
}

/// <summary>
/// Implementation of Firebase token validator.
/// </summary>
public class FirebaseTokenValidator : IFirebaseTokenValidator
{
    private readonly IConfiguration _configuration;

    public FirebaseTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeFirebaseApp();
    }

    private void InitializeFirebaseApp()
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        var firebaseSection = _configuration.GetSection("Firebase");
        
        if (!string.IsNullOrEmpty(firebaseSection["ServiceAccountFilePath"]))
        {
            try
            {
                var serviceAccountPath = firebaseSection["ServiceAccountFilePath"];
                
                // Check if the path is relative and prepend the base directory
                if (!Path.IsPathRooted(serviceAccountPath))
                {
                    serviceAccountPath = Path.Combine(AppContext.BaseDirectory, serviceAccountPath);
                }
                
                if (File.Exists(serviceAccountPath))
                {
                    var credential = GoogleCredential.FromFile(serviceAccountPath);
                    
                    FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                    {
                        Credential = credential,
                        ProjectId = firebaseSection["ProjectId"]
                    });
                }
                else
                {
                    Console.WriteLine("Firebase service account file not found: " + serviceAccountPath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - Firebase will work without initialization for dev-login
                Console.WriteLine("Firebase initialization skipped (dev mode): " + ex.Message);
            }
        }
        else
        {
            Console.WriteLine("Firebase not configured - using dev mode only");
        }
    }

    public async Task<FirebaseToken> ValidateTokenAsync(string token)
    {
        // If Firebase is not initialized, we cannot validate tokens
        // Return null or throw - the controller will handle this gracefully
        if (FirebaseAuth.DefaultInstance == null)
        {
            throw new InvalidOperationException("Firebase not initialized. Configure Firebase Service Account file in appsettings.");
        }
        
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        return await firebaseAuth.VerifyIdTokenAsync(token);
    }
}

// Helper class to get the application base directory
public static class AppContext
{
    public static string BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
}
