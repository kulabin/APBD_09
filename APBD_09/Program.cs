using APBD_09.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();  
builder.Services.AddOpenApi();

builder.Services.AddScoped<IWarehouseService,WarehouseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
