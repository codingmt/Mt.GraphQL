using System.Web.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Only needed when we use ApiController attribute on controllers
builder.Services.AddControllers().ConfigureApiBehaviorOptions(
    opt => opt.InvalidModelStateResponseFactory =
        ctx => ctx.ModelState.ToBadRequest(ctx));

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
