using CampusRelay.Api.Models.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
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
        var credential = GoogleCredential.FromJson(firebaseSection["ServiceAccountJson"]);
        
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
            ProjectId = firebaseSection["ProjectId"]
        });
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
                Email = decodedToken.Claims.GetValueOrDefault("email", ""),
                FullName = decodedToken.Claims.GetValueOrDefault("name", "Firebase User")
            };
            
            return user;
        }
        catch (FirebaseAuthException ex)
        {
            throw new SecurityTokenInvalidException("Invalid Firebase token", ex);
        }
    }
}
