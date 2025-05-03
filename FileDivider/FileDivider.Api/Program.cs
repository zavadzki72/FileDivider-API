using FileDivider.Api.Data;
using FileDivider.Api.Middlewares;
using FileDivider.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<FileDivisorService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

