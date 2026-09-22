using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace CampusRelay.Api.Services;

/// <summary>
/// Validates Firebase ID tokens using Firebase Admin SDK.
/// This replaces Azure AD token validation with Firebase Authentication.
/// Uses environment variables for Render deployment.
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
    private static bool _isInitialized = false;

    public FirebaseTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeFirebaseApp();
    }

    private void InitializeFirebaseApp()
    {
        // Only initialize once
        if (_isInitialized)
            return;

        try
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var projectId = firebaseSection["ProjectId"] ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
            
            // Try to get Firebase credentials from environment variable (Render)
            var base64Json = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_B64");
            
            if (!string.IsNullOrEmpty(base64Json))
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Json));
                var credential = GoogleCredential.FromJson(json);
                
                FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                {
                    Credential = credential,
                    ProjectId = projectId
                });
                _isInitialized = true;
                Console.WriteLine("Firebase initialized from environment variable");
                return;
            }
            
            // Fallback to external file for local development
            var serviceAccountPath = firebaseSection["ServiceAccountFilePath"];
            
            if (!string.IsNullOrEmpty(serviceAccountPath))
            {
                if (!Path.IsPathRooted(serviceAccountPath))
                {
                    serviceAccountPath = Path.Combine(FirebaseAppContext.BaseDirectory, serviceAccountPath);
                }
                
                if (File.Exists(serviceAccountPath))
                {
                    var credential = GoogleCredential.FromFile(serviceAccountPath);
                    
                    FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                    {
                        Credential = credential,
                        ProjectId = projectId
                    });
                    _isInitialized = true;
                    Console.WriteLine("Firebase initialized from file: " + serviceAccountPath);
                    return;
                }
                else
                {
                    Console.WriteLine("Firebase service account file not found: " + serviceAccountPath);
                }
            }
            
            if (!string.IsNullOrEmpty(projectId) && projectId != "REPLACE_WITH_YOUR_FIREBASE_PROJECT_ID")
            {
                Console.WriteLine("Firebase not configured - using dev mode only");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Firebase initialization skipped (dev mode): " + ex.Message);
        }
    }

    public async Task<FirebaseToken> ValidateTokenAsync(string token)
    {
        // If Firebase is not initialized, we cannot validate tokens
        if (FirebaseAuth.DefaultInstance == null)
        {
            throw new InvalidOperationException("Firebase not initialized. Configure Firebase Service Account in environment variables.");
        }
        
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        return await firebaseAuth.VerifyIdTokenAsync(token);
    }
}
