using FirebaseAdmin.Auth;

namespace App.Services.Infras
{
    public class FirebaseProvider
    {
        public async void VerifyToken(string idToken)
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            string uid = decodedToken.Uid;
        }

    }
}
