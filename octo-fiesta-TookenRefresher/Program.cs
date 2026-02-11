using Microsoft.Playwright;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;

public class Program
{
    static async Task Main(string[] args)
    {
        Refresher refs = new();
        await refs.StartRefresher();
    }
}

public class Refresher()
{
    tokens tokens = new tokens();

    public async Task StartRefresher()
    {
        await LoadTokenJson();

        if (!await DeezerRefresh())
        {
            Console.WriteLine("something went wrong. couldnt update the tooken.");
        }

        /*
        if (tokens != null)
        {
            if (tokens.ExpireDate_Deezer.HasValue == true && DateTimeOffset.FromUnixTimeSeconds((long)tokens.ExpireDate_Deezer.Value) < DateTimeOffset.UtcNow)
            {
                Console.WriteLine(DateTimeOffset.UtcNow + " " + DateTimeOffset.FromUnixTimeSeconds((long)tokens.ExpireDate_Deezer.Value));
               if(!await DeezerRefresh())
                {
                    Console.WriteLine("something went wrong. couldnt update the tooken.");
                }
            }
            else if(tokens.ExpireDate_Deezer.HasValue == false)
            {
                await DeezerRefresh();
            }
        }
        */
    }

    private async Task LoadTokenJson()
    {
        string fileName = "Token.json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(filePath))
        {
            tokens = JsonSerializer.Deserialize<tokens>(File.ReadAllText(filePath));
        }
        else
        {
            await using FileStream createStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(createStream, tokens);
        }
    }

    private async Task SaveTokenJson()
    {
        string fileName = "Token.json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        string json = JsonSerializer.Serialize(tokens);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task<bool> DeezerRefresh()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false //I dont really know why but if its true it doesnt work.
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        Console.WriteLine("going to the deezer login");
        await page.GotoAsync("https://account.deezer.com/en/login/");
        await page.WaitForTimeoutAsync(5000);

        try
        {
            // handle cookies first
            var acceptBtn = page.Locator("#gdpr-btn-accept-all");
            await acceptBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await acceptBtn.ClickAsync();
            Console.WriteLine("cookies accepted");

            //handle email and pass
            Console.WriteLine("email and password being typed");
            await page.GetByLabel("email").FillAsync("kisacikdevran0@gmail.com");
            await page.GetByTestId("password-field").FillAsync("devran20092009");
            Console.WriteLine("finished typing");

            //handle the fuckass huminity thing
            await page.GetByTestId("login-button").ClickAsync();
            Console.WriteLine("login button clicked waiting 35s to very humanity");
            await page.WaitForTimeoutAsync(35000);
            await page.GetByTestId("login-button").ClickAsync();
            Console.WriteLine(page.Url);
            Console.WriteLine("waiting for 5 seconds to load the page");
            await page.WaitForTimeoutAsync(5000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
            Console.ReadKey();
            return false;
        }
        var cookies = await context.CookiesAsync("https://www.deezer.com/");
        foreach (var c in cookies)
        {
            if (c != null && c.Name == "arl")
            {
                Console.WriteLine(c.Value);
                tokens.Token_Deezer = c.Value;
                tokens.ExpireDate_Deezer = c.Expires;
                await SaveTokenJson();
                string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "AppSettings.json"));
                using var doc = JsonDocument.Parse(json);
                string path = doc.RootElement.GetProperty("OctoAppsettingPath").GetString();
                JsonNode node = JsonNode.Parse(path);
                node["Arl"] = c.Value;
                return true;
                
            }
            else if(cookies.Last() == c)
            {
                return false;
            }
        }
        return false;
    }
}