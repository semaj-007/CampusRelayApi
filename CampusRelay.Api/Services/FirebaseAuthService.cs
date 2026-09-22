using CampusRelay.Api.Models.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens;
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
        
        if (!string.IsNullOrEmpty(firebaseSection["ServiceAccountJson"]))
        {
            var credential = GoogleCredential.FromJson(firebaseSection["ServiceAccountJson"]);
            
            FirebaseApp.Create(new FirebaseAdmin.AppOptions()
            {
                Credential = credential,
                ProjectId = firebaseSection["ProjectId"]
            });
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
            // Use FirebaseAuthException directly instead of SecurityTokenInvalidException
            throw new FirebaseAuthException("Invalid Firebase token", ex);
        }
    }
}
