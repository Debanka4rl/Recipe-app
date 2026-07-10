using Microsoft.AspNetCore.Mvc;
using supaathrowaway.Models;
using System.Text;
using System.Text.Json;
using Supabase;

namespace supaathrowaway.Controllers;

public class HomeController : Controller
{
    private readonly Supabase.Client _supabase;
    private readonly string _geminiKey;
    private static readonly HttpClient _http = new();

    public HomeController(Supabase.Client supabase, IConfiguration config)
    {
        _supabase = supabase;
        _geminiKey = config["Gemini:ApiKey"] ?? "";
    }

    
    public async Task<IActionResult> home()
    {
        try { return View((await _supabase.From<Recipe>().Get()).Models); }
        catch (Exception ex) { ViewBag.ErrorMessage = $"Database error: {ex.Message}"; return View(new List<Recipe>()); }
    }

    
    public IActionResult Create() => View();

    
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Recipe newRecipe)
    {
        if (!ModelState.IsValid) return View(newRecipe);
        try { await _supabase.From<Recipe>().Insert(newRecipe); return RedirectToAction(nameof(home)); }
        catch (Exception ex) { ViewBag.ErrorMessage = ex.Message; return View(newRecipe); }
    }

    
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try { await _supabase.From<Recipe>().Where(x => x.Id == id).Delete(); }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(home));
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var recipe = (await _supabase.From<Recipe>().Where(x => x.Id == id).Get()).Models.FirstOrDefault();
            if (recipe == null) return RedirectToAction(nameof(home));
            return View(recipe);
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; return RedirectToAction(nameof(home)); }
    }

     [HttpPost]
    public async Task<IActionResult> GetSummary(int id)
    {
        try
        {
            var recipe = (await _supabase.From<Recipe>().Where(x => x.Id == id).Get()).Models.FirstOrDefault();
            if (recipe == null) return NotFound("Recipe not found.");

            string summary = await AskGemini(recipe.Name, recipe.Description);
            return Content(summary);
        }
        catch
        {
            return Content("An unexpected error occurred while generating the summary.");
        }
    }
    // gemini stuff because evryone needs ai
    private async Task<string> AskGemini(string name, string description)
    {
        if (string.IsNullOrEmpty(_geminiKey)) return "Gemini API Key missing in appsettings.json.";

        try
        {
             var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key={_geminiKey}";

            var payload = new
            {
                contents = new[] {
                new {
                    parts = new[] {
                        new { text = $"Write a mouth-watering, punchy 2-sentence summary of this recipe. Name: {name}. Description: {description}. Do not use markdown format." }
                    }
                }
            }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Timeout is bound by the HttpClient config (7 seconds)
            var response = await _http.PostAsync(url, content);

            // Catch 503 ServiceUnavailable or any other non-200 responses cleanly
            if (!response.IsSuccessStatusCode)
            {
                return $"Gemini 3.5 flash(3.1 flash lite , lol) is busy right now (Status: {response.StatusCode}). Click again in a few seconds!";
            }

            var rawJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(rawJson);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                return text?.Trim() ?? "No content returned from the model.";
            }

            return "Unexpected JSON structure received from Gemini API.";
        }
        catch (TaskCanceledException)
        {
            return "The Gemini 3.5 flash(again...3.1 flash lite) request timed out. Try clicking again.";
        }
        catch (Exception ex)
        {
            return $"AI Error: {ex.Message}";
        }
    }

}