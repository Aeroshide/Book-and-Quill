using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Web;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

/**
 * @author Aeroshide
 * 
 * This looks pretty smart so im authoring it
 */
public class GeniusResponse
{
    public Meta Meta { get; set; }
    public ResponseData Response { get; set; }
}

public class Meta
{
    public int Status { get; set; }
}

public class ResponseData
{
    public List<Hit> Hits { get; set; }
}

public class Hit
{
    public Result Result { get; set; }
}

public class Result
{
    public string Url { get; set; }
}

public class LyricsRequester
{
    private const string GeniusApiBaseUrl = "https://api.genius.com";
    private readonly string apiKey;

    public LyricsRequester(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public async Task<string> GetLyricsAsync(string songTitle)
    {
        var songUrl = await SearchSongAsync(songTitle);
        Debug.WriteLine("Song url: "+ songUrl);
        if (string.IsNullOrEmpty(songUrl))
        {
            return null;
        }
        var lyrics = await ExtractLyricsAsync(songUrl);
        return lyrics;
    }

    private async Task<string> SearchSongAsync(string songTitle)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(GeniusApiBaseUrl);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var encodedTitle = Uri.EscapeDataString(songTitle);
                var response = await httpClient.GetAsync($"/search?q={encodedTitle}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var geniusResponse = JsonConvert.DeserializeObject<GeniusResponse>(content);
                var firstHit = geniusResponse?.Response?.Hits?.FirstOrDefault();
                if (firstHit != null)
                {
                    var songUrl = firstHit.Result?.Url;
                    if (!string.IsNullOrEmpty(songUrl))
                    {
                        return songUrl;
                    }
                }

                return null;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }


    private async Task<string> ExtractLyricsAsync(string url)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                // Attempt to select the first part of the lyrics using the new XPath
                var lyricsNodePart1 = doc.DocumentNode.SelectSingleNode("//*[@id='lyrics-root']/div[2]");
                // Attempt to select the second part of the lyrics using the second new XPath
                var lyricsNodePart2 = doc.DocumentNode.SelectSingleNode("//*[@id='lyrics-root']/div[5]");

                // Initialize an empty string to hold the combined lyrics
                string lyrics = "";

                // If the first part is found, de-entitize and trim it, then add to lyrics
                if (lyricsNodePart1 != null)
                {
                    lyrics += HtmlEntity.DeEntitize(lyricsNodePart1.InnerText).Trim();
                }

                // If the second part is found, de-entitize and trim it, then add to lyrics
                if (lyricsNodePart2 != null)
                {
                    // Optionally, add a newline or space between parts if needed
                    lyrics += "\n" + HtmlEntity.DeEntitize(lyricsNodePart2.InnerText).Trim();
                }

                // If no specific parts were found, try broader searches
                if (string.IsNullOrEmpty(lyrics))
                {
                    var fallbackLyricsNode = doc.DocumentNode.SelectSingleNode("//div[@class='lyrics']");
                    if (fallbackLyricsNode == null)
                    {
                        fallbackLyricsNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'Lyrics__Container')]");
                    }

                    if (fallbackLyricsNode != null)
                    {
                        lyrics = HtmlEntity.DeEntitize(fallbackLyricsNode.InnerText).Trim();
                    }
                }

                return !string.IsNullOrEmpty(lyrics) ? lyrics : null;
            }
        }
        catch (Exception ex)
        {
            throw ex; // Consider logging the exception or handling it more gracefully
        }
    }

}