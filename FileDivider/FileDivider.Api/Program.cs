using Microsoft.AspNetCore.Http.Features;

using FileDivider.Api.Data;
using FileDivider.Api.Middlewares;
using FileDivider.Api.Services;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var mongoConnectionString = Environment.GetEnvironmentVariable("MongoDbConnectionString");

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    mongoConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
}

builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoConnectionString!;
    options.DatabaseName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value!;
});

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<FileDivisorService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(x => {
    x.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddCors(x => {
    x.AddPolicy("AllowAll", options => {
        options
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Run();
}
else
{
    var port = Environment.GetEnvironmentVariable("PORT");
    var url = string.Concat("http://0.0.0.0:", port);

    app.UseSwagger();
    app.UseSwaggerUI();

    app.Run(url);
}

