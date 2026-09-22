using System;
using System.IO;
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

    public FirebaseAuthService(IConfiguration configuration)
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

    public async Task<User> ValidateFirebaseTokenAndGetUser(string idToken, string provider)
    {
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        
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
            // Re-throw the original exception with additional context
            throw new Exception("Invalid Firebase token: " + ex.Message, ex);
        }
    }
}
