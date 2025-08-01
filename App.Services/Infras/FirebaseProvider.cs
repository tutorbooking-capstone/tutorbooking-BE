using FirebaseAdmin.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
