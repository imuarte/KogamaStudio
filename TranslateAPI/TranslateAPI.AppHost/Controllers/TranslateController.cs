using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace TranslateAPI.AppHost.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TranslateController : ControllerBase
    {
        public class TranslateRequest
        {
            public string[] texts { get; set; }
            public string targetLanguage { get; set; } = "en";
        }

        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest req)
        {   
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("OPENAI_API_KEY not configured");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var textsJson = JsonSerializer.Serialize(req.texts);
                var prompt = $"Translate each text in this JSON array to {req.targetLanguage}. Return ONLY a JSON array with translated texts in the same order.\n{textsJson}";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[] {
                        new { role = "system", content = "You are a translator. You receive a JSON array of texts. Return ONLY a JSON array of translated texts in the same order. No markdown, no explanations, just valid JSON." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.0
                };


                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, responseBody);

                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var translatedText = result.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new { translations = translatedText });
            }
        }
    }
}
