using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using System.IO;
using System.Threading;
using System.Configuration;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;

namespace SharedUtilities
{
    public static class GoogleCloudHelper
    {
        public static string jsonKeyFilePath = "kemp-hiring-bc36b5c73450.json";
        public static string AccountEmail = ConfigurationManager.AppSettings["GCPAccountEmail"];
        public static string ClientId = ConfigurationManager.AppSettings["GCPClientID"];
        public static string ClientSecret = ConfigurationManager.AppSettings["GCPClientSc"];
        public static long CurrentFileSizeInByte = 0;
        /// <summary>  
        /// Get Access Token From JSON Key Async  
        /// </summary>  
        /// <param name="jsonKeyFilePath">Path to your JSON Key file</param>  
        /// <param name="scopes">Scopes required in access token</param>  
        /// <returns>Access token as string Task</returns>  
        public static async Task<string> GetAccessTokenFromJSONKeyAsync(params string[] scopes)
        {
            try
            {
                using (var stream = new FileStream(jsonKeyFilePath, FileMode.Open, FileAccess.Read))
                {
                    return await GoogleCredential
                        .FromStream(stream) // Loads key file  
                        .CreateScoped(scopes) // Gathers scopes requested  
                        .UnderlyingCredential // Gets the credentials  
                        .GetAccessTokenForRequestAsync().ConfigureAwait(false); // Gets the Access Token  
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string GetAccessTokenFromJSONKey(params string[] scopes)
        {
            return GetAccessTokenFromJSONKeyAsync(scopes).Result;
        }


    }
}
