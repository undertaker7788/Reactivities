# .NET DI 生命週期筆記

這份筆記整理 ASP.NET Core DI（Dependency Injection）的服務生命週期，以及本專案整合 ASP.NET Identity `MapIdentityApi` 與 Resend 時遇到的實際案例。

## 三種基本生命週期

| 註冊方法 | 實例建立時機 | 適合的服務 |
| --- | --- | --- |
| `AddTransient` | 每次由 DI 容器解析時 | 無狀態、短暫使用的服務 |
| `AddScoped` | 每個 DI scope 一個；在 Web API 中通常是一個 HTTP request 一個 | `DbContext`、與 request 有關的服務 |
| `AddSingleton` | 整個應用程式生命週期僅一個 | 無狀態共用服務、快取、factory |

範例：

```csharp
builder.Services.AddTransient<IEmailSender<User>, EmailSender>();
builder.Services.AddScoped<IUserAccessor, UserAccessor>();
builder.Services.AddSingleton<ICache, MemoryCache>();
```

## 依賴規則

原則是：**生命週期較長的服務，不能持有生命週期較短的服務。**

```text
Singleton  → 不可依賴 Scoped
Scoped     → 可依賴 Scoped、Transient、Singleton
Transient  → 可依賴 Scoped，但必須在 scope 內被建立
```

### 為什麼 Singleton 不能依賴 Scoped？

Singleton 會活到應用程式結束；Scoped 只應活在一個 request（或自行建立的 scope）內。若 Singleton 持有 Scoped 服務，第一個 scope 的資料可能被錯誤地長期共用，甚至在 scope 結束後仍被使用。

## Root provider 與 HTTP request scope

應用程式啟動後的根 DI 容器稱為 **root provider**。它沒有 HTTP request scope。

```text
root provider
  └─ 無法安全解析 Scoped service

HTTP request scope
  └─ 可解析 Scoped / Transient / Singleton service
```

ASP.NET Core Controller 預設不是用 `AddScoped` 直接註冊的服務，但 MVC 會在每個 HTTP request 中，透過該 request 的 service scope 建立 Controller。因此可把它的使用情境視為「每個 request 一個 Controller」。

這是合法的依賴鏈：

```text
HTTP request scope
  → AccountController
    → EmailSender（Transient）
      → IResend（Transient）
        → IOptionsSnapshot<ResendClientOptions>（Scoped）
```

`Transient` 不表示每次呼叫方法都重建；它表示每次向 DI 容器要求時建立新實例。若 Controller 建構時只解析一次 `EmailSender`，則該 Controller 在本次 request 期間會持有該實例。

## `IOptions` 系列的生命週期

| 型別 | 生命週期與用途 |
| --- | --- |
| `IOptions<T>` | 可由 Singleton 安全使用；讀取設定值 |
| `IOptionsMonitor<T>` | 可由 Singleton 安全使用；可監聽設定變更 |
| `IOptionsSnapshot<T>` | Scoped；每個 scope 取得設定快照 |

因此只要某個服務建構子需要 `IOptionsSnapshot<T>`，該服務就必須在 scope 中解析，不能由 root provider 或 Singleton 直接持有。

## `IHttpClientFactory` 與 Typed Client

`AddHttpClient<TClient, TImplementation>()` 註冊的是 **Transient typed client**。

```csharp
builder.Services.AddHttpClient<IResend, ResendClient>();
```

這個模式適合一般 Controller、Minimal API endpoint 或 Scoped service。在這些 request scope 中建立 typed client 是安全的。

不要把 typed client 直接注入 Singleton。若 Singleton 需要發 HTTP request，較適合注入 `IHttpClientFactory`，並在實際操作時呼叫 `CreateClient()`。

## 本專案案例：`MapIdentityApi`、`IEmailSender<User>` 與 Resend

`MapIdentityApi<User>()` 的行為與一般 request endpoint 不同：它在建立 Identity endpoint 的啟動階段，就從 root provider 取得 `IEmailSender<User>`。

```csharp
app.MapGroup("/api").MapIdentityApi<User>();
```

因此以下做法可能失敗：

