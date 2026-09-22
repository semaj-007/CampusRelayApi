using System;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
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
        
        if (!string.IsNullOrEmpty(firebaseSection["ServiceAccountJson"]) && 
            firebaseSection["ServiceAccountJson"] != "REPLACE_WITH_ACTUAL_FIREBASE_SERVICE_ACCOUNT_JSON")
        {
            try
            {
                var credential = GoogleCredential.FromJson(firebaseSection["ServiceAccountJson"]);
                
                FirebaseApp.Create(new FirebaseAdmin.AppOptions()
                {
                    Credential = credential,
                    ProjectId = firebaseSection["ProjectId"]
                });
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
        // If Firebase is not initialized, return a mock token for development
        if (FirebaseAuth.DefaultInstance == null)
        {
            return new FirebaseToken(
                "dev-uid",
                new FirebaseAdmin.Auth.FirebaseTokenOptions
                {
                    Uid = "dev-firebase-user"
                });
        }
        
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        return await firebaseAuth.VerifyIdTokenAsync(token);
    }
}
