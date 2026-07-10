using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the standard, unauthenticated Supabase Client
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:AnonKey"];

builder.Services.AddScoped(_ => new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
{
    AutoConnectRealtime = true
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Route configured to default to your home action method
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=home}/{action=home}/{id?}");

app.Run();