```text
root provider
  → IEmailSender<User>（Transient 或 Scoped）
    → IResend / ResendClient
      → IOptionsSnapshot<ResendClientOptions>（Scoped）❌
```

錯誤通常類似：

```text
Cannot resolve IEmailSender<User> from root provider because it requires
scoped service IOptionsSnapshot<ResendClientOptions>.
```

### 為何 Controller 中使用相同 EmailSender 卻可行？

`AccountController` 是由 HTTP request scope 建立，所以其注入的 `EmailSender`、`IResend`、`IOptionsSnapshot` 都會在 scope 中解析；但 `MapIdentityApi` 是從 root provider 解析，沒有 scope。

### Resend 的官方註冊方式

Resend SDK 提供：

```csharp
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiToken"]!;
});
```

其原始碼呼叫：

```csharp
services.Configure(configureOptions);
return services.AddHttpClient<IResend, ResendClient>();
```

也就是以 Transient typed client 註冊 `IResend`。這可正常用於 request scope，但其文件沒有特別處理 `MapIdentityApi` 在 root provider 解析 EmailSender 的特殊情境。

### 可行的整合方式

讓 `IEmailSender<User>` 可由 root provider 建立，並在真正寄信時才建立 scope：

```csharp
builder.Services.AddSingleton<IEmailSender<User>, EmailSender>();
```

```csharp
public class EmailSender(IServiceScopeFactory scopeFactory) : IEmailSender<User>
{
    public async Task SendConfirmationLinkAsync(
        User user,
        string email,
        string confirmationLink)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var resend = scope.ServiceProvider.GetRequiredService<IResend>();

        var message = new EmailMessage
        {
            From = "onboarding@resend.dev",
            Subject = "Confirm your email address",
            HtmlBody = $"<a href='{confirmationLink}'>Confirm email</a>"
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }

    // 實作其餘 IEmailSender<User> 成員。
}
```

`IServiceScopeFactory` 本身是 Singleton，可安全地注入到 Singleton 服務。它呼叫 `CreateScope()` 或 `CreateAsyncScope()` 時，才會建立獨立的 scoped provider。

注意：不要把從自行建立 scope 解析出的 `IResend` 存成 Singleton 的欄位，也不要在 scope dispose 後使用它。

若仍保留 `MapIdentityApi<User>()`，將 `IEmailSender<User>` 註冊成 Scoped 在有 scope validation 的開發環境應會失敗；即使某些環境暫時能啟動，也不應視為正確的 lifetime 組合。

## 如何查詢 `AddXxx` 的預設生命週期

`AddXxx` 沒有一律相同的預設生命週期，應依套件實作確認。

1. 查官方文件或 API reference。
2. 查該 extension method 的原始碼，確認它最後呼叫 `AddTransient`、`AddScoped`、`AddSingleton` 或其他 helper。
3. 注意目前使用的套件版本；課程影片的套件版本可能與目前不同。
4. 在 `builder.Build()` 前列印 DI 註冊表做除錯：

```csharp
foreach (var service in builder.Services.OrderBy(x => x.ServiceType.FullName))
{
    Console.WriteLine(
        $"{service.Lifetime,-9} {service.ServiceType.Name} " +
        $"=> {service.ImplementationType?.Name ?? "factory/instance"}");
}
```

同一個 service type 可以被註冊多次；單一服務解析時，最後註冊的項目通常會覆寫前面的項目。因此不要同時手動註冊 `IResend`，又使用會註冊 `IResend` 的 `AddResend`，除非你很清楚覆寫的結果。

## 開發時的驗證

可明確啟用 DI 驗證，讓 lifetime 問題盡早在開發期出現：

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

`ValidateScopes` 特別有助於找出 root provider 或 Singleton 嘗試使用 Scoped service 的問題。

## 參考資料

- [Microsoft: Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Microsoft: IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Resend .NET SDK](https://github.com/resend/resend-dotnet)
- [Resend DI extension source](https://github.com/resend/resend-dotnet/blob/main/src/Resend/ResendDiExtensions.cs)
- [ASP.NET Core Identity API endpoint source](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs)
