using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QR_scanner_zxing.Resources.Raw
{
    public class ApiSettings
    {
        private const string ApiTokensKey = "API_TOKENS";

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
            return string.IsnullOrEmpty(jsonTokens) ? new Dictionary<string, string>() : JsonSerializer.Deserialie<Dictionary<string, string>>(jsonTokens);
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
