using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor;
using MudBlazor.Services;
using OSDC.Drilling.EarthGravity.WebApp;
using OSDC.Drilling.EarthGravity.WebPages;

var builder = WebApplication.CreateBuilder(args);
var webPagesConfiguration = new WebPagesHostConfiguration
{
    EarthGravityHostURL = builder.Configuration["EarthGravityHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices(configuration =>
{
    configuration.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    configuration.SnackbarConfiguration.PreventDuplicates = false;
    configuration.SnackbarConfiguration.NewestOnTop = false;
    configuration.SnackbarConfiguration.ShowCloseIcon = true;
    configuration.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddSingleton<IEarthGravityWebPagesConfiguration>(webPagesConfiguration);
builder.Services.AddSingleton<IEarthGravityAPIUtils, APIUtils>();

var app = builder.Build();
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);
app.UsePathBase("/EarthGravity/webapp");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStaticFiles();
app.UseRouting();
app.MapGet("/EarthGravity", () => Results.Redirect("/EarthGravity/webapp/Home"))
    .ExcludeFromDescription();
app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" })).ExcludeFromDescription();
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Healthy" })).ExcludeFromDescription();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();

public partial class Program;
