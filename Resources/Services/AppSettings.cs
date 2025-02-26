using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace QR_scanner_zxing.Resources.Raw
{
    public class AppSettings
    {
        //public const string ApiUrlKey = "https://api.tago.io/data";
        //public const string ApiTokenKey = "";


        //public static async Task SaveApiUrlAsync(string apiUrl)
        //{
        //    await SecureStorage.SetAsync(ApiUrlKey, apiUrl);
        //}

        //public static async Task<string> GetApiUrlAsync()
        //{
        //    return await SecureStorage.GetAsync(ApiUrlKey) ?? "https://api.tago.io/data";
        //}



        //public static async Task SaveApiTokenAsync(string token)
        //{
        //    await SecureStorage.SetAsync(ApiTokenKey, token);
        //}

        //public static async Task<string> GetApiTokenAsync()
        //{
        //    return await SecureStorage.GetAsync(ApiTokenKey) ?? string.Empty;
        //}


        private const string ApiTokensKey = "API_TOKENS";

        public static async Task SaveLastSentIndexAsync(string tokenName, int lastIndex)
        {
            string key = $"last_sent_index_{tokenName}";
            await SecureStorage.SetAsync(key, lastIndex.ToString());
        }

        public static async Task<int> GetLastSentIndexAsync(string tokenName)
        {
            string key = $"last_sent_index_{tokenName}";
            var indexStr = await SecureStorage.GetAsync(key);

            return indexStr != null ? int.Parse(indexStr) : 0;
        }

        public static void SaveApiToken(string tokenName, string tokenValue)
        {
            var tokens = GetApiTokens();
            tokens[tokenName] = tokenValue;
            string jsonTokens = JsonSerializer.Serialize(tokens);
            Preferences.Set(ApiTokensKey, jsonTokens);
        }

        public static Dictionary<string, string> GetApiTokens()
        {
            string jsonTokens = Preferences.Get(ApiTokensKey, string.Empty);
            return string.IsNullOrEmpty(jsonTokens) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(jsonTokens);
        }

        public static void DeleteApiToken(string tokenName)
        {
            var tokens = GetApiTokens();
            if (tokens.ContainsKey(tokenName))
            {
                tokens.Remove(tokenName);
                string jsonTokens = JsonSerializer.Serialize(tokens);
                Preferences.Set(ApiTokensKey, jsonTokens);
            }
        }
    }
}
