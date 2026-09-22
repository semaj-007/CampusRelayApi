using System;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
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
        if (FirebaseAuth.DefaultInstance != null)
            return;

        var firebaseSection = _configuration.GetSection("Firebase");
        var credential = Google.Apis.Auth.GoogleCredential.FromJson(firebaseSection["ServiceAccountJson"]);
        
        FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = credential,
            ProjectId = firebaseSection["ProjectId"]
        });
    }

    public async Task<FirebaseToken> ValidateTokenAsync(string token)
    {
        var firebaseAuth = FirebaseAuth.DefaultInstance;
        return await firebaseAuth.VerifyIdTokenAsync(token);
    }
}
