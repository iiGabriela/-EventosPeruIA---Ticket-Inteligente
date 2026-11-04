using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EventosIAPeru.API.OpenApi
{
    /// <summary>
    /// Agrega a Swagger los dos modos de autenticación, para probar desde el botón "Authorize":
    /// - Bearer: token (JWT) de Firebase.
    /// - X-User-Email: atajo solo para Development, sin token.
    /// </summary>
    public class SeguridadOpenApiTransformer : IOpenApiDocumentTransformer
    {
        private readonly IWebHostEnvironment _environment;

        public SeguridadOpenApiTransformer(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info.Title = "EventosPeruIA - TicketInteligente API";
            document.Info.Description = "Backend del proyecto DAW 2026-02. Firebase valida el login; la API identifica al usuario por su correo.";

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "idToken de Firebase (signInWithEmailAndPassword en el frontend)."
            };

            var requisito = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            };

            if (_environment.IsDevelopment())
            {
                document.Components.SecuritySchemes["X-User-Email"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-User-Email",
                    Description = "Solo Development: correo de un usuario registrado, para probar sin token."
                };
                requisito[new OpenApiSecuritySchemeReference("X-User-Email", document)] = new List<string>();
            }

            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Add(requisito);
            return Task.CompletedTask;
        }
    }
}
