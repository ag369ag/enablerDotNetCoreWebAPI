using IniParser.Model;
using IniParser;
using ITL.Enabler.API;
using Microsoft.AspNetCore.Authentication;
using testASPWebAPI.Auth;
using Scalar.AspNetCore;
using Microsoft.OpenApi.Models;
using testASPWebAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Dot net API", Version = "v1" });
    c.AddSecurityDefinition("basic", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "basic",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Basic Authorization Header"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "basic"
            }
        },
        new string []{}
        }
    });
});

builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuth>("BasicAuthentication", null);

builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    var configParser = new FileIniDataParser();
    IniData data = configParser.ReadFile("config.ini");
    string dbServer = data["Configuration"]["Server"];
    string dbTable = data["Database"]["Table"];
    string dbUsername = data["Database"]["Username"];
    string dbPassword = data["Database"]["Password"];
    options.UseSqlServer($@"Server = {dbServer};Database={dbTable};User Id ={dbUsername};Password = {dbPassword};Trusted_Connection=True;TrustServerCertificate=True;Integrated Security = False;");
    //options.UseSqlServer(builder.Configuration.Sources)
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}

app.UseSwagger(option =>
{
    option.RouteTemplate = "/openapi/{documentName}.json";
});
app.UseSwaggerUI(c =>
{
    string swaggerPath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
    c.SwaggerEndpoint($"{swaggerPath}/swagger/v1/swagger.json", "Web API");
});

app.Use(async (context, next) =>
{
    if(context.Request.Path == "/")
    {
        context.Response.Redirect("/scalar");
        return;
    }

    await next();
});

app.MapScalarApiReference();
/*
app.MapPost("/Pump/getTransactionByPumpID", () => { })
    .WithName("Getting transaction by pump ID")
    .WithTags("Get Transactions ");*/
//.WithSummary("Getting transaction by pump ID. Input should be \"pumpID\"");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
