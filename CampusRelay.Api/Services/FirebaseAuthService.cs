using System;
using System.IO;
using System.Text;
using CampusRelay.Api.Models.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CampusRelay.Api.Services;

/// <summary>
/// Service for validating Firebase ID tokens and extracting user information.
/// Replaces Azure AD authentication with Firebase Authentication.
/// Uses environment variables for Render deployment.
/// </summary>
public interface IFirebaseAuthService
{
    Task<User> ValidateFirebaseTokenAndGetUser(string idToken, string provider);
}

/// <summary>
/// Implementation of Firebase Authentication service.
/// Validates Firebase ID tokens and creates/updates user profiles.
/// </summary>
public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly IConfiguration _configuration;
    private static bool _isInitialized = false;

    public FirebaseAuthService(IConfiguration configuration)
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

    public async Task<User> ValidateFirebaseTokenAndGetUser(string idToken, string provider)
    {
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        
        if (firebaseAuth == null)
        {
            throw new InvalidOperationException("Firebase not initialized. Configure Firebase Service Account.");
        }
        
        try
        {
            var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
            
            var user = new User
            {
                SsoSub = decodedToken.Uid,
                Email = decodedToken.Claims?.GetValueOrDefault("email")?.ToString() ?? "",
                FullName = decodedToken.Claims?.GetValueOrDefault("name")?.ToString() ?? "Firebase User"
            };
            
            return user;
        }
        catch (FirebaseAuthException ex)
        {
            throw new Exception("Invalid Firebase token: " + ex.Message, ex);
        }
    }
}
