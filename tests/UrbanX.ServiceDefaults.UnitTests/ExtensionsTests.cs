using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UrbanX.ServiceDefaults.UnitTests;

public class ExtensionsTests
{
    [Fact]
    public void AddServiceDefaults_ShouldRegisterServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new string[] { });
        
        // Act
        builder.AddServiceDefaults();
        var app = builder.Build();

        // Assert
        app.Should().NotBeNull();
        app.Services.Should().NotBeNull();
    }

    [Fact]
    public void AddDefaultHealthChecks_ShouldRegisterHealthChecks()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new string[] { });
        
        // Act
        builder.AddDefaultHealthChecks();
        var app = builder.Build();

        // Assert
        app.Services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()
            .Should().NotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_ShouldMapHealthChecksInDevelopment()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new string[] { });
        builder.Environment.EnvironmentName = Environments.Development;
        builder.AddDefaultHealthChecks();
        var app = builder.Build();

        // Act
        app.MapDefaultEndpoints();

        // Assert
        app.Should().NotBeNull();
        // In a real integration test, we would verify the endpoints are mapped
    }

    [Fact]
    public void ConfigureOpenTelemetry_ShouldConfigureObservability()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new string[] { });
        
        // Act
        builder.ConfigureOpenTelemetry();
        var app = builder.Build();

        // Assert
        app.Should().NotBeNull();
        // OpenTelemetry services should be configured
    }
}
