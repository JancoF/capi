using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Habilitar CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        builder => {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Agregar HttpClient
builder.Services.AddHttpClient();

// Configurar para escuchar en todas las interfaces
builder.WebHost.UseUrls("http://0.0.0.0:80");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Endpoint para obtener todas las recetas
app.MapGet("/recetas", async (IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    var client = httpClientFactory.CreateClient();
    var flaskApiUrl = config["FlaskApiUrl"] ?? "http://host.docker.internal:5000";
    var response = await client.GetAsync($"{flaskApiUrl}/recetas");
    
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var recetas = JsonSerializer.Deserialize<List<Receta>>(content);
        return Results.Ok(recetas);
    }
    
    return Results.StatusCode((int)response.StatusCode);
})
.WithName("GetRecetas")
.WithOpenApi();

// Endpoint para obtener todas las recetas
app.MapGet("/buscar/{termino}", async (string termino, IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    var client = httpClientFactory.CreateClient();
    var flaskApiUrl = config["FlaskApiUrl"] ?? "http://host.docker.internal:5000";
    var response = await client.GetAsync($"{flaskApiUrl}/recetas");
    
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var recetas = JsonSerializer.Deserialize<List<Receta>>(content);
        
        // Función para calcular la similitud entre dos strings
        Func<string, string, bool> sonSimilares = (s1, s2) => 
        {
            return s1.Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Any(palabra => s2.Contains(palabra, StringComparison.OrdinalIgnoreCase));
        };

        // Buscar coincidencias exactas
        var resultadosExactos = recetas.Where(r => 
            r.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
            r.Ingredientes.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
            r.Instrucciones.Contains(termino, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        // Si no hay resultados exactos, buscar recomendaciones
        if (!resultadosExactos.Any())
        {
            var recomendaciones = recetas.Where(r => 
                sonSimilares(r.Nombre, termino) ||
                sonSimilares(r.Ingredientes, termino) ||
                sonSimilares(r.Instrucciones, termino)
            ).Take(5).ToList();

            return Results.Ok(new { 
                Resultados = recomendaciones, 
                EsRecomendacion = true,
                MensajeBusqueda = $"No se encontraron resultados exactos para '{termino}'. Aquí hay algunas recomendaciones:"
            });
        }

        return Results.Ok(new { 
            Resultados = resultadosExactos, 
            EsRecomendacion = false,
            MensajeBusqueda = $"Resultados para '{termino}':"
        });
    }
    
    return Results.StatusCode((int)response.StatusCode);
})
.WithName("BuscarRecetas")
.WithOpenApi();

app.Run();

// Modelo para las recetas
class Receta
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; }

    [JsonPropertyName("ingredientes")]
    public string Ingredientes { get; set; }

    [JsonPropertyName("instrucciones")]
    public string Instrucciones { get; set; }
}