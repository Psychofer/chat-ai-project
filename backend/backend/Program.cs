using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// CORS ayarları (Frontend ve mobil için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173",
                "https://chat-aii-app.vercel.app") // DOĞRU DOMAIN
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// SQLite veritabanı bağlantısı
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Veritabanını otomatik oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

// ✅ Kullanıcı kaydı (sadece rumuz)
app.MapPost("/api/users/register", async (ChatDbContext db, UserRegisterRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { message = "Kullanıcı adı boş olamaz" });
    }

    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (existingUser != null)
    {
        return Results.BadRequest(new { message = "Bu kullanıcı adı zaten alınmış" });
    }

    var user = new User
    {
        Username = request.Username,
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { userId = user.Id, username = user.Username });
});

// ✅ Mesaj gönderme ve AI analizi (SSE tolerant, data: satırlarını okur)
app.MapPost("/api/messages/send", async (
    ChatDbContext db,
    HttpClient httpClient,
    MessageSendRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { message = "Mesaj boş olamaz" });
    }

    // Kullanıcıyı kontrol et
    var user = await db.Users.FindAsync(request.UserId);
    if (user == null)
    {
        return Results.BadRequest(new { message = "Kullanıcı bulunamadı" });
    }

    // Varsayılan duygu değeri
    string sentimentValue = "nötr";
    double scoreValue = 0.5;
    bool aiSuccess = false;

    var aiBaseUrl = "https://jaxfel-sentiment-analysis-turkish.hf.space";

    try
    {
        // 1) POST -> event_id al
        var postUrl = $"{aiBaseUrl}/gradio_api/call/predict";
        var aiRequestBody = new { data = new object[] { request.Text } };
        var postJson = JsonSerializer.Serialize(aiRequestBody);
        var postContent = new StringContent(postJson, System.Text.Encoding.UTF8, "application/json");

        var postResponse = await httpClient.PostAsync(postUrl, postContent);
        var postResult = await postResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[AI] POST result: {postResult}");

        string? eventId = null;

        // POST sonucu normal JSON (ör. {"event_id":"..."} ) gelebilir, parse etmeye çalış
        try
        {
            using var postDoc = JsonDocument.Parse(postResult);
            if (postDoc.RootElement.TryGetProperty("event_id", out var ev))
                eventId = ev.GetString();
        }
        catch
        {
            // JSON değilse regex ile event_id yakala
            var m = Regex.Match(postResult, "\"event_id\"\\s*:\\s*\"(?<id>[^\"]+)\"");
            if (m.Success) eventId = m.Groups["id"].Value;
        }

        if (!string.IsNullOrEmpty(eventId))
        {
            // 2) GET -> SSE/stream sonucu al
            var getUrl = $"{aiBaseUrl}/gradio_api/call/predict/{eventId}";
            var getResponse = await httpClient.GetAsync(getUrl);
            var getResult = await getResponse.Content.ReadAsStringAsync();

            Console.WriteLine($"[AI] Raw GET response (truncated 2000 chars): {getResult?.Substring(0, Math.Min(2000, getResult?.Length ?? 0))}");

            JsonDocument? parsed = null;

            // 1. Önce doğrudan JSON parse etmeye çalış
            try
            {
                parsed = JsonDocument.Parse(getResult);
            }
            catch
            {
                // 2. SSE formatı: "data: ..." satırlarını topla ve birleştir
                var matches = Regex.Matches(getResult, @"^data:\s*(.+)$", RegexOptions.Multiline);
                if (matches.Count > 0)
                {
                    var dataParts = matches.Select(m => m.Groups[1].Value.Trim()).ToArray();
                    var combined = string.Concat(dataParts); // genelde tek parça olur; birden fazlaysa birleşik JSON üretir

                    try
                    {
                        parsed = JsonDocument.Parse(combined);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AI] data-lines parse hatası: {ex.Message}");
                        parsed = null;
                    }
                }
            }

            if (parsed != null)
            {
                using (parsed)
                {
                    var root = parsed.RootElement;

                    // CASE A: Dönen temel yapı bir dizi ise (örneğin: [{"sentiment":"negatif",...}])
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var first = root[0];
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            if (first.TryGetProperty("sentiment", out var s) && s.ValueKind == JsonValueKind.String)
                                sentimentValue = s.GetString() ?? "nötr";
                            if (first.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number)
                                scoreValue = sc.GetDouble();
                            aiSuccess = true;
                        }
                        else if (first.ValueKind == JsonValueKind.String)
                        {
                            var raw = first.GetString() ?? "";
                            if (raw.Contains("pozitif", StringComparison.OrdinalIgnoreCase) || raw.Contains("positive", StringComparison.OrdinalIgnoreCase))
                                sentimentValue = "pozitif";
                            else if (raw.Contains("negatif", StringComparison.OrdinalIgnoreCase) || raw.Contains("negative", StringComparison.OrdinalIgnoreCase))
                                sentimentValue = "negatif";
                            aiSuccess = true;
                        }
                    }
                    // CASE B: Dönen temel yapı bir obje ise (ör. { "sentiment":"pozitif", "score":0.9 })
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("sentiment", out var ds) && ds.ValueKind == JsonValueKind.String)
                            sentimentValue = ds.GetString() ?? "nötr";
                        if (root.TryGetProperty("score", out var sc2) && sc2.ValueKind == JsonValueKind.Number)
                            scoreValue = sc2.GetDouble();
                        aiSuccess = true;
                    }

                    if (aiSuccess)
                        Console.WriteLine($"[AI] ✅ AI analizi başarılı: {sentimentValue} (score: {scoreValue})");
                    else
                        Console.WriteLine("[AI] ⚠️ AI parse edildi ama duygu çıkarılamadı.");
                }
            }
            else
            {
                Console.WriteLine("[AI] ❌ GET yanıtından JSON çıkarılamadı.");
            }
        }
        else
        {
            Console.WriteLine("[AI] ❌ event_id alınamadı (POST sonucu parse edilemedi).");
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine($"[AI] ⏱️ Zaman aşımı - Space muhtemelen uyuyor");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AI] ❌ İstek hatası: {ex.Message}");
    }

    // Eğer AI servisi hiç çalışmadıysa veya parse başarısızsa, basit kelime analizi yap
    if (!aiSuccess)
    {
        Console.WriteLine("[AI] 🔄 Yedek analiz devreye girdi");
        var lowerText = request.Text.ToLower();

        var pozitifKelimeler = new[] { "harika", "muhteşem", "güzel", "mutlu", "seviyorum",
                                       "mükemmel", "süper", "iyi", "hoş", "keyifli", "güler",
                                       "başarılı", "heyecanlı", "sevinçli", "havalı" };
        var negatifKelimeler = new[] { "kötü", "berbat", "üzgün", "nefret", "mutsuz",
                                       "korkunç", "rezalet", "üzücü", "sinir", "iğrenç",
                                       "berbat", "sıkıcı", "bıktım", "tiksiniyorum", "pis" };

        var pozitifSkor = pozitifKelimeler.Count(k => lowerText.Contains(k));
        var negatifSkor = negatifKelimeler.Count(k => lowerText.Contains(k));

        if (pozitifSkor > negatifSkor)
        {
            sentimentValue = "pozitif";
            scoreValue = 0.75;
            aiSuccess = true;
        }
        else if (negatifSkor > pozitifSkor)
        {
            sentimentValue = "negatif";
            scoreValue = 0.25;
            aiSuccess = true;
        }

        Console.WriteLine($"[AI] Yedek sonuç: {sentimentValue} (pozitif:{pozitifSkor}, negatif:{negatifSkor})");
    }

    // Mesajı veritabanına kaydet
    var message = new Message
    {
        UserId = request.UserId,
        Text = request.Text,
        Sentiment = sentimentValue,
        SentimentScore = scoreValue,
        CreatedAt = DateTime.UtcNow
    };

    db.Messages.Add(message);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        messageId = message.Id,
        userId = message.UserId,
        username = user.Username,
        text = message.Text,
        sentiment = message.Sentiment,
        sentimentScore = message.SentimentScore,
        createdAt = message.CreatedAt
    });
});

// ✅ Tüm mesajları getir
app.MapGet("/api/messages", async (ChatDbContext db) =>
{
    var messages = await db.Messages
        .Include(m => m.User)
        .OrderBy(m => m.CreatedAt)
        .Select(m => new
        {
            messageId = m.Id,
            userId = m.UserId,
            username = m.User.Username,
            text = m.Text,
            sentiment = m.Sentiment,
            sentimentScore = m.SentimentScore,
            createdAt = m.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(messages);
});

app.Run();

// 📦 Veritabanı Modelleri
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = "";
    public string Sentiment { get; set; } = "nötr";
    public double SentimentScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}

// 📨 Request Modelleri
public record UserRegisterRequest(string Username);
public record MessageSendRequest(int UserId, string Text);

