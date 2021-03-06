# MercadoLibre's .NET SDK

This is a .NET SDK for MercadoLibre's Platform. [![Build status](https://ci.appveyor.com/api/projects/status/wc02olyp8oc69l2j?svg=true)](https://ci.appveyor.com/project/comsechq/mercadolibre-net-sdk)

## Is this the official Mercado libre SDK for .NET?

No. We forked the .NET SDK a while back and ended up tweaking it to better suits our needs.

## How do I install it?

To install the SDK with [nuget](https://www.nuget.org/packages/MercadoLibreSdk/x.x.x), run the following command in the Package Manager Console:

```nuget
PM> Install-Package MercadoLibreSdk -prerelease
```

## How do I start using it?

The first thing to do is to instanciate the `MeliApiService` class.

You have to obtain an access token after creating your own application. Read the [Getting Started](http://developers.mercadolibre.com/first-step/) guide for more information.

Once you have a _client id_ and _client secret_ for your application, instanciate `MeliCredentials` and assign it to the `MeliApiService.Credentials` property.


```csharp
var m = new MeliApiService 
        {
            Credentials = new MeliCredentials(MeliSite.Argentina, 1234, "a secret")
        };
```
With this instance you can start working on MercadoLibre's APIs.

There are some design considerations worth mentioning:

1. This SDK is a thin layer on top of [HttpClient](https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.118).aspx) to handle the [OAuth](https://en.wikipedia.org/wiki/OAuth) WebServer flow for you.
2. [Json.NET](http://www.newtonsoft.com/json) is used to serialize and deserialising to and from JSON. It's up to you to call the relevant methods with classes that match the expected json format.
3. [http-params](https://github.com/bounav/http-params) library to generate URIs. The `HttpParams` class is a simple wrapper for `System.Collections.Specialized.NameValueCollection` with a fluent interface. Values are **URL encoded** _automatically_!

## How do I redirect users to authorize my application?

This is a 2-step process.

First get the link to redirect the user. This is very easy! Just:

```csharp
var redirectUrl = m.GetAuthUrl(1234, MeliSite.Argentina, "http://somecallbackurl");
```

This will give you the url to redirect the user. The callback url **must** match redirect URI that you specified for your mercado libre application.

Once the user is redirected to your callback url, you'll receive in the query string, a parameter named `code`. You'll need this for the second part of the process.

```csharp
m.AuthorizeAsync("the received code", "http://somecallbackurl");
```

This method will set the `AccessToken` and `RefreshToken` properties on the `MeliCredentials` instance. 

An access token represent an authorization for your web application to act on behalf of a mercado libre user.

The `refresh token` is only set if your application has the `offline_access` scope enabled.

At this stage your are ready to make call to the API on behalf of the user.

## How do I refresh the access token?

Access tokens are only valid for 6 hours. As long as your app has the `offline_access` scope you will obtain a `refresh token` along with the `access token`. 

When the `refresh token` is set, `MeliApiService` will automatically renew the `access token` after the first `401` unauthorized answer it receives.

If you need to track access and refresh token changes (for example to store the tokens to use them later) you can subscribe to a `TokensChanged` event:

```csharp
var credentials = new MeliCredentials(MeliSite.Argentina, 123456, "clientSecret", "accessToken", "refreshToken");

credentials.TokensChanged += (sender, args) => { doSomethingWithNewTokenValues(args.Info); };

var service = new MeliApiService(new HttpClient()) {Credentials = credentials};

var success = await service.AuthorizeAsync(code, callBackUrl);
```

## Making authenticated calls to the API

As long as the `Credentials.AccessToken` property is set on the `MeliApiService`, an `Authorization: Bearer YOUR_TOKEN` header will be set automatically when making requests.

Read more about [authenticating requests](https://global-selling.mercadolibre.com/devsite/authentication-and-authorization-global-selling) on the official API docs.

A handle result policy will automatically refresh the token when it's expired.

## Do I always need to include the ```access_token``` as a parameter?

Yes. From April 2021 onwawrds, every request API will need an `Authorization` HTTP header.

## Making GET calls

```csharp
var p = new HttpParams().Add("a param", "a value")
                        .Add("another_param", "another value")
                        .Add("you can chain", "the method calls");

var response = await m.GetAsync("/users/me", p);

if (response.IsSuccessStatusCode)
{
    var json = await r.Content.ReadAsStringAsync();

    // You can then use Json.NET to deserialize the json
}
```

## Making POST calls

```csharp
var p = new HttpParams().Add("a param", "a value");

var r = await m.PostAsync("/items", p, new { foo = "bar" });
```

## Making PUT calls

```csharp
var p = new HttpParams().Add("a param", "a value");

var r = await m.PutAsync("/items/123", p, new { foo = "bar" });
```

## Making DELETE calls

```csharp
var p = new HttpParams().Add("a param", "a value");

var r = await m.DeleteAsync("/items/123", p, new { foo = "bar" });
```

## Strongly typed calls

If you know what JSON you're expecting you can create your own classes decorated with the `System.Text.Json` attribute.

```csharp
public class Category
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

var categories = await m.GetAsync<Category[]>("/sites/MLB/categories");
```

## Deserializing with an anonymous type

If you just need a few values back from an API call, Json.NET has a really cool `DeserializeAnonymousType` method:

```csharp
var json = @"{""refresh_token"":""refresh"",""access_token"":""access"",""user_id"":123456789}";

var token = JsonConvert.DeserializeAnonymousType (json, new {refresh_token="", access_token = ""});

var refreshToken = token.refresh_token;
```
