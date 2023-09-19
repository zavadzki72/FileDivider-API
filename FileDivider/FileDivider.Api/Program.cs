using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

